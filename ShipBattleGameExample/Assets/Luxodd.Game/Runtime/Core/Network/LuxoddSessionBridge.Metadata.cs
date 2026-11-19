using System;
using Luxodd.Game.Scripts.Runtime.Viewport.Context;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Luxodd.Game.Scripts.Network
{
    public partial class LuxoddSessionBridge
    {
        private string _runtimeMetadataRequestId;
        private string _runtimeMetadataOwnerToken;
        private string _runtimeMetadataOwnerEndpoint;
        private bool _runtimeMetadataRetired;

        [Serializable]
        private sealed class RuntimeMetadataWire
        {
            public int version;
            public string requestId;
            public bool hasRuntime;
            public string platform;
            public string arcadeLayout;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void InitLuxoddRuntimeMetadata(string goName);
        [DllImport("__Internal")]
        private static extern void BeginLuxoddRuntimeMetadata(string requestId, string token, string endpoint);
        [DllImport("__Internal")]
        private static extern void RetireLuxoddRuntimeMetadata();
#endif

        private void Update()
        {
            if (_runtimeMetadataRetired)
                return;

            var service = _sessionFlowController != null ? _sessionFlowController.SocketService : null;
            if (SessionPayload != null || (service != null &&
                (service.IsFastRestartInProgress || service.IsSessionReplacementInProgress ||
                 service.IsSessionARecoveryBlockedAfterFastRestart)))
            {
                RetireRuntimeMetadataBootstrap();
                return;
            }

            if (service == null)
                return;

            if (!service.TryGetInitialUrlMetadataOwner(out var token, out var endpoint))
            {
                if (_runtimeMetadataRequestId != null)
                    RetireRuntimeMetadataBootstrap();
                return;
            }
            if (_runtimeMetadataRequestId != null)
                return;

            _runtimeMetadataOwnerToken = token;
            _runtimeMetadataOwnerEndpoint = endpoint;
            _runtimeMetadataRequestId = Guid.NewGuid().ToString("N");
#if UNITY_WEBGL && !UNITY_EDITOR
            BeginLuxoddRuntimeMetadata(_runtimeMetadataRequestId, token, endpoint);
#endif
        }

        // Only the validated JavaScript projection reaches this receiver. No session/connection calls.
        [Preserve]
        private void OnLuxoddRuntimeMetadata(string json)
        {
            if (_runtimeMetadataRetired || _runtimeMetadataRequestId == null ||
                SessionPayload != null || _isHandlingSessionPayloadEvent || string.IsNullOrWhiteSpace(json) ||
                json.Length > 1024)
                return;

            var service = _sessionFlowController != null ? _sessionFlowController.SocketService : null;
            if (service == null || !service.TryGetInitialUrlMetadataOwner(out var token, out var endpoint) ||
                !string.Equals(token, _runtimeMetadataOwnerToken, StringComparison.Ordinal) ||
                !string.Equals(endpoint, _runtimeMetadataOwnerEndpoint, StringComparison.Ordinal))
            {
                RetireRuntimeMetadataBootstrap();
                return;
            }

            RuntimeMetadataWire wire;
            try { wire = JsonUtility.FromJson<RuntimeMetadataWire>(json); }
            catch (ArgumentException) { return; }
            if (wire == null || wire.version != 1 ||
                !string.Equals(wire.requestId, _runtimeMetadataRequestId, StringComparison.Ordinal))
                return;

            RetireRuntimeMetadataBootstrap();
            LuxoddRuntimeContext.ApplyHostRuntimeContext(wire.hasRuntime, wire.platform, wire.arcadeLayout);
        }

        private void RetireRuntimeMetadataBootstrap()
        {
            if (_runtimeMetadataRetired)
                return;
            _runtimeMetadataRetired = true;
            _runtimeMetadataOwnerToken = null;
            _runtimeMetadataOwnerEndpoint = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            RetireLuxoddRuntimeMetadata();
#endif
        }
    }
}
