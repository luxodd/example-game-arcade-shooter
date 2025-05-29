using Core.Audio;
using Game.CameraInner;
using UnityEngine;

namespace Game.Level
{
    public class PlaySoundFxTriggerHandler : MonoBehaviour
    {
        [SerializeField] private SfxType _sfxType;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.TryGetComponent<CameraActivatorBehaviour>(out var activatorBehaviour) ==
                false) return;

            if (activatorBehaviour.IsActivate)
            {
                AudioManager.Instance.PlaySfx(_sfxType);
            }
        }
    }
}