using System;
using Game.Enemy;
using Game.Events;
using Game.PlayerShip;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Level
{
    public class LevelBehaviour : MonoBehaviour
    {
        public LevelMap ActiveLevel { get; private set; }
        public PlayerShipBehaviour ActivePlayer { get; private set; }

        [field: SerializeField] public Transform ParentForProjectiles { get; private set; }
        [field: SerializeField] public EnemyShipResourceProvider EnemyShipProvider { get; private set; }

        [SerializeField] private Transform _parentForLevelObject;
        [SerializeField] private Transform _parentForPlayerShip;
        [SerializeField] private LevelResourceProvider _levelResourceProvider;
        [SerializeField] private LevelDataBase _levelDataBase;

        [SerializeField] private PlayerShipBehaviour _playerShipBehaviourPrefab;

        public void PrepareLevel(int currentLevel)
        {
            var levelId = currentLevel % _levelDataBase.LevelCount == 0
                ? currentLevel
                : currentLevel % _levelDataBase.LevelCount;
            var levelData = _levelDataBase.GetLevelData(levelId);
            var levelMapPrefab = _levelResourceProvider.ProvideLevel(levelData.LevelName);
            ActiveLevel = Instantiate(levelMapPrefab, _parentForLevelObject);
        }

        public void DestroyLevel()
        {
            Destroy(ActiveLevel.gameObject);
            ActiveLevel = null;
        }

        public void PreparePlayerShip()
        {
            ActivePlayer = Instantiate(_playerShipBehaviourPrefab, _parentForPlayerShip);
            ActivePlayer.AddPlayedDeathListener(OnPlayerShipDeathEventHandler);
            EventAggregator.Post(this, new PlayerPreparedEvent() { Player = ActivePlayer });
        }

        public void DestroyPlayerShip()
        {
            ActivePlayer.RemovePlayedDeathListener(OnPlayerShipDeathEventHandler);
            Destroy(ActivePlayer.gameObject);
        }

        public void ClearProjectiles()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(ClearProjectiles)}] OK, child count: {ParentForProjectiles.childCount}");

            var childCount = ParentForProjectiles.childCount;

            var counter = 0;

            while (counter < childCount)
            {
                var child = ParentForProjectiles.GetChild(0);
                var projectile = child.GetComponent<ProjectileBehaviour>();
                projectile.Deactivate();
                Destroy(child.gameObject);
                counter++;
            }
        }

        public void ClearEnemies()
        {
            ActiveLevel.SpawnBehaviour.ClearEnemies();
        }

        private void OnPlayerShipDeathEventHandler()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnPlayerShipDeathEventHandler)}] OK");
        }
    }
}