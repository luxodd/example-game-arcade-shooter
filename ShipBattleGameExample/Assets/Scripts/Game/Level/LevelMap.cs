using System.Collections.Generic;
using Game.Enemy;
using Game.Enemy.Boss.ArachnoDominator;
using Game.Events;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Level
{
    public enum LevelCompletionState
    {
        LevelComplete,
        LevelFailed,
        ConnectionIssue,
        Terminated
    }

    public class LevelMap : MonoBehaviour
    {
        [field: SerializeField] public Transform LeftBounds { get; private set; }
        [field: SerializeField] public Transform RightBounds { get; private set; }
        [field: SerializeField] public Transform CameraStartPoint { get; private set; }
        [field: SerializeField] public Transform LevelEndPoint { get; private set; }

        [field: SerializeField] public EnemySpawnBehaviour SpawnBehaviour { get; private set; }

        [field: SerializeField] public float LevelSpeed { get; private set; } = 2f;

        [field: SerializeField] public ArachnoDominatorBehaviour DominatorBehaviour { get; private set; }

        [SerializeField] private List<TriggerHandler> _triggers = new List<TriggerHandler>();

        public void ActivateLevel()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(ActivateLevel)}] OK");
            SpawnBehaviour.Activate();
        }

        public void DeactivateLevel()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(DeactivateLevel)}] OK");
            SpawnBehaviour.Deactivate();
            UnsubscribeFromEvents();
        }

        public void DeactivateGameplayProcess()
        {
            SpawnBehaviour.Deactivate();
        }

        public void DeactivateEnemies()
        {
            SpawnBehaviour.StopSpawning();
        }

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            DeactivateLevel();
        }

        private void SubscribeToEvents()
        {
            foreach (var trigger in _triggers)
            {
                trigger.TriggerEvent.AddListener(OnTriggerEnterHandler);
            }
        }

        private void UnsubscribeFromEvents()
        {
            foreach (var trigger in _triggers)
            {
                trigger?.TriggerEvent.RemoveAllListeners();
            }
        }

        private void OnTriggerEnterHandler(TriggerType triggerType)
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(OnTriggerEnterHandler)}] OK, triggerType: {triggerType}");

            switch (triggerType)
            {
                case TriggerType.LevelEnd:
                {
                    var completionState = LevelCompletionState.LevelComplete;
                    EventAggregator.Post(this, new LevelCompletionEvent() { CompletionState = completionState });
                    break;
                }
                case TriggerType.ActivateEnemies:
                    EventAggregator.Post(this, new LevelMapEvents() { EventType = LevelMapEventType.ActivateEnemies });
                    break;
                case TriggerType.DeactivateEnemies:
                    EventAggregator.Post(this,
                        new LevelMapEvents() { EventType = LevelMapEventType.DeactivateEnemies });
                    break;
                case TriggerType.BossStarted:
                    EventAggregator.Post(this, new BossStartedEvent());
                    break;
                case TriggerType.CameraStopped:
                    EventAggregator.Post(this, new LevelMapEvents() { EventType = LevelMapEventType.CameraStop });
                    break;
            }
        }
    }
}