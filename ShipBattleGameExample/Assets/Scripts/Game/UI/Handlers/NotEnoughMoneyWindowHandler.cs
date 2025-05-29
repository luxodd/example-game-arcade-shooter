using System;
using System.Collections.Generic;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class NotEnoughMoneyWindowHandler : MonoBehaviour
    {
        private const string CancelButtonKey = "Cancel";

        private INotEnoughMoneyWindowView _notEnoughMoneyWindowView;

        private Action _cancelButtonClickCallback;
        private Action<int> _creditsButtonClickCallback;

        private int _counter = 0;
        private List<int> _chargedCreditsCountList = new List<int>();

        public void PrepareNotEnoughMoneyWindow(INotEnoughMoneyWindowView notEnoughMoneyWindowView)
        {
            _notEnoughMoneyWindowView = notEnoughMoneyWindowView;
            _notEnoughMoneyWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ShowNotEnoughMoneyWindow()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ShowNotEnoughMoneyWindow)}] OK");
            _notEnoughMoneyWindowView.Show();
        }

        public void HideNotEnoughMoneyWindow()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(HideNotEnoughMoneyWindow)}] OK");
            _notEnoughMoneyWindowView.Hide();
        }

        public void SetDifferenceCreditsCount(int creditsCount)
        {
            _notEnoughMoneyWindowView.SetDifferenceCreditsCount(creditsCount);
        }

        public void SetCreditsButtonClickCallback(System.Action<int> clickCallback)
        {
            _creditsButtonClickCallback = clickCallback;
            _notEnoughMoneyWindowView.SetCreditsButtonClickCallback(OnCreditsButtonClickHandler);
        }

        public void SetChargedCreditsCount(List<int> chargedCreditsCountList)
        {
            _chargedCreditsCountList = chargedCreditsCountList;
            _notEnoughMoneyWindowView.SetChargeCreditsCount(chargedCreditsCountList);
        }

        public void SetCancelButtonClickCallback(System.Action cancelButtonClickCallback)
        {
            _cancelButtonClickCallback = cancelButtonClickCallback;
            _notEnoughMoneyWindowView.SetCancelButtonClickCallback(OnCancelButtonClickHandler);
        }

        public void SetErrorMessageDisplayedCallback(System.Action errorDisplayedCallback)
        {
            _notEnoughMoneyWindowView.SetErrorMessageDisplayedCallback(errorDisplayedCallback);
        }

        public void ShowTransactionErrorMessage(int attemptsLeft, bool shouldGoBack = false)
        {
            _notEnoughMoneyWindowView.ShowTransactionErrorMessage(attemptsLeft, shouldGoBack);
        }

        public void SetKeyboardNavigatorFocused(bool isFocused)
        {
            _notEnoughMoneyWindowView.KeyboardNavigator.SetFocus(isFocused);
        }

        public void SetKeyboardNavigatorItemAsDeselect()
        {
            _notEnoughMoneyWindowView.KeyboardNavigator.DeselectItem();
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            switch (stringValue)
            {
                case CancelButtonKey:
                    OnCancelButtonClickHandler();
                    break;

                default:
                    var intValue = int.Parse(stringValue);
                    OnCreditsButtonClickHandler(_chargedCreditsCountList[intValue - 1]);
                    break;
            }
        }

        private void OnCancelButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _cancelButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnCreditsButtonClickHandler(int creditsCount)
        {
            if (_counter > 0) return;

            _counter++;
            _creditsButtonClickCallback?.Invoke(creditsCount);

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}