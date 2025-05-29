using System.Collections.Generic;
using UnityEngine;

namespace Game.PlayerShip
{
    [CreateAssetMenu(menuName = "Create/Data Base/Player Ship Weapon Data Base", fileName = "PlayerShipWeaponDataBase",
        order = 0)]
    public class PlayerShipWeaponDataBase : ScriptableObject
    {
        [SerializeField]
        private List<PlayerWeaponDescriptor> _playerWeaponDescriptorList = new List<PlayerWeaponDescriptor>();

        [SerializeField] private int _vulcanCannonId = 0;

        public PlayerWeaponDescriptor DefaultPrimaryWeapon => ProvideWeaponDescriptor(_vulcanCannonId);

        public PlayerWeaponDescriptor ProvideWeaponDescriptor(int weaponId)
        {
            var result = _playerWeaponDescriptorList.Find(weapon => weapon.Id == weaponId);
            return result;
        }
    }
}
