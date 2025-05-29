using System;
using Core.UI;
using DG.Tweening;
using Game.UI.Items.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public interface IMainMenuView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetOnPlayButtonClickedCallback(System.Action callback);
        void SetOnLeaderboardButtonClickedCallback(System.Action callback);
        void SetOnExitButtonClickedCallback(System.Action callback);
        void SetHighScore(int points);
        void SetPlayerName(string playerName);
        void SetPlayerScore(int score);

        void SetResponseText(string text);

        void SetBuildVersion(string version);
        void SetCredits(int credits);
    }

    public class MainMenuView : BaseView, IMainMenuView
    {
        public override ViewType ViewType => ViewType.MainMenu;
        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _playerScoreText;
        [SerializeField] private TMP_Text _highScoreText;
        [SerializeField] private TMP_Text _responseText;
        [SerializeField] private TMP_Text _buildVersionText;
        [SerializeField] private TMP_Text _creditsCountText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _exitButton;

        [SerializeField] private RectTransform _enemyShip;
        [SerializeField] private float _duration;
        [SerializeField] private Ease _ease = Ease.InOutBounce;

        [SerializeField] private Vector2 _borders;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;

        private string _playerNameFormatted;

        private string _buildVersionFormatted;

        private Tweener _tweener;

        private System.Action _onPlayButtonClicked = null;
        private System.Action _onLeaderboardButtonClicked = null;
        private System.Action _onExitButtonClicked = null;

        public void SetOnPlayButtonClickedCallback(Action callback)
        {
            _onPlayButtonClicked = callback;
        }

        public void SetOnLeaderboardButtonClickedCallback(Action callback)
        {
            _onLeaderboardButtonClicked = callback;
        }

        public void SetOnExitButtonClickedCallback(Action callback)
        {
            _onExitButtonClicked = callback;
        }

        public void SetHighScore(int points)
        {
            _highScoreText.text = points.ToString();
        }

        public void SetPlayerName(string playerName)
        {
            _playerNameText.text = string.Format(_playerNameFormatted, playerName);
        }

        public void SetPlayerScore(int score)
        {
            _playerScoreText.text = score.ToString();
        }

        public void SetResponseText(string text)
        {
            _responseText.text = text;
        }

        public void SetBuildVersion(string version)
        {
            _buildVersionText.text = string.Format(_buildVersionFormatted, version);
        }

        public void SetCredits(int credits)
        {
            _creditsCountText.text = credits.ToString();
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _playerNameFormatted = _playerNameText.text;
            _buildVersionFormatted = _buildVersionText.text;

            _playButton.onClick.AddListener(OnPlayButtonClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        protected override void OnHide()
        {
            base.OnHide();
            _tweener.Kill();
            _keyboardNavigator.Deactivate();
        }

        protected override void OnShow()
        {
            base.OnShow();
            StartEnemyMove();
            _keyboardNavigator.Activate();
        }

        private void OnPlayButtonClicked()
        {
            _onPlayButtonClicked?.Invoke();
        }

        private void OnLeaderboardButtonClicked()
        {
            _onLeaderboardButtonClicked?.Invoke();
        }

        private void OnExitButtonClicked()
        {
            _onExitButtonClicked?.Invoke();
        }

        private void StartEnemyMove()
        {
            MoveToLeftBorder();
        }

        private void MoveToLeftBorder()
        {
            _tweener = _enemyShip.DOAnchorPos(new Vector2(-_borders.x, _enemyShip.anchoredPosition.y), _duration)
                .SetEase(_ease)
                .OnComplete(OnMoveToLeftBorderCompleted);
        }

        private void OnMoveToLeftBorderCompleted()
        {
            MoveToRightBorder();
        }

        private void MoveToRightBorder()
        {
            _tweener = _enemyShip.DOAnchorPos(new Vector2(_borders.x, _enemyShip.anchoredPosition.y), _duration)
                .SetEase(_ease)
                .OnComplete(OnMoveToRightBorderCompleted);
        }

        private void OnMoveToRightBorderCompleted()
        {
            MoveToLeftBorder();
        }
    }
}