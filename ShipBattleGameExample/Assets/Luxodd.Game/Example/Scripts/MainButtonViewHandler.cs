using System;
using Luxodd.Game.Scripts.HelpersAndUtils.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Luxodd.Game.Example.Scripts
{
    public class MainButtonViewHandler : MonoBehaviour
    {
        [SerializeField] private Button _connectToServerButton;
        [SerializeField] private ToggleButton _sendHealthStatusToServerButton;
        [SerializeField] private Button _getUserProfileButton;
        [SerializeField] private Button _addCreditsButton;
        [SerializeField] private Button _chargeCreditsButton;
        [SerializeField] private Button _storageCommandsButton;

        private Action _onConnectedToServerButtonClickedCallback;
        private Action _onGetUserProfileButtonClickedCallback;
        private Action _onAddCreditsButtonClickedCallback;
        private Action _onChargeCreditsButtonClickedCallback;
        private Action _onStorageCommandsButtonClickedCallback;
        private Action<bool> _onSendHealthStatusToServerButtonClickedCallback;

        public void SetOnConnectedToServerButtonClickedCallback(Action onConnectedToServerButtonClickedCallback)
        {
            _onConnectedToServerButtonClickedCallback = onConnectedToServerButtonClickedCallback;
        }

        public void SetOnGetUserProfileButtonClickedCallback(Action onGetUserProfileButtonClickedCallback)
        {
            _onGetUserProfileButtonClickedCallback = onGetUserProfileButtonClickedCallback;
        }

        public void SetOnAddCreditsButtonClickedCallback(Action onAddCreditsButtonClickedCallback)
        {
            _onAddCreditsButtonClickedCallback = onAddCreditsButtonClickedCallback;
        }

        public void SetOnChargeCreditsButtonClickedCallback(Action onChargeCreditsButtonClickedCallback)
        {
            _onChargeCreditsButtonClickedCallback = onChargeCreditsButtonClickedCallback;
        }

        public void SetOnSendHealthStatusToServerButtonClickedCallback(
            Action<bool> onSendHealthStatusToServerButtonClickedCallback)
        {
            _onSendHealthStatusToServerButtonClickedCallback = onSendHealthStatusToServerButtonClickedCallback;
        }
        
        public void SetOnStorageCommandsButtonClickedCallback(Action onStorageCommandsButtonClickedCallback)
        {
            _onStorageCommandsButtonClickedCallback = onStorageCommandsButtonClickedCallback;
        }
        
        private void Awake()
        {
            _connectToServerButton.onClick.AddListener(OnConnectToServerButtonClickedHandler);
            _getUserProfileButton.onClick.AddListener(OnGetUserProfileButtonClickedHandler);
            _addCreditsButton.onClick.AddListener(OnAddCreditsButtonClickedHandler);
            _chargeCreditsButton.onClick.AddListener(OnChargeCreditsButtonClickedHandler);
            _sendHealthStatusToServerButton.ToggleEvent.AddListener(OnSendHealthStatusToServerButtonClickedHandler);
            _storageCommandsButton.onClick.AddListener(OnStorageCommandsButtonClickedHandler);
        }
        
        private void OnConnectToServerButtonClickedHandler()
        {
            _onConnectedToServerButtonClickedCallback?.Invoke();    
        }

        private void OnGetUserProfileButtonClickedHandler()
        {
            _onGetUserProfileButtonClickedCallback?.Invoke();
        }

        private void OnAddCreditsButtonClickedHandler()
        {
            _onAddCreditsButtonClickedCallback?.Invoke();
        }

        private void OnChargeCreditsButtonClickedHandler()
        {
            _onChargeCreditsButtonClickedCallback?.Invoke();
        }

        private void OnSendHealthStatusToServerButtonClickedHandler(bool value)
        {
            _onSendHealthStatusToServerButtonClickedCallback?.Invoke(value);
        }
        
        private void OnStorageCommandsButtonClickedHandler()
        {
            _onStorageCommandsButtonClickedCallback?.Invoke();
        }
    }
}
