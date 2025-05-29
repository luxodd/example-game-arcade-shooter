using System;
using UnityEngine;

namespace Game.Statistics
{
    public class LevelStatisticData
    {
        public int TotalScore { get; private set; }
        public int EnemiesKilled { get; private set; }
        public int Accuracy { get; private set; }
        public float LevelProgress { get; private set; }
        public float TimeInSeconds { get; private set; }
        public DateTime TimeStamp { get; private set; }


        public static LevelStatisticData Create(int totalScore, int enemiesKilled, float accuracy,
            float levelProgress, float timeInSeconds, DateTime timeStamp)
        {
            return new LevelStatisticData()
            {
                TotalScore = totalScore,
                EnemiesKilled = enemiesKilled,
                Accuracy = Mathf.CeilToInt(accuracy),
                LevelProgress = levelProgress,
                TimeInSeconds = timeInSeconds,
                TimeStamp = timeStamp
            };
        }

        public static LevelStatisticData CreateRandom()
        {
            var totalScore = UnityEngine.Random.Range(500, 10000);
            var totalEnemies = UnityEngine.Random.Range(30, 50);
            var enemiesKilled = UnityEngine.Random.Range(1, totalEnemies);
            var accuracy = UnityEngine.Random.Range(0, 100);
            var levelProgress = UnityEngine.Random.Range(0f, 100f);
            var timeTaken = UnityEngine.Random.Range(10f, 150f);
            return new LevelStatisticData()
            {
                TotalScore = totalScore,
                EnemiesKilled = enemiesKilled,
                Accuracy = accuracy,
                LevelProgress = levelProgress,
                TimeInSeconds = timeTaken,
                TimeStamp = DateTime.Now
            };
        }
    }
}