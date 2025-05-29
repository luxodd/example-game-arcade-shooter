using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemyBaseMovementBehaviour : MonoBehaviour
    {
        public SimpleEvent MovementCompletedEvent { get; private set; } = new SimpleEvent();
        protected EnemyShipData _enemyShipData;
        protected bool _isMoving;

        public virtual void ProvideDependencies(EnemyShipData enemyShipData)
        {
            _enemyShipData = enemyShipData;
        }

        public virtual void Activate()
        {
            if (_isMoving) return;

            _isMoving = true;
        }

        public virtual void Deactivate()
        {
            _isMoving = false;
        }

        public virtual void Continue()
        {

        }
    }
}