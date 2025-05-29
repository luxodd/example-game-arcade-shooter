using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Statistics
{
    public class ScoreManager : MonoBehaviour
    {
        public IIntReadOnlyProperty Score => _score;
        public IIntReadOnlyProperty HighScore => _highScore;
        public ISimpleEvent OnNewHighScoreEvent => _onNewHighScoreEvent;

        private IntProperty _score = new IntProperty();
        private IntProperty _highScore = new IntProperty();
        private SimpleEvent _onNewHighScoreEvent = new SimpleEvent();

        public void ClearScore()
        {
            _score.SetValue(0);
        }

        public void SetHighScore(int score)
        {
            _highScore.SetValue(score);
        }

        public void AddScore(int score)
        {
            var newScore = _score.Value + score;
            _score.SetValue(newScore);
        }

        public void RecordResult()
        {
            if (_score.Value <= _highScore.Value) return;

            _highScore.SetValue(_score.Value);
            _onNewHighScoreEvent.Notify();
        }
    }
}