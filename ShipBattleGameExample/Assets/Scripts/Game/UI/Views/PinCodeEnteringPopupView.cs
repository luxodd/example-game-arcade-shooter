using System;
using Core.UI;
using Game.UI.Items.Keyboard;
using Game.UI.UseCases;
using Luxodd.Game.Scripts.HelpersAndUtils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface IPinCodeEnteringPopupView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        TMP_InputField InputField { get; }
        void SetNextButtonClickedCallback(Action<string> callback);
        void SetCloseButtonClickedCallback(Action closeCallback);
        void SetErrorMessageDisplayedCallback(Action errorDisplayedCallback);

        void SetInputFieldSelectedCallback(Action<string> inputFieldSelectedCallback);

        void ShowIncorrectPinCodeErrorMessage(int attemptsLeft, bool shouldGoBack = false);
    }

    public class PinCodeEnteringPopupView : BaseView, IPinCodeEnteringPopupView
    {
        private const int MinimalLength = 4;
        public override ViewType ViewType => ViewType.PinCodeEnteringPopup;
        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private int _minimalLength = 5;

        [SerializeField] private TMP_InputField _pinCodeInputField;

        [SerializeField] private TMP_Text _pinCodeErrorMessageText;
        [SerializeField] private PinCodeMessagesDescriptor _messagesDescriptor;
        [SerializeField] private float _delayBeforeGoBack = 3f;

        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _nextButton;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;

        private Action<string> _nextButtonCallback;
        private Action _closeButtonCallback;
        private Action<string> _inputFieldSelectedCallback;
        private Action _onErrorMessageDisplayed;
        public TMP_InputField InputField => _pinCodeInputField;

        public void SetNextButtonClickedCallback(Action<string> callback)
        {
            _nextButtonCallback = callback;
        }

        public void SetCloseButtonClickedCallback(Action closeCallback)
        {
            _closeButtonCallback = closeCallback;
        }

        public void SetErrorMessageDisplayedCallback(Action errorDisplayedCallback)
        {
            _onErrorMessageDisplayed = errorDisplayedCallback;
        }

        public void SetInputFieldSelectedCallback(Action<string> inputFieldSelectedCallback)
        {
            _inputFieldSelectedCallback = inputFieldSelectedCallback;
        }

        public void ShowIncorrectPinCodeErrorMessage(int attemptsLeft, bool shouldGoBack = false)
        {
            var message = _messagesDescriptor.GetPinCodeMessage(attemptsLeft);
            _pinCodeErrorMessageText.text = message;
            _pinCodeErrorMessageText.gameObject.SetActive(true);
            CoroutineManager.DelayedAction(_delayBeforeGoBack, OnErrorMessageDisplayed);
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _nextButton.onClick.AddListener(OnNextButtonClicked);
            _pinCodeInputField.onValueChanged.AddListener(OnPinCodeInputFieldValueChanged);
            _pinCodeInputField.onSelect.AddListener(OnPinCodeInputFieldOnSelect);
            _pinCodeInputField.onSubmit.AddListener(OnPinCodeInputFieldOnSubmit);

            _pinCodeErrorMessageText.gameObject.SetActive(false);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _keyboardNavigator.Activate();
            _pinCodeInputField.text = string.Empty;
            OnPinCodeInputFieldValueChanged(string.Empty);
            _pinCodeInputField.ActivateInputField();
        }

        protected override void OnHide()
        {
            _pinCodeInputField.text = string.Empty;
            _pinCodeInputField.DeactivateInputField(true);
            _pinCodeInputField.OnDeselect(null);
            _pinCodeErrorMessageText.gameObject.SetActive(false);
            _closeButton.OnSelect(null);
            _keyboardNavigator.Deactivate();
            base.OnHide();
        }

        private void OnNextButtonClicked()
        {
            _nextButtonCallback?.Invoke(_pinCodeInputField.text);
        }

        private void OnCloseButtonClicked()
        {
            _closeButtonCallback?.Invoke();
        }

        private void OnPinCodeInputFieldOnSelect(string value)
        {
            _inputFieldSelectedCallback?.Invoke(value);
        }

        private void OnPinCodeInputFieldValueChanged(string stringValue)
        {
            _nextButton.interactable =
                string.IsNullOrEmpty(stringValue) == false && stringValue.Length >= _minimalLength;
        }

        private void OnPinCodeInputFieldOnSubmit(string pinCodeRaw)
        {
            _nextButtonCallback?.Invoke(pinCodeRaw);
            _nextButton.interactable = false;
        }

        private void OnErrorMessageDisplayed()
        {
            _onErrorMessageDisplayed?.Invoke();
        }
    }
}