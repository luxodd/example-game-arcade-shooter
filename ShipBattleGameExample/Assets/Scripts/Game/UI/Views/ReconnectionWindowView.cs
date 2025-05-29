using Core.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public interface IReconnectionWindowView : IView
    {
        void SwitchToReconnection();
        void SwitchToReconnectionFailed();
    }

    public class ReconnectionWindowView : BaseView, IReconnectionWindowView
    {
        public override ViewType ViewType => ViewType.ReconnectionWindow;

        [SerializeField] private TMP_Text _reconnectionText;

        [SerializeField] private GameObject _spinnerObject;

        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private float _rotationAngle = 90f;
        [SerializeField] private Ease _ease = Ease.OutBounce;

        private bool _isReconnecting;

        private Tweener _animationTweener;

        public void SwitchToReconnection()
        {
            StartReconnectionAnimation();
        }

        public void SwitchToReconnectionFailed()
        {
            _isReconnecting = false;
            StopAnimation();
        }

        protected override void OnHide()
        {
            base.OnHide();
            StopAnimation();
            _isReconnecting = false;
        }

        [ContextMenu("Start Animation")]
        private void TestStartAnimation()
        {
            Show();
            SwitchToReconnection();
        }

        private void StartReconnectionAnimation()
        {
            if (_isReconnecting) return;

            _isReconnecting = true;

            _animationTweener = _spinnerObject.transform
                .DOLocalRotate(new Vector3(0, 0, _rotationAngle), _animationDuration)
                .SetEase(_ease)
                .SetLoops(-1, LoopType.Incremental);
        }

        private void StopAnimation()
        {
            _animationTweener?.Kill();
        }
    }
}