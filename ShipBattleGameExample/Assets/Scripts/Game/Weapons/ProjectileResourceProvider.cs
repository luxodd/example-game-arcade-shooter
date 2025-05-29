using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    [CreateAssetMenu(menuName = "Create/Resources/Projectile Provider", fileName = "ProjectileResourceProvider",
        order = 0)]
    public class ProjectileResourceProvider : ScriptableObject
    {
        [SerializeField] private string _projectilePrefixPath;

        [SerializeField]
        private List<ProjectileResourceData> _projectileResourceDataList = new List<ProjectileResourceData>();

        public ProjectileBehaviour ProvideProjectilePrefab(string projectileKey)
        {
            var resourceData = _projectileResourceDataList.Find(data => data.ProjectileKey == projectileKey);
            var fullPath = _projectilePrefixPath + resourceData.ProjectilePath;
            var result = Resources.Load<ProjectileBehaviour>(fullPath);
            return result;
        }
    }

    [Serializable]
    public class ProjectileResourceData
    {
        [field: SerializeField] public string ProjectileKey { get; private set; }
        [field: SerializeField] public string ProjectilePath { get; private set; }
    }
}