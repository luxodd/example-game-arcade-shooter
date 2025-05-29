using System.Collections;
using Core.Audio;
using DG.Tweening;
using UnityEngine;

namespace Game.CameraInner
{
    public interface ICameraBounds
    {
        CameraBounds GetCameraBounds();
    }
    
    public class CameraFollowBehaviour : MonoBehaviour, ICameraBounds
    {
        public Transform CurrentPosition => _cameraActivator.transform;
        
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _target;
        [SerializeField] private CameraActivatorBehaviour _cameraActivator;
        [SerializeField] private CameraActivatorBehaviour _cameraDeActivator;
        [SerializeField] private CameraActivatorBehaviour _cameraDeActivatorSecondary;
        [SerializeField] private float _speed;
        [SerializeField] private float _followSpeed = 2f;
        [SerializeField] private Vector3 _offsetForTarget;
        [SerializeField] private Vector3 _offsetForBounds;
        [SerializeField] private Vector3 _offsetForDeactivator;
        
        [SerializeField] private float _smallShakeDuration;
        [SerializeField] private float _bigShakeDuration;
        [SerializeField] private float _extraSmallShakeDuration;
        
        [SerializeField] private float _smallShakeStrength;
        [SerializeField] private float _smallShakeRandomness;
        [SerializeField] private int _smallShakeVibrato;
        
        [SerializeField] private float _bigShakeStrength;
        [SerializeField] private float _bigShakeRandomness;
        [SerializeField] private int _bigShakeVibrato;
        
        [SerializeField] private float _extraSmallShakeStrength;
        [SerializeField] private float _extraSmallRandomness;
        [SerializeField] private int _extraSmallShakeVibrato;
        
        private bool _isInTheGame = false;
        
        private Transform _leftBound;
        private Transform _rightBound;
        
        private CameraBounds _cameraBounds = new CameraBounds();

        public void SetStartPosition(Vector3 position)
        {
            transform.position = position;
        }
        
        public void SetMovementBounds(Transform leftBound, Transform rightBound)
        {
            _leftBound = leftBound;
            _rightBound = rightBound;
        }

        public void SetLevelSpeed(float levelSpeed)
        {
            _speed = levelSpeed;
        }

        public void InTheGame()
        {
            _isInTheGame = true;
            GetCameraBounds();
            _cameraActivator.SetPosition(_cameraBounds.TopRight);
            _cameraDeActivator.SetPosition(_cameraBounds.BottomLeft);
            _cameraDeActivatorSecondary.SetPosition(_cameraBounds.BottomLeft);
        }

        public void OutTheGame()
        {
            _isInTheGame = false;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        [ContextMenu("Small Shake")]
        public void SmallShake()
        {
            _camera.DOShakePosition(_smallShakeDuration, _smallShakeStrength, _smallShakeVibrato, _smallShakeRandomness);
            
            AudioManager.Instance.PlaySfx(SfxType.ExplosionLarge);
        }
        
        [ContextMenu("Extra Small Shake")]
        public void ExtraSmallShake()
        {
            _camera.DOShakePosition(_smallShakeDuration, _smallShakeStrength, _smallShakeVibrato, _smallShakeRandomness);

            AudioManager.Instance.PlaySfx(SfxType.ExplosionSmall);
        }

        [ContextMenu("Big Shake")]
        public void BigShake()
        {
            _camera.DOShakePosition(_bigShakeDuration, _bigShakeStrength, _bigShakeVibrato, _bigShakeRandomness);
            AudioManager.Instance.PlaySfx(SfxType.ExplosionExtraLarge);
        }
        
        private Vector3 _newPosition;
        private Vector3 _targetPosition;

        private void FixedUpdate()
        {
            if (_isInTheGame == false || _target == null) return;
            
            _targetPosition = _target.position + _offsetForTarget;
            _targetPosition.y = transform.position.y;
            
            _newPosition = Vector3.Lerp(transform.position, _targetPosition, _followSpeed * Time.fixedDeltaTime);
            
            if (_newPosition.x + _offsetForBounds.x > _rightBound.position.x)
            {
                _newPosition.x = _rightBound.position.x - _offsetForBounds.x;
            }
            
            if (_newPosition.x - _offsetForBounds.x < _leftBound.position.x)
            {
                _newPosition.x = _leftBound.position.x + _offsetForBounds.x;
            }
            
            transform.position = _newPosition;
        }

        private void Update()
        {
            if (_isInTheGame == false || _target == null) return;
            
            transform.position+= Vector3.up * (_speed * Time.deltaTime);
        }

        private void CalculateCameraBounds()
        {
            _cameraBounds.BottomLeft = _camera.ViewportToWorldPoint(new Vector3(0, 0, 0));
            _cameraBounds.TopRight = _camera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        }

        public CameraBounds GetCameraBounds()
        {
            CalculateCameraBounds();
            return _cameraBounds;
        }
        
        private IEnumerator ScreenShake(float duration, float magnitude)
        {
            Vector3 originalPos = Camera.main.transform.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, originalPos + new Vector3(x, y, 0), Time.deltaTime * _speed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Camera.main.transform.localPosition = new Vector3(0f, 0f, -10f);
        }
    }
    
    public struct CameraBounds
    {
        public Vector2 BottomLeft { get; set; }
        public Vector2 TopRight { get; set; }
    }
}
