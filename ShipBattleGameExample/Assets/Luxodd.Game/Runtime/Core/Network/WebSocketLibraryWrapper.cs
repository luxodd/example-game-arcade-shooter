using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Luxodd.Game.Scripts.Network
{
    public enum SessionOptionAction
    {
        Restart,
        Continue,
        End,
        Cancel
    }

    public class WebSocketLibraryWrapper : MonoBehaviour
    {
        [DllImport("__Internal")]
        public static extern void ConnectWebSocket(string url);

        [DllImport("__Internal", EntryPoint = "ConnectWebSocket")]
        private static extern void ConnectWebSocket(string url, int transportGeneration, int connectionAttemptId);

        [DllImport("__Internal")]
        public static extern void SendWebSocketMessage(string message);

        [DllImport("__Internal")]
        public static extern void CloseWebSocket();

        [DllImport("__Internal")]
        public static extern void NavigateToHome();

        [DllImport("__Internal")]
        public static extern void SendSessionEndMessage();

        [DllImport("__Internal")]
        public static extern void SendSessionOptionsMessageWithAction(string action);

        [DllImport("__Internal")]
        public static extern void SendFastRestartSessionOptionsMessage();

        [DllImport("__Internal")]
        private static extern void SendFastRestartWithContinueSessionOptionsMessage();

        [DllImport("__Internal")]
        private static extern void SendPreparedFastRestartWithContinueSessionOptionsMessage();

        [DllImport("__Internal")]
        private static extern void SendRestartReadyMessage(string requestId);
        
        [DllImport("__Internal")]
        public static extern void SendPrizeWonMessage(string ticketsJson);
        
        [DllImport("__Internal")]
        private static extern string GetParentHost();
        
        [DllImport("__Internal")]
        private static extern void GetWebSocketProtocol();

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] 
        private static extern void RegisterHostMessageListener(string goName);
#else
        private static void RegisterHostMessageListener(string goName) =>
            Debug.Log(
                $"[{DateTime.Now}][{nameof(RegisterHostMessageListener)}][{nameof(CloseWebSocketConnection)}] OK, goName={goName}");
#endif

        [DllImport("__Internal")]
        private static extern void SetExpectedHostOrigin(string origin);

        public ISimpleEvent<SessionOptionAction> OnSessionOptionAction => _onSessionOptionAction;

        public ISimpleEvent<string> MessageReceivedEvent => _webSocketMessageEvent;
        public ISimpleEvent<string> WebSocketConnectionErrorEvent => _webSockedConnectionErrorEvent;
        public ISimpleEvent WebSocketOpenedEvent => _webSocketOpenedEvent;
        public ISimpleEvent<int> WebSocketClosedEvent => _webSocketClosedEvent;

        private readonly SimpleEvent<string> _webSocketMessageEvent = new SimpleEvent<string>();
        private readonly SimpleEvent<string> _webSockedConnectionErrorEvent = new SimpleEvent<string>();
        private readonly SimpleEvent _webSocketOpenedEvent = new SimpleEvent();
        private readonly SimpleEvent<int> _webSocketClosedEvent = new SimpleEvent<int>();

        private readonly SimpleEvent<SessionOptionAction> _onSessionOptionAction =
            new SimpleEvent<SessionOptionAction>();

        private int _currentTransportGeneration;
        private int _currentConnectionAttemptId;

        internal ISimpleEvent<string> OnRestartPreparationRequested => _onRestartPreparationRequested;
        private readonly SimpleEvent<string> _onRestartPreparationRequested = new SimpleEvent<string>();
#if UNITY_EDITOR
        private Action<string> _restartReadyTestHook;
#endif

        public void StartWebSocket(string url)
        {
            StartWebSocket(url, 0, 0);
        }

        public void StartWebSocket(string url, int transportGeneration, int connectionAttemptId)
        {
            SetCurrentTransportIdentity(transportGeneration, connectionAttemptId);
            LoggerHelper.Log($"[{GetType().Name}][{nameof(StartWebSocket)}] OK, endpoint: {GetSafeWebSocketEndpoint(url)}");
            _ = ConnectAsync(url, transportGeneration, connectionAttemptId);
        }

        internal bool MatchesTransportIdentity(int transportGeneration, int connectionAttemptId)
        {
            return _currentTransportGeneration == transportGeneration &&
                   _currentConnectionAttemptId == connectionAttemptId;
        }

        internal void SetCurrentTransportIdentity(int transportGeneration, int connectionAttemptId)
        {
            _currentTransportGeneration = transportGeneration;
            _currentConnectionAttemptId = connectionAttemptId;
        }

        public void CloseWebSocketConnection()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(CloseWebSocketConnection)}] OK");
            CloseWebSocket();
        }

        public Task ConnectAsync(string url)
        {
            return ConnectAsync(url, 0, 0);
        }

        private Task ConnectAsync(string url, int transportGeneration, int connectionAttemptId)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(ConnectAsync)}] OK, endpoint: {GetSafeWebSocketEndpoint(url)}");
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                ConnectWebSocket(url, transportGeneration, connectionAttemptId);
#else
                ConnectWebSocket(url);
#endif
                LoggerHelper.Log("Connecting to WebSocket...");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"WebSocket connection failed ({ex.GetType().Name}).");
                _webSockedConnectionErrorEvent.Notify("WebSocket connection failed.");
            }

            return Task.CompletedTask;
        }

        private static string GetSafeWebSocketEndpoint(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var websocketUri)
                ? websocketUri.GetLeftPart(UriPartial.Path)
                : "WebSocket endpoint";
        }

        public void SendMessageToWebSocket(string message)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(SendMessageToWebSocket)}] " + message);
            try
            {
                SendWebSocketMessage(message);
                LoggerHelper.Log("Message sent: " + message);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error sending WebSocket message: " + ex.Message);
            }
        }

        public void GoToHome()
        {
            NavigateToHome();
        }

        public void NotifySessionEnd()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(NotifySessionEnd)}] OK");
#if UNITY_WEBGL && !UNITY_EDITOR
            SendSessionEndMessage();
#endif
        }

        public void SendSessionOptionsRestart()
        {
            PostSessionOptionCommand(SessionOptionAction.Restart);
        }

        public void SendSessionOptionsFastRestart()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendFastRestartSessionOptionsMessage();
#endif
        }

        public void SendSessionOptionsContinue()
        {
            PostSessionOptionCommand(SessionOptionAction.Continue);
        }

        internal void SendSessionOptionsFastRestartWithContinue()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendFastRestartWithContinueSessionOptionsMessage();
#endif
        }

        internal void SendSessionOptionsFastRestartWithContinuePrepared()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendPreparedFastRestartWithContinueSessionOptionsMessage();
#endif
        }

        internal void SendRestartReady(string requestId)
        {
#if UNITY_EDITOR
            _restartReadyTestHook?.Invoke(requestId);
#elif UNITY_WEBGL
            SendRestartReadyMessage(requestId);
#endif
        }

        public void OnHostRestartPrepare(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId) == false)
            {
                _onRestartPreparationRequested.Notify(requestId);
            }
        }

        public void SendSessionOptionsEnd()
        {
            PostSessionOptionCommand(SessionOptionAction.End);
        }

        public void NotifyPrizeWon(string ticketsJson)
        {
            if (string.IsNullOrEmpty(ticketsJson))
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            SendPrizeWonMessage(ticketsJson);
#endif
        }

        [ContextMenu("Test Prize Won Message")]
        public void TestPrizeWonMessage()
        {
            const string testTicketsJson =
                "[{\"prize_name\":\"$5 Demo Cash\",\"prize_text\":\"Win $5!\",\"qr_data\":\"https://staging-app.luxodd.com/redeem/84SPQJMG\",\"ticket_code\":\"84SPQJMG\",\"ticket_id\":\"26f912aa-5b2f-47b1-929e-a3c48cbea024\"}]";

            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestPrizeWonMessage)}] Sending test prize_tickets payload");
            NotifyPrizeWon(testTicketsJson);
        }

        public void OnHostSessionAction(string action)
        {
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][Host→Game] Action allowed: {action}");

            var actionPascalCase = action.ToPascalCase();
            var sessionOptionAction = (SessionOptionAction)Enum.Parse(typeof(SessionOptionAction), actionPascalCase);

            _onSessionOptionAction.Notify(sessionOptionAction);

        }

        public void OnWebSocketOpen()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketOpen)}] OK");
            _webSocketOpenedEvent.Notify();
        }

        public void OnWebSocketOpenForTransport(string callbackPayload)
        {
            if (TryGetCurrentTransportPayload(callbackPayload, out _) == false)
            {
                return;
            }

            OnWebSocketOpen();
        }

        public void OnWebSocketMessage(string message)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketMessage)}] WebSocket message received.");
            _webSocketMessageEvent.Notify(message);
        }

        public void OnWebSocketMessageForTransport(string callbackPayload)
        {
            if (TryGetCurrentTransportPayload(callbackPayload, out var message) == false)
            {
                return;
            }

            OnWebSocketMessage(message);
        }

        public void OnWebSocketClose(int code)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketClose)}] OK, code: {code}");
            _webSocketClosedEvent.Notify(code);
        }

        public void OnWebSocketCloseForTransport(string callbackPayload)
        {
            if (TryGetCurrentTransportPayload(callbackPayload, out var codeText) == false ||
                int.TryParse(codeText, out var code) == false)
            {
                return;
            }

            OnWebSocketClose(code);
        }

        public void OnWebSocketError(string error)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketError)}] WebSocket connection error received.");
            _webSockedConnectionErrorEvent.Notify(error);
        }

        public void OnWebSocketErrorForTransport(string callbackPayload)
        {
            if (TryGetCurrentTransportPayload(callbackPayload, out var error) == false)
            {
                return;
            }

            OnWebSocketError(error);
        }

        private bool TryGetCurrentTransportPayload(string callbackPayload, out string payload)
        {
            payload = null;
            if (string.IsNullOrEmpty(callbackPayload))
            {
                return false;
            }

            var generationSeparator = callbackPayload.IndexOf('|');
            var attemptSeparator = generationSeparator >= 0
                ? callbackPayload.IndexOf('|', generationSeparator + 1)
                : -1;
            if (generationSeparator < 0 || attemptSeparator < 0 ||
                int.TryParse(callbackPayload.Substring(0, generationSeparator), out var generation) == false ||
                int.TryParse(callbackPayload.Substring(generationSeparator + 1,
                    attemptSeparator - generationSeparator - 1), out var attemptId) == false ||
                generation != _currentTransportGeneration || attemptId != _currentConnectionAttemptId)
            {
                return false;
            }

            payload = callbackPayload.Substring(attemptSeparator + 1);
            return true;
        }

        public void ReceiveMessageFromWebSocket(string message)
        {
            LoggerHelper.Log($"[{DateTime.Now}]Message received from WebSocket.");
            _webSocketMessageEvent.Notify(message);
        }

        private void PostSessionOptionCommand(SessionOptionAction sessionOptionAction)
        {
            var actionOption = sessionOptionAction.ToString().ToLower();
            SendSessionOptionsMessageWithAction(actionOption);
        }

        private void Awake()
        {
            RegisterHostMessageListener(gameObject.name);
        }
    }
}
