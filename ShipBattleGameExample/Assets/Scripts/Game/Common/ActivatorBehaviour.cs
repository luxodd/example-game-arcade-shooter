using UnityEngine;

namespace Game.Common
{
    public interface ITargetable
    {
        //few methods for targetable
        Transform Target { get; }
        void SetTarget(Transform target);
    }
    
    public class ActivatorBehaviour : MonoBehaviour
    {
        protected bool _isActivated = false;
        public void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            OnActivate();
        }

        public void Deactivate()
        {
            _isActivated = false;
            OnDeactivate();
        }

        protected virtual void OnActivate(){}
        protected virtual void OnDeactivate(){}
    }
}
