mergeInto(LibraryManager.library, {
    ConnectWebSocket: function (urlPtr, transportGeneration, connectionAttemptId) {
        var url = UTF8ToString(urlPtr);
        var socket = new WebSocket(url);
        var hasTransportIdentity = typeof transportGeneration === "number" &&
            typeof connectionAttemptId === "number";
        var transportIdentity = hasTransportIdentity
            ? transportGeneration + "|" + connectionAttemptId + "|"
            : "";

        function sendToUnity(methodName, payload) {
            if (typeof unityInstance === "undefined") {
                console.error("unityInstance is not defined");
                return;
            }

            if (hasTransportIdentity) {
                unityInstance.SendMessage("WebSocketLibraryWrapper", methodName + "ForTransport",
                    transportIdentity + (payload || ""));
            } else if (typeof payload === "undefined") {
                unityInstance.SendMessage("WebSocketLibraryWrapper", methodName);
            } else {
                unityInstance.SendMessage("WebSocketLibraryWrapper", methodName, payload);
            }
        }

        window.UnityWebSocket = socket;
        window.UnityWebSocketPendingMessages = [];

        socket.onopen = function () {
            if (window.UnityWebSocket !== socket) {
                return;
            }

            console.log("WebSocket connected.");
            if (!window.UnityWebSocketPendingMessages) {
                window.UnityWebSocketPendingMessages = [];
            }

            var pendingMessages = window.UnityWebSocketPendingMessages;
            while (pendingMessages.length > 0) {
                if (window.UnityWebSocket !== socket || socket.readyState !== WebSocket.OPEN) {
                    break;
                }

                try {
                    socket.send(pendingMessages[0]);
                    pendingMessages.shift();
                } catch (error) {
                    console.warn("WebSocket message send failed.");
                    break;
                }
            }

            sendToUnity("OnWebSocketOpen");
        };

        socket.onmessage = function (event) {
            if (window.UnityWebSocket !== socket) {
                return;
            }

            console.log("WebSocket message received.");
            sendToUnity("OnWebSocketMessage", event.data);
        };

        socket.onclose = function (event) {
            if (window.UnityWebSocket !== socket) {
                return;
            }

            console.log("WebSocket connection closed, code:", event.code);
            window.UnityWebSocketPendingMessages = [];
            window.UnityWebSocket = null;
            sendToUnity("OnWebSocketClose", event.code);
        };

        socket.onerror = function (error) {
            if (window.UnityWebSocket !== socket) {
                return;
            }

            console.error("WebSocket connection error.");
            sendToUnity("OnWebSocketError", error.type);
        };
    },

    SendWebSocketMessage: function (messagePtr) {
        var message = UTF8ToString(messagePtr);
        var socket = window.UnityWebSocket;
        if (!socket) {
            console.warn("No active WebSocket connection.");
            return;
        }

        if (socket.readyState === WebSocket.OPEN) {
            try {
                socket.send(message);
            } catch (error) {
                console.warn("WebSocket message send failed.");
            }
            return;
        }

        if (socket.readyState === WebSocket.CONNECTING) {
            if (!window.UnityWebSocketPendingMessages) {
                window.UnityWebSocketPendingMessages = [];
            }

            var pendingMessages = window.UnityWebSocketPendingMessages;
            if (pendingMessages.length >= 100) {
                console.warn("WebSocket pending message queue is full.");
                return;
            }

            pendingMessages.push(message);
            return;
        }

        console.warn("WebSocket connection is not open.");
    },

    CloseWebSocket: function () {
        if (window.UnityWebSocket) {
            console.log("Closing WebSocket connection...");
            window.UnityWebSocketPendingMessages = [];
            window.UnityWebSocket.close();
        } else {
            console.error("No active WebSocket connection to close");
        }
    },

    NavigateToHome: function () {
        var currentUrl = window.location.origin;
        var homeUrl = currentUrl + "/home";
        console.log("Navigating to: " + homeUrl);
        window.location.href = homeUrl;
    },

    SendSessionEndMessage: function () {
        console.log("Sending session_end postMessage to parent window");
        window.parent.postMessage({
            type: "session_end"
        }, "*");
    },
	
	SendSessionOptionsMessageWithAction: function (actionPtr) {
        var action = UTF8ToString(actionPtr);
        if (!action) {
            console.warn("SendSessionOptionsMessageWithAction: empty action");
            return;
        }
        console.log("Sending session_options with action:", action);
        window.parent.postMessage({ type: "session_options", action: action }, "*");
    },

    SendFastRestartSessionOptionsMessage: function () {
        window.parent.postMessage({
            type: "session_options",
            action: "restart",
            keepGameLoaded: true
        }, "*");
    },

    SendFastRestartWithContinueSessionOptionsMessage: function () {
        window.parent.postMessage({
            type: "session_options",
            action: "both",
            keepGameLoaded: true
        }, "*");
    },

    SendPreparedFastRestartWithContinueSessionOptionsMessage: function () {
        window.parent.postMessage({
            type: "session_options",
            action: "both",
            keepGameLoaded: true,
            restartPreparationVersion: 1
        }, "*");
    },

    SendRestartReadyMessage: function (requestIdPtr) {
        var requestId = UTF8ToString(requestIdPtr);
        if (!requestId || requestId.trim().length === 0) return;
        window.parent.postMessage({
            type: "restart_ready",
            requestId: requestId
        }, "*");
    },

    SendPrizeWonMessage: function (ticketsJsonPtr) {
        var ticketsJson = UTF8ToString(ticketsJsonPtr);
        if (!ticketsJson) {
            return;
        }

        var tickets;
        try {
            tickets = JSON.parse(ticketsJson);
        } catch (e) {
            console.warn("SendPrizeWonMessage: invalid tickets json", e);
            return;
        }

        if (!Array.isArray(tickets) || tickets.length === 0) {
            return;
        }

        window.parent.postMessage({
            type: "prize_won",
            tickets: tickets
        }, "*");
    }
});
