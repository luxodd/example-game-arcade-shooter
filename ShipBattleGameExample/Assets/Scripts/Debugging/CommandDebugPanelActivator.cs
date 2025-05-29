using System;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Debugging
{
    public class CommandDebugPanelActivator : MonoBehaviour
    {
        [SerializeField] private CommandQueueInspector _commandQueueInspector;

        [SerializeField] private int _timesBeforeActivate = 2;
        [SerializeField] private float _delayBeforeActivate = 0.5f;
        [SerializeField] private KeyCode _keyToActivate = KeyCode.F;
        
        private bool _isActivated = false;

        private int _currentTimes = 0;
        private float _timeCounter = 0f;
        
        public void Activate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Activate)}] OK");
            
            _commandQueueInspector.gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Deactivate)}] OK");
            _commandQueueInspector.gameObject.SetActive(false);
        }

        private void Start()
        {
            _commandQueueInspector.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyUp(_keyToActivate))
            {
                CalculateActivation();
            }
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
    }
}
