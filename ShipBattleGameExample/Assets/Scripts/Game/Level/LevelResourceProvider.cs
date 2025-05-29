using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
    [CreateAssetMenu(menuName = "Create/Level/Resource Provider", fileName = "LevelResourceProvider", order = 0)]
    public class LevelResourceProvider : ScriptableObject
    {
        [SerializeField] private string _levelPathPrefix = "Levels";
        [SerializeField] private List<LevelResourceData> _levelResources = new List<LevelResourceData>();

        public LevelMap ProvideLevel(string levelId)
        {
            var levelResource = _levelResources.Find(levelResourceData => levelResourceData.LevelId == levelId);
            var fullPath = $"{_levelPathPrefix}/{levelResource.Path}";
            var levelMap = Resources.Load<LevelMap>(fullPath);
            return levelMap;
        }
    }

    [Serializable]
    public class LevelResourceData
    {
        [field: SerializeField] public string LevelId { get; private set; }
        [field: SerializeField] public string Path { get; private set; }
    }
}