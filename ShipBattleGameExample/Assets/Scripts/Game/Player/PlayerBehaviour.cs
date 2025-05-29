using System;
using System.Collections.Generic;
using Core.Storage;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Player
{
    public enum LevelState
    {
        Closed,
        Opened,
        Completed,
        Failed,
    }

    public class PlayerBehaviour : MonoBehaviour, IDataHolder, IStorable
    {
        private const string PlayerProgressKey = "player_progress";
        private const string CurrentLevelKey = "current_level";
        private const string LevelStatesKey = "level_states";

        private const int DefaultLevel = 1;
        public IReadOnlyProperty<string> PlayerName => _playerName;
        [field: SerializeField] public int CurrentLevel { get; private set; }

        private CustomProperty<string> _playerName = new CustomProperty<string>();

        private List<LevelState> _levelStates = new List<LevelState>();

        public bool IsLevelCompleted(int levelId)
        {
            var levelIndex = levelId - 1;
            if (levelIndex >= _levelStates.Count) return false;
            var levelState = _levelStates[levelIndex];
            return levelState == LevelState.Completed;
        }

        public void CompleteLevel()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(CompleteLevel)}] OK, current level is {CurrentLevel}");
            if (CurrentLevel > _levelStates.Count)
            {
                _levelStates.Add(LevelState.Completed);
            }
            //if we have more then one level
            //CurrentLevel++;  
        }

        public void SetPlayerName(string playerName)
        {
            _playerName.SetValue(playerName);
        }

        public Dictionary<string, object> GetData()
        {
            var data = new Dictionary<string, object>
            {
                [CurrentLevelKey] = CurrentLevel,
                [LevelStatesKey] = _levelStates
            };
            return data;
        }

        public void Save(Dictionary<string, object> data)
        {
            data[PlayerProgressKey] = GetData();
        }

        public void Load(Dictionary<string, object> data)
        {
            if (!data.TryGetValue(PlayerProgressKey, out var playerBehaviourData)) return;

            var playerBehaviour =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(playerBehaviourData.ToString());
            CurrentLevel = Convert.ToInt32(playerBehaviour[CurrentLevelKey]);

            var levelsStates = (JArray)playerBehaviour[LevelStatesKey];
            _levelStates = levelsStates.ToObject<List<LevelState>>();

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(Load)}] OK, current level is {CurrentLevel}, states count is {_levelStates.Count}");
        }

        public void Clear()
        {
            CurrentLevel = DefaultLevel;
            _levelStates.Clear();
        }
    }
}