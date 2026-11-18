using System;
using System.Globalization;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Luxodd.Game.Scripts.Runtime.Viewport.Context
{
     public static class LuxoddRuntimeContext
    {
        /// <summary>Platform supplied by authoritative host metadata; Unknown without an authoritative value.</summary>
        public static LuxoddRuntimePlatform Platform { get; private set; }

        /// <summary>Host cabinet layout, meaningful only for the Arcade platform.</summary>
        public static LuxoddArcadeType ArcadeType { get; private set; }

        /// <summary>Whether authoritative host metadata contained a runtime object, including unsupported values.</summary>
        public static bool HasHostRuntimeContext { get; private set; }

        public static bool IsWeb => Platform == LuxoddRuntimePlatform.Web;
        public static bool IsArcade => Platform == LuxoddRuntimePlatform.Arcade;
        public static bool IsPawn => IsArcade && ArcadeType == LuxoddArcadeType.Pawn;
        public static bool IsRook => IsArcade && ArcadeType == LuxoddArcadeType.Rook;

        /// <summary>Raised when Platform, ArcadeType, or HasHostRuntimeContext changes.</summary>
        public static event Action RuntimeContextChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHostRuntimeContext()
        {
            Platform = LuxoddRuntimePlatform.Unknown;
            ArcadeType = LuxoddArcadeType.Unknown;
            HasHostRuntimeContext = false;
            RuntimeContextChanged = null;
        }

        internal static void ApplyHostRuntimeContext(bool hasHostRuntimeContext, string platform, string arcadeLayout)
        {
            var effectivePlatform = LuxoddRuntimePlatform.Unknown;
            var effectiveArcadeType = LuxoddArcadeType.Unknown;
            if (hasHostRuntimeContext)
            {
                if (string.Equals(platform, "web", StringComparison.Ordinal))
                {
                    effectivePlatform = LuxoddRuntimePlatform.Web;
                }
                else if (string.Equals(platform, "arcade", StringComparison.Ordinal))
                {
                    effectivePlatform = LuxoddRuntimePlatform.Arcade;
                    if (string.Equals(arcadeLayout, "pawn", StringComparison.Ordinal))
                        effectiveArcadeType = LuxoddArcadeType.Pawn;
                    else if (string.Equals(arcadeLayout, "rook", StringComparison.Ordinal))
                        effectiveArcadeType = LuxoddArcadeType.Rook;
                }
            }

            if (Platform == effectivePlatform && ArcadeType == effectiveArcadeType &&
                HasHostRuntimeContext == hasHostRuntimeContext)
            {
                return;
            }

            Platform = effectivePlatform;
            ArcadeType = effectiveArcadeType;
            HasHostRuntimeContext = hasHostRuntimeContext;

            var subscribers = RuntimeContextChanged;
            if (subscribers == null)
                return;

            foreach (Action subscriber in subscribers.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    // Do not log exception messages: developer callbacks may contain credentials.
                    Debug.LogWarning($"[LuxoddRuntimeContext] RuntimeContextChanged subscriber failed ({exception.GetType().Name}).");
                }
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int Luxodd_IsMobileRuntime();
        [DllImport("__Internal")] private static extern int Luxodd_GetMaxTouchPoints();
        [DllImport("__Internal")] private static extern int Luxodd_HasCoarsePointer();

        [DllImport("__Internal")]
        private static extern void Luxodd_GetVisualViewportInsets(
            out float left,
            out float top,
            out float right,
            out float bottom,
            out float viewWidth,
            out float viewHeight
        );
#endif

        public static bool IsMobileRuntime
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Luxodd_IsMobileRuntime() != 0;
#else
                return Application.isMobilePlatform;
#endif
            }
        }

        // -------- Debug helpers (optional) --------

        public static int MaxTouchPoints
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Luxodd_GetMaxTouchPoints();
#else
                return UnityEngine.Input.touchSupported ? UnityEngine.Input.touchCount : 0;
#endif
            }
        }

        public static bool HasCoarsePointer
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Luxodd_HasCoarsePointer() != 0;
#else
                return UnityEngine.Input.touchSupported;
#endif
            }
        }

        // -------- VisualViewport Insets (WebGL mobile safe area) --------

        public struct VisualViewportInsets
        {
            public float Left;
            public float Top;
            public float Right;
            public float Bottom;

            public float ViewWidth;
            public float ViewHeight;

            public bool IsZero => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;
        }

        public static VisualViewportInsets GetVisualViewportInsets()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Luxodd_GetVisualViewportInsets(out float left, out float top, out float right, out float bottom, out float viewW, out float viewH);

            return new VisualViewportInsets
            {
                Left = left,
                Top = top,
                Right = right,
                Bottom = bottom,
                ViewWidth = viewW,
                ViewHeight = viewH
            };
#else
            return default;
#endif
        }

        public static string GetVisualViewportInsetsDebugString()
        {
            VisualViewportInsets i = GetVisualViewportInsets();

            // In editor/non-WebGL this will be zeros, which is expected.
            return string.Format(CultureInfo.InvariantCulture,
                "VisualViewport:\n" +
                "Insets L:{0:0} T:{1:0} R:{2:0} B:{3:0}\n" +
                "View {4:0}x{5:0}",
                i.Left, i.Top, i.Right, i.Bottom, i.ViewWidth, i.ViewHeight);
        }
    }
}
