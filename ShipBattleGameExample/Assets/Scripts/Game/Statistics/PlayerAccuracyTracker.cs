using System;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;

namespace Game.Statistics
{
    public class PlayerAccuracyTracker
    {
        public float Accuracy { get; private set; }

        private int _shotsFired;
        private int _shotsHit;

        private bool _isInTracking;

        public void StartTracking()
        {
            _isInTracking = true;
            _shotsFired = 0;
            _shotsHit = 0;
        }

        public void StopTracking()
        {
            _isInTracking = false;
            if (_shotsFired == 0)
            {
                Accuracy = 0;
            }
            else
            {
                Accuracy = (_shotsHit / (float)_shotsFired) * 100f;
            }

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(StopTracking)}] OK, accuracy : {Accuracy}, shots : {_shotsFired}, shots hit : {_shotsHit}");
        }

        public void IntermediateResult()
        {
            StopTracking();
        }

        public void PlayerShot()
        {
            if (_isInTracking == false) return;

            _shotsFired++;
        }

        public void PlayerHit()
        {
            if (_isInTracking == false) return;

            _shotsHit++;
        }
    }
}