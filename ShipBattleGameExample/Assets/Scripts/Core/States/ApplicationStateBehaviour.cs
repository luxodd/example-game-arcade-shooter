using System;
using System.Collections;
using Game.UI.Handlers;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using UnityEngine;

namespace Core.States
{
    public enum ApplicationState
    {
        Bootstrap,
        LaunchLoading,
        MainMenu,
        PrepareGame,
        Gameplay,
        GameOver,
        RestartGame,
        BackToMainMenu,
        Leaderboard,
    }

    public class ApplicationStateBehaviour : MonoBehaviour
    {
        [Header("States")] [SerializeField] private BootstrapState _bootstrapState;
        [SerializeField] private MainMenuState _mainMenuState;
        [SerializeField] private LaunchLoadingState _launchLoadingState;
        [SerializeField] private PrepareGameState _prepareGameState;
        [SerializeField] private GameplayState _gameplayState;
        [SerializeField] private GameOverState _gameOverState;
        [SerializeField] private BackToMainMenuState _backToMainMenuState;
        [SerializeField] private RestartGameState _restartGameState;

        [Header("Other")] [SerializeField] private ApplicationState _startState = ApplicationState.Bootstrap;
        [SerializeField] private ReconnectionWindowHandler _reconnectionWindowHandler;
        [SerializeField] private ReconnectService _reconnectService;
        [SerializeField] private WebSocketCommandHandler _websocketCommandHandler;

        private ApplicationState _currentState = ApplicationState.Bootstrap;
        private IState _currentStateBehaviour;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f);
            LoggerHelper.Log($"[{GetType().Name}][{nameof(Start)}] OK");
            SwitchState(_startState, true);
        }

        private void UpdateState()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(UpdateState)}] OK, {_currentState}");
            switch (_currentState)
            {
                case ApplicationState.Bootstrap:
                    OnBootstrap();
                    break;
                case ApplicationState.LaunchLoading:
                    OnLaunchLoading();
                    break;
                case ApplicationState.MainMenu:
                    OnMainMenu();
                    break;
                case ApplicationState.PrepareGame:
                    OnPrepareGame();
                    break;
                case ApplicationState.Gameplay:
                    OnGameplay();
                    break;
                case ApplicationState.GameOver:
                    OnGameOver();
                    break;
                case ApplicationState.BackToMainMenu:
                    OnBackToMainMenu();
                    break;
                case ApplicationState.Leaderboard:
                    OnLeaderboard();
                    break;
                case ApplicationState.RestartGame:
                    OnRestartGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SwitchState(ApplicationState newState, bool force = false)
        {
            LoggerHelper.Log(
                $"[{GetType().Name}][{nameof(SwitchState)}] OK, was: {_currentState}, newState: {newState} force: {force}");
            if (_currentState == newState && force == false)
            {
                return;
            }

            _currentStateBehaviour?.OnStateExit();
            _currentState = newState;

            UpdateState();
        }

        private void OnBootstrap()
        {
            _currentStateBehaviour = _bootstrapState;

            _mainMenuState.SetOnPlayButtonClickedCallback(() => SwitchState(ApplicationState.PrepareGame));
            _bootstrapState.OnStateEnter();

            _reconnectService.ReconnectStateChangedEvent.AddListener(OnReconnectionServiceStatusChanged);
            _websocketCommandHandler.OnCommandProcessStateChangeEvent.AddListener(OnCommandProcessStateChange);

            SwitchState(ApplicationState.LaunchLoading);
        }

        private void OnLaunchLoading()
        {
            _currentStateBehaviour = _launchLoadingState;

            _launchLoadingState.SetOnLoadingComplete(() => SwitchState(ApplicationState.MainMenu));
            _launchLoadingState.OnStateEnter();
        }

        private void OnMainMenu()
        {
            _currentStateBehaviour = _mainMenuState;

            _mainMenuState.OnStateEnter();
        }

        private void OnPrepareGame()
        {
            _currentStateBehaviour = _prepareGameState;

            _prepareGameState.SetOnLoadingComplete(() => SwitchState(ApplicationState.Gameplay));
            _prepareGameState.OnStateEnter();
        }

        private void OnGameplay()
        {
            _currentStateBehaviour = _gameplayState;

            _gameplayState.SetOnCompleteAction(StateCompleteHandler);
            _gameplayState.OnStateEnter();
        }

        private void OnGameOver()
        {
            _currentStateBehaviour = _gameOverState;

            _gameOverState.OnStateEnter();
        }

        private void OnBackToMainMenu()
        {
            _currentStateBehaviour = _backToMainMenuState;

            _backToMainMenuState.SetOnCompleteAction(StateCompleteHandler);
            _backToMainMenuState.OnStateEnter();
        }

        private void OnRestartGame()
        {
            _currentStateBehaviour = _restartGameState;

            _restartGameState.SetOnCompleteAction(StateCompleteHandler);
            _restartGameState.OnStateEnter();
        }

        private void OnLeaderboard()
        {

        }

        private void StateCompleteHandler(ApplicationState nextState)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(StateCompleteHandler)}] OK, nextState: {nextState}");
            SwitchState(nextState);
        }

        private void OnReconnectionServiceStatusChanged(ReconnectionState reconnectionState)
        {
            switch (reconnectionState)
            {
                case ReconnectionState.Connecting:
                    _reconnectionWindowHandler.ShowReconnectionWindow();
                    _reconnectionWindowHandler.SwitchToReconnection();
                    break;
                case ReconnectionState.Connected:
                    _reconnectionWindowHandler.HideReconnectionWindow();
                    break;
                case ReconnectionState.ConnectingFailed:
                    _reconnectionWindowHandler.SwitchToReconnectionFailed();
                    break;
            }
        }

        private void OnCommandProcessStateChange(CommandProcessState commandProcessState)
        {
            switch (commandProcessState)
            {
                case CommandProcessState.None:
                    break;
                case CommandProcessState.Sent:
                    _reconnectionWindowHandler.ShowReconnectionWindow();
                    _reconnectionWindowHandler.SwitchToReconnection();
                    break;
                case CommandProcessState.Received:
                    _reconnectionWindowHandler.HideReconnectionWindow();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(commandProcessState), commandProcessState, null);
            }
        }
    }
}
