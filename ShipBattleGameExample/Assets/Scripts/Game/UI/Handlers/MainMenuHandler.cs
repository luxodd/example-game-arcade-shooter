using System;
using Game.Player;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class MainMenuHandler : MonoBehaviour
    {
        private const string PlayButtonKey = "Play";
        private const string LeaderboardButtonKey = "Leaderboard";
        private const string ExitButtonKey = "Exit";

        private IMainMenuView _mainMenuView;

        private Action _onPlayButtonClickedCallback;
        private Action _onLeaderboardButtonClickedCallback;
        private Action _onExitButtonClickedCallback;

        private int _counter = 0;

        public void PrepareView(IMainMenuView mainMenuView)
        {
            _mainMenuView = mainMenuView;

            _mainMenuView.SetBuildVersion(Application.version);
            _mainMenuView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ProvideDependencies(PlayerBehaviour player)
        {
            player.PlayerName.AddListener(SetPlayerName);
        }

        public void SetPlayerName(string playerName)
        {
            _mainMenuView.SetPlayerName(playerName);
        }

        public void SetResponseText(string responseText)
        {
            _mainMenuView.SetResponseText(responseText);
        }

        public void SetOnPlayButtonClickedCallback(System.Action onPlayButtonClickedCallback)
        {
            _onPlayButtonClickedCallback = onPlayButtonClickedCallback;
            _mainMenuView.SetOnPlayButtonClickedCallback(OnPlayButtonClicked);
        }

        public void SetOnLeaderboardButtonClickedCallback(System.Action onLeaderboardButtonClickedCallback)
        {
            _onLeaderboardButtonClickedCallback = onLeaderboardButtonClickedCallback;
            _mainMenuView.SetOnLeaderboardButtonClickedCallback(OnLeaderboardButtonClicked);
        }

        public void SetOnExitButtonClickedCallback(System.Action onExitButtonClickedCallback)
        {
            _onExitButtonClickedCallback = onExitButtonClickedCallback;
            _mainMenuView.SetOnExitButtonClickedCallback(OnExitButtonClicked);
        }

        public void ShowMainMenu()
        {
            _mainMenuView.Show();
            _counter = 0;
        }

        public void HideMainMenu()
        {
            _mainMenuView.Hide();
        }

        public void OnCreditsCountChangedHandler(int creditsCount)
        {
            _mainMenuView.SetCredits(creditsCount);
        }

        public void SetHighScore(int highScore)
        {
            _mainMenuView.SetHighScore(highScore);
        }

        public void SetKeyboardNavigatorFocused(bool isFocused)
        {
            _mainMenuView.KeyboardNavigator.SetFocus(isFocused);
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnVirtualKeyboardKeySubmit)}] OK, buttonKey:{stringValue}");
            switch (stringValue)
            {
                case PlayButtonKey:
                    OnPlayButtonClicked();

                    break;
                case LeaderboardButtonKey:
                    OnLeaderboardButtonClicked();
                    break;
                case ExitButtonKey:
                    OnExitButtonClicked();
                    break;
            }
        }

        private void OnPlayButtonClicked()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnPlayButtonClicked)}] OK, counter: {_counter}");

            if (_counter > 0) return;
            _counter++;

            _onPlayButtonClickedCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnLeaderboardButtonClicked()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLeaderboardButtonClicked)}] OK, counter: {_counter}");

            if (_counter > 0) return;
            _counter++;

            _onLeaderboardButtonClickedCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnExitButtonClicked()
        {
            if (_counter > 0) return;
            _counter++;

            _onExitButtonClickedCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}