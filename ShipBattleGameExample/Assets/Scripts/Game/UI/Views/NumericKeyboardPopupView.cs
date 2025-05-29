using System;
using Core.UI;
using DG.Tweening;
using Game.UI.Items.Keyboard;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Views
{
    public interface INumericKeyboardPopupView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void HideWithAction(Action onHide);
    }

    public class NumericKeyboardPopupView : BaseView, INumericKeyboardPopupView
    {
        public override ViewType ViewType => ViewType.NumericKeyboardPopup;
        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private Transform _keyboardParent;
        [SerializeField] private Transform _startAppearPoint;
        [SerializeField] private Transform _endAppearPoint;

        [SerializeField] private float _appearAnimationDuration = 0.5f;
        [SerializeField] private float _disappearAnimationDuration = 0.5f;

        [SerializeField] private Ease _appearAnimationEase = Ease.OutQuad;
        [SerializeField] private Ease _disappearAnimationEase = Ease.InQuad;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;

        private Action _onHideAction;

        public void HideWithAction(Action onHide)
        {
            _onHideAction = onHide;
            Hide();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _keyboardParent.gameObject.SetActive(false);
            _keyboardParent.localPosition = _startAppearPoint.localPosition;
        }

        protected override void OnShow()
        {
            base.OnShow();
            StartAppearAnimation();
        }

        protected override void OnBeforeHide(Action onDone)
        {
            StartDisappearAnimation(() => base.OnBeforeHide(onDone));
        }

        [ContextMenu("Start Appear Animation")]
        private void TestStartAppearAnimation()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestStartAppearAnimation)}] OK");
            StartAppearAnimation();
        }

        [ContextMenu("Start Disappear Animation")]
        private void TestDisappearAnimation()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestDisappearAnimation)}] OK");
            StartDisappearAnimation(null);
        }

        private void StartAppearAnimation()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartAppearAnimation)}] OK");

            _keyboardParent.gameObject.SetActive(true);
            _keyboardParent.DOLocalMove(_endAppearPoint.localPosition, _appearAnimationDuration)
                .SetEase(_appearAnimationEase)
                .OnComplete(OnStartAppearAnimationCompletedHandler);
        }

        private void OnStartAppearAnimationCompletedHandler()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnStartAppearAnimationCompletedHandler)}] OK");
            _keyboardParent.transform.localPosition = _endAppearPoint.localPosition;
        }

        private void StartDisappearAnimation(Action onDone)
        {
            _keyboardParent.DOLocalMove(_startAppearPoint.localPosition, _disappearAnimationDuration)
                .SetEase(_disappearAnimationEase)
                .OnComplete(() =>
                {
                    onDone?.Invoke();
                    OnDisappearAnimationCompletedHandler();
                });
        }

        private void OnDisappearAnimationCompletedHandler()
        {
            _keyboardParent.transform.localPosition = _startAppearPoint.localPosition;
            _keyboardParent.gameObject.SetActive(false);
            _onHideAction?.Invoke();
            _onHideAction = null;
        }
    }
}