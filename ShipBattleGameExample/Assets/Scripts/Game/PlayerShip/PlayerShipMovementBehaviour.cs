using System;
using Game.CameraInner;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.PlayerShip
{
    public interface IControlAdapter
    {
        Vector2 MovementVector { get; }
        bool IsMoving { get; }
        SimpleEvent PrimaryAttack { get; set; }
        SimpleEvent SecondaryAttack { get; set; }
    }

    public class PlayerShipMovementBehaviour : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Vector2 _shiftedFromBoundValue;
        [SerializeField] private float _defaultMovementSpeed = 0f;
        [SerializeField] private Transform _shadow = null;
        
        [SerializeField] private bool _isTestMovement = false;
        [SerializeField] private Transform _testLeftBounds;
        [SerializeField] private Transform _testRightBounds;
        
        private PlayerShipDescriptor _playerShipDescriptor = null;
        private IControlAdapter _controlAdapter;

        private Vector2 _movementVector = Vector2.zero;
        private Vector2 _idleMovementVector = Vector2.zero;
        private Vector2 _nextMovePosition = Vector2.zero;
        private Vector2 _idleMovePosition = Vector2.zero;

        private Vector3 _rotationVector = Vector3.zero;

        private PlayerShipState _playerShipState;
        
        private ISimpleEvent<PlayerShipState> _onPlayerShipStateChange;

        private Transform _leftBound;
        private Transform _rightBound;
        private ICameraBounds _cameraBoundsHandler;

        private CameraBounds _cameraBounds = new CameraBounds();

        private bool _isCanMove => _playerShipState is PlayerShipState.Invulnerable or PlayerShipState.Normal;
        
        private bool _activeMovement;

        public void ProvideDependencies(IControlAdapter controlAdapter,
            PlayerShipDescriptor playerShipDescriptor,
            ISimpleEvent<PlayerShipState> onPlayerShipStateChange,
            float defaultMovementSpeed)
        {
            _controlAdapter = controlAdapter;
            _playerShipDescriptor = playerShipDescriptor;
            _onPlayerShipStateChange = onPlayerShipStateChange;
            _defaultMovementSpeed = defaultMovementSpeed;
            
            onPlayerShipStateChange.AddListener(OnPlayerShipStateChange);

            if (_isTestMovement)
            {
                _leftBound = _testLeftBounds;
                _rightBound = _testRightBounds;
            }
        }

        public void SetMovementBounds(Transform leftBound, Transform rightBound, ICameraBounds cameraBounds)
        {
            _leftBound = leftBound;
            _rightBound = rightBound;
            _cameraBoundsHandler = cameraBounds;
        }

        public void SetLevelSpeed(float speed)
        {
            _defaultMovementSpeed = speed;
        }

        public void ActivateMovement()
        {
            _activeMovement = true;
        }

        public void DeactivateMovement()
        {
            _activeMovement = false;
        }

        private void FixedUpdate()
        {
            if (_activeMovement == false) return;
            
            if (_controlAdapter == null ||
                _controlAdapter.IsMoving == false ||
                _isCanMove == false)
            {

                if ((_controlAdapter == null || _controlAdapter.IsMoving != false) && _isCanMove == true) return;
                CalculateIdleMovementVector();
                _idleMovePosition = _rigidbody.position + _idleMovementVector;
                _rigidbody.MovePosition(_idleMovePosition);

                return;
            }
            
            RotateOnMotion();
            
            _movementVector = _controlAdapter.MovementVector;

            _nextMovePosition = _rigidbody.position +
                                _movementVector * (Time.fixedDeltaTime * _playerShipDescriptor.ShipSpeed);

            if (_movementVector.y == 0)
            {
                CalculateIdleMovementVector();
                _nextMovePosition += _idleMovementVector;
            }

            _cameraBounds = _isTestMovement ? GetTestCameraBounds() : _cameraBoundsHandler.GetCameraBounds();
            
            if (_nextMovePosition.x - _shiftedFromBoundValue.x <= _leftBound.position.x)
            {
                _nextMovePosition.x = _shiftedFromBoundValue.x + _leftBound.position.x;
            }

            if (_nextMovePosition.x + _shiftedFromBoundValue.x >= _rightBound.position.x)
            {
                _nextMovePosition.x = _rightBound.position.x - _shiftedFromBoundValue.x;
            }

            if (_nextMovePosition.y - _shiftedFromBoundValue.y <= _cameraBounds.BottomLeft.y)
            {
                _nextMovePosition.y = _shiftedFromBoundValue.y + _cameraBounds.BottomLeft.y;
            }

            if (_nextMovePosition.y + _shiftedFromBoundValue.y >= _cameraBounds.TopRight.y)
            {
                _nextMovePosition.y = _cameraBounds.TopRight.y - _shiftedFromBoundValue.y;
            }

            _rigidbody.MovePosition(_nextMovePosition);
        }

        private void CalculateIdleMovementVector()
        {
            _idleMovementVector = Vector2.up * (Time.fixedDeltaTime * _defaultMovementSpeed);
        }

        private void OnPlayerShipStateChange(PlayerShipState playerShipState)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnPlayerShipStateChange)}] OK, playerShipState: {playerShipState}");
            _playerShipState = playerShipState;
        }

        private void RotateOnMotion()
        {
            if (_controlAdapter == null) return;
            
            if (_controlAdapter.MovementVector.x > 0)
            {
                //rotate to right
                _rotationVector.y = _playerShipDescriptor.RotateAngle;
            }
            else if (_controlAdapter.MovementVector.x < 0)
            {
                //rotate to left
                _rotationVector.y = -_playerShipDescriptor.RotateAngle;
            }
            else
            {
                _rotationVector.y = 0f;
            }
            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(_rotationVector), Time.fixedDeltaTime * _playerShipDescriptor.RotationSpeed);
            _shadow.rotation = Quaternion.Slerp(_shadow.rotation, Quaternion.Euler(_rotationVector), Time.fixedDeltaTime * _playerShipDescriptor.RotationSpeed);
        }
        
        private void TestCalculateCameraBounds()
        {
            if (Camera.main == null) return;
            _cameraBounds.BottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
            _cameraBounds.TopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
        }

        private CameraBounds GetTestCameraBounds()
        {
            TestCalculateCameraBounds();
            return _cameraBounds;
        }
    }
}