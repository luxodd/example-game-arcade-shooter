using System;
using System.Collections.Generic;
using Core.UI;
using Game.UI.Items;
using Game.UI.Items.Keyboard;
using Game.UI.UseCases;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface INotEnoughMoneyWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetCreditsButtonClickCallback(Action<int> creditsClickedCallback);
        void SetCancelButtonClickCallback(Action cancelButtonClickedCallback);
        void SetErrorMessageDisplayedCallback(Action errorDisplayedCallback);

        void SetDifferenceCreditsCount(int creditsCount);

        void SetChargeCreditsCount(List<int> chargeCreditsCountList);

        void ShowTransactionErrorMessage(int attemptsLeft, bool shouldGoBack = false);
    }

    public class NotEnoughMoneyWindowView : BaseView, INotEnoughMoneyWindowView
    {
        public override ViewType ViewType => ViewType.NotEnoughMoneyWindow;

        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private List<CreditButtonItem> _creditButtonItems;
        [SerializeField] private Button _cancelButton;

        [SerializeField] private TMP_Text _creditsCountText;

        [SerializeField] private TMP_Text _transactionErrorMessageText;
        [SerializeField] private TransactionErrorMessagesDescriptor _transactionErrorMessageDescriptor;
        [SerializeField] private float _delayBeforeGoBack = 3f;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;

        private System.Action<int> _creditsClickCallback;
        private System.Action _cancelButtonClickCallback;

        private Action _onErrorMessageDisplayed;

        private string _creditsCountTextFormat;

        public void SetCreditsButtonClickCallback(Action<int> creditsClickedCallback)
        {
            _creditsClickCallback = creditsClickedCallback;
        }

        public void SetCancelButtonClickCallback(Action cancelButtonClickedCallback)
        {
            _cancelButtonClickCallback = cancelButtonClickedCallback;
        }

        public void SetErrorMessageDisplayedCallback(Action errorDisplayedCallback)
        {
            _onErrorMessageDisplayed = errorDisplayedCallback;
        }

        public void SetDifferenceCreditsCount(int creditsCount)
        {
            _creditsCountText.text = string.Format(_creditsCountTextFormat, creditsCount);
        }

        public void SetChargeCreditsCount(List<int> chargeCreditsCountList)
        {
            HideButtons();
            for (var i = 0; i < chargeCreditsCountList.Count; i++)
            {
                _creditButtonItems[i].gameObject.SetActive(true);
                _creditButtonItems[i].SetCreditsCount(chargeCreditsCountList[i]);
                var i1 = i;
                _creditButtonItems[i]
                    .SetButtonClickCallback(() => _creditsClickCallback?.Invoke(chargeCreditsCountList[i1]));
            }
        }

        public void ShowTransactionErrorMessage(int attemptsLeft, bool shouldGoBack = false)
        {
            var errorMessage = _transactionErrorMessageDescriptor.GetTransactionErrorMessage(attemptsLeft);
            _transactionErrorMessageText.text = errorMessage;
            _transactionErrorMessageText.gameObject.SetActive(true);
            if (shouldGoBack == false) return;

            CoroutineManager.DelayedAction(_delayBeforeGoBack, OnErrorMessageDisplayed);
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _creditsCountTextFormat = _creditsCountText.text;

            _cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _keyboardNavigator.Activate();
        }

        protected override void OnHide()
        {
            base.OnHide();
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnHide)}] OK");
            _keyboardNavigator.Deactivate();
            _transactionErrorMessageText.gameObject.SetActive(false);
        }

        private void OnCancelButtonClicked()
        {
            _cancelButtonClickCallback?.Invoke();
        }

        private void HideButtons()
        {
            foreach (var creditButtonItem in _creditButtonItems)
            {
                creditButtonItem.gameObject.SetActive(false);
            }
        }

        private void OnErrorMessageDisplayed()
        {
            _onErrorMessageDisplayed?.Invoke();
        }
    }
}