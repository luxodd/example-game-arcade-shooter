using System;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Items.Keyboard
{
    public class KeyButtonItem : NavigableItem
    {
        public ISimpleEvent<string> KeySelectedEvent => _keySelectedEvent;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private string _keyValue;
        [SerializeField] private bool _shouldUseKeyValueOnButton = true;
        [SerializeField] private Button _button;

        private readonly SimpleEvent<string> _keySelectedEvent = new SimpleEvent<string>();

        public override void Execute()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Execute)}] OK, key: {_keyValue}");
            OnButtonClicked();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _button.onClick.AddListener(OnButtonClicked);
            if (_shouldUseKeyValueOnButton)
            {
                _text.text = _keyValue;
            }
        }

        private void OnButtonClicked()
        {
            if (_isSelected)
            {
                _keySelectedEvent.Notify(_keyValue);
            }
        }
    }
}