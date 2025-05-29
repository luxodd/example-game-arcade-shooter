using System.Collections.Generic;
using Core.Audio;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyBigTankBehaviour : EnemyBaseBehaviour
    {
        [SerializeField] private ParticleSystem _smokeEffect;
        [SerializeField] private ParticleSystem _fireEffect;
        [SerializeField] private GameObject _parentForParticles;
        [SerializeField] private float _delayBeforeParticlesAppear;

        [SerializeField] private SpriteRenderer _turretSprite;
        [SerializeField] private SpriteRenderer _chassisSprite;

        [SerializeField] private BoxCollider2D _chassisCollider;
        [SerializeField] private PolygonCollider2D _turretCollider;

        [SerializeField] private float _coefficientOfTurretDestruction = 0.3f;

        [SerializeField] private Color _destroyedColor;

        [SerializeField] private List<SfxType> _tankHitSounds = new List<SfxType>();

        private bool _isTurretAlive = true;

        protected override void OnAwake()
        {
            base.OnAwake();
            _damageableUnit.HealthPoints.AddListener(OnTankHealthPointValueChangedHandler);
            SetShipSpriteRenderer(_turretSprite);
            SetMainCollider(_turretCollider);
        }

        protected override void OnDestroyStateHandler()
        {
            base.OnDestroyStateHandler();

            CoroutineManager.DelayedAction(_delayBeforeParticlesAppear, () => { _parentForParticles.SetActive(true); });
        }

        protected override void OnDestroyInner()
        {
            base.OnDestroyInner();
            _damageableUnit?.HealthPoints.RemoveListener(OnTankHealthPointValueChangedHandler);
        }

        private void OnTankHealthPointValueChangedHandler(int healthPoint)
        {
            PlayHitSound();

            if (_isTurretAlive && healthPoint <= _damageableUnit.MaxHealthPoints * _coefficientOfTurretDestruction)
            {
                DestroyTurret();
                SetShipSpriteRenderer(_chassisSprite);
                SetMainCollider(_chassisCollider);
            }

            if (_damageableUnit.IsDead)
            {
                DestroyChassis();
            }
        }

        private void DestroyTurret()
        {
            _isTurretAlive = false;
            var tankAttackBehaviour = _attackBehaviour as EnemyBigTankAttackBehaviour;
            tankAttackBehaviour.DestroyTurret();

            var explosion = Instantiate(_explodeParticles);
            explosion.transform.position = _turretSprite.transform.position;
            explosion.gameObject.SetActive(true);
            explosion.Play();

            _turretSprite.gameObject.SetActive(false);

        }

        private void DestroyChassis()
        {
            PlayExplosionSound();
            var explosion = Instantiate(_explodeParticles);
            explosion.transform.position = _chassisSprite.transform.position;
            explosion.gameObject.SetActive(true);
            explosion.Play();
            ChangeChassisColorToBlack();
            DeactivateCollider();
            CoroutineManager.DelayedAction(0.5f, ShowFireParticlesAfterDestroying);
        }

        private void ShowSmokeParticlesAfterDestroying()
        {
            _smokeEffect.gameObject.SetActive(true);
            _smokeEffect.Play();
        }

        private void ChangeChassisColorToBlack()
        {
            _chassisSprite.color = _destroyedColor;
        }

        private void DeactivateCollider()
        {
            _chassisCollider.enabled = false;
            _turretCollider.enabled = false;
        }

        private void ShowFireParticlesAfterDestroying()
        {
            _smokeEffect.gameObject.SetActive(false);
            _fireEffect.gameObject.SetActive(false);

            _parentForParticles.SetActive(true);
            _fireEffect.gameObject.SetActive(true);
            _fireEffect.Play();

            CoroutineManager.DelayedAction(0.3f, ShowSmokeParticlesAfterDestroying);
        }

        private void PlayHitSound()
        {
            var index = UnityEngine.Random.Range(0, _tankHitSounds.Count);
            var soundType = _tankHitSounds[index];
            AudioManager.Instance.PlaySfx(soundType);
        }

        private void PlayExplosionSound()
        {
            AudioManager.Instance.PlayRandomExplosionSound();
        }
    }
}