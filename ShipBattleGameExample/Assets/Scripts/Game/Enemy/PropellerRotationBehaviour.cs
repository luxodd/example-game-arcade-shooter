using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Enemy
{
    public class PropellerRotationBehaviour : MonoBehaviour
    {
        [SerializeField] private List<Transform> _propellers = new List<Transform>();
        [SerializeField] private float _duration = 1f;
        [SerializeField] private Vector3 _rotationVector = new Vector3(0f, 0f, 0f);

        [SerializeField] private Ease _ease = Ease.Linear;

        private List<Tweener> _tweeners = new List<Tweener>();

        public void ActivatePropellers()
        {
            for (int i = 0; i < _propellers.Count; i++)
            {
                var tweener = _propellers[i].DOLocalRotate(_rotationVector, _duration, RotateMode.LocalAxisAdd)
                    .SetEase(_ease)
                    .SetLoops(-1, LoopType.Incremental);
                _tweeners.Add(tweener);
            }
        }

        public void DeactivatePropellers()
        {
            foreach (var tweener in _tweeners)
            {
                tweener?.Kill();
            }
        }

        [ContextMenu("Activate Propellers")]
        private void TestActivatePropellers()
        {
            ActivatePropellers();
        }

        [ContextMenu("Deactivate Propellers")]
        private void TestDeactivatePropellers()
        {
            DeactivatePropellers();
        }

        private void OnDestroy()
        {
            DeactivatePropellers();
        }
    }
}