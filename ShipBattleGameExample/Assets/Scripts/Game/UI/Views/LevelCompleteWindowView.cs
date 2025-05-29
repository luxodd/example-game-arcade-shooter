using System;
using Core.UI;
using Game.UI.Items.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface ILevelCompleteWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetLevelNumber(int levelNumber);

        void SetGameResultData(int totalScore, int enemyKill, float accuracy, float levelProgress,
            float totalSeconds);

        void SetNextButtonClickedHandler(System.Action callback);
        void SetMainMenuButtonClickedHandler(System.Action callback);
    }

    public class LevelCompleteWindowView : BaseView, ILevelCompleteWindowView
    {
        public override ViewType ViewType => ViewType.LevelCompleteWindow;

        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _gameResultDataText;

        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _mainMenuButton;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;

        private System.Action _nextButtonClickedCallback;
        private System.Action _mainMenuButtonClickedCallback;

        private string _titleTextFormat;

        public void SetLevelNumber(int levelNumber)
        {
            _titleText.text = string.Format(_titleTextFormat, levelNumber);
        }

        public void SetGameResultData(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            _gameResultDataText.text =
                StatisticHelper.GetStatisticFormat(totalScore, enemyKill, accuracy, levelProgress, totalSeconds);
        }

        public void SetNextButtonClickedHandler(Action callback)
        {
            _nextButtonClickedCallback = callback;
        }

        public void SetMainMenuButtonClickedHandler(Action callback)
        {
            _mainMenuButtonClickedCallback = callback;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _titleTextFormat = _titleText.text;

            _nextButton.onClick.AddListener(OnNextButtonClicked);
            _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _keyboardNavigator.Activate();
        }

        protected override void OnHide()
        {
            base.OnHide();
            _keyboardNavigator.Deactivate();
        }

        private void OnNextButtonClicked()
        {
            _nextButtonClickedCallback?.Invoke();
        }

        private void OnMainMenuButtonClicked()
        {
            _mainMenuButtonClickedCallback?.Invoke();
        }
    }
}