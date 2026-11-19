using System.Collections;
using System.Collections.Generic;
using Core.Audio;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyBaseAttackBehaviour : MonoBehaviour
    {
        [SerializeField] private List<Transform> _projectileSpawnPoints;

        [SerializeField] private Transform _target;

        [SerializeField] private SfxType _gunSound;

        private Coroutine _coroutine;

        private EnemyShipWeaponData _weaponData;
        private ProjectileBehaviour _projectilePrefab;

        private Transform _parentForProjectiles;
        protected bool IsCanAttack = true;

        protected Transform AttackTarget => _target;

        private Transform _projectileSpawnPoint => _projectileSpawnPoints[0];

        private bool _isAttacking;

        public void ProvideDependencies(EnemyShipWeaponData weaponData, ProjectileBehaviour projectilePrefab,
            Transform parentForProjectiles)
        {
            _weaponData = weaponData;
            _projectilePrefab = projectilePrefab;
            _parentForProjectiles = parentForProjectiles;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void Activate()
        {
            if (_isAttacking) return;

            _coroutine = StartCoroutine(StartAttack());
            OnActivate();
        }

        public void Deactivate()
        {
            _isAttacking = false;
            if (_coroutine == null) return;
            StopCoroutine(_coroutine);
            OnDeactivate();
        }

        private IEnumerator StartAttack()
        {
            _isAttacking = true;

            // An enemy can be activated before ProvideDependencies (seen on Fast Restart): wait for its weapon.
            while (_weaponData == null && _isAttacking) yield return null;
            if (_weaponData == null) yield break;
            yield return new WaitForSeconds(_weaponData.DelayBeforeAttack.GetRandom());
            while (_isAttacking && _weaponData != null)
            {
                if (IsCanAttack == false)
                {
                    yield return null;
                }

                var count = 0;
                while (_weaponData != null && count < _weaponData.QuantityPerTime)
                {
                    // Restart destroys the player ship and the level (the projectile parent) while this
                    // coroutine waits; touching them then threw, which halts a release WebGL build.
                    if (_weaponData == null || !_projectilePrefab || !_target || !_parentForProjectiles || !_projectileSpawnPoint)
                    {
                        _isAttacking = false;
                        yield break;
                    }

                    var projectile = Instantiate(_projectilePrefab, _parentForProjectiles);
                    projectile.transform.position = _projectileSpawnPoint.position;
                    projectile.transform.rotation = _projectileSpawnPoint.rotation;

                    if (!_target)
                    {
                        yield break;
                    }

                    if (!projectile || !projectile.transform || !projectile.gameObject)
                    {
                        yield break;
                    }

                    var direction = (_target.position - projectile.transform.position).normalized;
                    projectile.SetProjectileData(ProjectileOwner.Enemy, _weaponData.Damage, _weaponData.ProjectileSpeed,
                        _weaponData.AutoDestroyTime, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation,
                        direction);
                    count++;
                    if (AudioManager.Instance) AudioManager.Instance.PlaySfx(_gunSound);
                    if (_weaponData == null) break;
                    yield return new WaitForSeconds(_weaponData.FireRate);
                }

                if (_weaponData == null) break;
                yield return new WaitForSeconds(_weaponData.DelayBetweenAttacks.GetRandom());
            }

            _isAttacking = false;
        }

        protected virtual void OnActivate()
        {
        }

        protected virtual void OnDeactivate()
        {
        }
    }
}