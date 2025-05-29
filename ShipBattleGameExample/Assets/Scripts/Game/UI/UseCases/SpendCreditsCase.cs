using System;
using System.Collections.Generic;
using Game.Player;
using Game.Settings;
using Game.UI.Handlers;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using UnityEngine;

namespace Game.UI.UseCases
{
    public class SpendCreditsCase : MonoBehaviour
    {
        [SerializeField] private WalletService _walletService;
        [SerializeField] private DefaultGameSettings _settings;

        [SerializeField] private NotEnoughMoneyWindowHandler _notEnoughMoneyWindowHandler;

        [SerializeField] private CreditsWidgetHandler _creditsWidgetHandler;
        [SerializeField] private PinCodeEnteringPopupHandler _pinCodeEnteringPopupHandler;

        [SerializeField] private WebSocketCommandHandler _websocketCommandHandler;
        [SerializeField] private ErrorHandlerService _errorHandlerService;

        [SerializeField] private int _pinCodeErrorAttemptsTotal = 3;
        [SerializeField] private int _pinCodeErrorStatusCode = 412;
        [SerializeField] private int _balanceLessThenRequiredStatusCode = 402;
        [SerializeField] private int _otherCreditsErrorStatusCode = 500;

        private Action _onCreditsChargeFailureCallback;
        private Action _onCreditsChargeCancelCallback;
        private Action _onCreditsChargeSuccessCallback;

        private int _creditsToSpend;

        private int _pinCodeErrorAttemptCounter = 0;
        private int _otherCreditsErrorAttemptCounter = 0;

        private Action<int, int, Action, Action<int, string>> _nextCommand;
        private int _creditsCount = 0;
        private int _pinCode = 0;
        private Action _onCommandSuccess;
        private Action<int, string> _onCommandFailure;

        private bool _isNotEnoughMoneyWindowVisible;

        public void SpendCredits(int amount, Action onSuccess, Action onCreditsChargeSuccess,
            Action onCreditsChargeCancel, Action onCreditsChargeFailure)
        {
            ResetAttempts();

            _onCreditsChargeFailureCallback = onCreditsChargeFailure;
            _onCreditsChargeSuccessCallback = onCreditsChargeSuccess;
            _onCreditsChargeCancelCallback = onCreditsChargeCancel;

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(SpendCredits)}] OK, amount: {amount}, credits: {_walletService.Credits.Value}");

            _creditsToSpend = amount;

            if (_walletService.CanSpendCredits(amount))
            {
                PrepareAndShowPinCodeEnteringPopupForChargeCredits(amount, onSuccess, OnSpendCreditsFailure);
            }
            else
            {
                var difference = amount - _walletService.Credits.Value;
                PrepareAndShowNotEnoughMoneyWindow(difference);
            }
        }

        private void OnSpendCreditsSuccess(int amount, Action onSuccess)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnSpendCreditsSuccess)}] OK");
            _creditsWidgetHandler.RemoveCreditsAnimated(_walletService.Credits.Value, amount, () =>
            {
                _creditsWidgetHandler.HideCreditsWidget();
                onSuccess?.Invoke();
            });
            _websocketCommandHandler.SendUserBalanceRequestCommand(OnUserBalanceRequestSuccess,
                OnUserBalanceRequestFailure);
        }

        private void OnSpendCreditsFailure(int statusCode, string errorMessage)
        {
            LoggerHelper.LogError(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnSpendCreditsFailure)}] OK, some error occured! Status:{statusCode} , error: {errorMessage}");
            HandleErrorStatusCode(statusCode);
        }

        private void PrepareAndShowNotEnoughMoneyWindow(int differenceCreditsCount)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(PrepareAndShowNotEnoughMoneyWindow)}] OK, difference: {differenceCreditsCount}");

            _notEnoughMoneyWindowHandler.SetDifferenceCreditsCount(differenceCreditsCount);

            _notEnoughMoneyWindowHandler.SetCreditsButtonClickCallback(OnChargeCreditsButtonClickHandler);
            _notEnoughMoneyWindowHandler.SetCancelButtonClickCallback(OnNotEnoughMoneyWindowNoButtonClickHandler);
            var defaultCreditsToChargeValue = GetCreditsToCharge(differenceCreditsCount);

            _notEnoughMoneyWindowHandler.SetChargedCreditsCount(defaultCreditsToChargeValue);

            _isNotEnoughMoneyWindowVisible = true;
            _notEnoughMoneyWindowHandler.ShowNotEnoughMoneyWindow();
        }

        private void OnChargeCreditsButtonClickHandler(int creditsCount)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeCreditsButtonClickHandler)}] OK,  credits: {creditsCount}");

            _notEnoughMoneyWindowHandler.SetKeyboardNavigatorFocused(false);

            PrepareAndShowPinCodeEnteringPopupForAddingCredits(creditsCount, null, null);
        }

        private void OnAddBalanceSuccess(int creditsCount, int pinCode)
        {
            _creditsWidgetHandler.AddCreditsAnimated(_walletService.Credits.Value, creditsCount,
                () =>
                {
                    SendChargeCreditsCommand(_creditsToSpend, pinCode,
                        _onCreditsChargeSuccessCallback, OnChargeAfterAddingFailure);
                });

            _walletService.AddCredits(creditsCount);
        }

        private void OnAddBalanceFailure(int statusCode, string errorMessage)
        {
            LoggerHelper.LogError(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnAddBalanceFailure)}] OK, some error occured! Status: {statusCode} Error: {errorMessage}");
            HandleErrorStatusCode(statusCode);
        }

        private void OnChargeAfterAddingSuccess(int amount)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeAfterAddingSuccess)}] OK, " +
                             $"credits: {_walletService.Credits.Value}, credits to spend: {_creditsToSpend}, amount: {amount}");
            _creditsWidgetHandler.RemoveCreditsAnimated(_walletService.Credits.Value, amount, (() =>
            {
                _creditsWidgetHandler.HideCreditsWidget();
                _onCreditsChargeSuccessCallback?.Invoke();

                _websocketCommandHandler.SendUserBalanceRequestCommand(OnUserBalanceRequestSuccess,
                    OnUserBalanceRequestFailure);
            }));

            _walletService.SpendCredits(_creditsToSpend);

            _creditsToSpend = 0;
        }

        private void OnChargeAfterAddingFailure(int statusCode, string errorMessage)
        {
            LoggerHelper.LogError(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeAfterAddingFailure)}] OK, some error occured! Status: {statusCode}  Error: {errorMessage}");
            HandleErrorStatusCode(statusCode);
        }

        private void OnNotEnoughMoneyWindowNoButtonClickHandler()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnNotEnoughMoneyWindowNoButtonClickHandler)}] OK");
            _notEnoughMoneyWindowHandler.HideNotEnoughMoneyWindow();
            _onCreditsChargeCancelCallback?.Invoke();
        }

        private List<int> GetCreditsToCharge(int differences)
        {
            var result = new List<int>(_settings.CreditsForDeposit);
            //check if we have the same value
            //if not then check if more or less than difference 
            //if less, then remove last value and add difference at the start
            //if more, then 
            //example: 3, 5, 10, 20
            //difference: 1
            //difference: 6

            if (result.Contains(differences))
                return result;

            var numberToRemove = new List<int>();
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] < differences)
                {
                    numberToRemove.Add(result[i]);
                    continue;
                }

                if (i > 0)
                {
                    result[i - 1] = differences;
                }
                else
                {
                    result[i] = differences;
                }

                break;
            }

            if (numberToRemove.Count > 0)
            {
                numberToRemove.RemoveAt(numberToRemove.Count - 1);
            }

            numberToRemove.ForEach(i => result.Remove(i));

            return result;
        }

        private void OnUserBalanceRequestSuccess(int userBalance)
        {
            _walletService.SetCredits(userBalance);
        }

        private void OnUserBalanceRequestFailure(int statusCode, string errorMessage)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnUserBalanceRequestFailure)}] Error: {errorMessage}");
        }

        private void ResetAttempts()
        {
            _pinCodeErrorAttemptCounter = _pinCodeErrorAttemptsTotal;
            _otherCreditsErrorAttemptCounter = _pinCodeErrorAttemptsTotal;
        }

        private void HandleErrorStatusCode(int statusCode)
        {
            if (statusCode == _pinCodeErrorStatusCode)
            {
                HandlePinCodeErrorStatusCode();
            }
            else if (statusCode == _otherCreditsErrorStatusCode)
            {
                HandleOtherCreditsErrorStatusCode();
            }
            else if (statusCode == _balanceLessThenRequiredStatusCode)
            {
                LoggerHelper.Log(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleErrorStatusCode)}] OK, statusCode:{statusCode}, reason: balance less then required");
            }
        }

        private void HandlePinCodeErrorStatusCode()
        {
            if (_pinCodeErrorAttemptCounter <= 0)
            {
                _pinCodeEnteringPopupHandler.ShowPinCodeEnteringPopup();
                _pinCodeEnteringPopupHandler.ShowPinCodeErrorMessage(_pinCodeErrorAttemptCounter);
                _pinCodeEnteringPopupHandler.SetErrorMessageDisplayedCallback(OnPinCodeErrorDisplayedCallback);
                return;
            }

            _pinCodeEnteringPopupHandler.ShowPinCodeEnteringPopup();
            _pinCodeEnteringPopupHandler.ShowPinCodeErrorMessage(_pinCodeErrorAttemptCounter);
            _pinCodeErrorAttemptCounter--;
        }

        private void HandleOtherCreditsErrorStatusCode()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleOtherCreditsErrorStatusCode)}] OK, other credits attempts: {_otherCreditsErrorAttemptCounter}");
            if (_otherCreditsErrorAttemptCounter <= 0)
            {
                _notEnoughMoneyWindowHandler.SetErrorMessageDisplayedCallback(OnOtherErrorDisplayedCallback);
                _notEnoughMoneyWindowHandler.ShowTransactionErrorMessage(_otherCreditsErrorAttemptCounter,
                    _otherCreditsErrorAttemptCounter <= 0);
                _notEnoughMoneyWindowHandler.ShowNotEnoughMoneyWindow();
                return;
            }

            _notEnoughMoneyWindowHandler.ShowTransactionErrorMessage(_otherCreditsErrorAttemptCounter);
            _notEnoughMoneyWindowHandler.ShowNotEnoughMoneyWindow();
            _otherCreditsErrorAttemptCounter--;
        }

        private void OnOtherErrorDisplayedCallback()
        {
            _notEnoughMoneyWindowHandler.HideNotEnoughMoneyWindow();
            _errorHandlerService.HandleCreditsError("Recharge credits failed");
        }

        private void OnPinCodeErrorDisplayedCallback()
        {
            _errorHandlerService.HandleCreditsError("Incorrect pin code");
            _pinCodeEnteringPopupHandler.HidePinCodeEnteringPopup();
            _notEnoughMoneyWindowHandler.HideNotEnoughMoneyWindow();
        }


        #region PinCode entering popup

        private void PrepareAndShowPinCodeEnteringPopup(int creditsCount)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(PrepareAndShowPinCodeEnteringPopup)}] OK,  credits: {creditsCount}");
            _pinCodeEnteringPopupHandler.SetNextButtonClickCallback(OnEnterButtonClickPinCodePopupHandler);
            _pinCodeEnteringPopupHandler.SetCloseButtonClickCallback(OnPinCodeEnteringCloseButtonClickHandler);

            _notEnoughMoneyWindowHandler.SetKeyboardNavigatorFocused(false);

            _pinCodeEnteringPopupHandler.SetInputFieldSelectedCallback(OnPinCodeEnteringPopupInputFieldSelectedHandler);
            _pinCodeEnteringPopupHandler.ShowPinCodeEnteringPopup();
        }


        private void OnPinCodeEnteringCloseButtonClickHandler()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnPinCodeEnteringCloseButtonClickHandler)}] OK");

            _pinCodeEnteringPopupHandler.HidePinCodeEnteringPopup();
            if (_isNotEnoughMoneyWindowVisible)
            {
                _notEnoughMoneyWindowHandler.SetKeyboardNavigatorFocused(true);
            }
            else
            {
                _onCreditsChargeCancelCallback?.Invoke();
            }
        }

        private void OnEnterButtonClickPinCodePopupHandler(string pinCodeRaw)
        {
            if (string.IsNullOrEmpty(pinCodeRaw)) return;

            _pinCode = int.Parse(pinCodeRaw);

            _notEnoughMoneyWindowHandler.HideNotEnoughMoneyWindow();
            _pinCodeEnteringPopupHandler.HidePinCodeEnteringPopup();

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnEnterButtonClickPinCodePopupHandler)}] OK, pin code: {_pinCode}, credits: {_creditsCount}");
            _nextCommand?.Invoke(_creditsCount, _pinCode, _onCommandSuccess, _onCommandFailure);
        }

        private void OnPinCodeEnteringPopupInputFieldSelectedHandler(string pinCodeRaw)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnPinCodeEnteringPopupInputFieldSelectedHandler)}] OK");
        }

        #endregion

        #region Charge Credits

        private void PrepareAndShowPinCodeEnteringPopupForChargeCredits(int creditsCount, Action onSuccess,
            Action<int, string> onFailure)
        {
            _nextCommand = SendChargeCreditsCommand;
            _creditsCount = creditsCount;
            _onCommandSuccess = onSuccess;
            _onCommandFailure = onFailure;
            PrepareAndShowPinCodeEnteringPopup(creditsCount);
        }

        private void SendChargeCreditsCommand(int creditsCount, int pinCode, Action onSuccess,
            Action<int, string> onFailure)
        {
            _websocketCommandHandler.SendChargeUserBalanceRequestCommand(creditsCount, pinCode,
                () => OnSpendCreditsSuccess(creditsCount, onSuccess)
                , onFailure);
        }

        #endregion

        #region Add Credits

        private void PrepareAndShowPinCodeEnteringPopupForAddingCredits(int creditsCount, Action onSuccess,
            Action<int, string> onFailure)
        {
            _nextCommand = SendAddCreditsCommand;
            _creditsCount = creditsCount;
            _onCommandSuccess = onSuccess;
            _onCommandFailure = onFailure;
            PrepareAndShowPinCodeEnteringPopup(creditsCount);
        }

        private void SendAddCreditsCommand(int creditsCount, int pinCode, Action onSuccess,
            Action<int, string> onFailure)
        {
            _websocketCommandHandler.SendAddBalanceRequestCommand(creditsCount, pinCode,
                () => OnAddBalanceSuccess(creditsCount, pinCode),
                OnAddBalanceFailure);
        }

        #endregion

        //Testing

        #region Testing

        [ContextMenu("Test show Pin Code Popup")]
        private void TestShowPinCodeEnteringPopup()
        {
            var credits = UnityEngine.Random.Range(1, 5);
            OnChargeCreditsButtonClickHandler(credits);
        }

        #endregion
    }
}