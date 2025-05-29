using System.Collections.Generic;
using Game.Common;
using UnityEngine;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class ArachnoDominatorBehaviour : ActivatorBehaviour
    {
        [SerializeField] private List<ActivatorBehaviour> _weaponActivators = new List<ActivatorBehaviour>();
        [SerializeField] private BossHeadAttackBehaviour _headAttackBehaviour;

        public void SetTarget(Transform target)
        {
            _headAttackBehaviour.SetTarget(target);
            foreach (var activatorBehaviour in _weaponActivators)
            {
                var targetable = activatorBehaviour as ITargetable;
                if (targetable != null)
                {
                    targetable.SetTarget(target);
                }
            }
        }

        public void SetEnemyShipProvider(EnemyShipResourceProvider shipResourceProvider)
        {
            _headAttackBehaviour.DroneSpawner.SetEnemyResourceProvider(shipResourceProvider);
        }
        
        protected override void OnActivate()
        {
            base.OnActivate();
            _weaponActivators.ForEach(activator => activator.Activate());
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _weaponActivators.ForEach(activator => activator.Deactivate());
        }
        
        [ContextMenu("Activate")]
        private void TestActivateAllWeapons()
        {
            OnActivate();
        }

        [ContextMenu("Deactivate")]
        private void TestDeactivateAllWeapons()
        {
            OnDeactivate();
        }
    }
}
