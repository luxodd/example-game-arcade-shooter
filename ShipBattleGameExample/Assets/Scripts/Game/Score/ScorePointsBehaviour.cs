using System;
using System.Collections.Generic;
using System.Linq;
using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Score
{
    public class ScorePointsBehaviour : MonoBehaviour
    {
        public int CurrentScore { get; set; } = 0;
        public int HighScore => GetHighScore();

        public IReadOnlyCollection<LeaderboardRecord> LeaderboardRecords => _leaderboardRecords;

        [SerializeField] private PresetLeaderboardDescriptor _leaderboardDescriptor;

        private SimpleEvent<int> _scoreChangedEvent = new SimpleEvent<int>();

        private List<LeaderboardRecord> _leaderboardRecords = new List<LeaderboardRecord>();

        public void SetScore(int score)
        {
            CurrentScore = score;
            NotifyScoreChanged(CurrentScore);
        }

        public void AddScore(int score)
        {
            CurrentScore += score;
            NotifyScoreChanged(CurrentScore);
        }

        public void AddScoreChangeListener(System.Action<int> listener)
        {
            _scoreChangedEvent.AddListener(listener);
        }

        public void RemoveScoreChangeListener(System.Action<int> listener)
        {
            _scoreChangedEvent.RemoveListener(listener);
        }

        private void Awake()
        {
            PrepareLeaderboardRecords();
            Load();
        }

        private void NotifyScoreChanged(int score)
        {
            _scoreChangedEvent.Notify(score);
        }

        private void Save()
        {
            var scorePointsStorage = GetScorePointsStorage();
            var storageRaw = JsonUtility.ToJson(scorePointsStorage);
            PlayerPrefs.SetString(nameof(ScorePointsBehaviour), storageRaw);
        }

        private void Load()
        {
            if (PlayerPrefs.HasKey(nameof(ScorePointsBehaviour)) == false) return;

            var storageRaw = PlayerPrefs.GetString(nameof(ScorePointsBehaviour), string.Empty);
            var scorePointsStorage = JsonUtility.FromJson<ScorePointsStorage>(storageRaw);
            InitLeaderboardRecords(scorePointsStorage);
        }

        private int GetHighScore()
        {
            return _leaderboardRecords.Max(record => record.Score);
        }

        private void PrepareLeaderboardRecords()
        {
            _leaderboardRecords.Clear();
            foreach (var leaderboardDescriptor in _leaderboardDescriptor.LeaderboardDescriptorList)
            {
                _leaderboardRecords.Add(new LeaderboardRecord()
                {
                    PlayerName = leaderboardDescriptor.UserName,
                    Score = leaderboardDescriptor.Score,
                    Rank = leaderboardDescriptor.Rank,
                    LevelId = leaderboardDescriptor.Level
                });
            }
        }

        private ScorePointsStorage GetScorePointsStorage()
        {
            return new ScorePointsStorage();
        }

        private void InitLeaderboardRecords(ScorePointsStorage scorePointsStorage)
        {
            _leaderboardRecords.Clear();
            _leaderboardRecords.AddRange(scorePointsStorage.LeaderboardRecords);
        }
    }

    [Serializable]
    public class ScorePointsStorage
    {
        public List<LeaderboardRecord> LeaderboardRecords;
    }

    [Serializable]
    public class LeaderboardRecord
    {
        public string PlayerName;
        public int Score;
        public int Rank;
        public string LevelId;
    }
}