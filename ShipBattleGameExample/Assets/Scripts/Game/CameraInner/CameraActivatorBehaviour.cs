using UnityEngine;

namespace Game.CameraInner
{
    public enum ActivatorType
    {
        Primary,
        Secondary
    }
    
    public class CameraActivatorBehaviour : MonoBehaviour
    {
        [field: SerializeField] public bool IsActivate { get;  private set; }
        [field: SerializeField] public ActivatorType ActivatorType { get;  private set; }
        [SerializeField] private Vector3 _shiftedPosition;

        public void SetPosition(Vector3 position)
        {
            var activatorPosition = gameObject.transform.position + _shiftedPosition;
            activatorPosition.y = position.y + _shiftedPosition.y;
            transform.position = activatorPosition;
        }
    }
}