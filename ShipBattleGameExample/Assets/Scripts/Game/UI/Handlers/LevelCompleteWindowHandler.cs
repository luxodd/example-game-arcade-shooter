using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class LevelCompleteWindowHandler : MonoBehaviour
    {
        private const string NextButtonKey = "Next";

        private ILevelCompleteWindowView _levelCompleteWindowView;

        private Action _nextButtonClickCallback;

        private int _counter = 0;

        public void PrepareLevelCompleteWindow(ILevelCompleteWindowView levelCompleteWindowView)
        {
            _levelCompleteWindowView = levelCompleteWindowView;
            _levelCompleteWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ShowLevelCompleteWindow()
        {
            _levelCompleteWindowView.Show();
        }

        public void HideLevelCompleteWindow()
        {
            _levelCompleteWindowView.Hide();
        }

        public void SetNextButtonClickedHandler(System.Action callback)
        {
            _nextButtonClickCallback = callback;
            _levelCompleteWindowView.SetNextButtonClickedHandler(OnNextButtonClickHandler);
        }

        public void SetMainMenuButtonClickedHandler(System.Action callback)
        {
            _levelCompleteWindowView.SetMainMenuButtonClickedHandler(callback);
        }

        public void SetGameResultData(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            _levelCompleteWindowView.SetGameResultData(totalScore, enemyKill, accuracy, levelProgress, totalSeconds);
        }

        public void SetLevelNumber(int levelNumber)
        {
            _levelCompleteWindowView.SetLevelNumber(levelNumber);
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            switch (stringValue)
            {
                case NextButtonKey:
                    OnNextButtonClickHandler();
                    break;
            }
        }

        private void OnNextButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _nextButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}