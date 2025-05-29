using System;
using Core.UI;
using Game.UI.Items.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface IContinueGameWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetContinueButtonClickedCallback(System.Action callback);
        void SetCancelButtonClickedCallback(System.Action callback);

        void SetCreditsCount(int creditsCount);
    }

    public class ContinueGameWindowView : BaseView, IContinueGameWindowView
    {
        public override ViewType ViewType => ViewType.ContinueGameWindow;
        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;
        [SerializeField] private TMP_Text _creditsCountText;

        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _cancelButton;

        private string _creditsCountTextFormat;

        private System.Action _continueButtonClickedCallback;
        private System.Action _cancelButtonClickedCallback;

        public void SetContinueButtonClickedCallback(Action callback)
        {
            _continueButtonClickedCallback = callback;
        }

        public void SetCancelButtonClickedCallback(Action callback)
        {
            _cancelButtonClickedCallback = callback;
        }

        public void SetCreditsCount(int creditsCount)
        {
            _creditsCountText.text = string.Format(_creditsCountTextFormat, creditsCount);
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _creditsCountTextFormat = _creditsCountText.text;

            _continueButton.onClick.AddListener(OnContinueButtonClicked);
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
            _keyboardNavigator.Deactivate();
        }

        private void OnContinueButtonClicked()
        {
            _continueButtonClickedCallback?.Invoke();
        }

        private void OnCancelButtonClicked()
        {
            _cancelButtonClickedCallback?.Invoke();
        }
    }
}