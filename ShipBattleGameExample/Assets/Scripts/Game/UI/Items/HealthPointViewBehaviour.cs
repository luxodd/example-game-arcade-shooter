using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Events;
using Game.PlayerShip;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Game.UI.Items
{
    public class HealthPointViewBehaviour : MonoBehaviour
    {
        [SerializeField] private List<Image> _healthPointSegments = new List<Image>();
        [SerializeField] private Gradient _healthPointGradient = new Gradient();
        
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private Ease _ease = Ease.InOutQuad;
        
        //for test
        [SerializeField] private FloatMiniMaxValue _delayBetweenShots = new FloatMiniMaxValue();
        [SerializeField] private IntMiniMaxValue _numberOfShots = new IntMiniMaxValue();
        [SerializeField] private IntMiniMaxValue _maxHealthPoints = new IntMiniMaxValue();
        
        private int _maximumHP;
        private int _currentHP;
        
        private int _segmentsCount;
        private int _currentSegment;
        
        private float _segmentSize;

        public void Setup(int maximumHP)
        {
            _maximumHP = maximumHP;
            _currentHP = maximumHP;
            _segmentsCount = _healthPointSegments.Count;
            _segmentSize = _maximumHP /(float) _segmentsCount;
            ResetSegments();
        }

        public void SetHP(int currentHP)
        {
            _currentHP = currentHP;
            UpdateSegments();
        }

        public void RecoveryHealthPoints(int currentHP)
        {
            _currentHP = currentHP;
            ResetSegmentsAnimated();
            CoroutineManager.DelayedAction(_animationDuration * _segmentsCount * 0.5f, UpdateSegments);
        }

        public void ResetSegments()
        {
            foreach (var segment in _healthPointSegments)
            {
                segment.color = _healthPointGradient.Evaluate(1f);
                segment.fillAmount = 1f;
            }
        }

        private void ResetSegmentsAnimated()
        {
            for (int i = 0; i < _healthPointSegments.Count; i++)
            {
                var segment = _healthPointSegments[i];
                CoroutineManager.DelayedAction(i * _animationDuration * 0.5f,
                    () => StartAnimateSegment(segment, _healthPointGradient.Evaluate(1f), 1f));
            }
        }

        private void Awake()
        {
            _segmentsCount = _healthPointSegments.Count;
            ResetSegments();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            //EventAggregator.Subscribe<PlayerShipStateChangeEvent>(OnPlayerShipStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            //EventAggregator.Unsubscribe<PlayerShipStateChangeEvent>(OnPlayerShipStateChanged);
        }
        
        private void UpdateSegments()
        {
            _currentSegment = (int)(_currentHP / _segmentSize);
            var resultOfDivide = _currentHP / _segmentSize;
            var progress = resultOfDivide - _currentSegment;
            var color = _healthPointGradient.Evaluate(progress);
            if (_currentSegment >= _segmentsCount) return;
            
            var image = _healthPointSegments[_currentSegment];
            // LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(UpdateSegments)}] OK, currentSegment: " +
            //           $"{_currentSegment}, color: {color}, progress: {progress}, resultOfModularDivide: {resultOfDivide}, currentHP: {_currentHP}");
            StartAnimateSegment(image, color, progress);
            UpdateOthersSegments(_currentSegment);
        }

        private void StartAnimateSegment(Image segment, Color color, float progress)
        {
            segment.DOKill();
            segment.DOColor(color, _animationDuration)
                .SetEase(_ease);
            DOTween.To(() => segment.fillAmount, x => segment.fillAmount = x, progress, _animationDuration)
                .SetEase(_ease);
        }

        private void UpdateOthersSegments(int currentSegment)
        {
            if (currentSegment + 1 >= _segmentsCount) return;

            for (int i = currentSegment + 1; i < _healthPointSegments.Count; i++)
            {
                var segment = _healthPointSegments[i];
                if (segment.fillAmount == 0f) continue;
                
                CoroutineManager.DelayedAction(_delayBetweenShots.GetRandom(),
                    () => StartAnimateSegment(segment, _healthPointGradient.Evaluate(0f), 0f));
            }
        }

        [ContextMenu("Update Segments")]
        private void TestAnimateSegment()
        {
            // var value = UnityEngine.Random.value;
            // var color = _healthPointGradient.Evaluate(value);
            // var index = UnityEngine.Random.Range(0, _healthPointSegments.Count);
            // var image = _healthPointSegments[index];
            // LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestAnimateSegment)}] OK, value: {value}, color: {color}, index: {index}");
            // StartAnimateSegment(image, color, value);
            //ResetSegments();
            
            var maxHP = UnityEngine.Random.Range(_healthPointSegments.Count, _healthPointSegments.Count*10);
            Setup(maxHP);
            var currentHP = UnityEngine.Random.Range(0, maxHP);
            SetHP(currentHP);
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestAnimateSegment)}] OK, maxHP = {maxHP}, currentHP = {currentHP}");
        }

        [ContextMenu("Double Hit")]
        private void TestDoubleHitAnimation()
        {
            var maxHealthPoint = _maxHealthPoints.GetRandom();
            Setup(maxHealthPoint);
            
            var hitAmount = _numberOfShots.GetRandom();
            var hpToReduceMax = maxHealthPoint/hitAmount;
            var currentHealthPoint = maxHealthPoint;

            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestDoubleHitAnimation)}] OK, " +
                      $"maxHP:{maxHealthPoint}, hitAmount:{hitAmount}, hpToReduceMax:{hpToReduceMax}");
            
            for (int i = 0; i < hitAmount; i++)
            {
                var hpToReduce = UnityEngine.Random.Range(1, hpToReduceMax+1);
                currentHealthPoint -= hpToReduce;
                var delay = _delayBetweenShots.GetRandom();
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(TestDoubleHitAnimation)}] OK, " +
                          $"currentHealthPoint:{currentHealthPoint}, delay:{delay}");
                var point = currentHealthPoint;
                CoroutineManager.DelayedAction(delay, () => SetHP(point));
            }
        }

        [ContextMenu("Reset Segments")]
        private void TestResetSegment()
        {
            foreach (var segment in _healthPointSegments)
            {
                segment.color = _healthPointGradient.Evaluate(0f);
                segment.fillAmount = 0f;
            }
            ResetSegmentsAnimated();
        }

        private void OnPlayerShipStateChanged(object sender, PlayerShipStateChangeEvent eventData)
        {
            if (eventData.State != PlayerShipState.Invulnerable) return;
            
            ResetSegmentsAnimated();
        }
    }
}
