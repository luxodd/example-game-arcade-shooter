using System;
using Core.Audio;
using Game.CameraInner;
using Game.Common;
using Game.Enemy.Boss.ArachnoDominator;
using Game.Events;
using Game.Level;
using Game.UI.Handlers;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States.StateHelpers
{
    public class BossPartGameplayStateHelper : ActivatorBehaviour
    {
        [SerializeField] private LevelBehaviour _levelBehaviour;
        [SerializeField] private GameScreenHandler _gameScreenHandler;
        [SerializeField] private CameraFollowBehaviour _cameraFollowBehaviour;
        [SerializeField] private LevelBossGameplayHelper _levelBossGameplayHelper;

        protected override void OnActivate()
        {
            base.OnActivate();
            SubscribeToEvents();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventAggregator.Subscribe<BossStartedEvent>(OnBossStartedHandler);
            EventAggregator.Subscribe<BossDefeatedEvent>(OnBossDefeatedHandler);
            EventAggregator.Subscribe<BossPartDestroyedEvent>(OnBossPartDestroyedHandler);
            EventAggregator.Subscribe<BossExplosionEvent>(OnBossExplosionHandler);
            
            var bossHealth = _levelBehaviour.ActiveLevel.DominatorBehaviour.GetComponent<BossHealthSystemBehaviour>();
            _levelBossGameplayHelper.ProvideDependencies(_levelBehaviour.ActivePlayer, bossHealth);
            
            bossHealth.HealthPoints.AddListener(OnBossHealthPointChangedHandler);
            _gameScreenHandler.SetBossMaximumHealthPoints(bossHealth.MaxHealthPoints);
            _gameScreenHandler.SetBossHealthPoints(bossHealth.MaxHealthPoints);
            
        }

        private void UnsubscribeFromEvents()
        {
            EventAggregator.Unsubscribe<BossStartedEvent>(OnBossStartedHandler);
            EventAggregator.Unsubscribe<BossDefeatedEvent>(OnBossDefeatedHandler);
            EventAggregator.Unsubscribe<BossPartDestroyedEvent>(OnBossPartDestroyedHandler);
            EventAggregator.Unsubscribe<BossExplosionEvent>(OnBossExplosionHandler);
            
            var bossHealth = _levelBehaviour.ActiveLevel.DominatorBehaviour.GetComponent<BossHealthSystemBehaviour>();
            bossHealth?.HealthPoints.RemoveListener(OnBossHealthPointChangedHandler);
        }
        
        private void OnBossStartedHandler(object sender, BossStartedEvent eventArgs)
        {
            if (_isActivated == false) return;
            
            _gameScreenHandler.ShowBossHealthBar();
            _levelBehaviour.ActiveLevel.DominatorBehaviour.SetTarget(_levelBehaviour.ActivePlayer.transform);
            _levelBehaviour.ActiveLevel.DominatorBehaviour.Activate();
        }

        private void OnBossDefeatedHandler(object sender, BossDefeatedEvent eventArgs)
        {
            if (_isActivated == false) return;
            
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnBossDefeatedHandler)}] OK");
            _gameScreenHandler.HideBossHealthBar();
            ContinueLevelMovement();
        }

        private void OnBossHealthPointChangedHandler(int healthPoint)
        {
            if (_isActivated == false) return;
            
            _gameScreenHandler.SetBossHealthPoints(healthPoint);
        }

        private void OnBossPartDestroyedHandler(object sender, BossPartDestroyedEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnBossPartDestroyedHandler)}] OK");
            AudioManager.Instance.PlayRandomExplosionSound();
            _cameraFollowBehaviour.SmallShake();
        }

        public void OnBossExplosionHandler(object sender, BossExplosionEvent eventArgs)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnBossExplosionHandler)}] OK");
            _cameraFollowBehaviour.BigShake();
            if (eventArgs.IsExplosion)
            {
                AudioManager.Instance.PlayRandomExplosionSound();
            }
            else
            {
                AudioManager.Instance.PlayEnergyShieldImpactSound();
            }
        }
        
        private void ContinueLevelMovement()
        {
            _cameraFollowBehaviour.SetLevelSpeed(_levelBehaviour.ActiveLevel.LevelSpeed);
            _levelBehaviour.ActivePlayer.ContinueMovement();
            _levelBehaviour.ActivePlayer.SetLevelSpeed(_levelBehaviour.ActiveLevel.LevelSpeed);
        }
    }
}
