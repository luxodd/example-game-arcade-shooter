using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Luxodd.Integration
{
    /// <summary>
    /// Fits the game to the viewport the host gives it, and keeps it fitted when that viewport
    /// changes (the platform's cabinet layouts, LUXL-2509, resize the game's frame on a HELP tap).
    ///
    /// The game is configured once with the aspect it was designed for (<see cref="Configure"/>).
    /// When the viewport is that shape or wider, nothing is touched: the game looks exactly as it
    /// does full screen today. Only when the viewport is narrower (a 16:9 game in the 4:3 slot):
    ///  - landscape Screen Space canvases switch to CanvasScaler Expand, so the whole reference
    ///    canvas fits and nothing anchored to an edge is cut off; their own settings come back
    ///    when the viewport is wide again;
    ///  - the main camera keeps the designed width (<see cref="CameraFit.KeepWidth"/>: orthographic
    ///    size or vertical field of view grow, showing more above and below) or is letterboxed to
    ///    the designed aspect (<see cref="CameraFit.Letterbox"/>), as the game chose. The camera is
    ///    fitted only when the viewport or the main camera changes; a size or field of view the
    ///    game sets itself in between becomes the new base. A game whose camera script sets its size
    ///    every frame should use <see cref="CameraFit.None"/>.
    ///
    /// Cabinet identity comes only from the plugin's LuxoddRuntimeContext (read by name, so this
    /// file builds on every plugin version). <see cref="UseRookPortraitLayout"/> is true only when
    /// the cabinet is a Rook AND the viewport is portrait: the platform can host a game on a Rook
    /// in a landscape slot. Rook is never inferred from the viewport shape alone.
    /// </summary>
    [DefaultExecutionOrder(-3000)]
    public sealed class LuxoddLayout : MonoBehaviour
    {
        public enum CameraFit { None, KeepWidth, Letterbox }

        private const string ContextTypeName = "Luxodd.Game.Scripts.Runtime.Viewport.Context.LuxoddRuntimeContext";

        /// <summary>The aspect (width / height) the game's camera and landscape UI were designed for.</summary>
        public static float DesignAspect { get; private set; } = 16f / 9f;
        public static CameraFit Fit { get; private set; } = CameraFit.None;
        public static bool FitCanvases { get; private set; } = true;

        /// <summary>Raised when the viewport changes shape, or the host's cabinet identity arrives or changes.</summary>
        public static event Action Changed;

        public static float ViewportAspect => Mathf.Max(1, Screen.width) / (float)Mathf.Max(1, Screen.height);
        public static bool PortraitViewport => Screen.height > Screen.width;
        /// <summary>The viewport is more than 1% narrower than the designed aspect.</summary>
        public static bool NarrowerThanDesign => ViewportAspect < DesignAspect * 0.99f;
        public static bool IsArcade => ReadContext("IsArcade");
        public static bool IsRookCabinet => ReadContext("IsRook");
        /// <summary>Use a portrait Rook layout: the host says Rook AND the viewport is portrait.</summary>
        public static bool UseRookPortraitLayout => IsRookCabinet && PortraitViewport;

        /// <summary>Sets the designed aspect and how the camera is fitted. Call once, before or after boot.</summary>
        public static void Configure(float designAspect, CameraFit fit, bool fitCanvases = true)
        {
            DesignAspect = designAspect > 0f ? designAspect : 16f / 9f;
            Fit = fit;
            FitCanvases = fitCanvases;
            if (instance != null) instance.Apply(true);
        }

        private struct CanvasOriginal { public CanvasScaler.ScreenMatchMode mode; }
        private struct CameraOriginal { public Rect rect; public float size, fov, appliedSize, appliedFov; }

        private static LuxoddLayout instance;
        private static PropertyInfo[] contextProperties;
        private readonly Dictionary<CanvasScaler, CanvasOriginal> canvases = new Dictionary<CanvasScaler, CanvasOriginal>();
        private Camera fittedCamera;
        private CameraOriginal cameraOriginal;
        private Camera barsCamera;
        private int width, height;
        private bool lastPortrait, lastArcade, lastRook;
        private float nextCheck, nextScan;
        private string lastLog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            Changed = null;
            contextProperties = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("[LuxoddLayout]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LuxoddLayout>();
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(true);

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.1f;
            bool resized = Screen.width != width || Screen.height != height;
            bool arcade = IsArcade, rook = IsRookCabinet, portrait = PortraitViewport;
            bool identity = arcade != lastArcade || rook != lastRook || portrait != lastPortrait;
            bool cameraChanged = Camera.main != fittedCamera;
            if (resized || cameraChanged)
            {
                width = Screen.width;
                height = Screen.height;
                Apply(true);
            }
            else if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.5f; // windows instantiated later get fitted too
                ApplyCanvases();
            }
            if (identity)
            {
                lastArcade = arcade;
                lastRook = rook;
                lastPortrait = portrait;
                Changed?.Invoke();
            }
        }

        private void Apply(bool log)
        {
            ApplyCanvases();
            ApplyCamera();
            if (!log) return;
            string line = $"Viewport={Screen.width}x{Screen.height} ({ViewportAspect:0.###}), Design={DesignAspect:0.###}, " +
                $"Narrower={NarrowerThanDesign}, Fit={Fit}, Arcade={IsArcade}, RookCabinet={IsRookCabinet}, RookPortrait={UseRookPortraitLayout}";
            if (line != lastLog) { Debug.Log("[GameLayout] " + line); lastLog = line; }
        }

        // Canvases ----------------------------------------------------------------------------

        private void ApplyCanvases()
        {
            var stale = new List<CanvasScaler>();
            foreach (var pair in canvases) if (pair.Key == null) stale.Add(pair.Key);
            foreach (var key in stale) canvases.Remove(key);

#if UNITY_6000_5_OR_NEWER
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include);
#elif UNITY_2023_1_OR_NEWER
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var scalers = FindObjectsOfType<CanvasScaler>(true);
#endif
            foreach (var scaler in scalers)
            {
                var canvas = scaler.GetComponent<Canvas>();
                if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                var reference = scaler.referenceResolution;
                if (reference.x < reference.y) continue; // portrait canvases (plugin popups) fit landscape already
                bool known = canvases.TryGetValue(scaler, out var original);
                if (!known) original = new CanvasOriginal { mode = scaler.screenMatchMode };
                var pixels = canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null
                    ? canvas.worldCamera.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
                float aspect = pixels.width / Mathf.Max(1f, pixels.height);
                bool narrower = FitCanvases && aspect < reference.x / reference.y * 0.99f;
                if (narrower)
                {
                    if (!known) canvases.Add(scaler, original);
                    if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
                        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }
                else if (known)
                {
                    scaler.screenMatchMode = original.mode;
                    canvases.Remove(scaler);
                }
            }
        }

        // Camera ------------------------------------------------------------------------------

        private void ApplyCamera()
        {
            var main = Camera.main;
            if (main != fittedCamera)
            {
                RestoreCamera();
                fittedCamera = main;
                if (main != null)
                    cameraOriginal = new CameraOriginal { rect = main.rect, size = main.orthographicSize, fov = main.fieldOfView,
                        appliedSize = main.orthographicSize, appliedFov = main.fieldOfView };
            }
            if (fittedCamera == null) { SetBars(false); return; }
            // A camera the game itself places in part of the screen is the game's business.
            if (cameraOriginal.rect != new Rect(0, 0, 1, 1)) { SetBars(false); return; }

            // The game changed its own camera since we fitted it (a zoom, a new level): that value
            // is the game's new base; fit from it.
            if (fittedCamera.orthographicSize != cameraOriginal.appliedSize) cameraOriginal.size = fittedCamera.orthographicSize;
            if (fittedCamera.fieldOfView != cameraOriginal.appliedFov) cameraOriginal.fov = fittedCamera.fieldOfView;

            bool narrower = NarrowerThanDesign;
            float aspect = ViewportAspect;
            switch (narrower ? Fit : CameraFit.None)
            {
                case CameraFit.KeepWidth:
                    fittedCamera.rect = cameraOriginal.rect;
                    if (fittedCamera.orthographic)
                        fittedCamera.orthographicSize = cameraOriginal.size * DesignAspect / aspect;
                    else
                    {
                        float halfWidth = Mathf.Tan(cameraOriginal.fov * 0.5f * Mathf.Deg2Rad) * DesignAspect;
                        fittedCamera.fieldOfView = 2f * Mathf.Atan(halfWidth / aspect) * Mathf.Rad2Deg;
                    }
                    SetBars(false);
                    break;
                case CameraFit.Letterbox:
                    fittedCamera.orthographicSize = cameraOriginal.size;
                    fittedCamera.fieldOfView = cameraOriginal.fov;
                    float h = aspect / DesignAspect;
                    fittedCamera.rect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
                    SetBars(true);
                    break;
                default:
                    RestoreCamera();
                    break;
            }
            cameraOriginal.appliedSize = fittedCamera.orthographicSize;
            cameraOriginal.appliedFov = fittedCamera.fieldOfView;
        }

        private void RestoreCamera()
        {
            if (fittedCamera != null)
            {
                fittedCamera.rect = cameraOriginal.rect;
                fittedCamera.orthographicSize = cameraOriginal.size;
                fittedCamera.fieldOfView = cameraOriginal.fov;
            }
            SetBars(false);
        }

        /// <summary>A camera that only clears to black behind a letterboxed game.</summary>
        private void SetBars(bool on)
        {
            if (!on) { if (barsCamera != null) barsCamera.enabled = false; return; }
            if (barsCamera == null)
            {
                var go = new GameObject("[LuxoddLayout bars]") { hideFlags = HideFlags.HideInHierarchy };
                go.transform.SetParent(transform, false);
                barsCamera = go.AddComponent<Camera>();
                barsCamera.cullingMask = 0;
                barsCamera.clearFlags = CameraClearFlags.SolidColor;
                barsCamera.backgroundColor = Color.black;
                barsCamera.useOcclusionCulling = false;
                barsCamera.allowHDR = false;
                barsCamera.allowMSAA = false;
            }
            barsCamera.depth = (fittedCamera != null ? fittedCamera.depth : 0f) - 100f;
            barsCamera.enabled = true;
        }

        private void RestoreAll()
        {
            foreach (var pair in canvases) if (pair.Key != null) pair.Key.screenMatchMode = pair.Value.mode;
            canvases.Clear();
            RestoreCamera();
            fittedCamera = null;
        }

        // Host identity -----------------------------------------------------------------------

        private static bool ReadContext(string property)
        {
            if (contextProperties == null)
            {
                contextProperties = new PropertyInfo[2];
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(ContextTypeName, false);
                    if (type == null) continue;
                    contextProperties[0] = type.GetProperty("IsArcade", BindingFlags.Public | BindingFlags.Static);
                    contextProperties[1] = type.GetProperty("IsRook", BindingFlags.Public | BindingFlags.Static);
                    break;
                }
            }
            var info = property == "IsArcade" ? contextProperties[0] : contextProperties[1];
            if (info == null) return false;
            try { return (bool)info.GetValue(null, null); }
            catch (Exception) { return false; }
        }
    }
}
