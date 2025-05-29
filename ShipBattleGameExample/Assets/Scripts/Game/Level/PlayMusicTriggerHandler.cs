using Core.Audio;
using Game.CameraInner;
using UnityEngine;

namespace Game.Level
{
    public class PlayMusicTriggerHandler : MonoBehaviour
    {
        [SerializeField] private MusicType _musicType;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.gameObject.TryGetComponent<CameraActivatorBehaviour>(out var activatorBehaviour)) return;

            if (activatorBehaviour.IsActivate)
            {
                AudioManager.Instance.PlayMusic(_musicType);
            }
        }
    }
}