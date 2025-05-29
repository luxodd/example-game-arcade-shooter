using TMPro;
using UnityEngine;

namespace Game.UI.Items.Keyboard
{
    public class InputFieldItem : NavigableItem
    {
        [SerializeField] private TMP_InputField _inputField;

        public override void Execute()
        {
            _inputField.ActivateInputField();
        }

        protected override void OnSelect()
        {
            base.OnSelect();
            _inputField.ActivateInputField();
        }

        protected override void OnDeselect()
        {
            base.OnDeselect();
            _inputField.DeactivateInputField();
        }
    }
}