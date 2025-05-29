using Game.Enemy.Boss.ArachnoDominator;
using Game.PlayerShip;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.Level
{
    public class LevelBossGameplayHelper : MonoBehaviour
    {
        [SerializeField] private PlayerShipBehaviour _playerShipBehaviour;
        [SerializeField] private BossHealthSystemBehaviour _bossHealthSystemBehaviour;

        [SerializeField] private float _criticalHitPointsPercentage;

        [SerializeField] private float _delayBeforeActivation;

        private int _bossMaxHealth;
        private bool _isBehaviourActivated = false;
        private float _criticalHitPoints;

        public void ProvideDependencies(PlayerShipBehaviour playerShipBehaviour,
            BossHealthSystemBehaviour bossHealthSystemBehaviour)
        {
            _playerShipBehaviour = playerShipBehaviour;
            _bossHealthSystemBehaviour = bossHealthSystemBehaviour;

            _bossMaxHealth = _bossHealthSystemBehaviour.MaxHealthPoints;

            _criticalHitPoints = _bossMaxHealth * _criticalHitPointsPercentage;
            SubscribeToEvents();
        }

        private void OnLevelEnd()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _bossHealthSystemBehaviour.HealthPoints.AddListener(OnBossHealthPointChanged);
        }

        private void UnsubscribeFromEvents()
        {
            _bossHealthSystemBehaviour.HealthPoints.RemoveListener(OnBossHealthPointChanged);
        }

        private void OnBossHealthPointChanged(int newHealthPoint)
        {
            if (newHealthPoint <= _criticalHitPoints && _isBehaviourActivated == false)
            {
                _isBehaviourActivated = true;
                _bossHealthSystemBehaviour.DroneSpawner.DeactivateEnemies();

                _playerShipBehaviour.Deactivate();

                _bossHealthSystemBehaviour.DroneSpawner.Deactivate();

                CoroutineManager.DelayedAction(_delayBeforeActivation,
                    () => _playerShipBehaviour.LaunchBigBomb(_bossHealthSystemBehaviour.DroneSpawner.transform));
            }
        }
    }
}