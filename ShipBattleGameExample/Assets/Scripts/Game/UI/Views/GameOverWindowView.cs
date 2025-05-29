using System;
using Core.UI;
using Game.UI.Items.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface IGameOverWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetYesButtonClickedHandler(System.Action callback);
        void SetNoButtonClickedHandler(System.Action callback);

        void SetCreditsCount(int creditsCount);

        void SetGameResultData(int totalScore, int enemyKill, float accuracy, float levelProgress,
            float totalSeconds);

        void SetDifferenceData(int totalScore, int enemyKill, float accuracy, float levelProgress,
            float totalSeconds);

        void SetMotivatedPhrase(string phrase);

        void SetLevelNumber(int levelNumber);
    }

    public class GameOverWindowView : BaseView, IGameOverWindowView
    {
        public override ViewType ViewType => ViewType.GameOverWindow;
        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _noButton;

        [SerializeField] private TMP_Text _creditsText;
        [SerializeField] private TMP_Text _gameResultText;
        [SerializeField] private TMP_Text _levelNumberText;
        [SerializeField] private TMP_Text _differencePreviousText;
        [SerializeField] private TMP_Text _motivatedPhraseText;
        [SerializeField] private GameObject _differenceDataHolder;
        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;

        private System.Action _yesButtonClickedHandler;
        private System.Action _noButtonClickedHandler;

        private string _creditsTextFormat;
        private string _levelNumberTextFormat;

        public void SetYesButtonClickedHandler(Action callback)
        {
            _yesButtonClickedHandler = callback;
        }

        public void SetNoButtonClickedHandler(Action callback)
        {
            _noButtonClickedHandler = callback;
        }

        public void SetCreditsCount(int creditsCount)
        {
            _creditsText.text = string.Format(_creditsTextFormat, creditsCount);
        }

        public void SetGameResultData(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            _differenceDataHolder.SetActive(false);

            var statisticText = StatisticHelper.GetStatisticFormat(totalScore, enemyKill, accuracy,
                levelProgress, totalSeconds);
            _gameResultText.text = statisticText;
        }

        public void SetDifferenceData(int totalScore, int enemyKill, float accuracy, float levelProgress,
            float totalSeconds)
        {
            _differencePreviousText.text =
                StatisticHelper.GetDifferenceFormat(totalScore, enemyKill, accuracy, levelProgress, totalSeconds);
        }

        public void SetMotivatedPhrase(string phrase)
        {
            _motivatedPhraseText.text = phrase;

            _differenceDataHolder.SetActive(true);
        }

        public void SetLevelNumber(int levelNumber)
        {
            _levelNumberText.text = string.Format(_levelNumberTextFormat, levelNumber);
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _creditsTextFormat = _creditsText.text;
            _levelNumberTextFormat = _levelNumberText.text;

            _yesButton.onClick.AddListener(OnYesButtonClicked);
            _noButton.onClick.AddListener(OnNoButtonClicked);
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

        private void OnYesButtonClicked()
        {
            _yesButtonClickedHandler?.Invoke();
        }

        private void OnNoButtonClicked()
        {
            _noButtonClickedHandler?.Invoke();
        }
    }

    public static class StatisticHelper
    {
        private const string StatisticFormat =
            "Your Result:\nTotalScore: <color=#FFFF00><size=70>{0}</size></color>\nEnemy kills: " +
            "<color=#FFFF00><size=70>{1}</size></color>" +
            "\nAccuracy: <color=#FFFF00><size=70>{2:F2}%</size></color>\nLevel Progress: " +
            "<color=#FFFF00><size=70>{3:F2}%</size></color>\nTime: <color=#FFFF00><size=70>{4}</size></color>\n";

        private const string DifferenceStatisticFormat = "Compare with Previous:\nTotalScore: {0}%\nEnemy Kills:{1}%" +
                                                         "\nAccuracy: {2}%\nLevel Progress: {3}%\nTime: {4}%\n";

        private const string RedColorStringFormat = "<color=#FF0000>{0}</color>";
        private const string GreenColorStringFormat = "<color=#00FF00>{0}</color>";

        public static string GetStatisticFormat(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            var timeInString = ConvertToString(totalSeconds);
            return string.Format(StatisticFormat, totalScore, enemyKill, accuracy, levelProgress, timeInString);
        }

        public static string GetDifferenceFormat(int totalScore, int enemyKill, float accuracy,
            float levelProgress, float totalSeconds)
        {
            var totalScoreString = totalScore > 0
                ? string.Format(GreenColorStringFormat, totalScore)
                : string.Format(RedColorStringFormat, -totalScore);

            var enemyKillString = enemyKill > 0
                ? string.Format(GreenColorStringFormat, enemyKill)
                : string.Format(RedColorStringFormat, -enemyKill);

            var accuracyString = accuracy > 0
                ? string.Format(GreenColorStringFormat, accuracy)
                : string.Format(RedColorStringFormat, -accuracy);

            var levelProgressString = levelProgress > 0
                ? string.Format(GreenColorStringFormat, (int)levelProgress)
                : string.Format(RedColorStringFormat, (int)-levelProgress);

            var totalSecondsString = totalSeconds > 0
                ? string.Format(GreenColorStringFormat, (int)totalSeconds)
                : string.Format(RedColorStringFormat, (int)-totalSeconds);

            return string.Format(DifferenceStatisticFormat, totalScoreString, enemyKillString, accuracyString,
                levelProgressString, totalSecondsString);
        }

        private static string ConvertToString(float totalSeconds)
        {
            var seconds = (int)totalSeconds % 60;
            var minutes = (int)totalSeconds / 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}