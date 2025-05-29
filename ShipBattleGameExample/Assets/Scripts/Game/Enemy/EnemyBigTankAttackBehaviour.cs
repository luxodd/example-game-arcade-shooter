using System.Collections;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyBigTankAttackBehaviour : EnemyBaseAttackBehaviour
    {
        [SerializeField] private float _turretRotationSpeed;
        [SerializeField] private Transform _turretTransform;

        private bool _isActive;

        private Coroutine _turretRotationCoroutine;

        public void DestroyTurret()
        {
            Deactivate();
        }

        protected override void OnActivate()
        {
            _isActive = true;
            _turretRotationCoroutine = StartCoroutine(TurretRotation());
        }

        protected override void OnDeactivate()
        {
            _isActive = false;
            StopCoroutine(_turretRotationCoroutine);
        }

        private IEnumerator TurretRotation()
        {
            if (_isActive == false) yield break;

            while (_isActive)
            {
                if (_isActive == false) yield break;

                IsCanAttack = false;

                if (AttackTarget == false)
                {
                    yield break;
                }

                Vector2 direction = (AttackTarget.position - transform.position).normalized;

                Vector2 currentForward = transform.up;

                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                float turretAngle = Mathf.Atan2(currentForward.y, currentForward.x) * Mathf.Rad2Deg;
                float finalAngle = (targetAngle - turretAngle) - 180f;

                _turretTransform.localRotation = Quaternion.Lerp(_turretTransform.localRotation,
                    Quaternion.Euler(0, 0, finalAngle), _turretRotationSpeed * Time.deltaTime);

                if (_turretTransform.localRotation == Quaternion.Euler(0, 0, finalAngle))
                {
                    IsCanAttack = true;
                }

                yield return null;
            }
        }
    }
}