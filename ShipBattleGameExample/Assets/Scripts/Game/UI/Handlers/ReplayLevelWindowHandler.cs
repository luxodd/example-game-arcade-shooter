using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class ReplayLevelWindowHandler : MonoBehaviour
    {
        private const string PlayAgainButtonKey = "Play";
        private const string NotNowButtonKey = "No";

        private IReplayLevelWindowView _replayLevelWindowView;

        private Action _playAgainButtonClickCallback;
        private Action _notNowButtonClickCallback;

        private int _counter = 0;

        public void PrepareView(IReplayLevelWindowView replayLevelWindowView)
        {
            _replayLevelWindowView = replayLevelWindowView;

            _replayLevelWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ShowReplayLevelWindow()
        {
            _replayLevelWindowView.Show();
        }

        public void HideReplayLevelWindow()
        {
            _replayLevelWindowView.Hide();
        }

        public void SetPlayAgainButtonClick(Action callback)
        {
            _playAgainButtonClickCallback = callback;
            _replayLevelWindowView.SetPlayAgainButtonCallback(OnPlayAgainButtonClicked);
        }

        public void SetNotNowButtonClick(Action callback)
        {
            _notNowButtonClickCallback = callback;
            _replayLevelWindowView.SetNotNowButtonCallback(OnNotNowButtonClicked);
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            switch (stringValue)
            {
                case PlayAgainButtonKey:
                    OnPlayAgainButtonClicked();
                    break;
                case NotNowButtonKey:
                    OnNotNowButtonClicked();
                    break;
            }
        }

        private void OnPlayAgainButtonClicked()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnPlayAgainButtonClicked)}] OK");
            if (_counter > 0) return;

            _counter++;

            _playAgainButtonClickCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnNotNowButtonClicked()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnNotNowButtonClicked)}] OK");

            if (_counter > 0) return;

            _counter++;

            _notNowButtonClickCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}