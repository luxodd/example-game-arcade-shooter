using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using Utils;

namespace Game.Enemy
{
    public class EnemyDroneX4MovementBehaviour : EnemyBaseMovementBehaviour
    {
        [SerializeField] private List<MovementPhaseTiming> _phaseTimings = new List<MovementPhaseTiming>();

        [SerializeField] private float _spiralRadiusGrowth;
        [SerializeField] private Vector2 _diagonalSpeed;
        [SerializeField] private float _startRadius = 0f;

        [SerializeField] private float _appearingSpeed = 0f;
        [SerializeField] private float _appearingRotationSpeed = 0f;

        [SerializeField] private MovementPath _movementPath;

        [SerializeField] private PropellerRotationBehaviour _propellerRotation;

        [SerializeField] private List<MovementPhase> _testMovementPhases = new List<MovementPhase>();

        [SerializeField] private bool _isTesting = false;
        [SerializeField] private EnemyShipData _testEnemyShipData;

        [SerializeField] private float _angle = 0f;
        [SerializeField] private bool _isClockwise;
        private float _radius;

        public enum MovementPhase
        {
            None = -1,
            Appearing = 0,
            RunPropeller = 1,
            SpiralMovement = 2,
            Completed = 3
        }

        public enum MovementPath
        {
            None = -1,
            LeftSpiral = 0,
            RightSpiral = 1,
            ForwardSin = 2
        }

        [SerializeField] private Vector2 _appearingShiftingDirection;

        private MovementPhase _movementPhase;

        private Queue<MovementPhase> _movementPhases = new Queue<MovementPhase>();

        private Coroutine _coroutine;

        private Vector3 _currentPosition;
        private float _currentTimeCounter;
        private float _currentPhaseTime;

        private Vector2 _startPosition;
        private Vector2 _diagonalOffset;
        private float _sign;

        private Vector3 _endPosition;

        private float _maximumSpeed;

        public void SetStartRotation(Vector3 startRotation)
        {
            transform.rotation = Quaternion.Euler(startRotation);
        }

        public void PrepareMoveLeft()
        {
            _sign = 1f;
            _movementPath = MovementPath.LeftSpiral;
        }

        public void PrepareMoveRight()
        {
            _sign = -1f;
            _movementPath = MovementPath.RightSpiral;
            _diagonalSpeed = new Vector2(_diagonalSpeed.x * _sign, _diagonalSpeed.y);
        }

        public void PrepareMoveForward()
        {
            _sign = 1f;
            _diagonalSpeed = new Vector2(0f, _diagonalSpeed.y);
            _movementPath = MovementPath.ForwardSin;
        }

        public override void Activate()
        {
            base.Activate();
            _coroutine = StartCoroutine(StartMoving());
        }

        public override void Deactivate()
        {
            base.Deactivate();
            _currentPosition = transform.position;

            _isMoving = false;

            _propellerRotation.DeactivatePropellers();
            if (_coroutine == null) return;

            StopCoroutine(_coroutine);
        }

        public override void Continue()
        {
            base.Continue();

            _coroutine = StartCoroutine(StartMoving());
        }

        [ContextMenu("Activate Movement")]
        private void TestActivate()
        {

            _enemyShipData = _testEnemyShipData;
            Activate();
        }

        private void Awake()
        {
            PrepareMovementPhase();
            _sign = _isClockwise ? 1 : -1;
        }

        private void PrepareMovementPhase()
        {
            _movementPhases = _isTesting
                ? new Queue<MovementPhase>(_testMovementPhases)
                : new Queue<MovementPhase>(Enum.GetValues(typeof(MovementPhase)).Cast<MovementPhase>().ToList());
        }

        private IEnumerator StartMoving()
        {
            while (_movementPhases.Count > 0 && (_movementPhase == MovementPhase.None ||
                                                 _movementPhase != MovementPhase.Completed))
            {
                _movementPhase = _movementPhases.Dequeue();
                if (_movementPhase == MovementPhase.Completed)
                {
                    continue;
                }

                var movementPhaseTiming = GetPhaseTiming(_movementPhase);
                _isRunOnce = false;
                _currentTimeCounter = 0f;
                _currentPhaseTime = movementPhaseTiming.Duration.GetRandom();

                if (_endPosition == Vector3.zero)
                {
                    _endPosition = transform.up * ((-1) * _currentPhaseTime);
                }

                while (_currentTimeCounter < _currentPhaseTime)
                {
                    _currentTimeCounter += Time.deltaTime;
                    UpdateMovementPhase();
                    yield return null;
                }
            }

            OnCompletedMovementPhase();
        }

        private void UpdateMovementPhase()
        {
            switch (_movementPhase)
            {
                case MovementPhase.None:
                    break;
                case MovementPhase.Appearing:
                    OnAppearingMovementPhase();
                    break;
                case MovementPhase.RunPropeller:
                    OnAppearingMovementPhase();
                    OnRunPropellerMovementPhase();
                    break;
                case MovementPhase.SpiralMovement:
                    OnMovementPhase();
                    break;
                case MovementPhase.Completed:
                    OnCompletedMovementPhase();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MovementPhaseTiming GetPhaseTiming(MovementPhase phase)
        {
            var result = _phaseTimings.Find(x => x.Phase == phase);
            return result;
        }

        private void OnAppearingMovementPhase()
        {
            transform.position = Vector3.Lerp(transform.position,
                transform.position + _endPosition,
                Time.deltaTime * _appearingSpeed);

            transform.rotation =
                Quaternion.Lerp(transform.rotation, Quaternion.Euler(Vector3.zero),
                    _appearingRotationSpeed * Time.deltaTime);
        }

        private bool _isRunOnce = false;

        private void OnRunPropellerMovementPhase()
        {
            if (_isRunOnce) return;
            _isRunOnce = true;

            _propellerRotation.ActivatePropellers();
            PrepareSpiralMovementPhase();
        }

        private void PrepareSpiralMovementPhase()
        {
            _startPosition = transform.position;
            _maximumSpeed = _enemyShipData.MaximumSpeed.GetRandom();

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(PrepareSpiralMovementPhase)}] OK, angle: {_angle}");
        }

        private void OnMovementPhase()
        {
            switch (_movementPath)
            {
                case MovementPath.None:
                    break;
                case MovementPath.LeftSpiral:
                case MovementPath.RightSpiral:
                    SpiralMovement();
                    break;
                case MovementPath.ForwardSin:
                    SinMovement();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnCompletedMovementPhase()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnCompletedMovementPhase)}] OK");
            _propellerRotation.DeactivatePropellers();
            MovementCompletedEvent.Notify();
        }

        private void SpiralMovement()
        {
            _angle += _sign * (_maximumSpeed * Time.deltaTime);
            _startRadius += _spiralRadiusGrowth * Time.deltaTime;

            var y = _startRadius * Mathf.Cos(_angle);
            var x = _startRadius * Mathf.Sin(_angle);

            if (_isRunOnce == false)
            {
                _isRunOnce = true;
                _startPosition = transform.position - new Vector3(x, y, 0);
            }

            _diagonalOffset += new Vector2(_diagonalSpeed.x, _diagonalSpeed.y) * Time.deltaTime;

            transform.position = _startPosition + _diagonalOffset + new Vector2(x, y);
        }

        private void SinMovement()
        {
            _angle += _sign * (_maximumSpeed * Time.deltaTime);
            _startRadius += _spiralRadiusGrowth * Time.deltaTime;

            var x = _startRadius * Mathf.Sin(_angle);
            var y = 0f;

            if (_isRunOnce == false)
            {
                _isRunOnce = true;
                _startPosition = transform.position;
            }

            _diagonalOffset += new Vector2(_diagonalSpeed.x, _diagonalSpeed.y) * Time.deltaTime;

            transform.position = _startPosition + _diagonalOffset + new Vector2(x, y);
        }

        [Serializable]
        public class MovementPhaseTiming
        {
            [field: SerializeField] public MovementPhase Phase { get; set; }
            [field: SerializeField] public FloatMiniMaxValue Duration { get; set; }
        }
    }
}