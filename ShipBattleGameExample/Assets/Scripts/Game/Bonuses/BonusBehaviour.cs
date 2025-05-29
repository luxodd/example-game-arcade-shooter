using System;
using System.Collections;
using Game.CameraInner;
using Game.Common;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Game.Bonuses
{
    public class BonusBehaviour : ActivatorBehaviour
    {
        public enum MovementType
        {
            Linear,
            Spiral,
        }
        
        public BonusType Bonus => _bonusDescriptor.Bonus;
        public int BonusValue => _bonusDescriptor.BonusValue;
        
        [SerializeField] private BonusDescriptor _bonusDescriptor;
        
        [SerializeField] private FloatMiniMaxValue _movementSpeedRandom;
        [SerializeField] private FloatMiniMaxValue _rotationSpeedRandom;
        [SerializeField] private FloatMiniMaxValue _spiralRadiusRandom;
        [SerializeField] private FloatMiniMaxValue _movementAccelerationRandom;
        [SerializeField] private FloatMiniMaxValue _movementTimeRandom;
        
        private Coroutine _coroutine;
        private float _angle;
        private float _sign;
        private float _linearSpeed;
        private float _maximumSpeed;
        private float _maximumRotationSpeed;
        private float _spiralRadiusGrowth;
        private float _startRadius;
        private float _rotationAngle;
        private float _rotationSpeed;
        private float _movementTime;
        
        private float _timeCounter;
        
        private Vector2 _startPosition;
        private Vector2 _linearPosition;
        private Vector2 _direction;

        private bool _isRunOnce;
        
        protected override void OnActivate()
        {
            base.OnActivate();
            _coroutine = StartCoroutine(StartBonusMovement());
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            
            if (_coroutine == null) return;
            
            StopCoroutine(_coroutine);
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDeactivate)}] OK");
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.gameObject.TryGetComponent<CameraActivatorBehaviour>(out var cameraActivatorBehaviour))
                return;
            if (cameraActivatorBehaviour.IsActivate)
            {
                Activate();
            }
        }

        [ContextMenu("Activate Movement")]
        private void TestActivate()
        {
            Activate();
        }

        [ContextMenu("Deactivate Movement")]
        private void TestDeactivate()
        {
            Deactivate();
        }

        private IEnumerator StartBonusMovement()
        {
            //linear movement
            //circle movement
            //random direction
            while (_isActivated)
            {
                var movementType = UnityEngine.Random.value > 0.5 ? MovementType.Linear : MovementType.Spiral;
                _movementTime = _movementTimeRandom.GetRandom();
                _linearSpeed = _movementSpeedRandom.GetRandom();
                _maximumSpeed = _movementSpeedRandom.MaxValue;
                _startRadius = _spiralRadiusRandom.GetRandom();
                
                _rotationSpeed = _rotationSpeedRandom.GetRandom();
                
                _sign = Random.value > 0.5 ? -1 : 1;

                _angle = 0f;
                
                _direction = UnityEngine.Random.insideUnitCircle;
                _isRunOnce = false;
                
                _timeCounter = 0f;
                while (_timeCounter < _movementTime)
                {
                    if (movementType == MovementType.Linear)
                    {
                        LinearMovement();
                    }
                    else
                    {
                        SpiralMovement();
                    }
                    
                    _timeCounter += Time.deltaTime;
                    yield return null;
                }
                yield return null;    
            }
        }
        
        private void SpiralMovement()
        {
            _angle += _sign * (_rotationSpeed * Time.deltaTime);
            _startRadius += _spiralRadiusGrowth * Time.deltaTime;

            var y = _startRadius * Mathf.Cos(_angle);
            var x = _startRadius * Mathf.Sin(_angle);

            if (_isRunOnce == false)
            {
                _isRunOnce = true;
                _startPosition = transform.position - new Vector3(x, y, 0);
            }

            transform.position = _startPosition + new Vector2(x, y); 
        }

        private void LinearMovement()
        {
            _linearPosition = transform.position;
            _linearPosition += _direction * (Time.deltaTime * _linearSpeed);
            transform.position = _linearPosition;
        }
    }
}
