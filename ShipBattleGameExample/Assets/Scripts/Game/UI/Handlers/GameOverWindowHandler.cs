using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class GameOverWindowHandler : MonoBehaviour
    {
        private const string RestartButtonKey = "Restart";
        private const string NextButtonKey = "Next";

        private IGameOverWindowView _gameOverWindowView;

        private Action _restartButtonClickCallback;
        private Action _nextButtonClickCallback;

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

        public void SetRestartButtonCallback(System.Action callback)
        {
            _restartButtonClickCallback = callback;
            _gameOverWindowView.SetRestartButtonClickedHandler(OnRestartButtonClickHandler);
        }

        public void SetNextButtonCallback(System.Action callback)
        {
            _nextButtonClickCallback = callback;
            _gameOverWindowView.SetNextButtonClickedHandler(OnNextButtonClickHandler);
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

        public void SetRestartView()
        {
            _gameOverWindowView.SetRestartView();
        }

        public void SetNextView()
        {
            _gameOverWindowView.SetNextView();
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            switch (stringValue)
            {
                case RestartButtonKey:
                    OnRestartButtonClickHandler();
                    break;
                case NextButtonKey:
                    OnNextButtonClickHandler();
                    break;
            }
        }

        private void OnRestartButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _restartButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
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