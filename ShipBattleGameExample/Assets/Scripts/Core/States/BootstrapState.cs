using System;
using Core.Audio;
using Core.Storage;
using Core.UI;
using Game.Player;
using Game.Settings;
using Game.Statistics;
using Game.UI;
using Game.UI.Handlers;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.States
{
    public class BootstrapState : BaseState
    {
        [SerializeField] private UIManager _uiManager;
        [FormerlySerializedAs("_websocketService")] [SerializeField] private WebSocketService _webSocketService;
        [SerializeField] private WalletService _walletService;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private HealthStatusCheckService _healthStatusCheckService;
        [SerializeField] private LeaderboardService _leaderboardService;
        [SerializeField] private StorageService _storageService;
        
        [SerializeField] private WebSocketCommandHandler _websocketCommandHandler;

        [SerializeField] private DefaultGameSettings _defaultGameSettings;
        [SerializeField] private PlayerBehaviour _playerBehaviour;

        [Header("UI Handlers")]
        [SerializeField] private LoadingScreenHandler _loadingScreenHandler;
        [SerializeField] private MainMenuHandler _mainMenuHandler;
        [SerializeField] private GameScreenHandler _gameScreenHandler;
        [SerializeField] private ReconnectionWindowHandler _reconnectionWindowHandler;
        [SerializeField] private ContinueGameWindowHandler _continueGameWindowHandler;
        [SerializeField] private LevelCompleteWindowHandler _levelCompleteWindowHandler;
        [SerializeField] private GameOverWindowHandler _gameOverWindowHandler;
        [SerializeField] private NotEnoughMoneyWindowHandler _notEnoughMoneyWindowHandler;
        [SerializeField] private CreditsWidgetHandler _creditsWidgetHandler;
        [SerializeField] private LeaderboardWindowHandler _leaderboardWindowHandler;
        [SerializeField] private PinCodeEnteringPopupHandler _pinCodeEnteringPopupHandler;
        [SerializeField] private NumericKeyboardPopupHandler _numericKeyboardPopupHandler;
        [SerializeField] private CompletedDemoWindowHandler _completedDemoWindowHandler;
        [SerializeField] private ReplayLevelWindowHandler _replayLevelWindowHandler;
        
        [Header("For Debugging")]
        [SerializeField] private bool _needToConnectToServer = true;

        public override void OnStateEnter()
        {
            _storageService.Register(_playerBehaviour);
            
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            if (_needToConnectToServer)
            {
                _webSocketService.ConnectToServer(OnConnectedToServerSuccessHandler,
                    () => OnConnectedToServerFailureHandler(-1,null));
            }

            var loadingScreenView = _uiManager.ProvideView<ILoadingScreenView>(ViewType.LoadingScreen);
            _loadingScreenHandler.PrepareView(loadingScreenView);

            var mainMenuView = _uiManager.ProvideView<IMainMenuView>(ViewType.MainMenu);
            _mainMenuHandler.PrepareView(mainMenuView);
            _mainMenuHandler.ProvideDependencies(_playerBehaviour);

            var gameScreenView = _uiManager.ProvideView<IGameScreenView>(ViewType.GameScreen);
            _gameScreenHandler.PrepareGameScreen(gameScreenView);

            var reconnectionWindowView = _uiManager.ProvideView<IReconnectionWindowView>(ViewType.ReconnectionWindow);
            _reconnectionWindowHandler.PrepareView(reconnectionWindowView);

            var continueGameWindowView = _uiManager.ProvideView<IContinueGameWindowView>(ViewType.ContinueGameWindow);
            _continueGameWindowHandler.PrepareContinueGameWindow(continueGameWindowView);

            var gameOverWindowView = _uiManager.ProvideView<IGameOverWindowView>(ViewType.GameOverWindow);
            _gameOverWindowHandler.PrepareView(gameOverWindowView);
            
            var levelCompleteWindowView = _uiManager.ProvideView<ILevelCompleteWindowView>(ViewType.LevelCompleteWindow);
            _levelCompleteWindowHandler.PrepareLevelCompleteWindow(levelCompleteWindowView);

            var notEnoughMoneyWindowView =
                _uiManager.ProvideView<INotEnoughMoneyWindowView>(ViewType.NotEnoughMoneyWindow);
            _notEnoughMoneyWindowHandler.PrepareNotEnoughMoneyWindow(notEnoughMoneyWindowView);

            var creditsWidgetView = _uiManager.ProvideView<ICreditsWidgetView>(ViewType.CreditsWidget);
            _creditsWidgetHandler.PrepareView(creditsWidgetView);
            
            var leaderboardWindowView = _uiManager.ProvideView<ILeaderboardWindowView>(ViewType.LeaderboardWindow);
            _leaderboardWindowHandler.PrepareView(leaderboardWindowView);
            
            var pinCodeEnteringPopupView = _uiManager.ProvideView<IPinCodeEnteringPopupView>(ViewType.PinCodeEnteringPopup);
            _pinCodeEnteringPopupHandler.PrepareView(pinCodeEnteringPopupView);

            var numericKeyboardPopupView =
                _uiManager.ProvideView<INumericKeyboardPopupView>(ViewType.NumericKeyboardPopup);
            _numericKeyboardPopupHandler.PrepareView(numericKeyboardPopupView);
            _numericKeyboardPopupHandler.PrepareInputField(_pinCodeEnteringPopupHandler.PinCodeInputField);

            _walletService.Credits.AddListener(_mainMenuHandler.OnCreditsCountChangedHandler);
            
            var completedDemoWindowView = _uiManager.ProvideView<ICompletedDemoWindowView>(ViewType.CompletedDemoWindow);
            _completedDemoWindowHandler.PrepareView(completedDemoWindowView);

            var replayLevelWindowView = _uiManager.ProvideView<IReplayLevelWindowView>(ViewType.ReplayDemoWindow);
            _replayLevelWindowHandler.PrepareView(replayLevelWindowView);

            SetupDefaultGameSettings();
        }

        public override void OnStateExit()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateExit)}] OK");
        }

        private void SetupDefaultGameSettings()
        {
            _audioManager.SetupMusicDefaultVolume(_defaultGameSettings.MusicVolume);
            _audioManager.SetupSfxDefaultVolume(_defaultGameSettings.SfxVolume);
            _walletService.SetCredits(_defaultGameSettings.CreditsCount);
        }

        private void OnConnectedToServerSuccessHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnConnectedToServerSuccessHandler)}] OK");
            
            _websocketCommandHandler.SendProfileRequestCommand(OnProfileRequestSuccessHandler, OnProfileRequestFailureHandler);
            _websocketCommandHandler.SendUserBalanceRequestCommand(OnUserBalanceRequestSuccessHandler, OnUserBalanceRequestFailureHandler);
            
            _leaderboardService.PullLastLeaderboardData(
                ()=> _leaderboardWindowHandler.PrepareLeaderboardEntries(_leaderboardService.LeaderboardData),
                null);
            CoroutineManager.NextFrameAction(1, ()=>_storageService.Load());
            _healthStatusCheckService.Activate();
        }

        [ContextMenu("Test Send Balance Request")]
        private void TestSendRequestForBalance()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestSendRequestForBalance)}] OK");
            _websocketCommandHandler.SendUserBalanceRequestCommand(OnUserBalanceRequestSuccessHandler, OnUserBalanceRequestFailureHandler);
        }

        private void OnConnectedToServerFailureHandler(int statusCode, string errorMessage)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnConnectedToServerFailureHandler)}] OK, status: {statusCode} error: {errorMessage}");
        }

        private void OnProfileRequestSuccessHandler(string profileName)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnProfileRequestSuccessHandler)}] OK, profileName: {profileName}");
            _playerBehaviour.SetPlayerName(profileName);
        }

        private void OnProfileRequestFailureHandler(int statusCode, string errorMessage)
        {
            LoggerHelper.LogError($"[{DateTime.Now}][{GetType().Name}][{nameof(OnProfileRequestFailureHandler)}] OK, status: {statusCode} Error: {errorMessage}");
        }

        private void OnUserBalanceRequestSuccessHandler(int balance)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnUserBalanceRequestSuccessHandler)}] OK, balance: {balance}");
            _walletService.SetCredits(balance);
        }

        private void OnUserBalanceRequestFailureHandler(int statusCode, string errorMessage)
        {
            LoggerHelper.LogError($"[{DateTime.Now}][{GetType().Name}][{nameof(OnUserBalanceRequestFailureHandler)}] OK, status:{statusCode} Error: {errorMessage}");
        }

        private void OnApplicationQuit()
        {
            _healthStatusCheckService?.Deactivate();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _healthStatusCheckService?.Deactivate();
            }
        }
    }
}
