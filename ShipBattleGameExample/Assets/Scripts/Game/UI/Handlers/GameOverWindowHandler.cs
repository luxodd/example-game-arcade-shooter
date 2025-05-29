using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class GameOverWindowHandler : MonoBehaviour
    {
        private const string YesButtonKey = "Yes";
        private const string NoButtonKey = "No";

        private IGameOverWindowView _gameOverWindowView;

        private Action _yesButtonClickCallback;
        private Action _noButtonClickCallback;

        private int _counter = 0;

        public void PrepareView(IGameOverWindowView gameOverWindowView)
        {
            _gameOverWindowView = gameOverWindowView;
            _gameOverWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ShowGameOverWindow()
        {
            _gameOverWindowView.Show();
        }

        public void HideGameOverWindow()
        {
            _gameOverWindowView.Hide();
        }

        public void SetYesButtonCallback(System.Action callback)
        {
            _yesButtonClickCallback = callback;
            _gameOverWindowView.SetYesButtonClickedHandler(OnYesButtonClickHandler);
        }

        public void SetNoButtonCallback(System.Action callback)
        {
            _noButtonClickCallback = callback;
            _gameOverWindowView.SetNoButtonClickedHandler(OnNoButtonClickHandler);
        }

        public void SetCreditsCount(int creditsCount)
        {
            _gameOverWindowView.SetCreditsCount(creditsCount);
        }

        public void SetLevelNumber(int levelNumber)
        {
            _gameOverWindowView.SetLevelNumber(levelNumber);
        }

        public void SetGameResultData(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            _gameOverWindowView.SetGameResultData(totalScore, enemyKill, accuracy, levelProgress, totalSeconds);
        }

        public void SetDifferenceData(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            _gameOverWindowView.SetDifferenceData(totalScore, enemyKill, accuracy, levelProgress, totalSeconds);
        }

        public void SetMotivatedPhrase(string phrase)
        {
            _gameOverWindowView.SetMotivatedPhrase(phrase);
        }

        public void SetKeyboardNavigatorFocused(bool isFocused)
        {
            _gameOverWindowView.KeyboardNavigator.SetFocus(isFocused);
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            switch (stringValue)
            {
                case YesButtonKey:
                    OnYesButtonClickHandler();
                    break;
                case NoButtonKey:
                    OnNoButtonClickHandler();
                    break;
            }
        }

        private void OnYesButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _yesButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnNoButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _noButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}