using System;
using Core.UI;
using DG.Tweening;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using TMPro;
using UnityEngine;

namespace Game.UI.Views
{
    public interface ICreditsWidgetView : IView
    {
        void SetCreditsCount(int creditsCount);
        void AddAnimatedCredits(int currentCredits, int creditsToAdd, Action onDone);
        void RemoveAnimatedCredits(int currentCredits, int creditsToRemove, Action onDone);
    }
    
    public class CreditsWidgetView : BaseView, ICreditsWidgetView
    {
        public override ViewType ViewType => ViewType.CreditsWidget;
        
        [SerializeField] private GameObject _creditsWidgetParent;
        [SerializeField] private TMP_Text _creditsText;
        
        [SerializeField] private Transform _startAppearPoint;
        [SerializeField] private Transform _endAppearPoint;
        
        [SerializeField] private float _appearAnimationDuration = 0.5f;
        [SerializeField] private float _disappearAnimationDuration = 0.5f;
        
        [SerializeField] private float _creditsAddAnimationDuration = 0.5f;
        [SerializeField] private float _creditsRemoveAnimationDuration = 0.5f;

        [SerializeField] private Ease _appearAnimationEase = Ease.OutQuad;
        [SerializeField] private Ease _disappearAnimationEase = Ease.InQuad;
        
        [SerializeField] private Ease _creditsAddAnimationEase = Ease.OutQuad;
        [SerializeField] private Ease _creditsRemoveAnimationEase = Ease.InQuad;

        [SerializeField] private float _strength;
        [SerializeField] private float _randomness;
        [SerializeField] private int _vibrato;
        
        [SerializeField] private float _strengthToRemove;
        [SerializeField] private float _randomnessToRemove;
        [SerializeField] private int _vibratoToRemove;
        
        [SerializeField] private Color _addCreditsColor;
        [SerializeField] private Color _removeCreditsColor;
        
        private int _creditsToAddCount;
        private int _currentCreditsCount;
        private int _creditsToRemoveCount;
        
        private Color _currentCreditsColor;
        
        private Action _onDone;
        
        public void SetCreditsCount(int creditsCount)
        {
            _currentCreditsCount = creditsCount;
            _creditsText.text = _currentCreditsCount.ToString();
        }

        public void AddAnimatedCredits(int currentCredits, int creditsToAdd, Action onDone)
        {
            _currentCreditsCount = currentCredits;
            _onDone = onDone;
            _creditsToAddCount = creditsToAdd;
            StartCreditsAddAnimation();
        }

        public void RemoveAnimatedCredits(int currentCredits, int creditsToRemove, Action onDone)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(RemoveAnimatedCredits)}] OK, " +
                      $"newCreditsCount = {currentCredits - creditsToRemove}, _creditsToRemoveCount = {creditsToRemove}");
            _currentCreditsCount = currentCredits;
            _onDone = onDone;
            _creditsToRemoveCount = creditsToRemove;
            StartCreditsRemoveAnimation();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _creditsWidgetParent.SetActive(false);
            _creditsWidgetParent.transform.localPosition = _startAppearPoint.localPosition;
        }

        protected override void OnShow()
        {
            base.OnShow();
            StartAppearAnimation();
        }

        protected override void OnBeforeHide(Action onDone)
        {
            StartDisappearAnimation(()=> base.OnBeforeHide(onDone));
        }

        protected override void OnHide()
        {
            base.OnHide();
            _creditsText.color = Color.white;
        }

        [ContextMenu("Start Appear Animation")]
        private void TestAppearAnimation()
        {
            StartAppearAnimation();
        }

        [ContextMenu("Start Disappear Animation")]
        private void TestDisappearAnimation()
        {
            StartDisappearAnimation(null);
        }

        [ContextMenu("Start Credits Add Animation")]
        private void TestCreditsAddAnimation()
        {
            var credits = UnityEngine.Random.Range(5, 100);
            var creditsToAdd = UnityEngine.Random.Range(1, 50);
            AddAnimatedCredits(credits, creditsToAdd, null);
        }

        [ContextMenu("Start Credits Remove Animation")]
        private void TestCreditsRemoveAnimation()
        {
            var credits = UnityEngine.Random.Range(5, 100);
            var creditsToRemove = UnityEngine.Random.Range(1, credits);
            RemoveAnimatedCredits(credits, creditsToRemove, null);
        }

        private void StartAppearAnimation()
        {
            _creditsWidgetParent.SetActive(true);
            _creditsWidgetParent.transform.DOLocalMove(_endAppearPoint.localPosition, _appearAnimationDuration)
                .SetEase(_appearAnimationEase)
                .OnComplete(OnAppearAnimationComplete);
        }

        private void OnAppearAnimationComplete()
        {
            _creditsWidgetParent.transform.localPosition = _endAppearPoint.localPosition;
        }

        private void StartDisappearAnimation(Action onDone)
        {
            _creditsWidgetParent.transform.DOLocalMove(_startAppearPoint.localPosition, _disappearAnimationDuration)
                .SetEase(_disappearAnimationEase)
                .OnComplete(()=>
                {
                    onDone?.Invoke();
                    OnDisappearAnimationComplete();
                });
        }

        private void OnDisappearAnimationComplete()
        {
            _creditsWidgetParent.transform.localPosition = _startAppearPoint.localPosition;
        }

        private void StartCreditsAddAnimation()
        {
            var newCreditsCount = _currentCreditsCount + _creditsToAddCount;
            DOTween.To(()=> _currentCreditsCount, (x)=> _currentCreditsCount = x, newCreditsCount, _creditsAddAnimationDuration)
                .SetEase(_creditsAddAnimationEase)
                .OnUpdate(OnCreditsAddAnimationUpdate)
                .OnComplete(OnCreditsAddAnimationComplete);
            _creditsText.transform.DOShakeScale(_creditsAddAnimationDuration, _strength, _vibrato, _randomness);
            _currentCreditsColor = _creditsText.color;
            _creditsText.color = _addCreditsColor;
        }

        private void OnCreditsAddAnimationUpdate()
        {
            SetCreditsCount(_currentCreditsCount);
        }

        private void OnCreditsAddAnimationComplete()
        {
            _creditsText.color  = Color.white;
            _onDone?.Invoke();
        }

        private void StartCreditsRemoveAnimation()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartCreditsRemoveAnimation)}] OK, " +
                      $"newCreditsCount = {_currentCreditsCount - _creditsToRemoveCount}, _creditsToRemoveCount = {_creditsToRemoveCount}");
            
            var newCreditsCount = _currentCreditsCount - _creditsToRemoveCount;
            DOTween.To(()=> _currentCreditsCount, (x)=> _currentCreditsCount = x, newCreditsCount, _creditsRemoveAnimationDuration)
                .SetEase(_creditsRemoveAnimationEase)
                .OnUpdate(OnCreditsAddAnimationUpdate)
                .OnComplete(OnCreditsRemoveAnimationComplete);
            
            _creditsText.transform.DOShakeScale(_creditsRemoveAnimationDuration, _strengthToRemove, _vibratoToRemove, _randomnessToRemove);
            _currentCreditsColor = _creditsText.color;
            _creditsText.color = _removeCreditsColor;
        }

        private void OnCreditsRemoveAnimationComplete()
        {
            _creditsText.color  = Color.white;
            _onDone?.Invoke();
        }
    }
}
