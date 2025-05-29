using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyPathMovementBehaviour : EnemyBaseMovementBehaviour
    {
        [SerializeField] private List<Transform> _waypoints = new List<Transform>();

        [SerializeField] private float _rangeBeforeNextWaypoint;

        private List<Vector3> _waypointPositions = new List<Vector3>();

        private int _currentWaypointIndex;
        private int _lastWaypointIndex;
        private int TotalWaypoints => _waypoints.Count;

        private float _currentSpeed;

        private Vector3 _currentPosition;

        private Coroutine _coroutine;

        private Vector3 _startPosition;

        private bool _isWaypointReady = false;

        public override void Activate()
        {
            if (_isMoving) return;

            _isMoving = true;

            PrepareWaypointPositions();

            _startPosition = transform.position;
            _coroutine = StartCoroutine(StartMovement());
        }

        public override void Deactivate()
        {
            base.Deactivate();
            _isMoving = false;
            _lastWaypointIndex = _currentWaypointIndex;
            if (_coroutine == null) return;
            StopCoroutine(_coroutine);
        }

        public override void Continue()
        {
            base.Continue();

            if (_isMoving) return;
            _isMoving = true;
            _coroutine = StartCoroutine(StartMovement());
        }

        private IEnumerator StartMovement()
        {
            _currentSpeed = 0f;
            _currentWaypointIndex = _lastWaypointIndex != 0 ? _lastWaypointIndex : 0;
            if (_currentWaypointIndex >= TotalWaypoints)
            {
                yield break;
            }

            var distanceToPoint = Vector3.Distance(_startPosition, _waypointPositions[_currentWaypointIndex]);
            var currentTime = 0f;
            var direction = (_waypointPositions[_currentWaypointIndex] - _startPosition).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;

            var randomDelay = _enemyShipData.RandomDelay.GetRandom();
            var maxSpeed = _enemyShipData.MaximumSpeed.GetRandom();

            yield return new WaitForSeconds(randomDelay);
            _currentPosition = _startPosition;

            while (_currentWaypointIndex < TotalWaypoints)
            {
                _currentSpeed += _enemyShipData.Acceleration * Time.deltaTime;

                _currentSpeed = Mathf.Clamp(_currentSpeed, 0f, maxSpeed);

                _currentPosition += direction * (_currentSpeed * Time.deltaTime);

                transform.position = _currentPosition;

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle),
                    Time.deltaTime * _enemyShipData.RotationSpeed);

                currentTime += Time.deltaTime;

                var distance = Vector3.Distance(transform.position, _waypointPositions[_currentWaypointIndex]);
                if (distance < _rangeBeforeNextWaypoint)
                {
                    _startPosition = transform.position;
                    _currentWaypointIndex++;

                    if (_currentWaypointIndex < TotalWaypoints)
                    {
                        currentTime = 0f;
                        direction = (_waypointPositions[_currentWaypointIndex] - _startPosition).normalized;
                        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;
                    }
                }

                yield return null;
            }

            _isMoving = false;
            MovementCompletedEvent.Notify();
        }

        private void PrepareWaypointPositions()
        {
            if (_isWaypointReady) return;

            _isWaypointReady = true;

            _waypointPositions.Clear();
            var waypointParent = _waypoints[0].parent;
            if (waypointParent != null)
            {
                var mirrorScale = UnityEngine.Random.value > 0.5f ? Vector3.one : new Vector3(-1f, 1f, 1f);
                waypointParent.localScale = mirrorScale;
            }

            _waypointPositions.AddRange(_waypoints.Select(wayPoint => wayPoint.position));
        }
    }
}