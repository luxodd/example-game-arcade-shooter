using UnityEngine;

namespace Game.PlayerShip
{
    [CreateAssetMenu(menuName = "Create/Player/Ship Descriptor", fileName = "PlayerShipDescriptor", order = 0)]
    public class PlayerShipDescriptor : ScriptableObject
    {
        [field: SerializeField] public int DefaultPlayerShipLives { get; private set; }
        [field: SerializeField] public int MaximumPlayerShipLives { get; private set; }
        [field: SerializeField] public int DefaultPlayerShipHP { get; private set; }
        [field: SerializeField] public float ShipSpeed { get; set; }
        [field: SerializeField] public float RotationSpeed { get; set; }
        [field: SerializeField] public float RotateAngle { get; set; }
        [field: SerializeField] public float InvulnerabilityDuration { get; set; }
        [field: SerializeField] public float InvisibleTime { get; set; }
        [field: SerializeField] public float DieDuration { get; set; }
    }
}