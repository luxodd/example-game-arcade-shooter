using Game.Level;
using Game.PlayerShip;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;


namespace Game.Events
{
    public class LevelCompletionEvent : IEventData
    {
        public LevelCompletionState CompletionState { get; set; }
    }

    public class PlayerPreparedEvent : IEventData
    {
        public PlayerShipBehaviour Player { get; set; }
    }

    public enum LevelMapEventType
    {
        ActivateEnemies = 0,
        DeactivateEnemies = 1,
        CameraStop = 2,
    }

    public class LevelMapEvents : IEventData
    {
        public LevelMapEventType EventType { get; set; }
    }

    public class GameplayEvent
    {

    }

    public class GameOverEvent : IEventData
    {
        public Transform CurrentLevelPosition { get; set; }
    }

    public class PlayerShipDeath : IEventData
    {
    }

    public class EnemyDeathEvent : IEventData
    {
        public int Score { get; set; }
    }

    public class EnemyHitEvent : IEventData
    {
        public int Score { get; set; }
    }

    public class PlayerShotEvent : IEventData
    {
    }

    public class ProjectileDestroyEvent : IEventData
    {
        public ProjectileOwner Owner { get; set; }
        public int Damage { get; set; }
        public ProjectileDestroyReason Reason { get; set; }
    }

    public class LevelPreparedEvent : IEventData
    {
        public Transform StartPoint { get; set; }
        public Transform EndPoint { get; set; }
    }

    public class PlayerShipStateChangeEvent : IEventData
    {
        public PlayerShipState State { get; set; }
    }

    public class BossStartedEvent : IEventData
    {
    }

    public class BossDefeatedEvent : IEventData
    {
    }

    public class BossPartDestroyedEvent : IEventData
    {
    }

    public class BossExplosionEvent : IEventData
    {
        public bool IsExplosion { get; set; }
    }
}