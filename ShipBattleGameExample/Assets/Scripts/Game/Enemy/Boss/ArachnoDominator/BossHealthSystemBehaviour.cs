using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Audio;
using Game.Events;
using Luxodd.Game.HelpersAndUtils.Unit;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using Utils;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class BossHealthSystemBehaviour : MonoBehaviour, IVitalitySystem
    {
        public IIntReadOnlyProperty HealthPoints
        {
            get
            {
                if (_damageableUnit == null)
                {
                    PrepareDamageableUnit();
                }
                
                return _damageableUnit.HealthPoints;
            }
        }

        public SimpleEvent OnDeathEvent { get; private set; } = new SimpleEvent();
        public bool IsDead => _damageableUnit.IsDead;
        public int MaxHealthPoints
        {
            get
            {
                if (_damageableUnit == null)
                {
                   PrepareDamageableUnit();
                }
 
                return _damageableUnit.MaxHealthPoints;
            }
        }

        public DroneSpawnerBehaviour DroneSpawner => _headAttacker.DroneSpawner;
        
        [SerializeField] private List<DamageablePartBehaviour> _damageablePart = new List<DamageablePartBehaviour>();
        
        [SerializeField] private EnergyShieldBehaviour _energyShield;
        
        [SerializeField] private BossHeadAttackBehaviour _headAttacker;
        
        [SerializeField] private FloatMiniMaxValue _delayBetweenExplosions;
        [SerializeField] private IntMiniMaxValue _amountExplosions;
        
        [SerializeField] private List<Transform> _explosionPoints = new List<Transform>();
        [SerializeField] private ParticleSystem _explosionParticle;
        [SerializeField] private ParticleSystem _sparkleParticle;
        
        private IDamageableUnit _damageableUnit;
        
        public void RegenerateHealthPoints()
        {
            
        }

        public void Heal(int amount)
        {
            _damageableUnit.Heal(amount);
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            PrepareDamageableUnit();
        }

        private void PrepareDamageableUnit()
        {
            if (_damageableUnit != null) return;
            //calculate total HP in each damageable part and summarize it
            var totalHealthPoints = 0;
            foreach (var damageablePart in _damageablePart)
            {
                totalHealthPoints += damageablePart.MaxHealthPoints;
                damageablePart.OnDamageEvent.AddListener(OnDamageableUnitOnDamageHandler);
                damageablePart.OnDeathEvent.AddListener(()=> OnDamageableUnitOnDeath(damageablePart));
            }
            _damageableUnit = new VitalitySystem(totalHealthPoints);
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(PrepareDamageableUnit)}] OK, total health points: {totalHealthPoints}");
        }

        private void OnDamageableUnitOnDamageHandler(int damage)
        {
            //deal damage to boss inner
            _damageableUnit.DealDamage(damage);
        }

        private void OnDamageableUnitOnDeath(DamageablePartBehaviour damageablePartBehaviour)
        {
            // change color of energy shield
            //check if boss is dead
            var notDeadCount = _damageablePart.Count(damageablePart => damageablePart.IsDead == false);
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDamageableUnitOnDeath)}] OK, part name: {damageablePartBehaviour.name}, not dead: {notDeadCount}");
            if (notDeadCount > 1)
            {
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDamageableUnitOnDeath)}] OK, there are {notDeadCount} dead parties");
                _energyShield.DealDamageToShield();
            }
            else if (notDeadCount == 1)
            {
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDamageableUnitOnDeath)}] OK, only one part left");
                _energyShield.DestroyShield(OnDestroyShieldCompleted);
            }
            else
            {
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDamageableUnitOnDeath)}] OK, boss dead");
                BossDeathAnimation();
            }
        }

        private void OnDestroyShieldCompleted()
        {
            //activate boss head drone attack
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDestroyShieldCompleted)}] OK");
            _headAttacker.Activate();
        }

        [ContextMenu("Death Animation")]
        private void BossDeathAnimation()
        {
            _headAttacker.Deactivate();
            AudioManager.Instance.PlayMusic(MusicType.Boss1Transmission);
            StartCoroutine(BossDeathAnimationCoroutine());
        }

        private void OnBossDeathAnimationCompleted()
        {
            OnDeathEvent.Notify();
            EventAggregator.Post(this, new BossDefeatedEvent());
            AudioManager.Instance.PlayMusic(MusicType.Boss1OutroBegin);
        }

        private IEnumerator BossDeathAnimationCoroutine()
        {
            var counter = 0f;
            var explosionsAmount = _amountExplosions.GetRandom();

            while (counter < explosionsAmount)
            {
                var explosionPoint = GetRandomExplosionPoints();
                var isExplosion = UnityEngine.Random.value > 0.33f;
                var particlePrefab = isExplosion? _explosionParticle : _sparkleParticle;
                var particle = Instantiate(particlePrefab, explosionPoint.position, Quaternion.identity);
                particle.gameObject.SetActive(true);
                
                EventAggregator.Post(this, new BossExplosionEvent());
                
                counter++;
                yield return new WaitForSeconds(_delayBetweenExplosions.GetRandom());
            }
            
            OnBossDeathAnimationCompleted();
        }


        private int _previousIndex = -1;
        
        private Transform GetRandomExplosionPoints()
        {
            var index = UnityEngine.Random.Range(0, _explosionPoints.Count);
            int countMax = 10;
            var counter = 0;
            while (_previousIndex == index || counter < countMax)
            {
                index = UnityEngine.Random.Range(0, _explosionPoints.Count);
                counter++;
            }
            
            _previousIndex = index;
            return _explosionPoints[index];
        }
    }
}
