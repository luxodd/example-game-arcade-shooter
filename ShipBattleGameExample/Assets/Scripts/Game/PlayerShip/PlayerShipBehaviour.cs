using System;
using System.Collections;
using System.Collections.Generic;
using Game.Bonuses;
using Game.CameraInner;
using Game.Enemy;
using Game.Events;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Unit;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.PlayerShip
{
    public enum PlayerShipState
    {
        Normal,
        Dead,
        Invulnerable,
    }

    public class PlayerShipBehaviour : MonoBehaviour
    {
        public ISimpleEvent<int> PlayerShipLivesChanged => _playerShipLivesChanged;
        public IIntReadOnlyProperty PlayerHealthPoints => _damageableUnit.HealthPoints;
        public int MaxPlayerHealthPoints => _damageableUnit.MaxHealthPoints;

        [SerializeField] private PlayerShipDescriptor _playerShipDescriptor;
        [SerializeField] private PlayerShipMovementBehaviour _playerShipMovementBehaviour;
        [SerializeField] private PlayerAttackBehaviour _playerAttackBehaviour;

        [SerializeField] private PlayerShipState _playerShipState;

        [SerializeField] private SpriteRenderer _playerShip;
        [SerializeField] private SpriteRenderer _shadow;

        [SerializeField] private List<SpriteRenderer> _parts = new List<SpriteRenderer>();

        [SerializeField] private Collider2D _playerShipCollider;

        [SerializeField] private ParticleSystem _explodeParticles;
        [SerializeField] private ParticleSystem _sparkEffect;

        private IDamageableUnit _damageableUnit;

        private SimpleEvent<int> _playerShipLivesChanged = new SimpleEvent<int>();

        private int _playerLives;

        private bool _isInvulnerable => _playerShipState == PlayerShipState.Invulnerable;

        private bool _isDead => _playerShipState == PlayerShipState.Dead;

        private bool _isCanMove => _playerShipState is PlayerShipState.Invulnerable or PlayerShipState.Normal;

        private SimpleEvent<PlayerShipState> _onPlayerShipStateChangeEvent = new SimpleEvent<PlayerShipState>();

        private Coroutine _invulnerableCoroutine;

        private PlayerShipStateChangeEvent _stateChangeEvent = new PlayerShipStateChangeEvent();

        private PlayerWeaponService _primaryWeaponService;

        public void ProvideDependencies(IControlAdapter controlAdapter, PlayerWeaponService primaryWeaponService,
            float defaultMovementSpeed)
        {
            _primaryWeaponService = primaryWeaponService;
            _playerShipMovementBehaviour.ProvideDependencies(controlAdapter, _playerShipDescriptor,
                _onPlayerShipStateChangeEvent, defaultMovementSpeed);
            _playerAttackBehaviour.ProvideDependencies(primaryWeaponService, controlAdapter,
                _onPlayerShipStateChangeEvent);
        }

        public void SetMovementBounds(Transform leftBound, Transform rightBound, ICameraBounds cameraBounds)
        {
            _playerShipMovementBehaviour.SetMovementBounds(leftBound, rightBound, cameraBounds);
        }

        public void SetStartPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetLevelSpeed(float speed)
        {
            _playerShipMovementBehaviour.SetLevelSpeed(speed);
        }

        public void AddPlayedDeathListener(Action action)
        {
            _damageableUnit.OnDeathEvent.AddListener(action);
        }

        public void RemovePlayedDeathListener(Action action)
        {
            _damageableUnit.OnDeathEvent.RemoveListener(action);
        }

        public void Activate()
        {
            SwitchState(PlayerShipState.Invulnerable);

            _playerShipMovementBehaviour.ActivateMovement();

            ResetPlayerShip();
        }

        public void Deactivate()
        {
            _playerShipMovementBehaviour.DeactivateMovement();
        }

        public void ContinueMovement()
        {
            _playerShipMovementBehaviour.ActivateMovement();
        }

        public void LaunchBigBomb(Transform target)
        {
            _playerAttackBehaviour.LaunchBigBossBomb(target);
        }

        #region Unity Events

        private void Awake()
        {
            ResetPlayerShip();
            PrepareDamageableUnit();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isDead) return;

            if (collision.gameObject.TryGetComponent<BonusBehaviour>(out var bonusBehaviour))
            {
                LoggerHelper.Log(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(OnTriggerEnter2D)}] OK, {collision.gameObject.name}");
                var bonusType = bonusBehaviour.Bonus;
                var bonusValue = bonusBehaviour.BonusValue;
                bonusBehaviour.Deactivate();
                ApplyBonus(bonusType, bonusValue);

                return;
            }

            if (_isInvulnerable) return;

            if (collision.gameObject.TryGetComponent<EnemyBaseBehaviour>(out var enemyBaseBehaviour))
            {
                if (enemyBaseBehaviour.Type == EnemyType.DroneX4 || enemyBaseBehaviour.Type == EnemyType.DroneX3)
                {
                    enemyBaseBehaviour.DestroyEnemy();
                    _damageableUnit.DealDamage(enemyBaseBehaviour.DamageOnDestroy);
                    return;
                }
            }

            if (collision.gameObject.TryGetComponent(out ProjectileBehaviour projectileBehaviour) == false) return;

            if (projectileBehaviour.Owner == ProjectileOwner.Player) return;

            projectileBehaviour.Deactivate();

            var impactParticle = projectileBehaviour.Impact == ProjectileHitImpact.Explode
                ? _explodeParticles
                : _sparkEffect;

            var hitParticlePosition = _playerShipCollider.ClosestPoint(collision.transform.position);
            var particle = Instantiate(impactParticle, hitParticlePosition, Quaternion.identity);
            particle.gameObject.SetActive(true);

            _damageableUnit.DealDamage(projectileBehaviour.Damage);
        }

        #endregion

        private void UpdateState()
        {
            switch (_playerShipState)
            {
                case PlayerShipState.Normal:
                    OnNormalState();
                    //normal behaviour
                    break;
                case PlayerShipState.Dead:
                    OnDeadState();
                    //display die particle
                    break;
                case PlayerShipState.Invulnerable:
                    OnInvulnerableState();
                    //start invulnerability
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SwitchState(PlayerShipState newState, bool force = false)
        {
            LoggerHelper.Log(
                $"[{GetType().Name}][{nameof(SwitchState)}] OK, old:{_playerShipState}, switching to {newState}");
            if (_playerShipState == newState && force == false) return;

            _playerShipState = newState;

            _stateChangeEvent.State = newState;

            _onPlayerShipStateChangeEvent.Notify(_playerShipState);
            EventAggregator.Post(this, _stateChangeEvent);
            UpdateState();
        }

        private void OnNormalState()
        {

        }

        private void OnInvulnerableState()
        {
            _damageableUnit.RegenerateHealthPoints();
            _invulnerableCoroutine = StartCoroutine(StartInvulnerability());
            CoroutineManager.DelayedAction(_playerShipDescriptor.InvulnerabilityDuration, StopInvulnerability);
        }

        private void OnDeadState()
        {
            _playerShipCollider.enabled = false;
            StartDeathEffect();
        }

        private IEnumerator StartInvulnerability()
        {
            var endTime = Time.time + _playerShipDescriptor.InvulnerabilityDuration;
            var visible = true;
            while (endTime > Time.time)
            {
                _parts.ForEach(spriteRenderer => spriteRenderer.enabled = !visible);
                visible = !visible;
                yield return new WaitForSeconds(_playerShipDescriptor.InvisibleTime);
            }
        }

        private void StopInvulnerability()
        {
            StopCoroutine(_invulnerableCoroutine);
            _playerShipCollider.enabled = true;
            _parts.ForEach(partSprite => partSprite.enabled = true);

            SwitchState(PlayerShipState.Normal);
        }

        private void ResetPlayerShip()
        {
            _playerLives = _playerShipDescriptor.DefaultPlayerShipLives;
            _playerShipLivesChanged.Notify(_playerLives);
        }

        private void OnPlayerShipDeath()
        {
            _playerLives--;
            SwitchState(PlayerShipState.Dead);
            _playerShipLivesChanged.Notify(_playerLives);
            _primaryWeaponService.ResetLevel();
        }

        private void StartDeathEffect()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartDeathEffect)}] OK");

            _parts.ForEach(partSprite => partSprite.enabled = false);
            _explodeParticles.gameObject.SetActive(true);
            _explodeParticles.Play();

            CoroutineManager.DelayedAction(_playerShipDescriptor.DieDuration, StopDeathEffect);
        }

        private void StopDeathEffect()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(StopDeathEffect)}] OK, player lives: {_playerLives}");
            if (_playerLives >= 0)
            {
                SwitchState(PlayerShipState.Invulnerable);
            }
            else
            {
                //nothing
            }

        }

        private void PrepareDamageableUnit()
        {
            _damageableUnit = new VitalitySystem(_playerShipDescriptor.DefaultPlayerShipHP);
            _damageableUnit.OnDeathEvent.AddListener(OnPlayerShipDeath);
        }

        private void ApplyBonus(BonusType bonusType, int bonusValue = 0)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ApplyBonus)}] OK, bonus type: {bonusType}");
            if (bonusType == BonusType.WeaponPrimary)
            {
                _primaryWeaponService.UpgradeLevel();
            }
            else if (bonusType == BonusType.RecoveryHealth)
            {
                _damageableUnit.Heal(bonusValue);
            }
        }
    }
}