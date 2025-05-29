using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
    [CreateAssetMenu(menuName = "Create/Level/Data Base", fileName = "LevelDataBase", order = 0)]
    public class LevelDataBase : ScriptableObject
    {
        [SerializeField] private List<LevelData> _levelDataList = new List<LevelData>();

        public int LevelCount => _levelDataList.Count;

        public LevelData GetLevelData(int levelId)
        {
            var result = _levelDataList.Find(levelData => levelData.Id == levelId);
            return result;
        }
    }

    [Serializable]
    public class LevelData
    {
        [field: SerializeField] public int Id { get; set; }
        [field: SerializeField] public string LevelName { get; set; }
    }
}