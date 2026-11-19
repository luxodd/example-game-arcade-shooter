mergeInto(LibraryManager.library, {
    InitLuxoddRuntimeMetadata: function (goNamePtr) {
        // One private lifecycle per iframe document. Never replay session credentials.
        if (window.__LuxoddRuntimeMetadata) return;
        var target = UTF8ToString(goNamePtr);
        var request = null;
        var retired = false;
        var timer = null;
        var attempts = 0;

        function retire() {
            retired = true;
            request = null;
            if (timer !== null) clearTimeout(timer);
            timer = null;
        }

        function instance() {
            return window.unityInstance && typeof window.unityInstance.SendMessage === "function"
                ? window.unityInstance : null;
        }

        function launchMatches(token, endpoint) {
            try {
                if (window.parent === window || window.parent.location.origin !== window.location.origin ||
                    !/^https?:$/.test(window.location.protocol)) return false;
                var url = new URL(window.location.href);
                var tokens = url.searchParams.getAll("token");
                var endpoints = url.searchParams.getAll("ws");
                if (tokens.length !== 1 || tokens[0] !== token || !token.trim() || endpoints.length > 1)
                    return false;
                // Match WebSocketService's existing non-local parent endpoint override.
                var expected = window.parent.location.host.indexOf("localhost") === -1
                    ? (window.location.protocol === "https:" ? "wss://" : "ws://") +
                        window.parent.location.host + "/ws"
                    : endpoints[0];
                return !!expected && new URL(expected).href === new URL(endpoint).href;
            } catch (e) { return false; }
        }

        function notifyUnityReady() {
            if (retired || !request || timer !== null) return;
            if (++attempts > 30) { retire(); return; }
            if (instance()) {
                // Capture the origin before sending; never infer trust from an incoming message.
                window.parent.postMessage({ type: "luxodd:runtime-ready", version: 1,
                    requestId: request.id }, request.origin);
            }
            timer = setTimeout(function () { timer = null; notifyUnityReady(); }, 1000);
        }

        window.addEventListener("message", function (event) {
            if (retired || !request || !instance() || event.source !== window.parent ||
                event.origin !== request.origin || !launchMatches(request.token, request.endpoint)) return;
            var data = event.data;
            if (!data || typeof data !== "object" || Array.isArray(data) ||
                data.type !== "luxodd:runtime-metadata" || data.version !== 1 ||
                data.requestId !== request.id) return;
            var keys = Object.keys(data);
            if (keys.length !== 4 || !Object.prototype.hasOwnProperty.call(data, "runtime")) return;
            var runtime = data.runtime;
            if (runtime !== null) {
                if (typeof runtime !== "object" || Array.isArray(runtime) ||
                    Object.keys(runtime).some(function (key) {
                        return key !== "platform" && key !== "arcadeLayout";
                    }) ||
                    (runtime.platform !== undefined && (typeof runtime.platform !== "string" || runtime.platform.length > 64)) ||
                    (runtime.arcadeLayout !== undefined && (typeof runtime.arcadeLayout !== "string" || runtime.arcadeLayout.length > 64)))
                    return;
            }
            var projection = JSON.stringify({ version: 1, requestId: request.id,
                hasRuntime: runtime !== null, platform: runtime && runtime.platform,
                arcadeLayout: runtime && runtime.arcadeLayout });
            var unity = instance();
            retire();
            unity.SendMessage(target, "OnLuxoddRuntimeMetadata", projection);
        });
        window.addEventListener("pagehide", retire);
        window.__LuxoddRuntimeMetadata = {
            begin: function (id, token, endpoint) {
                if (retired || request) return;
                if (!launchMatches(token, endpoint)) { retire(); return; }
                request = { id: id, token: token, endpoint: endpoint, origin: window.location.origin };
                notifyUnityReady();
            },
            notifyUnityReady: notifyUnityReady,
            retire: retire
        };
    },
    BeginLuxoddRuntimeMetadata: function (requestIdPtr, tokenPtr, endpointPtr) {
        var state = window.__LuxoddRuntimeMetadata;
        if (state) state.begin(UTF8ToString(requestIdPtr), UTF8ToString(tokenPtr), UTF8ToString(endpointPtr));
    },
    RetireLuxoddRuntimeMetadata: function () {
        var state = window.__LuxoddRuntimeMetadata;
        if (state) state.retire();
    }
});
