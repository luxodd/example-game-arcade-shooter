using UnityEngine;
using Utils;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Create/Enemy/Ship Weapon Data", fileName = "EnemyShipWeaponData", order = 0)]
    public class EnemyShipWeaponData : ScriptableObject
    {
        [field: SerializeField] public int WeaponId { get; private set; }
        [field: SerializeField] public int Damage { get; private set; }
        [field: SerializeField] public int QuantityPerTime { get; private set; }
        [field: SerializeField] public float ProjectileSpeed { get; private set; }
        [field: SerializeField] public float AutoDestroyTime { get; private set; }
        [SerializeField] public FloatMiniMaxValue DelayBeforeAttack;
        [SerializeField] public FloatMiniMaxValue DelayBetweenAttacks;
        [field: SerializeField] public float FireRate { get; private set; }
        [field: SerializeField] public string ProjectileId { get; private set; }
    }
}