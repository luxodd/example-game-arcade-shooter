using System;
using Game.UI.Views;
using TMPro;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class NumericKeyboardPopupHandler : MonoBehaviour
    {
        private const string SpaceKey = "Space";
        private const string SubmitKey = "Submit";
        private const string BackspaceKey = "Backspace";
        private const string HideKey = "Hide";
        private const string LeftCaretKey = "<";
        private const string RightCaretKey = ">";

        private INumericKeyboardPopupView _numericKeyboardPopupView;
        private TMP_InputField _inputField;

        private Action _numericKeyboardSubmitAction;

        private int _characterLimit = 0;

        public void PrepareView(INumericKeyboardPopupView numericKeyboardPopupView)
        {
            _numericKeyboardPopupView = numericKeyboardPopupView;

            _numericKeyboardPopupView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void PrepareInputField(TMP_InputField inputField)
        {
            _inputField = inputField;
            _characterLimit = _inputField.characterLimit;
        }

        public void ShowNumericKeyboardPopup()
        {
            _numericKeyboardPopupView.Show();
            _numericKeyboardPopupView.KeyboardNavigator.Activate();
        }

        public void HideNumericKeyboardPopup()
        {
            _numericKeyboardPopupView.Hide();
            _numericKeyboardPopupView.KeyboardNavigator.Deactivate();
        }

        public void SetKeyboardSubmitAction(Action callback)
        {
            _numericKeyboardSubmitAction = callback;
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            switch (stringValue)
            {
                case HideKey:
                    HideNumericKeyboardPopup();
                    break;
                case SubmitKey:
                    //submit
                    if (_inputField.text.Length < _characterLimit) return;
                    _numericKeyboardSubmitAction?.Invoke();

                    _numericKeyboardPopupView.HideWithAction(() =>
                    {
                        _numericKeyboardPopupView.KeyboardNavigator.Deactivate();
                        _inputField.OnSubmit(null);
                    });
                    break;
                case LeftCaretKey:
                    _inputField.caretPosition -= 1;
                    break;
                case RightCaretKey:
                    _inputField.caretPosition += 1;
                    break;
                case BackspaceKey:
                    _inputField.text = _inputField.text[..^1];
                    break;
                case SpaceKey:
                    _inputField.text += " ";
                    break;
                default:
                    if (_inputField.text.Length >= _characterLimit) return;
                    _inputField.text += stringValue;
                    _inputField.caretPosition += 1;
                    break;
            }
        }
    }
}
