using System;
using Core.Audio;
using DG.Tweening;
using Game.CameraInner;
using Game.Events;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Unit;
using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Enemy
{
    public enum EnemyShipState
    {
        None = 0,
        Activating,
        MovingAndAttacking,
        Deactivating,
        Destroying,
    }

    public class EnemyBaseBehaviour : MonoBehaviour
    {
        public ISimpleEvent<EnemyBaseBehaviour> DestroyedEvent => _destroyedEvent;

        public int Score => _scoreCalculationBehaviour.Score;
        public int DamageOnDestroy => _shipData.DamageOnDestroy;
        public EnemyType Type => _shipData.Type;
        public bool IsActivateByActivator => _shouldActivateByActivator;

        [SerializeField] private EnemyShipData _shipData;

        [SerializeField] private EnemyBaseMovementBehaviour _movementBehaviour;
        [SerializeField] protected EnemyBaseAttackBehaviour _attackBehaviour;

        [SerializeField] private EnemyScoreCalculationBehaviour _scoreCalculationBehaviour;

        [SerializeField] private bool _shouldToDeactivateAfterMovement = true;
        [SerializeField] private bool _shouldActivateByActivator = false;

        [SerializeField] protected ParticleSystem _explodeParticles;
        [SerializeField] protected ParticleSystem _sparkParticles;

        [SerializeField] protected Collider2D _enemyShipCollider2D;

        //for test
        [SerializeField] private EnemyShipWeaponData _weaponData;
        [SerializeField] private ProjectileBehaviour _projectilePrefab;
        [SerializeField] private Transform _parentForProjectiles;

        [SerializeField] private SpriteRenderer _shipSpriteRenderer;
        [SerializeField] private float _animationDuration;
        [SerializeField] private Ease _ease;
        [SerializeField] private Color _hitColor;

        protected IDamageableUnit _damageableUnit;

        private EnemyShipState _shipState;
        private readonly SimpleEvent<EnemyBaseBehaviour> _destroyedEvent = new SimpleEvent<EnemyBaseBehaviour>();

        private bool _isDestroyed = false;
        private Tweener _tweener;

        private bool _wasActivated = false;

        private bool _shouldBlowupOnDestroy = true;

        public void ProvideDependencies(Transform parentForProjectiles)
        {
            _attackBehaviour.ProvideDependencies(_weaponData, _projectilePrefab, parentForProjectiles);
        }

        public void SetTarget(Transform target)
        {
            _attackBehaviour.SetTarget(target);
        }

        public void AddShipDeathListener(System.Action callback)
        {
            _damageableUnit.OnDeathEvent.AddListener(callback);
        }

        public void RemoveShipDeathListener(System.Action callback)
        {
            _damageableUnit.OnDeathEvent.RemoveListener(callback);
        }

        public void Activate()
        {
            _wasActivated = true;
            SwitchStateTo(EnemyShipState.Activating);
        }

        public void Deactivate()
        {
            _attackBehaviour.Deactivate();
            _movementBehaviour.Deactivate();
            _scoreCalculationBehaviour.Pause();

            SwitchStateTo(EnemyShipState.None);
        }

        public void DestroyEnemy(bool withBlowup = true)
        {
            _shouldBlowupOnDestroy = withBlowup;
            SwitchStateTo(EnemyShipState.Deactivating);
        }

        public void DealDamage(int damage)
        {
            DealDamageInner(damage);
        }

        public void ContinueGameplay()
        {
            if (_wasActivated == false) return;

            _attackBehaviour.Activate();
            _movementBehaviour.Continue();

            _scoreCalculationBehaviour.Resume();
            SwitchStateTo(EnemyShipState.Activating);
        }

        [ContextMenu("Test Activating")]
        private void TestActivate()
        {
            ProvideDependencies(_parentForProjectiles);
            Activate();
        }

        #region Unity Events

        private void Awake()
        {
            PrepareDamageableUnit();
            _movementBehaviour.MovementCompletedEvent.AddListener(OnMovementEndHandler);
            OnAwake();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _damageableUnit?.OnDeathEvent.RemoveAllListeners();
            _damageableUnit?.OnDamageEvent.RemoveAllListeners();
            _destroyedEvent.Notify(this);

            _destroyedEvent.RemoveAllListeners();
            OnDestroyInner();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.TryGetComponent<CameraActivatorBehaviour>(out var cameraActivatorBehaviour))
            {
                if (cameraActivatorBehaviour.IsActivate && _shouldActivateByActivator)
                {
                    Activate();
                    return;
                }

                if (cameraActivatorBehaviour.IsActivate == false &&
                    cameraActivatorBehaviour.ActivatorType == ActivatorType.Secondary)
                {
                    DestroyEnemy(false);
                }
            }

            if (other.gameObject.TryGetComponent<ProjectileBehaviour>(out var projectileBehaviour) == false) return;

            if (_shipState != EnemyShipState.MovingAndAttacking) return;

            if (projectileBehaviour.Owner == ProjectileOwner.Enemy || projectileBehaviour.Owner == ProjectileOwner.None)
            {
                return;
            }

            projectileBehaviour.Deactivate();

            var damage = projectileBehaviour.Damage;

            DealDamageInner(damage);
            ShowHitEffect(other.transform.position);
        }

        #endregion

        private void DealDamageInner(int damage)
        {
            _damageableUnit.DealDamage(damage);
            AudioManager.Instance.PlayMetalHitSound();
            EventAggregator.Post(this, new EnemyHitEvent() { Score = _shipData.ScorePointsByHit });
        }

        private void ShowHitEffect(Vector3 position)
        {
            var sparkPosition = _enemyShipCollider2D.ClosestPoint(position);
            Instantiate(_sparkParticles, sparkPosition, Quaternion.identity);
        }

        private void PrepareDamageableUnit()
        {
            _damageableUnit = new VitalitySystem(_shipData.HealthPoints);
            _damageableUnit.OnDeathEvent.AddListener(OnEnemyDeath);
            _damageableUnit.OnDamageEvent.AddListener(OnEnemyTakeDamage);
        }

        private void OnEnemyDeath()
        {
            _isDestroyed = true;
            _shipSpriteRenderer = null;

            _tweener?.Kill();
            _tweener = null;
            SwitchStateTo(EnemyShipState.Destroying);
            EventAggregator.Post(this, new EnemyDeathEvent() { Score = Score });
        }

        private void OnEnemyTakeDamage(int damage)
        {
            if (_isDestroyed) return;

            if (_shipSpriteRenderer == null) return;

            _tweener = _shipSpriteRenderer.DOColor(_hitColor, _animationDuration)
                .SetEase(_ease)
                .OnComplete(OnEnemyChangeColorCompleteHandler);
        }

        private void OnEnemyChangeColorCompleteHandler()
        {
            if (_isDestroyed == false) _shipSpriteRenderer.color = Color.white;
        }

        private void SwitchStateTo(EnemyShipState newState)
        {
            if (_shipState == newState) return;

            _shipState = newState;
            UpdateState();
        }

        private void UpdateState()
        {
            switch (_shipState)
            {
                case EnemyShipState.Activating:
                    OnActivatingHandler();
                    break;
                case EnemyShipState.MovingAndAttacking:
                    OnMovingAndAttackingHandler();
                    break;
                case EnemyShipState.Deactivating:
                    OnDeactivatingHandler();
                    break;
                case EnemyShipState.Destroying:
                    OnDestroyHandler();
                    break;
                case EnemyShipState.None:
                    //do nothing
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnActivatingHandler()
        {
            _movementBehaviour.ProvideDependencies(_shipData);
            _scoreCalculationBehaviour.ProvideDependencies(_shipData);
            _attackBehaviour.Activate();
            _scoreCalculationBehaviour.Activate();
            OnActivateInner();
            SwitchStateTo(EnemyShipState.MovingAndAttacking);
        }

        private void OnMovingAndAttackingHandler()
        {
            _movementBehaviour.Activate();
        }

        private void OnDeactivatingHandler()
        {
            _movementBehaviour.Deactivate();
            _attackBehaviour.Deactivate();
            SwitchStateTo(EnemyShipState.Destroying);
        }

        private void OnDestroyHandler()
        {
            _movementBehaviour.Deactivate();
            _attackBehaviour.Deactivate();
            _scoreCalculationBehaviour.Deactivate();

            ShowExplosionParticle();

            OnDestroyStateHandler();
        }

        private void ShowExplosionParticle()
        {
            if (_shouldBlowupOnDestroy == false) return;
            var explosion = Instantiate(_explodeParticles);
            explosion.transform.position = transform.position;
            explosion.gameObject.SetActive(true);
            explosion.Play();
        }

        private void OnMovementEndHandler()
        {
            if (_shouldToDeactivateAfterMovement == false) return;

            SwitchStateTo(EnemyShipState.Deactivating);
        }

        protected virtual void OnDestroyStateHandler()
        {
        }

        protected virtual void OnAwake()
        {
        }

        protected virtual void OnDestroyInner()
        {
        }

        protected virtual void OnActivateInner()
        {
        }

        protected void SetShipSpriteRenderer(SpriteRenderer renderer)
        {
            _shipSpriteRenderer = renderer;
        }

        protected void SetMainCollider(Collider2D collider)
        {
            _enemyShipCollider2D = collider;
        }
    }
}