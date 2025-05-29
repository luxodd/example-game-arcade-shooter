using UnityEngine;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class FlameActivatorBehaviour : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _flameParticles;

        public void ActivateFlame()
        {
            _flameParticles.gameObject.SetActive(true);
            _flameParticles.Play();
        }

        public void DeactivateFlame()
        {
            _flameParticles.Stop();
            _flameParticles.gameObject.SetActive(false);
        }
    
        private void Awake()
        {
            _flameParticles.Stop();
        }
    }
}
