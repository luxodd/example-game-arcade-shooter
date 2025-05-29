using System;
using System.Collections.Generic;
using Core.Audio;
using DG.Tweening;
using Game.Common;
using Game.Events;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Unit;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class DamageablePartBehaviour : MonoBehaviour, IDamageableUnit
    {
        public IIntReadOnlyProperty HealthPoints => _damageableUnit.HealthPoints;
        public SimpleEvent<int> OnDamageEvent => _damageableUnit.OnDamageEvent;
        public SimpleEvent OnDeathEvent => _damageableUnit.OnDeathEvent;
        public bool IsDead => _damageableUnit.IsDead;
        public int MaxHealthPoints => _damageableUnit.MaxHealthPoints;
    
        [SerializeField] private int _defaultHealth = 100;

        [SerializeField] private Collider2D _collider2D;
        [SerializeField] private Rigidbody2D _rigidbody2D;
    
        [SerializeField] private List<ActivatorBehaviour> _activators = new List<ActivatorBehaviour>();
    
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _hitColor;
        [SerializeField] private Color _deathColor;
        [SerializeField] private ParticleSystem _hitParticles;
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private float _hitDuration = 0.5f;
        [SerializeField] private Ease _hitEase = Ease.OutBounce;
    
        private IDamageableUnit _damageableUnit;
    
        private Tweener _tweener;
    
        public void RegenerateHealthPoints()
        {
            _damageableUnit.RegenerateHealthPoints();
        }

        public void Heal(int amount)
        {
            _damageableUnit.Heal(amount);
        }

        private void PrepareDamageableUnit()
        {
            _damageableUnit = new VitalitySystem(_defaultHealth);
        }

        private void Awake()
        {
            PrepareDamageableUnit();
            SubscribeToEvents();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var damage = 0;
            if (collision.gameObject.TryGetComponent<ProjectileBehaviour>(out var projectile))
            {
                if (projectile.Owner == ProjectileOwner.Player)
                {
                    damage = projectile.Damage;
                    projectile.Deactivate();
                }
            }

            if (collision.gameObject.TryGetComponent<BigBossBombMissileBehaviour>(out var bombMissileBehaviour))
            {
                if (bombMissileBehaviour.IsActivated)
                {
                    damage = bombMissileBehaviour.Damage;
                    bombMissileBehaviour.BlowUp();
                }
            }
        
            if (damage == 0) return;
            var hitPoint = _collider2D.ClosestPoint(collision.transform.position);
        
            Hit(damage, hitPoint);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    
        private void SubscribeToEvents()
        {
            _damageableUnit.OnDeathEvent.AddListener(OnDeath);
        }

        private void UnsubscribeFromEvents()
        {
            _damageableUnit?.OnDeathEvent.RemoveListener(OnDeath);
        }

        private void Hit(int damage, Vector2 hitPoint)
        {
            var isDead = _damageableUnit.DealDamage(damage);
            var particle = Instantiate(_hitParticles, hitPoint, Quaternion.identity);
            particle.gameObject.SetActive(true);
            _tweener = _spriteRenderer.DOColor(_hitColor, _hitDuration)
                .SetEase(_hitEase)
                .OnComplete(OnDamageablePartChangeColorCompleteHandler);
            AudioManager.Instance.PlayMetalHitSound();
        }

        private void OnDamageablePartChangeColorCompleteHandler()
        {
            if (_damageableUnit.IsDead) return;
            _spriteRenderer.color = Color.white;
        }

        private void OnDeath()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDeath)}] OK");
        
            _tweener?.Kill();
            _tweener = null;
        
            CoroutineManager.DelayedAction(_hitDuration, () => _spriteRenderer.color = _deathColor);
        
            var explosion = Instantiate(_deathParticles, transform.position, Quaternion.identity);
            explosion.gameObject.SetActive(true);
        
            _collider2D.enabled = false;
            
            _activators.ForEach(activator => activator.Deactivate());
        
            Destroy(_rigidbody2D);
            
            EventAggregator.Post(this, new BossPartDestroyedEvent());
        }

        public bool DealDamage(int damage)
        {
            return false;
        }
    }
}