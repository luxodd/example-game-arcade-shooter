using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Items
{
    public class CreditButtonItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _creditsText;
        [SerializeField] private Button _button;
        
        private System.Action _buttonClickCallback;

        public void SetCreditsCount(int creditsCount)
        {
            _creditsText.text = creditsCount.ToString();
        }

        public void SetButtonClickCallback(Action buttonClickCallback)
        {
            _buttonClickCallback = buttonClickCallback;
        }

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            _buttonClickCallback?.Invoke();
        }
    }
}
