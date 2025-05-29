using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Audio;
using Game.Enemy;
using Game.Enemy.Boss.ArachnoDominator;
using Game.PlayerShip;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Weapons
{
    public class BigBossBombMissileBehaviour : MonoBehaviour
    {
        public int Damage { get; private set; }
        public bool IsActivated { get; private set; }

        [SerializeField] private Transform _target;
        [SerializeField] private FlameActivatorBehaviour _flameActivator;

        [SerializeField] private PlayerWeaponDescriptor _weaponDescriptor;

        [SerializeField] private float _launchTime;
        [SerializeField] private float _flameDuration;
        [SerializeField] private float _maxSpeed;
        [SerializeField] private float _startSpeed;
        [SerializeField] private float _rotationSpeed;
        [SerializeField] private float _acceleration;
        [SerializeField] private float _destructionTime;
        [SerializeField] private float _currentSpeed;
        [SerializeField] private float _autoDestructionTime;
        [SerializeField] private float _maxRadius;
        [SerializeField] private float _shockWaveTime;

        [SerializeField] private SpriteRenderer _bombRenderer;

        [SerializeField] private ParticleSystem _bigExplosionParticle;

        private Coroutine _movemenCoroutine;

        private bool _isActive;

        private Vector3 _direction;

        public void SetupData(float startSpeed)
        {

        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _direction = (_target.position - transform.position).normalized;
        }

        public void BlowUp()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(BlowUp)}] OK");
            _flameActivator.DeactivateFlame();
            _bombRenderer.gameObject.SetActive(false);
            _bigExplosionParticle.gameObject.SetActive(true);
            if (_movemenCoroutine != null)
            {
                StopCoroutine(_movemenCoroutine);
            }

            DealDamage();
            CoroutineManager.DelayedAction(_autoDestructionTime, () =>
            {
                if (gameObject && gameObject.activeInHierarchy == true)
                {
                    Destroy(gameObject);
                }
            });
        }

        public void Activate()
        {
            IsActivated = true;
            _movemenCoroutine = StartCoroutine(HomingMovement());
        }

        private void Awake()
        {
            Damage = _weaponDescriptor.UpgradeDataList[0].Damage;
        }

        [ContextMenu("Launch")]
        private void TestActivate()
        {
            _direction = (_target.position - transform.position).normalized;
            Activate();
        }

        [ContextMenu("Blow Up")]
        private void TestBlowUp()
        {
            BlowUp();
        }

        private void DealDamage()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(DealDamage)}] OK");
            var allColliders2D = Physics2D.OverlapCircleAll(transform.position, _maxRadius);
            var enemies = new List<EnemyBaseBehaviour>();
            foreach (var collider2D in allColliders2D)
            {
                var enemy = collider2D.GetComponent<EnemyBaseBehaviour>();
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }

                var projectile = collider2D.GetComponent<ProjectileBehaviour>();
                projectile?.Deactivate();
            }

            //sorting by distance

            var sortedEnemies =
                enemies.OrderBy(enemy => Vector3.Distance(enemy.transform.position, transform.position));

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(DealDamage)}] OK, sorted enemies: {sortedEnemies.Count()}");

            foreach (var enemy in sortedEnemies)
            {
                var distance = Vector3.Distance(enemy.transform.position, transform.position);
                var time = Mathf.Lerp(0, _shockWaveTime, distance / _maxSpeed);
                CoroutineManager.DelayedAction(time, () => enemy.DealDamage(Damage));
            }
        }

        private IEnumerator HomingMovement()
        {
            var counter = 0f;
            _flameActivator.ActivateFlame();
            _currentSpeed = _startSpeed;
            yield return new WaitForSeconds(_flameDuration);
            while (counter < _launchTime)
            {

                if (_currentSpeed < _maxSpeed)
                {
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxSpeed, _acceleration * Time.deltaTime);
                }

                transform.position = Vector3.Lerp(transform.position, transform.position + transform.up * _currentSpeed,
                    _currentSpeed * Time.deltaTime);

                counter += Time.deltaTime;
                yield return null;
            }

            counter = 0f;
            AudioManager.Instance.PlayRocketLauncherSound();
            while (counter < _destructionTime)
            {
                if (_currentSpeed < _maxSpeed)
                {
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxSpeed, _acceleration * Time.deltaTime);
                }


                transform.position = Vector3.Lerp(transform.position, transform.position + transform.up * _currentSpeed,
                    _currentSpeed * Time.deltaTime);
                _direction = (_target.position - transform.position).normalized;

                var angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;

                float newAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle,
                    _rotationSpeed * Time.fixedDeltaTime / 360f);
                transform.rotation = Quaternion.Euler(0, 0, newAngle);

                counter += Time.deltaTime;
                yield return null;
            }

            BlowUp();
        }
    }
}