using System;
using Game.Events;
using Game.Level;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Statistics
{
    public class PlayerStatisticTrackingController : MonoBehaviour
    {
        public float Accuracy => _playerAccuracyTracker.Accuracy;
        public float TotalSeconds => _playerLevelTimeTracker.Time;
        public int EnemiesKilled => _playerEnemyKillsTracker.EnemiesKilled;
        public float LevelProgress => _playerLeveProgressTracker.LevelProgress;

        private PlayerAccuracyTracker _playerAccuracyTracker;
        private PlayerLevelTimeTracker _playerLevelTimeTracker;
        private PlayerEnemyKillsTracker _playerEnemyKillsTracker;
        private PlayerLeveProgressTracker _playerLeveProgressTracker;

        public void StartTracking()
        {
            SubscribeToEvents();
            _playerAccuracyTracker.StartTracking();
            _playerLevelTimeTracker.StartTracking();
            _playerEnemyKillsTracker.StartTracking();
            _playerLeveProgressTracker.StartTracking();
        }

        public void StopTracking()
        {
            _playerAccuracyTracker.StopTracking();
            _playerLevelTimeTracker.StopTracking();
            _playerEnemyKillsTracker.StopTracking();
            _playerLeveProgressTracker.StopTracking();
            UnsubscribeFromEvents();
        }

        #region Unity Events

        private void Awake()
        {
            PrepareTrackers();
        }

        #endregion

        private void SubscribeToEvents()
        {
            EventAggregator.Subscribe<ProjectileDestroyEvent>(OnProjectileDestroyEventHandler);
            EventAggregator.Subscribe<PlayerShotEvent>(OnPlayerShotEventHandler);
            EventAggregator.Subscribe<EnemyDeathEvent>(OnEnemyDeathEventHandler);
            EventAggregator.Subscribe<LevelPreparedEvent>(OnLevelPreparedEventHandler);
            EventAggregator.Subscribe<GameOverEvent>(OnGameOverEventHandler);
            EventAggregator.Subscribe<LevelCompletionEvent>(OnLevelCompletionEventHandler);
        }

        private void UnsubscribeFromEvents()
        {
            EventAggregator.Unsubscribe<ProjectileDestroyEvent>(OnProjectileDestroyEventHandler);
            EventAggregator.Unsubscribe<PlayerShotEvent>(OnPlayerShotEventHandler);
            EventAggregator.Unsubscribe<EnemyDeathEvent>(OnEnemyDeathEventHandler);
            EventAggregator.Unsubscribe<LevelPreparedEvent>(OnLevelPreparedEventHandler);
            EventAggregator.Unsubscribe<GameOverEvent>(OnGameOverEventHandler);
            EventAggregator.Unsubscribe<LevelCompletionEvent>(OnLevelCompletionEventHandler);
        }

        private void OnProjectileDestroyEventHandler(object sender, ProjectileDestroyEvent eventArgs)
        {
            if (eventArgs.Owner == ProjectileOwner.Player && eventArgs.Reason == ProjectileDestroyReason.ByHit)
            {
                _playerAccuracyTracker.PlayerHit();
            }
        }

        private void OnPlayerShotEventHandler(object sender, PlayerShotEvent eventArgs)
        {
            _playerAccuracyTracker.PlayerShot();
        }

        private void OnEnemyDeathEventHandler(object sender, EnemyDeathEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnEnemyDeathEventHandler)}] OK");
            _playerEnemyKillsTracker.KillEnemy();
        }

        private void OnLevelPreparedEventHandler(object sender, LevelPreparedEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{nameof(OnLevelPreparedEventHandler)}] OK");
            _playerLeveProgressTracker.SetupLevelPoints(eventArgs.StartPoint, eventArgs.EndPoint);
        }

        private void OnGameOverEventHandler(object sender, GameOverEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{nameof(OnGameOverEventHandler)}] OK");
            _playerLeveProgressTracker.SetCurrentLevelPosition(eventArgs.CurrentLevelPosition);
        }

        private void OnLevelCompletionEventHandler(object sender, LevelCompletionEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{nameof(OnLevelCompletionEventHandler)}] OK");
            if (eventArgs.CompletionState == LevelCompletionState.LevelComplete)
            {
                _playerLeveProgressTracker.LevelComplete();
            }
        }

        private void PrepareTrackers()
        {
            _playerAccuracyTracker = new PlayerAccuracyTracker();
            _playerLevelTimeTracker = new PlayerLevelTimeTracker();
            _playerEnemyKillsTracker = new PlayerEnemyKillsTracker();
            _playerLeveProgressTracker = new PlayerLeveProgressTracker();
        }
    }
}