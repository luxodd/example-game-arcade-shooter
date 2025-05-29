using System;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.PlayerShip
{
    public class PlayerWeaponService
    {
        private const int DefaultLevel = 1;
        public int Level { get; private set; }
        public float ProjectileSpeed => _currentLevelWeaponData.ProjectileSpeed;
        public float AutoDestroyTime => _currentLevelWeaponData.AutoDestroyTime;
        public int Damage => _currentLevelWeaponData.Damage;
        public int AmountInTime => _currentLevelWeaponData.AmountAtTime;
        public float Cooldown => _currentLevelWeaponData.Cooldown;
        public float FireRate => _currentLevelWeaponData.FireRate;

        public ProjectileBehaviour ProjectilePrefab { get; private set; }
        public Transform ParentForProjectiles { get; private set; }

        public ISimpleEvent<int> LevelChangedEvent => _levelChangedEvent;

        private readonly SimpleEvent<int> _levelChangedEvent = new SimpleEvent<int>();

        private bool IsCanUpgrade => Level < _playerWeaponDescriptor.UpgradeDataList.Count;

        private readonly PlayerWeaponDescriptor _playerWeaponDescriptor;
        private readonly ProjectileResourceProvider _projectileResourceProvider;
        private PlayerWeaponUpgradeData _currentLevelWeaponData;

        public PlayerWeaponService(PlayerWeaponDescriptor playerWeaponDescriptor,
            ProjectileResourceProvider projectileResourceProvider, Transform parentForProjectiles)
        {
            _playerWeaponDescriptor = playerWeaponDescriptor;
            _projectileResourceProvider = projectileResourceProvider;
            ParentForProjectiles = parentForProjectiles;

            PrepareProjectilePrefab();

            Level = DefaultLevel;
            PrepareCurrentLevelWeaponData();
        }

        public void UpgradeLevel()
        {
            if (IsCanUpgrade == false) return;

            Level++;
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(UpgradeLevel)}] OK, Level: {Level}");
            PrepareCurrentLevelWeaponData();
            _levelChangedEvent.Notify(Level);
        }

        public void ResetLevel()
        {
            Level = DefaultLevel;
            PrepareCurrentLevelWeaponData();
            _levelChangedEvent.Notify(Level);
        }

        private void PrepareCurrentLevelWeaponData()
        {
            _currentLevelWeaponData = _playerWeaponDescriptor.GetUpgradeData(Level);
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(PrepareCurrentLevelWeaponData)}] OK,  Level: {Level}, weaponData: {_currentLevelWeaponData.Level}");
        }

        private void PrepareProjectilePrefab()
        {
            ProjectilePrefab =
                _projectileResourceProvider.ProvideProjectilePrefab(_playerWeaponDescriptor.ProjectileId);
        }
    }
}