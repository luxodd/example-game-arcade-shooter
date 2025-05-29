namespace Game.Statistics
{
    public class PlayerEnemyKillsTracker
    {
        public int EnemiesKilled { get; private set; }

        private bool _isInTracking;

        public void StartTracking()
        {
            EnemiesKilled = 0;
            _isInTracking = true;
        }

        public void StopTracking()
        {
            _isInTracking = false;
        }

        public void KillEnemy()
        {
            if (_isInTracking == false) return;

            EnemiesKilled++;
        }
    }
}