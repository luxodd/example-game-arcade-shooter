using System.Collections.Generic;
using Game.CameraInner;
using Game.Enemy;
using Game.Events;
using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Level
{
    public class InLevelEnemyActivator : MonoBehaviour
    {
        [SerializeField] private List<EnemyBaseBehaviour> _enemies = new List<EnemyBaseBehaviour>();
        [SerializeField] private Transform _parentForProjectiles;
        private bool _isActivatedInGame = false;

        public void Activate()
        {
            if (_isActivatedInGame == false) return;
            _enemies.ForEach(enemy =>
            {
                if (enemy.IsActivateByActivator == false)
                {
                    enemy.Activate();
                }
            });
        }

        public void Deactivate()
        {
            _enemies.ForEach(enemy => enemy.Deactivate());
        }

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<CameraActivatorBehaviour>(out var activatorBehaviour) ==
                false) return;

            if (activatorBehaviour.IsActivate)
            {
                _isActivatedInGame = true;
                Activate();
            }
            else if (activatorBehaviour.ActivatorType == ActivatorType.Primary)
            {
                _isActivatedInGame = false;
                Deactivate();
            }
        }

        private void SubscribeToEvents()
        {
            EventAggregator.Subscribe<PlayerPreparedEvent>(OnPlayerShipPreparedEventHandler);
        }

        private void UnsubscribeFromEvents()
        {
            EventAggregator.Unsubscribe<PlayerPreparedEvent>(OnPlayerShipPreparedEventHandler);
        }

        private void OnPlayerShipPreparedEventHandler(object sender, PlayerPreparedEvent eventData)
        {
            _enemies.ForEach(enemy =>
            {
                enemy.ProvideDependencies(_parentForProjectiles);
                enemy.SetTarget(eventData.Player.transform);
            });
        }
    }
}