using System;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Debugging
{
    public class DebugPanelView : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        
        [SerializeField] private ToggleButton _healthCheckToggle;
        [SerializeField] private ToggleButton _loggerToggle;
        
        [Header("Test Section")]
        [SerializeField] private Button _testClearStorageButton;
        [SerializeField] private Button _testSaveCurrentProgressButton;
        [SerializeField] private Button _testLoadCurrentProgressButton;
        [SerializeField] private Button _testCompleteLevelButton;
        
        public ISimpleEvent<bool> HealthCheckToggleEvent => _healthCheckToggleEvent;
        private SimpleEvent<bool> _healthCheckToggleEvent = new SimpleEvent<bool>();
        
        private SimpleEvent<bool> _loggerToggleEvent = new SimpleEvent<bool>();
        
        private Action _onClearStorageButtonClicked;
        private Action _onSaveCurrentProgressButtonClicked;
        private Action _onLoadCurrentProgressButtonClicked;
        private Action _onCompleteLevelButtonClicked;
        
        private Action _onCloseButtonPressed;
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetCloseButtonCallback(Action onCloseButtonPressed)
        {
            _onCloseButtonPressed = onCloseButtonPressed;
        }
        
        public void SetHealthCheckToggleCallback(Action<bool> callback)
        {
            _healthCheckToggleEvent.AddListener(callback);
        }
        
        public void SetTestClearStorageButtonCallback(Action callback)
        {
            _onClearStorageButtonClicked = callback;
        }

        public void SetTestSaveCurrentProgressButtonCallback(Action callback)
        {
            _onSaveCurrentProgressButtonClicked = callback;
        }

        public void SetTestLoadCurrentProgressButtonCallback(Action callback)
        {
            _onLoadCurrentProgressButtonClicked = callback;
        }

        public void SetTestCompleteLevelButtonCallback(Action callback)
        {
            _onCompleteLevelButtonClicked = callback;
        }

        public void SetLoggerToggleCallback(Action<bool> callback)
        {
            _loggerToggleEvent.AddListener(callback);
        }

        public void SetLoggerToggleValue(bool isOn)
        {
            _loggerToggle.SetValue(isOn);
        }

        private void Awake()
        {
            _healthCheckToggle.ToggleEvent.AddListener(OnHealthCheckToggleHandler);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            
            _testClearStorageButton.onClick.AddListener(OnTestClearStorageButtonClicked);
            _testSaveCurrentProgressButton.onClick.AddListener(OnTestSaveCurrentProgressButtonClicked);
            _testLoadCurrentProgressButton.onClick.AddListener(OnTestLoadCurrentProgressButtonClicked);
            _testCompleteLevelButton.onClick.AddListener(OnTestCompleteLevelButtonClicked);
        }

        private void Start()
        {
            _loggerToggle.ToggleEvent.AddListener(OnLoggersToggleHandler);
            OnHealthCheckToggleHandler(_healthCheckToggle.ToggleValue);
        }

        private void OnHealthCheckToggleHandler(bool isOn)
        {
            _healthCheckToggleEvent.Notify(isOn);
        }

        private void OnLoggersToggleHandler(bool isOn)
        {
            _loggerToggleEvent.Notify(isOn);
        }

        private void OnCloseButtonClicked()
        {
            _onCloseButtonPressed?.Invoke();
        }
        
        private void OnTestClearStorageButtonClicked()
        {
            _onClearStorageButtonClicked?.Invoke();
        }
        
        private void OnTestSaveCurrentProgressButtonClicked()
        {
            _onSaveCurrentProgressButtonClicked?.Invoke();
        }
        
        private void OnTestLoadCurrentProgressButtonClicked()
        {
            _onLoadCurrentProgressButtonClicked?.Invoke();
        }
        
        private void OnTestCompleteLevelButtonClicked()
        {
            _onCompleteLevelButtonClicked?.Invoke();
        }
    }
}
