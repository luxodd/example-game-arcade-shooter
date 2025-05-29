using System.Collections;
using System.Collections.Generic;
using Core.Audio;
using Game.Common;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class TurretAttackBehaviour : ActivatorBehaviour, ITargetable
    {
        [SerializeField] private Transform _target;
        [SerializeField] private EnemyShipWeaponData _weaponData;
        [SerializeField] private TurretRotationBehaviour _turretRotationBehaviour;
        
        [SerializeField] private ProjectileBehaviour _projectilePrefab;
        
        [SerializeField] private Collider2D _collider;
        
        [SerializeField] private List<Transform> _projectileSpawnPoints = new List<Transform>();

        [SerializeField] private SfxType _turretSound;
        
        private Coroutine _coroutine;
        private bool _isAttacking;
        private Transform _parentForProjectiles;
        
        public void SetParentForProjectiles(Transform parentForProjectiles)
        {
            _parentForProjectiles = parentForProjectiles;
        }

        public Transform Target { get;  private set; }

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
            _isAttacking = false;
            
            _turretRotationBehaviour.Deactivate();
            
            if (_coroutine == null) return;
            
            StopCoroutine(_coroutine);
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

        private IEnumerator StartAttacking()
        {
            yield return new WaitForSeconds(_weaponData.DelayBeforeAttack.GetRandom());
            while (_isAttacking)
            {
                if (_turretRotationBehaviour.IsRotationReady == false)
                {
                    yield return null;
                }
                var count = 0;
                while (count < _weaponData.QuantityPerTime)
                {
                    var spawnPoint = GetSpawnPointForProjectiles(count);
                    var projectile = Instantiate(_projectilePrefab, _parentForProjectiles);
                    projectile.transform.position = spawnPoint.position;
                    projectile.transform.rotation = spawnPoint.rotation;

                    var direction = spawnPoint.up*(-1);//(_target.position - projectile.transform.position).normalized;
                    projectile.SetProjectileData(ProjectileOwner.Enemy, _weaponData.Damage, _weaponData.ProjectileSpeed,
                        _weaponData.AutoDestroyTime, spawnPoint.position, spawnPoint.rotation,
                        direction);
                    count++;
                    
                    AudioManager.Instance.PlaySfx(_turretSound);
                    
                    yield return new WaitForSeconds(_weaponData.FireRate);
                }

                yield return new WaitForSeconds(_weaponData.DelayBetweenAttacks.GetRandom());
            }
        }

        private Transform GetSpawnPointForProjectiles(int counter)
        {
            var index = counter % _projectileSpawnPoints.Count;
            return _projectileSpawnPoints[index];
        }
    }
}
