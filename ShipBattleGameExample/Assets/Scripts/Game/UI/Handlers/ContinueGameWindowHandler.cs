using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class ContinueGameWindowHandler : MonoBehaviour
    {
        private const string YesButtonKey = "Yes";
        private const string NoButtonKey = "No";

        private IContinueGameWindowView _continueGameWindowView;

        private Action _continueButtonClickCallback;
        private Action _cancelButtonClickCallback;

        private int _counter = 0;

        public void PrepareContinueGameWindow(IContinueGameWindowView continueGameWindowView)
        {
            _continueGameWindowView = continueGameWindowView;

            _continueGameWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ShowContinueGameWindow()
        {
            _continueGameWindowView.Show();
        }

        public void HideContinueGameWindow()
        {
            _continueGameWindowView.Hide();
        }

        public void SetCreditsCount(int creditsCount)
        {
            _continueGameWindowView.SetCreditsCount(creditsCount);
        }

        public void SetContinueButtonClickCallback(System.Action callback)
        {
            _continueButtonClickCallback = callback;
            _continueGameWindowView.SetContinueButtonClickedCallback(callback);
        }

        public void SetCancelButtonClickCallback(System.Action callback)
        {
            _cancelButtonClickCallback = callback;
            _continueGameWindowView.SetCancelButtonClickedCallback(callback);
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
            _continueButtonClickCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnNoButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _cancelButtonClickCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}