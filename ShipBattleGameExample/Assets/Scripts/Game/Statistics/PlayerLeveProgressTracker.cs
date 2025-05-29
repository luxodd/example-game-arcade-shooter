using System;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Statistics
{
    public class PlayerLeveProgressTracker
    {
        public float LevelProgress { get; private set; }

        private Transform _startLevelPosition;
        private Transform _endLevelPosition;
        private Transform _currentLevelPosition;

        public void SetupLevelPoints(Transform startLevelPosition, Transform endLevelPosition)
        {
            _startLevelPosition = startLevelPosition;
            _endLevelPosition = endLevelPosition;
        }

        public void SetCurrentLevelPosition(Transform currentLevelPosition)
        {
            _currentLevelPosition = currentLevelPosition;
        }

        public void LevelComplete()
        {
            _currentLevelPosition = _endLevelPosition;
        }

        public void StartTracking()
        {
            LevelProgress = 0f;
        }

        public void StopTracking()
        {
            CalculateLevelProgression();
        }

        private void CalculateLevelProgression()
        {
            var totalDistance = _endLevelPosition.position - _startLevelPosition.position;
            var playerDistance = _currentLevelPosition.position - _startLevelPosition.position;
            LevelProgress = playerDistance.y / totalDistance.y * 100f;

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(CalculateLevelProgression)}] OK, " +
                $"level progress: {LevelProgress}, total distance: {totalDistance}, player distance: {playerDistance}");
        }
    }
}