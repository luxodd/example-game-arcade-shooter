using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    [CreateAssetMenu(menuName = "Create/Data Base/Projectile Data Base", fileName = "ProjectileDataBase", order = 0)]
    public class ProjectileDataBase : ScriptableObject
    {
        [SerializeField] private List<ProjectileData> _projectileDataList = new List<ProjectileData>();

        public string ProvideProjectilePrefabKey(int projectileId)
        {
            var projectileData = _projectileDataList.Find(projectile => projectile.Id == projectileId);
            return projectileData.PrefabKey;
        }
    }

    [Serializable]
    public class ProjectileData
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public string PrefabKey { get; private set; }
    }
}