using System;
using Core.Storage;
using Game.Player;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using UnityEngine;

namespace Debugging
{
    public class DebugPanelActivator : MonoBehaviour
    {
        [SerializeField] private DebugPanelView _debugPanelView;
        
        [SerializeField] private StorageService _storageService;
        [SerializeField] private PlayerBehaviour _playerBehaviour;

        [SerializeField] private int _timesBeforeActivate = 2;
        [SerializeField] private float _delayBeforeActivate = 0.5f;
        [SerializeField] private KeyCode _keyToActivate = KeyCode.F;
        
        private bool _isActivated = false;

        private int _currentTimes = 0;
        private float _timeCounter = 0f;
        
        public void Activate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Activate)}] OK");
            
            if (_isActivated) return;
            
            _isActivated = true;
            
            _debugPanelView.Show();
        }

        public void Deactivate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Deactivate)}] OK");
            _isActivated = false;
            _debugPanelView.Hide();
        }

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            ResetActivation();
            _debugPanelView.Hide();
        }

        private void Update()
        {
            if (Input.GetKeyUp(_keyToActivate))
            {
                CalculateActivation();
            }
        }

        private void SubscribeToEvents()
        {
            _debugPanelView.HealthCheckToggleEvent.AddListener(OnHealthCheckToggleCallback);
            
            _debugPanelView.SetCloseButtonCallback(OnCloseButtonClickedHandler);
            
            _debugPanelView.SetTestClearStorageButtonCallback(TestClearStorageAndSave);
            _debugPanelView.SetTestSaveCurrentProgressButtonCallback(TestSaveCurrentProgress);
            _debugPanelView.SetTestLoadCurrentProgressButtonCallback(TestLoadCurrentProgress);
            _debugPanelView.SetTestCompleteLevelButtonCallback(TestCompleteLevel);
            
            _debugPanelView.SetLoggerToggleCallback(OnLoggerToggleCallback);
            
            EventAggregator.Subscribe<DebugLoggerSetupEvent>(OnDebugLoggerSetupEventHandler);
        }

        private void UnsubscribeFromEvents()
        {
            _debugPanelView.HealthCheckToggleEvent.AddListener(OnHealthCheckToggleCallback);
            _debugPanelView.SetCloseButtonCallback(OnCloseButtonClickedHandler);
            
            EventAggregator.Unsubscribe<DebugLoggerSetupEvent>(OnDebugLoggerSetupEventHandler);
        }

        private void OnHealthCheckToggleCallback(bool isOn)
        {
            //Send Event via event aggregator
            EventAggregator.Post(this, new DebugHealthCheckStatusEvent(){IsOn = isOn});
        }

        private void OnLoggerToggleCallback(bool isOn)
        {
            EventAggregator.Post(this, new DebugLoggerEnableEvent(){IsLogEnabled = isOn, IsWarningLogEnabled = isOn});
        }

        private void OnCloseButtonClickedHandler()
        {
            Deactivate();
        }
        
        private void CalculateActivation()
        {
            if (_isActivated) return;
            
            if (Time.time >= _timeCounter)
            {
                ResetActivation();
            }
            
            if (_currentTimes == 0)
            {
                _timeCounter = Time.time + _delayBeforeActivate;
            }

            if (Time.time <= _timeCounter)
            {
                _currentTimes++;
            }
            
            if (_currentTimes >= _timesBeforeActivate)
            {
                Activate();
            }
        }

        private void ResetActivation()
        {
            _currentTimes = 0;
            _timeCounter = _delayBeforeActivate;
        }

        private void OnDebugLoggerSetupEventHandler(object sender, DebugLoggerSetupEvent eventData)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDebugLoggerSetupEventHandler)}] OK");
            _debugPanelView.SetLoggerToggleValue(eventData.IsLogEnabled);;
        }
        
        #region For Debugging

        private void TestClearStorageAndSave()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestClearStorageAndSave)}] OK");
            _storageService.ClearStorage();
        }

        private void TestSaveCurrentProgress()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestSaveCurrentProgress)}] OK");
            _storageService.Save();
        }

        private void TestLoadCurrentProgress()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestLoadCurrentProgress)}] OK");
            _storageService.Load();
        }

        private void TestCompleteLevel()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestCompleteLevel)}] OK");
            _playerBehaviour.CompleteLevel();
        }
        #endregion
    }
}
