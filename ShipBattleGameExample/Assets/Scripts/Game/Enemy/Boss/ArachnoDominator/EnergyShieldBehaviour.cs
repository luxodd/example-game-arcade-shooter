using System;
using System.Collections;

using Core.Audio;
using DG.Tweening;
using Game.Weapons;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;
using Utils;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class EnergyShieldBehaviour : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _energyShieldRenderer;

        [SerializeField] private ParticleSystem _impactOnShieldParticles;

        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Collider2D _collider2D;

        [SerializeField] private float _damageDuration;
        [SerializeField] private float _destroyDuration;
        [SerializeField] private Ease _damageEase;
        [SerializeField] private Ease _destroyEase;
        [SerializeField] private float _fadeToValue;
        [SerializeField] private int _loopsCount;
        [SerializeField] private float _shieldRadius;

        [SerializeField] private Color _shieldColor;
        [SerializeField] private Color _damageColor;

        [SerializeField] private IntMiniMaxValue _particleAmount;
        [SerializeField] private FloatMiniMaxValue _durationAmount;

        private Action _onDeathAnimationComplete;

        public void DealDamageToShield()
        {
            StartShieldDamageAnimation();
        }

        public void DestroyShield(Action onComplete)
        {
            _onDeathAnimationComplete = onComplete;

            AudioManager.Instance.PlayMusic(MusicType.Boss1Transmission);
            
            StartShieldDamageAnimation();
            CoroutineManager.DelayedAction(_damageDuration, DestroyShieldInner);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<ProjectileBehaviour>(out var projectile) == false) return;
            if (projectile.Owner == ProjectileOwner.Enemy) return;

            var hitPoint = _collider2D.ClosestPoint(collision.transform.position);
            Instantiate(_impactOnShieldParticles, hitPoint, Quaternion.identity);
            projectile.Deactivate();
            AudioManager.Instance.PlayEnergyShieldImpactSound();
        }

        [ContextMenu("Start Shield Damage Animation")]
        private void StartShieldDamageAnimation()
        {
            _energyShieldRenderer.DOColor(_damageColor, _damageDuration)
                .SetEase(_damageEase)
                .SetLoops(_loopsCount, LoopType.Yoyo)
                .OnComplete(() => _energyShieldRenderer.color = _shieldColor);
        }

        private void StartShieldDestroyAnimation()
        {
            _energyShieldRenderer.DOFade(0f, _damageDuration)
                .SetEase(_destroyEase)
                .OnComplete(DestroyShieldComplete);
        }

        [ContextMenu("Destroy Shield")]
        private void DestroyShieldInner()
        {
            StartCoroutine(DestroyShieldCoroutine(StartShieldDestroyAnimation));
        }

        private void DestroyShieldComplete()
        {
            _collider2D.enabled = false;
            Destroy(_rigidbody2D);
            _energyShieldRenderer.gameObject.SetActive(false);

            _onDeathAnimationComplete?.Invoke();
        }

        private IEnumerator DestroyShieldCoroutine(Action onComplete)
        {
            var counter = 0;
            var totalAmount = _particleAmount.GetRandom();
            while (counter < totalAmount)
            {
                var randomPosition =
                    UnityEngine.Random.insideUnitCircle * _shieldRadius + (Vector2)(transform.position);
                Instantiate(_impactOnShieldParticles, randomPosition, Quaternion.identity);
                AudioManager.Instance.PlayEnergyShieldImpactSound();
                counter++;
                yield return new WaitForSeconds(_durationAmount.GetRandom());
            }

            onComplete?.Invoke();
        }
    }
}