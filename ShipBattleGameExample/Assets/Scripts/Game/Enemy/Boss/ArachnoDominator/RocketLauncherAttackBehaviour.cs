using System;
using System.Collections;
using Core.Audio;
using Game.Common;
using Game.Weapons;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class RocketLauncherAttackBehaviour : ActivatorBehaviour, ITargetable
    {
        [SerializeField] private Transform _launcherTransform;
        [SerializeField] private Transform _target;
        
        [SerializeField] private EnemyShipWeaponData _weaponData;
        [SerializeField] private TurretRotationBehaviour _turretRotationBehaviour;
    
        [SerializeField] private SpriteRenderer _rocketRenderer;
        [SerializeField] private ProjectileBehaviour _projectilePrefab;
        
        [SerializeField] private Collider2D _collider;
        
        private Transform _parentForProjectiles;
        
        private bool _isAttacking;
        private Coroutine _coroutine;
        private float _timeCounter;
        
        private ProjectileBehaviour _preparedToLaunchProjectile;

        public void SetParentForProjectiles(Transform parentForProjectiles)
        {
            _parentForProjectiles = parentForProjectiles;
        }

        public Transform Target { get; private set; }

        public void SetTarget(Transform target)
        {
            _target = target;
            Target = target;
            _turretRotationBehaviour.SetTarget(_target);
        }

        protected override void OnActivate()
        {
            if (_isAttacking) return;
            _isAttacking = true;
            
            _collider.enabled = true;
            _turretRotationBehaviour.Activate();

            _coroutine = StartCoroutine(StartAttacking());
        }

        protected override void OnDeactivate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDeactivate)}] OK");
            _isAttacking = false;
            
            _turretRotationBehaviour.Deactivate();
            
            if (_coroutine == null) return;
            
            StopCoroutine(_coroutine);
            
            _rocketRenderer.enabled = false;

            if (_preparedToLaunchProjectile != null)
            {
                Destroy(_preparedToLaunchProjectile.gameObject);
            }
        }

        [ContextMenu("Activate")]
        private void TestActivate()
        {
            Activate();
        }

        [ContextMenu("Deactivate")]
        private void TestDeactivate()
        {
            Deactivate();
        }

        private void Awake()
        {
            _rocketRenderer.gameObject.SetActive(false);
        }

        private IEnumerator StartAttacking()
        {
            while (_isAttacking)
            {
                if (_isActivated == false) yield break;
                
                _timeCounter = 0f;

                //cooldown
                var delayBefore = _weaponData.DelayBeforeAttack.GetRandom();
                while (_timeCounter < delayBefore)
                {
                    _timeCounter += Time.deltaTime;
                    yield return null;
                }
                
                //appearing
                _timeCounter = 0f;
                var appearingTime = _weaponData.FireRate;
                var startColor= new Color(1f, 1f, 1f, 0);
                var endColor= new Color(1f, 1f, 1f, 1);
                _rocketRenderer.color = startColor;
                _rocketRenderer.gameObject.SetActive(true);
                
                while (_timeCounter < appearingTime)
                {
                    _rocketRenderer.color = Color.Lerp(startColor, endColor, _timeCounter / appearingTime);
                    _timeCounter += Time.deltaTime;
                    yield return null;
                }
                
                //instantiate rocket
                var projectile = Instantiate(_projectilePrefab, _launcherTransform);
                projectile.transform.position = _launcherTransform.position;
                projectile.transform.rotation = _launcherTransform.rotation;

                _preparedToLaunchProjectile = projectile;
                
                //start flame
                var flameActivator = projectile.GetComponent<FlameActivatorBehaviour>();
                flameActivator.ActivateFlame();
                
                _rocketRenderer.gameObject.SetActive(false);
                
                var timeBeforeLaunching = _weaponData.DelayBetweenAttacks.GetRandom();
                _timeCounter = 0f;
                
                while (_timeCounter < timeBeforeLaunching)
                {
                    if (_turretRotationBehaviour.IsRotationReady == false)
                    {
                        yield return null;
                    }
                    
                    _timeCounter += Time.deltaTime;
                    yield return null;
                }

                if (_isActivated == false)
                {
                    flameActivator.DeactivateFlame();
                    GameObject.Destroy(projectile.gameObject);
                    yield break;
                }
                
                projectile.transform.SetParent(_parentForProjectiles);
                //launching
                var direction = (_target.position - projectile.transform.position).normalized;
                projectile.SetProjectileData(ProjectileOwner.Enemy, _weaponData.Damage, _weaponData.ProjectileSpeed,
                    _weaponData.AutoDestroyTime, _launcherTransform.position, _launcherTransform.rotation,
                    direction);
                
                //play launcher sound
                AudioManager.Instance.PlayRocketLauncherSound();
                _preparedToLaunchProjectile = null;
                yield return null;
            }
        }
    }
}
