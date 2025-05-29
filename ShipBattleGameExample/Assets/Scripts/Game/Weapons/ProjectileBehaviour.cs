using Game.Events;
using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Weapons
{
    public enum ProjectileOwner
    {
        None,
        Player,
        Enemy
    }

    public enum ProjectileDestroyReason
    {
        None,
        AutoDestroy,
        ByHit
    }

    public enum ProjectileHitImpact
    {
        None,
        Sparkle,
        Explode,
    }

    public class ProjectileBehaviour : MonoBehaviour
    {
        public int Damage { get; private set; }
        public ProjectileOwner Owner { get; private set; } = ProjectileOwner.None;
        public ProjectileDestroyReason DestroyReason { get; private set; }
        [field: SerializeField] public ProjectileHitImpact Impact { get; set; } = ProjectileHitImpact.None;
        private bool _isActive;
        private float _autoDestroyTime;

        private Vector3 _direction;
        private float _speed;

        public void SetProjectileData(ProjectileOwner owner, int damage, float speed, float autoDestroyTime,
            Vector3 position,
            Quaternion rotation,
            Vector3 direction)
        {
            Owner = owner;
            Damage = damage;
            _autoDestroyTime = autoDestroyTime;
            _speed = speed;
            transform.position = position;
            transform.rotation = rotation;
            _direction = direction;

            DestroyReason = ProjectileDestroyReason.AutoDestroy;

            Invoke(nameof(DeactivateInner), _autoDestroyTime);

            _isActive = true;
        }

        public void Deactivate()
        {
            DestroyReason = ProjectileDestroyReason.ByHit;
            CancelInvoke(nameof(DeactivateInner));
            DeactivateInner();
        }

        private void Update()
        {
            if (_isActive == false) return;
            {
                transform.position += _direction * (_speed * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            EventAggregator.Post(this, new ProjectileDestroyEvent()
                {
                    Owner = Owner,
                    Damage = Damage,
                    Reason = DestroyReason
                }
            );
        }

        private void DeactivateInner()
        {
            _isActive = false;
            Destroy(gameObject);
        }
    }
}