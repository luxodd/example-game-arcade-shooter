using System;
using System.Collections;
using System.Collections.Generic;
using Game.Common;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using Utils;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class DroneSpawnerBehaviour : ActivatorBehaviour, ITargetable
    {
        public Transform Target { get; private set; }
        public SimpleEvent FirstSpawnEvent { get; private set; } =  new SimpleEvent();
        
        [SerializeField] private FloatMiniMaxValue _cooldown;
        [SerializeField] private FloatMiniMaxValue _delay;
        [SerializeField] private EnemyBaseBehaviour _enemyPrefab;
        
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        
        [SerializeField] private Transform _target;

        [SerializeField] private string _enemyPrefabKey;
        
        private Coroutine _spawnCoroutine;
        
        private List<EnemyBaseBehaviour> _enemies = new List<EnemyBaseBehaviour>();
        
        private EnemyShipResourceProvider _resourceProvider;
        
        private bool _isFirstSpawn = false;

        public void SetTarget(Transform target)
        {
            Target = target;
            _target = target;
        }

        public void SetEnemyResourceProvider(EnemyShipResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _enemyPrefab = resourceProvider.ProvideShipBehaviour(_enemyPrefabKey);
        }

        public void DeactivateEnemies()
        {
            _enemies.ForEach(enemy => enemy.Deactivate());
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            _spawnCoroutine = StartCoroutine(StartSpawner());
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            
            if (_spawnCoroutine == null) return;
            
            StopCoroutine(_spawnCoroutine);
        }

        [ContextMenu("Activate Drone Spawner")]
        private void TestActivate()
        {
            Activate();
        }

        [ContextMenu("Deactivate Drone Spawner")]
        private void TestDeactivate()
        {
            Deactivate();
        }

        private IEnumerator StartSpawner()
        {
            yield return new WaitForEndOfFrame();
            
            while (_isActivated)
            {
                if (_isActivated == false)
                {
                    yield break;
                }
                
                var counter = 0f;

                var cooldownTime = _cooldown.GetRandom();

                while (counter < cooldownTime)
                {
                    counter += Time.deltaTime;
                    yield return null;
                }

                var index = 0;
                foreach (var spawnPoint in _spawnPoints)
                {
                    var enemy = Instantiate(_enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                    enemy.ProvideDependencies(null);
                    enemy.SetTarget(_target);

                    if (_isFirstSpawn == false)
                    {
                        _isFirstSpawn = true;
                        //notify
                        FirstSpawnEvent.Notify();
                    }

                    var bossDroneMovement = enemy.GetComponent<EnemyDroneX4MovementBehaviour>();

                    switch (index)
                    {
                        case 0:
                            bossDroneMovement?.PrepareMoveLeft();
                            break;
                        case 1:
                            bossDroneMovement?.PrepareMoveForward();
                            break;
                        case 2:
                            bossDroneMovement?.PrepareMoveRight();
                            break;
                    }
                    
                    enemy.Activate();
                    
                    enemy.DestroyedEvent.AddListener(OnEnemyDeathHandler);
                    _enemies.Add(enemy);
                    
                    index++;
                }
                
                yield return null;
            }

            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartSpawner)}] OK, completed");
        }

        private void OnEnemyDeathHandler(EnemyBaseBehaviour enemyBaseBehaviour)
        {
            _enemies.Remove(enemyBaseBehaviour);
        }
    }
}
