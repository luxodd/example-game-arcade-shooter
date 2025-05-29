using Game.CameraInner;
using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.Level
{
    public enum TriggerType
    {
        LevelEnd,
        BossStarted,
        ActivateEnemies,
        DeactivateEnemies,
        CameraStopped,
    }
    
    public class TriggerHandler : MonoBehaviour
    {
        public ISimpleEvent<TriggerType> TriggerEvent => _triggerEvent;
        [SerializeField] private TriggerType _triggerType;
        
        private readonly SimpleEvent<TriggerType> _triggerEvent = new SimpleEvent<TriggerType>();

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<CameraActivatorBehaviour>(out var activatorBehaviour))
            {
                _triggerEvent.Notify(_triggerType);
            }
        }
    }
}
