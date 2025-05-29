using System;
using Core.UI;
using Game.UI.Items.Keyboard;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface ICompletedDemoWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetOkayButtonCallback(System.Action okayButtonCallback);
    }

    public class CompletedDemoWindowView : BaseView, ICompletedDemoWindowView
    {
        public override ViewType ViewType => ViewType.CompletedDemoWindow;


        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;
        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;
        [SerializeField] private Button _okayButton;

        private Action _okayButtonCallback;

        public void SetOkayButtonCallback(Action okayButtonCallback)
        {
            _okayButtonCallback = okayButtonCallback;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _okayButton.onClick.AddListener(OnOkayButtonClicked);
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

        private void OnOkayButtonClicked()
        {
            _okayButtonCallback?.Invoke();
        }
    }
}