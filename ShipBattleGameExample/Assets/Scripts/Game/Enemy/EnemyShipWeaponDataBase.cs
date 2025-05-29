using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Create/Enemy/Ship Weapon Data Base", fileName = "EnemyShipWeaponDataBase", order = 0)]
    public class EnemyShipWeaponDataBase : ScriptableObject
    {
        [SerializeField] private List<EnemyShipWeaponData> _enemyShipWeaponDataList = null;

        public EnemyShipWeaponData ProvideEnemyShipWeaponData(int weaponId)
        {
            var result = _enemyShipWeaponDataList.Find(weaponData => weaponData.WeaponId == weaponId);
            return result;
        }
    }
}