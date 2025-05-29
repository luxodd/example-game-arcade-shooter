using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Create/Enemy/Ship Data Base", fileName = "EnemyShipDataBase", order = 0)]
    public class EnemyShipDataBase : ScriptableObject
    {
        [SerializeField] private List<EnemyShipData> _enemiesList = new List<EnemyShipData>();

        public EnemyShipData ProvideEnemyData(int enemyId)
        {
            var result = _enemiesList.Find(enemyData => enemyData.EnemyId == enemyId);
            return result;
        }
    }
}