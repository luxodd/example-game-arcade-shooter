using System;

namespace Game.Statistics
{
    public class PlayerLevelTimeTracker
    {
        public float Time => (float)(_endTime - _startTime).TotalSeconds;
        private DateTime _startTime;
        private DateTime _endTime;

        public void StartTracking()
        {
            _startTime = DateTime.Now;
        }

        public void StopTracking()
        {
            _endTime = DateTime.Now;
        }
    }
}