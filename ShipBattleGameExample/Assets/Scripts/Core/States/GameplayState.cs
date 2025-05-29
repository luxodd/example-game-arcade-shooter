using System;
using Core.Audio;
using Core.States.StateHelpers;
using Core.Storage;
using Game.CameraInner;
using Game.Events;
using Game.Level;
using Game.Player;
using Game.PlayerShip;
using Game.Settings;
using Game.Statistics;
using Game.UI.Handlers;
using Game.UI.UseCases;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using UnityEngine;

namespace Core.States
{
    public class GameplayState : BaseState
    {
        [Header("Services")]
        [SerializeField] private WalletService _walletService;
        [SerializeField] private LeaderboardService _leaderboardService;
        [SerializeField] private WebSocketCommandHandler _webSocketCommandHandler;
        [SerializeField] private WebSocketService _webSocketService;
        [SerializeField] private StorageService _storageService;

        [Header("UI Handlers")] [SerializeField]
        private LoadingScreenHandler _loadingScreenHandler;

        [SerializeField] private MainMenuHandler _mainMenuHandler;
        [SerializeField] private GameScreenHandler _gameScreenHandler;
        [SerializeField] private GameOverWindowHandler _gameOverWindowHandler;
        [SerializeField] private ContinueGameWindowHandler _continueGameWindowHandler;
        [SerializeField] private LevelCompleteWindowHandler _levelCompleteWindowHandler;
        [SerializeField] private NotEnoughMoneyWindowHandler _notEnoughMoneyWindowHandler;
        [SerializeField] private CreditsWidgetHandler _creditsWidgetHandler;
        [SerializeField] private CompletedDemoWindowHandler _completedDemoWindowHandler;

        [Header("Data")] [SerializeField] private DefaultGameSettings _defaultGameSettings;

        [Header("Gameplay")] [SerializeField] private LevelBehaviour _levelBehaviour;
        [SerializeField] private LevelBossGameplayHelper _levelBossGameplayHelper;
        [SerializeField] private BossPartGameplayStateHelper _bossPartGameplayStateHelper;

        [Header("Use cases")] [SerializeField] private SpendCreditsCase _spendCreditsCase;

        [Header("Others")] [SerializeField] private PlayerBehaviour _playerBehaviour;

        [SerializeField] private KeyboardControlAdapter _keyboardControlAdapter;
        [SerializeField] private CameraFollowBehaviour _cameraFollowBehaviour;

        [SerializeField] private ProjectileResourceProvider _projectileResourceProvider;
        [SerializeField] private PlayerShipWeaponDataBase _playerShipWeaponDataBase;

        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private PlayerStatisticTrackingController _playerStatisticTrackingController;

        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");

            _gameScreenHandler.HideBossHealthBar();

            var primaryWeaponService = new PlayerWeaponService(_playerShipWeaponDataBase.DefaultPrimaryWeapon,
                _projectileResourceProvider, _levelBehaviour.ParentForProjectiles);

            _levelBehaviour.ActivePlayer.ProvideDependencies(_keyboardControlAdapter, primaryWeaponService,
                _levelBehaviour.ActiveLevel.LevelSpeed);

            _cameraFollowBehaviour.SetLevelSpeed(_levelBehaviour.ActiveLevel.LevelSpeed);

            var spawnedBehaviour = _levelBehaviour.ActiveLevel.SpawnBehaviour;

            spawnedBehaviour.SetTarget(_levelBehaviour.ActivePlayer.transform);

            spawnedBehaviour.Difficulty.AddListener(_gameScreenHandler.OnDifficultyValueChanged);
            spawnedBehaviour.EnemiesCount.AddListener((count) =>
                _gameScreenHandler.OnEnemiesCountChanged(count, spawnedBehaviour.MaxEnemies));
            spawnedBehaviour.SpawnCooldown.AddListener(_gameScreenHandler.OnCooldownValueChanged);

            _keyboardControlAdapter.InTheGame();

            _levelBehaviour.ActivePlayer.SetMovementBounds(_levelBehaviour.ActiveLevel.LeftBounds,
                _levelBehaviour.ActiveLevel.RightBounds, _cameraFollowBehaviour);

            _cameraFollowBehaviour.enabled = true;

            _cameraFollowBehaviour.SetStartPosition(_levelBehaviour.ActiveLevel.CameraStartPoint.position);
            _levelBehaviour.ActivePlayer.SetStartPosition(_levelBehaviour.ActiveLevel.CameraStartPoint.position);

            _cameraFollowBehaviour.SetMovementBounds(_levelBehaviour.ActiveLevel.LeftBounds,
                _levelBehaviour.ActiveLevel.RightBounds);

            _cameraFollowBehaviour.SetTarget(_levelBehaviour.ActivePlayer.transform);

            _gameScreenHandler.SetPlayerMaximumHealthPoints(_levelBehaviour.ActivePlayer.MaxPlayerHealthPoints);
            _gameScreenHandler.SetPlayerHealthPoints(_levelBehaviour.ActivePlayer.MaxPlayerHealthPoints);

            _gameScreenHandler.ShowGameScreen();

            _loadingScreenHandler.HideLoadingScreen();

            _levelBehaviour.ActivePlayer.PlayerShipLivesChanged.AddListener(OnPlayerLivesChanged);
            _levelBehaviour.ActivePlayer.PlayerHealthPoints.AddListener(OnPlayerHealthPointsChanged);


            _playerStatisticTrackingController.StartTracking();

            _levelBehaviour.ActiveLevel.DominatorBehaviour.SetEnemyShipProvider(_levelBehaviour.EnemyShipProvider);

            _bossPartGameplayStateHelper.Activate();

            SubscribeToEvents();
            _scoreManager.ClearScore();

            EventAggregator.Post(this, new LevelPreparedEvent()
            {
                StartPoint = _levelBehaviour.ActiveLevel.CameraStartPoint,
                EndPoint = _levelBehaviour.ActiveLevel.LevelEndPoint
            });

            _webSocketCommandHandler.SendLevelBeginRequestCommand(_playerBehaviour.CurrentLevel,
                OnLevelBeginRequestSuccessHandler, OnLevelBeginRequestFailureHandler);

            ActivateGameplayProcess();
        }

        public override void OnStateExit()
        {
            UnsubscribeFromEvents();
            _bossPartGameplayStateHelper.Deactivate();
        }

        private void SubscribeToEvents()
        {
            EventAggregator.Subscribe<LevelCompletionEvent>(OnLevelCompletedHandler);
            EventAggregator.Subscribe<LevelMapEvents>(OnLevelMapEventHandler);

            EventAggregator.Subscribe<EnemyDeathEvent>(OnEnemyDeathHandler);
            EventAggregator.Subscribe<EnemyHitEvent>(OnEnemyHitHandler);

            _scoreManager.Score.AddListener(OnScoreChangedHandler);
        }

        private void UnsubscribeFromEvents()
        {
            EventAggregator.Unsubscribe<LevelCompletionEvent>(OnLevelCompletedHandler);
            EventAggregator.Unsubscribe<LevelMapEvents>(OnLevelMapEventHandler);

            EventAggregator.Unsubscribe<EnemyDeathEvent>(OnEnemyDeathHandler);
            EventAggregator.Unsubscribe<EnemyHitEvent>(OnEnemyHitHandler);

            _scoreManager.Score.RemoveListener(OnScoreChangedHandler);

            _levelBehaviour.ActivePlayer?.PlayerShipLivesChanged.RemoveListener(OnPlayerLivesChanged);
            _levelBehaviour.ActivePlayer?.PlayerHealthPoints.RemoveListener(OnPlayerHealthPointsChanged);
        }

        private void ActivateGameplayProcess()
        {
            _cameraFollowBehaviour.InTheGame();
            _levelBehaviour.ActivePlayer.Activate();
        }

        private void ContinueGameplayProcess()
        {
            _cameraFollowBehaviour.InTheGame();
        }

        private void DeactivateGameplayProcess()
        {
            _cameraFollowBehaviour.OutTheGame();
            _levelBehaviour.ActiveLevel.DeactivateGameplayProcess();
            _levelBehaviour.ActivePlayer.Deactivate();
        }

        private void ActivateEnemies()
        {
            _levelBehaviour.ActiveLevel.ActivateLevel();
        }

        private void DeactivateEnemies()
        {
            _levelBehaviour.ActiveLevel.DeactivateEnemies();
        }

        private int _previousPlayerLives;

        private void OnPlayerLivesChanged(int lives)
        {
            _gameScreenHandler.SetPlayerLives(lives);
            if (lives <= _previousPlayerLives)
            {
                _cameraFollowBehaviour.BigShake();
            }

            _previousPlayerLives = lives;

            if (lives < 0)
            {
                //if player lives < 0 then display continue game for credits
                //if no, then display the game over a window 
                DeactivateGameplayProcess();

                PrepareAndShowContinueGameWindow();
            }
        }

        private int _previousPlayerHealthPoints;

        private void OnPlayerHealthPointsChanged(int healthPoints)
        {
            _gameScreenHandler.SetPlayerHealthPoints(healthPoints);
            if (healthPoints <= _previousPlayerHealthPoints)
            {
                _cameraFollowBehaviour.SmallShake();
            }
            else
            {
                _gameScreenHandler.RecoveryHealthPoints(healthPoints);
            }

            _previousPlayerHealthPoints = healthPoints;
        }

        #region UI Handlers

        private void PrepareAndShowContinueGameWindow()
        {
            _continueGameWindowHandler.SetCancelButtonClickCallback(OnContinueGameWindowCancelButtonClickHandler);
            _continueGameWindowHandler.SetContinueButtonClickCallback(OnContinueGameWindowContinueButtonClickHandler);
            _continueGameWindowHandler.SetCreditsCount(_defaultGameSettings.CreditsForContinueGame);

            _creditsWidgetHandler.SetCreditsCount(_walletService.Credits.Value);
            _creditsWidgetHandler.ShowCreditsWidget();
            _continueGameWindowHandler.ShowContinueGameWindow();
        }

        private void OnContinueGameWindowCancelButtonClickHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnContinueGameWindowCancelButtonClickHandler)}] OK");
            _continueGameWindowHandler.HideContinueGameWindow();

            PrepareAndShowGameOverWindow();
        }

        private void OnContinueGameWindowContinueButtonClickHandler()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnContinueGameWindowContinueButtonClickHandler)}] OK");
            _continueGameWindowHandler.HideContinueGameWindow();

            _spendCreditsCase.SpendCredits(_defaultGameSettings.CreditsForContinueGame,
                OnContinueGameChargeSuccessHandler,
                OnContinueGameChargeSuccessHandler,
                OnContinueGameChargeCancelHandler,
                OnContinueGameChargeFailureHandler);
        }

        private void OnContinueGameChargeSuccessHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnContinueGameChargeSuccessHandler)}] OK");
            ActivateGameplayProcess();
            ActivateEnemies();
        }

        private void OnContinueGameChargeFailureHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnContinueGameChargeFailureHandler)}] OK");
        }

        private void OnContinueGameChargeCancelHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnContinueGameChargeCancelHandler)}] OK");
            _continueGameWindowHandler.ShowContinueGameWindow();
        }

        private void PrepareAndShowGameOverWindow()
        {
            _gameOverWindowHandler.SetYesButtonCallback(OnGameOverWindowYesButtonClickHandler);
            _gameOverWindowHandler.SetNoButtonCallback(OnGameOverWindowNoButtonClickHandler);

            _gameOverWindowHandler.SetCreditsCount(_defaultGameSettings.CreditsForGame);

            _gameOverWindowHandler.SetLevelNumber(_playerBehaviour.CurrentLevel);

            //prepare statistics
            EventAggregator.Post(this,
                new GameOverEvent() { CurrentLevelPosition = _cameraFollowBehaviour.CurrentPosition });
            _scoreManager.RecordResult();
            _playerStatisticTrackingController.StopTracking();

            var levelStatisticData = LevelStatisticData.Create(_scoreManager.Score.Value,
                _playerStatisticTrackingController.EnemiesKilled,
                _playerStatisticTrackingController.Accuracy,
                _playerStatisticTrackingController.LevelProgress,
                _playerStatisticTrackingController.TotalSeconds, DateTime.Now);

            _gameOverWindowHandler.SetGameResultData(levelStatisticData.TotalScore, levelStatisticData.EnemiesKilled,
                levelStatisticData.Accuracy, levelStatisticData.LevelProgress, levelStatisticData.TimeInSeconds);

            var differenceResult = _leaderboardService.CompareLevelStatisticData(levelStatisticData);

            if (differenceResult != null)
            {
                var phrase = differenceResult.Item2;
                var differenceData = differenceResult.Item1;
                _gameOverWindowHandler.SetDifferenceData(differenceData.TotalScore, differenceData.EnemiesKilled,
                    differenceData.Accuracy, differenceData.LevelProgress, differenceData.TimeInSeconds);
                _gameOverWindowHandler.SetMotivatedPhrase(phrase);
            }

            _leaderboardService.LogPlayerLevelResult(levelStatisticData);

            _webSocketCommandHandler.SendLevelEndRequestCommand(_playerBehaviour.CurrentLevel,
                levelStatisticData.TotalScore, OnLevelEndRequestSuccessHandler, OnLevelEndRequestFailureHandler);

            _gameOverWindowHandler.ShowGameOverWindow();
        }

        private void OnGameOverWindowYesButtonClickHandler()
        {
            _gameOverWindowHandler.SetKeyboardNavigatorFocused(false);
            _spendCreditsCase.SpendCredits(_defaultGameSettings.CreditsForGame, RestartGame,
                OnGameOverWindowCreditsChargeSuccessHandler,
                OnGameOverWindowCreditsChargeCancelHandler,
                OnGameOverWindowCreditsChargeFailureHandler);
        }

        private void OnGameOverWindowCreditsChargeSuccessHandler()
        {
            RestartGame();
        }

        private void OnGameOverWindowCreditsChargeCancelHandler()
        {
            _gameOverWindowHandler.SetKeyboardNavigatorFocused(true);
        }

        private void OnGameOverWindowCreditsChargeFailureHandler()
        {
            //is empty now, but later will add necessary behaviour
        }

        private void OnGameOverWindowNoButtonClickHandler()
        {
            _gameOverWindowHandler.HideGameOverWindow();
            _gameScreenHandler.HideGameScreen();
            
#if !UNITY_EDITOR
            _webSocketService.BackToSystem();
#else
            CompleteState(ApplicationState.BackToMainMenu);
#endif
        }

        private void RestartGame()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(RestartGame)}] OK");
            _gameOverWindowHandler.HideGameOverWindow();
            _gameScreenHandler.HideGameScreen();
            CompleteState(ApplicationState.RestartGame);
        }

        private void OnLevelCompletedHandler(object sender, LevelCompletionEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelCompletedHandler)}] OK");
            AudioManager.Instance.PlayMusic(MusicType.Boss1OutroLoop);

            DeactivateGameplayProcess();

            _playerStatisticTrackingController.StopTracking();

            _levelCompleteWindowHandler.SetLevelNumber(_playerBehaviour.CurrentLevel);

            var levelStatisticData = LevelStatisticData.CreateRandom();

            _levelCompleteWindowHandler.SetNextButtonClickedHandler(OnLevelCompleteNextLevelButtonClickHandler);

            _levelCompleteWindowHandler.SetGameResultData(_scoreManager.Score.Value,
                _playerStatisticTrackingController.EnemiesKilled,
                _playerStatisticTrackingController.Accuracy,
                _playerStatisticTrackingController.LevelProgress,
                _playerStatisticTrackingController.TotalSeconds);

            _webSocketCommandHandler.SendLevelEndRequestCommand(_playerBehaviour.CurrentLevel,
                levelStatisticData.TotalScore, OnLevelEndRequestSuccessHandler, OnLevelEndRequestFailureHandler);
            
            _levelCompleteWindowHandler.ShowLevelCompleteWindow();

            _playerBehaviour.CompleteLevel();
            _storageService.Save();
        }

        private void OnLevelCompleteNextLevelButtonClickHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelCompleteNextLevelButtonClickHandler)}] OK");
            _levelCompleteWindowHandler.HideLevelCompleteWindow();
            
#if !UNITY_EDITOR
            _completedDemoWindowHandler.SetOkayButtonClickCallback(OnCompletedDemoWindowOkayButtonClickHandler);
            _completedDemoWindowHandler.ShowCompletedDemoWindow();
#else
            _completedDemoWindowHandler.SetOkayButtonClickCallback(OnCompletedDemoWindowOkayButtonClickHandler);
            _completedDemoWindowHandler.ShowCompletedDemoWindow();
#endif
        }

        private void OnCompletedDemoWindowOkayButtonClickHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnCompletedDemoWindowOkayButtonClickHandler)}] OK");
            _completedDemoWindowHandler.HideCompletedDemoWindow();
#if !UNITY_EDITOR
            _webSocketService.BackToSystem();
#else
            CompleteState(ApplicationState.BackToMainMenu);
#endif
        }

        private void OnLevelMapEventHandler(object sender, LevelMapEvents eventArgs)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelMapEventHandler)}] OK, eventType: {eventArgs.EventType}");
            if (eventArgs.EventType == LevelMapEventType.ActivateEnemies)
            {
                ActivateEnemies();
            }
            else if (eventArgs.EventType == LevelMapEventType.DeactivateEnemies)
            {
                DeactivateEnemies();
            }
            else if (eventArgs.EventType == LevelMapEventType.CameraStop)
            {
                StopLevelMovement();
            }
        }

        private void StopLevelMovement()
        {
            _cameraFollowBehaviour.SetLevelSpeed(0f);
            _levelBehaviour.ActivePlayer.SetLevelSpeed(0f);
        }
        #endregion

        private void OnEnemyDeathHandler(object sender, EnemyDeathEvent eventArgs)
        {
            _scoreManager.AddScore(eventArgs.Score);
            _cameraFollowBehaviour.ExtraSmallShake();
        }

        private void OnEnemyHitHandler(object sender, EnemyHitEvent eventArgs)
        {
            _scoreManager.AddScore(eventArgs.Score);
        }

        private void OnScoreChangedHandler(int score)
        {
            _gameScreenHandler.SetPlayerScore(score);
        }
        
        private void OnLevelBeginRequestSuccessHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelBeginRequestSuccessHandler)}] OK");
        }

        private void OnLevelBeginRequestFailureHandler(int statusCode, string errorMessage)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelBeginRequestFailureHandler)}] OK, code:{statusCode} error: {errorMessage}");
        }

        private void OnLevelEndRequestSuccessHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelEndRequestSuccessHandler)}] OK");
        }

        private void OnLevelEndRequestFailureHandler(int statusCode, string errorMessage)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelEndRequestFailureHandler)}] OK, code:{statusCode} error: {errorMessage}");
        }
    }
}