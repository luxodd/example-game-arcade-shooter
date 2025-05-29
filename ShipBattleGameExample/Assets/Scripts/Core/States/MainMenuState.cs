using System;
using Core.Storage;
using Game.Player;
using Game.Settings;
using Game.Statistics;
using Game.UI.Handlers;
using Game.UI.UseCases;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.States
{
    public class MainMenuState : BaseState
    {
        [SerializeField] private WalletService _walletService;
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private LeaderboardService _leaderboardService;
        [SerializeField] private WebSocketService _webSocketService;
        [SerializeField] private StorageService _storageService;
        [SerializeField] private PlayerBehaviour _playerBehaviour;

        [SerializeField] private MainMenuHandler _mainMenuHandler;
        [FormerlySerializedAs("_fetchURlQueryString")] [SerializeField] private FetchUrlQueryString _fetchUrlQueryString;
        [SerializeField] private LoadingScreenHandler _loadingScreenHandler;
        [SerializeField] private LeaderboardWindowHandler _leaderboardWindowHandler;
        [SerializeField] private ReplayLevelWindowHandler _replayLevelWindowHandler;

        [SerializeField] private DefaultGameSettings _defaultGameSettings;

        [SerializeField] private SpendCreditsCase _spendCreditsCase;

        private System.Action _onPlayButtonClickedCallback;
        
        private bool _isItFirstLaunch = true;

        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            _mainMenuHandler.SetOnPlayButtonClickedCallback(OnPlayButtonClickedCallback);
            _mainMenuHandler.SetOnLeaderboardButtonClickedCallback(OnLeaderboardButtonClickedHandler);
            _mainMenuHandler.SetOnExitButtonClickedCallback(OnExitButtonClickedHandler);
            
            _mainMenuHandler.ShowMainMenu();
            _loadingScreenHandler.HideLoadingScreen();
            
            _leaderboardWindowHandler.SetCloseButtonClickCallback(OnLeaderboardCloseButtonClickedHandler);

            _scoreManager.HighScore.AddListener(OnHighScoreValueChangedHandler);
            _mainMenuHandler.SetHighScore(_scoreManager.HighScore.Value);
            
            _replayLevelWindowHandler.SetPlayAgainButtonClick(OnReplayLevelWindowPlayAgainButtonClickedHandler);
            _replayLevelWindowHandler.SetNotNowButtonClick(OnReplayLevelWindowNotNowButtonClickedHandler);
        }

        public override void OnStateExit()
        {
            _scoreManager.HighScore.RemoveListener(OnHighScoreValueChangedHandler);
        }

        public void SetOnPlayButtonClickedCallback(System.Action callback)
        {
            _onPlayButtonClickedCallback = callback;
        }

        private void OnPlayButtonClickedCallback()
        {
            if (IsDemoCompleted())
            {
                _replayLevelWindowHandler.ShowReplayLevelWindow();
                _mainMenuHandler.SetKeyboardNavigatorFocused(false);
            }
            else
            {
                CommonPlayButtonClickProcess();
            }
        }

        private void CommonPlayButtonClickProcess()
        {
            if (_isItFirstLaunch)
            {
                PlayFirstLaunch();
            }
            else
            {
                PlayNotFirstLaunch();
            }
        }

        private void PlayFirstLaunch()
        {
            _isItFirstLaunch = false;
            _onPlayButtonClickedCallback?.Invoke();
        }

        private void PlayNotFirstLaunch()
        {
            _mainMenuHandler.SetKeyboardNavigatorFocused(false);
            
            _spendCreditsCase.SpendCredits(_defaultGameSettings.CreditsForGame, OnEnoughCreditsCallback,
                OnChargeCreditsSuccess, OnChargeCreditsCancel, OnChargeCreditsFailure);
        }

        private void OnEnoughCreditsCallback()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnEnoughCreditsCallback)}] OK");
            _onPlayButtonClickedCallback?.Invoke();
        }

        private void OnChargeCreditsFailure()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeCreditsFailure)}] OK");
            _mainMenuHandler.SetKeyboardNavigatorFocused(true);
        }

        private void OnChargeCreditsCancel()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeCreditsCancel)}] OK");
            _mainMenuHandler.SetKeyboardNavigatorFocused(true);
        }

        private void OnChargeCreditsSuccess()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeCreditsSuccess)}] OK");
            _onPlayButtonClickedCallback?.Invoke();
        }

        private void OnHighScoreValueChangedHandler(int score)
        {
            _mainMenuHandler.SetHighScore(score);
        }

        private void OnLeaderboardButtonClickedHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLeaderboardButtonClickedHandler)}] OK");
            _mainMenuHandler.SetKeyboardNavigatorFocused(false);
            _leaderboardWindowHandler.ShowLeaderboard();
        }

        private void OnLeaderboardCloseButtonClickedHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLeaderboardCloseButtonClickedHandler)}] OK");
            _mainMenuHandler.SetKeyboardNavigatorFocused(true);
            _leaderboardWindowHandler.HideLeaderboard();
        }

        private void OnExitButtonClickedHandler()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(OnExitButtonClickedHandler)}] OK");
#if UNITY_EDITOR
            Application.Quit();
#else
            _webSocketService.BackToSystem();
#endif
        }
        
        private void OnReplayLevelWindowPlayAgainButtonClickedHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnReplayLevelWindowPlayAgainButtonClickedHandler)}] OK");
            _replayLevelWindowHandler.HideReplayLevelWindow();
            
            CommonPlayButtonClickProcess();
        }
        
        private void OnReplayLevelWindowNotNowButtonClickedHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnReplayLevelWindowNotNowButtonClickedHandler)}] OK");
            _replayLevelWindowHandler.HideReplayLevelWindow();
            _mainMenuHandler.SetKeyboardNavigatorFocused(true);
        }

        private bool IsDemoCompleted()
        {
            const int demoLevelId = 1;
            return _playerBehaviour.IsLevelCompleted(demoLevelId);
        }
    }
}
