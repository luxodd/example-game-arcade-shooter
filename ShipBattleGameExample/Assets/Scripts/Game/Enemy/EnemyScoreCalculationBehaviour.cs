using System.Collections;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyScoreCalculationBehaviour : MonoBehaviour
    {
        [SerializeField] private int _defaultScore = 10;
        public int Score => CalculateScore();
        private EnemyShipData _enemyShipData;

        private float _timeCounter;
        private float _delayTimeCounter;

        private Coroutine _coroutine;

        private bool _isPaused;

        public void ProvideDependencies(EnemyShipData shipData)
        {
            _enemyShipData = shipData;
        }

        public void Activate()
        {
            _coroutine = StartCoroutine(TimerCalculation());
        }

        public void Deactivate()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        private IEnumerator TimerCalculation()
        {
            _delayTimeCounter = 0f;
            while (_delayTimeCounter < _enemyShipData.DelayBeforeTimeScore)
            {
                if (_isPaused)
                {
                    yield return new WaitForEndOfFrame();
                }

                _delayTimeCounter += Time.deltaTime;
                yield return null;
            }

            _timeCounter = 0f;

            while (_timeCounter < _enemyShipData.DefaultTimeScore)
            {
                if (_isPaused)
                {
                    yield return new WaitForEndOfFrame();
                }

                _timeCounter += Time.deltaTime;
                yield return null;
            }
        }

        private int CalculateScore()
        {
            if (_enemyShipData == null) return _defaultScore;

            var timeProgress = _timeCounter / _enemyShipData.DefaultTimeScore;

            var score = Mathf.CeilToInt(Mathf.Lerp(_enemyShipData.ScorePointsMax, _enemyShipData.ScorePointsMin,
                timeProgress));
            return score;
        }
    }
}