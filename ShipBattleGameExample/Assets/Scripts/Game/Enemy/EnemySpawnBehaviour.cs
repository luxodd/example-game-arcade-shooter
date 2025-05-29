using System;
using System.Collections;
using System.Collections.Generic;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Enemy
{
    public class EnemySpawnBehaviour : MonoBehaviour
    {
        public IFloatReadOnlyProperty Difficulty => _difficultyProperty;
        public IIntReadOnlyProperty EnemiesCount => _enemiesCountProperty;
        public IFloatReadOnlyProperty SpawnCooldown => _spawnCooldownProperty;
        public int MaxEnemies => _maxEnemies;

        [SerializeField] private List<EnemyBaseBehaviour> _enemies = new List<EnemyBaseBehaviour>();

        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        [SerializeField] private Transform _parentForEnemies;
        [SerializeField] private Transform _parentForProjectiles;

        [SerializeField] private float _shiftedSpawnY;

        [SerializeField] private float _cooldown;
        [SerializeField] private float _delayBeforeSpawn;
        [SerializeField] private float _delayIfMaximumSpawn;

        [SerializeField] private int _maxEnemies;

        [SerializeField] private Transform _target;

        private float _cooldownCounter;

        private bool _isSpawning;

        private Coroutine _spawnCoroutine;

        private FloatProperty _difficultyProperty = new FloatProperty();
        private IntProperty _enemiesCountProperty = new IntProperty();
        private FloatProperty _spawnCooldownProperty = new FloatProperty();

        private List<EnemyBaseBehaviour> _spawnedEnemies = new List<EnemyBaseBehaviour>();

        private int CurrentEnemies => _spawnedEnemies.Count;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        [ContextMenu("Activate")]
        public void Activate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Activate)}] OK");
            _spawnCooldownProperty.SetValue(_cooldown);
            _spawnCoroutine = StartCoroutine(SpawnEnemies());
            _difficultyProperty.SetValue(_difficultyProperty.Value, true, true);
            _enemiesCountProperty.SetValue(_spawnedEnemies.Count, true, true);
            if (_enemies.Count > 0)
            {
                ActivateEnemies();
            }
        }

        public void Deactivate()
        {
            _isSpawning = false;
            DeactivateEnemies();

            if (_spawnCoroutine == null) return;

            StopCoroutine(_spawnCoroutine);
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            if (_spawnCoroutine == null) return;
            StopCoroutine(_spawnCoroutine);
        }

        public void ClearEnemies()
        {
            while (_spawnedEnemies.Count > 0)
            {
                var enemy = _spawnedEnemies[0];
                enemy.Deactivate();
                _spawnedEnemies.RemoveAt(0);
                Destroy(enemy.gameObject);
            }
        }

        public void ClearProjectiles()
        {
            while (_parentForProjectiles.childCount > 0)
            {
                var child = _parentForProjectiles.GetChild(0);
                var projectile = child.GetComponent<ProjectileBehaviour>();
                projectile.Deactivate();
                Destroy(child.gameObject);
            }
        }

        public void UpdateCooldown(float cooldown)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(UpdateCooldown)}] OK, cooldown: {cooldown}");
            _spawnCooldownProperty.SetValue(cooldown, true, true);
        }

        private int _totalEnemies = 0;
        private Vector3 _previousSpawnPosition;
        private int _previousSpawnIndex = -1;

        private IEnumerator SpawnEnemies()
        {
            _isSpawning = true;

            yield return new WaitForSeconds(_delayBeforeSpawn);
            while (_isSpawning)
            {
                if (CurrentEnemies >= _maxEnemies)
                {
                    yield return new WaitForSeconds(_delayIfMaximumSpawn);
                    continue;
                }

                _cooldownCounter = 0;
                while (_cooldownCounter < _spawnCooldownProperty.Value)
                {

                    _cooldownCounter += Time.deltaTime;
                    yield return null;
                }

                var enemyPrefab = _enemies[Random.Range(0, _enemies.Count)];

                var spawnIndex = Random.Range(0, _spawnPoints.Count);
                var counter = 0;
                var maximumAttempts = _maxEnemies;
                while (spawnIndex == _previousSpawnIndex && counter < maximumAttempts)
                {
                    spawnIndex = Random.Range(0, _spawnPoints.Count);
                    counter++;
                }

                _previousSpawnIndex = spawnIndex;
                var spawnPoint = _spawnPoints[spawnIndex];

                var spawnPosition = spawnPoint.position;
                spawnPosition.y = _target.position.y + _shiftedSpawnY;
                var enemyBehaviour = Instantiate(enemyPrefab, _parentForEnemies);
                enemyBehaviour.name = $"{enemyPrefab.name}_{_totalEnemies}";
                enemyBehaviour.transform.position = spawnPosition;
                enemyBehaviour.transform.rotation = spawnPoint.rotation;
                enemyBehaviour.SetTarget(_target);
                enemyBehaviour.ProvideDependencies(_parentForProjectiles);
                enemyBehaviour.DestroyedEvent.AddListener(OnEnemyDestroyed);
                _spawnedEnemies.Add(enemyBehaviour);

                _enemiesCountProperty.SetValue(_spawnedEnemies.Count);

                _difficultyProperty.SetValue(CalculateDifficulty());
                _totalEnemies++;
            }

            _isSpawning = false;
        }

        private void OnEnemyDestroyed(EnemyBaseBehaviour enemyBaseBehaviour)
        {
            _spawnedEnemies.Remove(enemyBaseBehaviour);
            _enemiesCountProperty.SetValue(_spawnedEnemies.Count);
            _difficultyProperty.SetValue(CalculateDifficulty());

        }

        private float CalculateDifficulty()
        {
            var activeEnemiesFactor = Mathf.Clamp01(_spawnedEnemies.Count / (float)_maxEnemies);
            return activeEnemiesFactor;
        }

        private void DeactivateEnemies()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                enemy.Deactivate();
            }
        }

        private void ActivateEnemies()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                enemy.ContinueGameplay();
            }
        }
    }
}