using UnityEngine;

namespace Game.Bonuses
{
    public enum BonusType
    {
        Score,
        WeaponPrimary,
        WeaponSecondary,
        WeaponBomb,
        AdditionalLife,
        RecoveryHealth,
    }
    
    [CreateAssetMenu(menuName = "Create/Game/Bonus Descriptor", fileName = "BonusDescriptor", order = 0)]
    public class BonusDescriptor : ScriptableObject
    {
        [field: SerializeField] public BonusType Bonus { get; private set; }
        [field: SerializeField] public int BonusValue { get; private set; } = 1;
        [field: SerializeField] public int BonusId { get; private set; }
        [field: SerializeField] public string PrefabKey { get; private set; }
    }
}
