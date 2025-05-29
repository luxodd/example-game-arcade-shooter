using UnityEngine;
using Utils;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Create/Enemy/Ship Data", fileName = "EnemyShipData", order = 0)]
    public class EnemyShipData : ScriptableObject
    {
        [field: SerializeField] public int EnemyId { get; private set; }
        [field: SerializeField] public EnemyType Type { get; private set; }
        [field: SerializeField] public int HealthPoints { get; private set; }
        [field: SerializeField] public string ShipPrefabKey { get; private set; }
        [field: SerializeField] public FloatMiniMaxValue MaximumSpeed { get; private set; }
        [field: SerializeField] public float Acceleration { get; private set; }
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public FloatMiniMaxValue RandomDelay { get; private set; }
        [field: SerializeField] public int WeaponId { get; private set; }
        [field: SerializeField] public int DamageOnDestroy { get; private set; }

        [field: SerializeField] public int ScorePointsMin { get; private set; }
        [field: SerializeField] public int ScorePointsMax { get; private set; }
        [field: SerializeField] public int ScorePointsByHit { get; private set; } = 10;
        [field: SerializeField] public float DefaultTimeScore { get; private set; }
        [field: SerializeField] public float DelayBeforeTimeScore { get; private set; }
    }
}