using System;
using Core.UI;
using Game.UI.Items.Keyboard;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface IReplayLevelWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetPlayAgainButtonCallback(System.Action playAgainButtonCallback);
        void SetNotNowButtonCallback(System.Action notNowButtonCallback);
    }

    public class ReplayLevelWindowView : BaseView, IReplayLevelWindowView
    {
        public override ViewType ViewType => ViewType.ReplayDemoWindow;

        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _notNowButton;

        private Action _playAgainButtonCallback;
        private Action _notNowButtonCallback;

        public void SetPlayAgainButtonCallback(Action playAgainButtonCallback)
        {
            _playAgainButtonCallback = playAgainButtonCallback;
        }

        public void SetNotNowButtonCallback(Action notNowButtonCallback)
        {
            _notNowButtonCallback = notNowButtonCallback;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _playAgainButton.onClick.AddListener(OnPlayAgainButtonClicked);
            _notNowButton.onClick.AddListener(OnNotNowButtonClicked);
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

        private void OnPlayAgainButtonClicked()
        {
            _playAgainButtonCallback?.Invoke();
        }

        private void OnNotNowButtonClicked()
        {
            _notNowButtonCallback?.Invoke();
        }
    }
}