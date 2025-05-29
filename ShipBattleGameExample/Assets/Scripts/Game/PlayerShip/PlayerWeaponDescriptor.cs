using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.PlayerShip
{

    [CreateAssetMenu(menuName = "Create/Player/Weapon Descriptor", fileName = "PlayerWeaponDescriptor", order = 0)]
    public class PlayerWeaponDescriptor : ScriptableObject
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public PlayerWeaponType WeaponType { get; private set; }
        [field: SerializeField] public PlayerWeaponAttackingType AttackingType { get; private set; }

        [field: SerializeField] public string ProjectileId { get; private set; }
        [field: SerializeField] public List<PlayerWeaponUpgradeData> UpgradeDataList { get; private set; }

        public PlayerWeaponUpgradeData GetUpgradeData(int upgradeLevel)
        {
            var result = UpgradeDataList.Find(upgradeData => upgradeData.Level == upgradeLevel);
            return result;
        }
    }

    public enum PlayerWeaponType
    {
        VulcanCannon = 0,
        IonLaser = 1,
        BendPlasma = 2,
        HomingMissile = 3,
        NuclearMissile = 4,
        ThermonuclearBomb = 5,
        DiffusionBomb = 6
    }

    public enum PlayerWeaponAttackingType
    {
        Primary = 0,
        Secondary = 1,
        Passive = 2
    }

    [Serializable]
    public class PlayerWeaponUpgradeData
    {
        [field: SerializeField] public int Level { get; set; }
        [field: SerializeField] public float ProjectileSpeed { get; set; }
        [field: SerializeField] public float AutoDestroyTime { get; set; }
        [field: SerializeField] public int Damage { get; set; }
        [field: SerializeField] public int AmountAtTime { get; set; }
        [field: SerializeField] public float Cooldown { get; set; }
        [field: SerializeField] public float FireRate { get; set; }
    }
}