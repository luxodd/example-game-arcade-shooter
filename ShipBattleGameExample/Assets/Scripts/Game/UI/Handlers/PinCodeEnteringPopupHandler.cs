using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using TMPro;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class PinCodeEnteringPopupHandler : MonoBehaviour
    {
        private const string CloseKey = "Close";
        private const string NextKey = "Next";
        public TMP_InputField PinCodeInputField => _pinCodeEnteringPopupView.InputField;
        private IPinCodeEnteringPopupView _pinCodeEnteringPopupView;

        private Action _closeButtonClickCallback;
        private Action<string> _nextButtonClickCallback;


        private int _counter
        {
            get { return _counterInner; }
            set
            {
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][Setter] OK, value: {value}");
                _counterInner = value;
            }
        }

        private int _counterInner = 0;

        public void PrepareView(IPinCodeEnteringPopupView pinCodeEnteringPopupView)
        {
            _pinCodeEnteringPopupView = pinCodeEnteringPopupView;
            _pinCodeEnteringPopupView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeySubmitted);
        }

        public void ShowPinCodeEnteringPopup()
        {
            _pinCodeEnteringPopupView.Show();
        }

        public void HidePinCodeEnteringPopup()
        {
            _pinCodeEnteringPopupView.Hide();
        }

        public void SetNextButtonClickCallback(Action<string> callback)
        {
            _nextButtonClickCallback = callback;
            _pinCodeEnteringPopupView.SetNextButtonClickedCallback(OnNextButtonClickHandler);
        }

        public void SetCloseButtonClickCallback(Action callback)
        {
            _closeButtonClickCallback = callback;
            _pinCodeEnteringPopupView.SetCloseButtonClickedCallback(OnCloseButtonClickHandler);
        }

        public void SetInputFieldSelectedCallback(Action<string> callback)
        {
            _pinCodeEnteringPopupView.SetInputFieldSelectedCallback(callback);
        }

        public void ShowPinCodeErrorMessage(int attemptsLeft)
        {
            _pinCodeEnteringPopupView.ShowIncorrectPinCodeErrorMessage(attemptsLeft);
        }

        public void SetErrorMessageDisplayedCallback(Action errorDisplayedCallback)
        {
            _pinCodeEnteringPopupView.SetErrorMessageDisplayedCallback(errorDisplayedCallback);
        }

        private void OnVirtualKeySubmitted(string stringValue)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnVirtualKeySubmitted)}] OK, string value:{stringValue}");

            switch (stringValue)
            {
                case CloseKey:
                    OnCloseButtonClickHandler();
                    break;

                case NextKey:
                    OnNextButtonClickHandler(_pinCodeEnteringPopupView.InputField.text);
                    ;
                    break;
            }
        }

        private void OnCloseButtonClickHandler()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnCloseButtonClickHandler)}] OK, counter: {_counter}");
            if (_counter > 0) return;

            _counter++;
            _closeButtonClickCallback?.Invoke();
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }

        private void OnNextButtonClickHandler(string pinCodeValue)
        {
            if (_counter > 0) return;

            _counter++;
            _nextButtonClickCallback?.Invoke(pinCodeValue);
            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}