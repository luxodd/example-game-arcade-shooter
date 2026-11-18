using System;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.Runtime.Viewport.Context;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Luxodd.Game.Scripts.Network
{
    [Serializable]
    internal class LuxoddSessionPayload
    {
#if NEWTONSOFT_JSON
        [JsonProperty("token")] public string Token { get; set; }
        [JsonProperty("wsUrl")] public string WsUrl { get; set; }
        [JsonProperty("runtime")] public LuxoddRuntimePayload Runtime { get; set; }
#else
        public string Token { get; set; }
        public string WsUrl { get; set; }
        public LuxoddRuntimePayload Runtime { get; set; }
#endif

        internal bool IsValid => string.IsNullOrWhiteSpace(Token) == false &&
                                 string.IsNullOrWhiteSpace(WsUrl) == false;

        internal bool Matches(LuxoddSessionPayload other)
        {
            return other != null &&
                   string.Equals(Token, other.Token, StringComparison.Ordinal) &&
                   string.Equals(WsUrl, other.WsUrl, StringComparison.Ordinal);
        }
    }
    
    [Serializable]
    internal class LuxoddRuntimePayload
    {
#if NEWTONSOFT_JSON
        [JsonProperty("platform")] public string Platform { get; set; }
        [JsonProperty("arcadeLayout")] public string ArcadeLayout { get; set; }
#else
        public string Platform { get; set; }
        public string ArcadeLayout { get; set; }
#endif
    }

    public partial class LuxoddSessionBridge : MonoBehaviour
    {
        
        public static LuxoddSessionBridge Instance { get; private set; }

        /// <summary>Token from events</summary>
        public string Token { get; private set; }

        /// <summary>Full WebSocket URL, sent from host.</summary>
        public string WebSocketUrl { get; private set; }
        
        internal LuxoddSessionPayload SessionPayload { get; private set; }

        /// <summary>Flag that event received and data valid</summary>
        public bool HasSession => !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(WebSocketUrl);

        /// <summary>Events that raised from payload luxodd:session.</summary>
        internal ISimpleEvent<LuxoddSessionPayload> OnSessionReceived => _onSessionReceived;
        
        private readonly SimpleEvent<LuxoddSessionPayload> _onSessionReceived = new  SimpleEvent<LuxoddSessionPayload>(); 
        private bool _isHandlingSessionPayloadEvent;
        private bool _sessionPayloadEventWasBuffered;
        private LuxoddSessionPayload _sessionPayloadBeingReceived;
        // Only the immediate predecessor of the accepted session is rejected as stale.
        private LuxoddSessionPayload _supersededSessionPayload;
        private SessionFlowController _sessionFlowController;

        [Serializable]
        private class SessionWirePayload
        {
            public string token;
            public string wsUrl;
            public RuntimeWirePayload runtime;
        }

        [Serializable]
        private class RuntimeWirePayload
        {
            public string platform;
            public string arcadeLayout;
        }

        [Serializable]
        private class PropertyNameWire
        {
            public string value;
        }

        internal static LuxoddSessionPayload DeserializeSessionPayload(string json)
        {
#if NEWTONSOFT_JSON
            return JsonConvert.DeserializeObject<LuxoddSessionPayload>(json);
#else
            return DeserializeSessionPayloadWithJsonUtility(json);
#endif
        }

        internal static LuxoddSessionPayload DeserializeSessionPayloadWithJsonUtility(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || string.Equals(json.Trim(), "null", StringComparison.Ordinal))
            {
                return null;
            }

            var wire = new SessionWirePayload();
            JsonUtility.FromJsonOverwrite(json, wire);

            return new LuxoddSessionPayload
            {
                Token = wire.token,
                WsUrl = wire.wsUrl,
                // JsonUtility can materialize an inline class even for a missing/null JSON object.
                Runtime = HasRuntimeObject(json) == false || wire.runtime == null ? null : new LuxoddRuntimePayload
                {
                    Platform = wire.runtime.platform,
                    ArcadeLayout = wire.runtime.arcadeLayout
                }
            };
        }

        private static bool HasRuntimeObject(string json)
        {
            // Inspect only top-level property names and object presence. JsonUtility parses all values.
            var depth = 0;
            var hasRuntimeObject = false;
            for (var index = 0; index < json.Length; index++)
            {
                var character = json[index];
                if (character == '{' || character == '[')
                    depth++;
                else if (character == '}' || character == ']')
                    depth--;
                else if (character == '"')
                {
                    var start = index;
                    while (++index < json.Length)
                    {
                        if (json[index] == '\\')
                            index++;
                        else if (json[index] == '"')
                            break;
                    }

                    if (depth != 1 || index >= json.Length)
                        continue;

                    var valueIndex = index + 1;
                    while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
                        valueIndex++;
                    if (valueIndex >= json.Length || json[valueIndex] != ':')
                        continue;

                    var nameJson = "{\"value\":" + json.Substring(start, index - start + 1) + "}";
                    var name = JsonUtility.FromJson<PropertyNameWire>(nameJson).value;
                    if (string.Equals(name, "runtime", StringComparison.Ordinal) == false)
                        continue;

                    valueIndex++;
                    while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
                        valueIndex++;
                    hasRuntimeObject = valueIndex < json.Length && json[valueIndex] == '{';
                }
            }

            return hasRuntimeObject;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitLuxoddSessionListener(string goName);

    [DllImport("__Internal")]
    private static extern string GetLuxoddSessionToken();

    [DllImport("__Internal")]
    private static extern string GetLuxoddSessionWsUrl();

#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR

        InitLuxoddSessionListener(gameObject.name);
        InitLuxoddRuntimeMetadata(gameObject.name);
        //Debug.Log("[LuxoddSessionBridge] InitLuxoddSessionListener called");
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLuxoddSession)}] OK, InitLuxoddSessionListener called");
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                RetireRuntimeMetadataBootstrap();
                Instance = null;
            }
        }

        
        public void OnLuxoddSession(string json)
        {
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLuxoddSession)}] Session event received.");

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"[{GetType().Name}][{nameof(OnLuxoddSession)}] Ignoring empty session payload.");
                return;
            }

            LuxoddSessionPayload payload = null;
            try
            {
                payload = DeserializeSessionPayload(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{GetType().Name}][{nameof(OnLuxoddSession)}] Ignoring malformed session payload ({e.GetType().Name}).");
                return;
            }

            if (payload == null || payload.IsValid == false)
            {
                Debug.LogWarning($"[{GetType().Name}][{nameof(OnLuxoddSession)}] Ignoring incomplete session payload.");
                return;
            }

            var traceMatchesCurrent = SessionPayload != null && SessionPayload.Matches(payload);
            var traceIsStale = _supersededSessionPayload != null && _supersededSessionPayload.Matches(payload);
            Debug.Log($"[FastRestartTrace] BridgeSessionReceived matchesCurrent={traceMatchesCurrent} " +
                      $"bridgeInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)} decision={(traceMatchesCurrent ? "DUPLICATE" : traceIsStale ? "STALE" : "NEW")}");

            if (SessionPayload != null && SessionPayload.Matches(payload))
            {
                Debug.Log($"[{GetType().Name}][{nameof(OnLuxoddSession)}] Ignoring duplicate session payload.");
                return;
            }

            if (_supersededSessionPayload != null && _supersededSessionPayload.Matches(payload))
            {
                Debug.Log($"[{GetType().Name}][{nameof(OnLuxoddSession)}] Ignoring superseded session payload.");
                return;
            }

            _isHandlingSessionPayloadEvent = true;
            _sessionPayloadEventWasBuffered = false;
            _sessionPayloadBeingReceived = payload;
            try
            {
                _onSessionReceived.Notify(payload);
                if (_sessionPayloadEventWasBuffered == false)
                {
                    AcceptSessionPayload(payload);
                }
            }
            finally
            {
                _isHandlingSessionPayloadEvent = false;
                _sessionPayloadEventWasBuffered = false;
                _sessionPayloadBeingReceived = null;
            }
        }

        internal void AcceptSessionPayload(LuxoddSessionPayload payload)
        {
            if (payload == null || payload.IsValid == false)
            {
                return;
            }

            RetireRuntimeMetadataBootstrap();

            if (SessionPayload != null && SessionPayload.Matches(payload) == false)
            {
                _supersededSessionPayload = SessionPayload;
            }

            Token = payload.Token;
            WebSocketUrl = payload.WsUrl;
            SessionPayload = payload;
            LuxoddRuntimeContext.ApplyHostRuntimeContext(payload.Runtime != null,
                payload.Runtime?.Platform, payload.Runtime?.ArcadeLayout);
        }

        internal void MarkSessionPayloadBuffered(LuxoddSessionPayload payload)
        {
            if (_isHandlingSessionPayloadEvent && _sessionPayloadBeingReceived != null &&
                _sessionPayloadBeingReceived.Matches(payload))
            {
                RetireRuntimeMetadataBootstrap();
                _sessionPayloadEventWasBuffered = true;
            }
        }
        
        public void RefreshFromJsState()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            Token = GetLuxoddSessionToken();
            WebSocketUrl = GetLuxoddSessionWsUrl();
            Debug.Log("[LuxoddSessionBridge] Refreshed session state from JavaScript.");
        }
        catch (Exception e)
        {
            Debug.LogError("[LuxoddSessionBridge] RefreshFromJsState failed (" + e.GetType().Name + ").");
        }
#endif
        }


        internal void BindSessionFlowController(SessionFlowController controller)
        {
            if (_sessionFlowController != null && _sessionFlowController != controller)
            {
                Debug.LogWarning("[LuxoddSessionBridge] A different session controller is already bound.");
                return;
            }

            _sessionFlowController = controller;
        }

        internal void UnbindSessionFlowController(SessionFlowController controller)
        {
            if (_sessionFlowController == controller)
            {
                _sessionFlowController = null;
            }
        }

        internal void ConnectThroughService()
        {
            var payload = SessionPayload;
            if (payload == null || payload.IsValid == false ||
                string.Equals(Token, payload.Token, StringComparison.Ordinal) == false ||
                string.Equals(WebSocketUrl, payload.WsUrl, StringComparison.Ordinal) == false)
            {
                Debug.LogWarning("[LuxoddSessionBridge] Cannot connect: no matching accepted session.");
                return;
            }

            var socketService = _sessionFlowController != null
                ? _sessionFlowController.SocketService
                : null;
            if (socketService == null || socketService.isActiveAndEnabled == false)
            {
                Debug.LogWarning("[LuxoddSessionBridge] Cannot connect: session service is unavailable.");
                return;
            }

            socketService.ConnectFromSessionBridge(payload);
        }

        public void ConnectUsingCurrentSession()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ConnectThroughService();
#else
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ConnectUsingCurrentSession)}] ConnectUsingCurrentSession: not in WebGL build.");
#endif
        }
    }
}
