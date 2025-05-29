using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Create/Enemy/Ship Resource Provider", fileName = "EnemyShipResourceProvider",
        order = 0)]
    public class EnemyShipResourceProvider : ScriptableObject
    {
        [SerializeField] private string _prefixPath;
        [SerializeField] private List<ShipResourceData> _shipResourceDataList = new List<ShipResourceData>();

        public EnemyBaseBehaviour ProvideShipBehaviour(string prefabKey)
        {
            var shipResourceData = _shipResourceDataList.Find(resourceData => resourceData.PrefabKey == prefabKey);
            var fullPath = Path.Combine(_prefixPath, shipResourceData.PrefabPath);
            var shipBehaviour = Resources.Load<EnemyBaseBehaviour>(fullPath);
            return shipBehaviour;
        }
    }

    [System.Serializable]
    public class ShipResourceData
    {
        [field: SerializeField] public string PrefabKey { get; private set; }
        [field: SerializeField] public string PrefabPath { get; private set; }
    }
}