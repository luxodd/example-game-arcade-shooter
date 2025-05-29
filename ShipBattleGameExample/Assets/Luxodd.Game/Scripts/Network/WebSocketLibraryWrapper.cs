using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Luxodd.Game.Scripts.Network
{
    public class WebSocketLibraryWrapper : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void ConnectWebSocket(string url);

        [DllImport("__Internal")]
        private static extern void SendWebSocketMessage(string message);
        
        [DllImport("__Internal")]
        private static extern void CloseWebSocket();
        
        [DllImport("__Internal")]
        private static extern void NavigateToHome();
        
        [DllImport("__Internal")]
        private static extern void SendSessionEndMessage();

        public ISimpleEvent<string> MessageReceivedEvent => _webSocketMessageEvent;
        public ISimpleEvent<string> WebSocketConnectionErrorEvent => _webSockedConnectionErrorEvent;
        public ISimpleEvent WebSocketOpenedEvent => _webSocketOpenedEvent;
        public ISimpleEvent<int> WebSocketClosedEvent => _webSocketClosedEvent;
        
        private readonly SimpleEvent<string> _webSocketMessageEvent = new SimpleEvent<string>();
        private readonly SimpleEvent<string> _webSockedConnectionErrorEvent = new SimpleEvent<string>();
        private readonly SimpleEvent _webSocketOpenedEvent = new SimpleEvent();
        private readonly SimpleEvent<int> _webSocketClosedEvent = new SimpleEvent<int>();
        
        public void StartWebSocket(string url)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(StartWebSocket)}] OK, url: {url}");
            _ = ConnectAsync(url);
        }

        public void CloseWebSocketConnection()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(CloseWebSocketConnection)}] OK");
            CloseWebSocket();
        }
        
        public Task ConnectAsync(string url)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(ConnectAsync)}] OK, url: {url}");
            try
            {
                ConnectWebSocket(url);
                LoggerHelper.Log("Connecting to WebSocket...");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("WebSocket connection failed: " + ex.Message);
            }

            return Task.CompletedTask;
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
            LoggerHelper.Log($"[{GetType().Name}][{nameof(NotifySessionEnd)}] OK");
#if UNITY_WEBGL && !UNITY_EDITOR
            SendSessionEndMessage();
#endif
        }

        public void OnWebSocketOpen()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(OnWebSocketOpen)}] OK");
            _webSocketOpenedEvent.Notify();
        }

        public void OnWebSocketMessage(string message)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(OnWebSocketMessage)}] " + message);
            _webSocketMessageEvent.Notify(message);
        }

        public void OnWebSocketClose(int code)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketClose)}] OK, code: {code}");
            _webSocketClosedEvent.Notify(code);
        }

        public void OnWebSocketError(string error)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketError)}] OK, message: {error}");
            _webSockedConnectionErrorEvent.Notify(error);
        }
        
        public void ReceiveMessageFromWebSocket(string message)
        {
            LoggerHelper.Log("Message from WebSocket: " + message);
            _webSocketMessageEvent.Notify(message);
        }
    }
}
