using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Core.Audio
{
    public enum SfxType
    {
        //Game
        DroneX3Attack = 0,
        DroneX4Attack,
        TankAttack,
        RocketLauncher,
        Turret,
        PlayerMainAttack,
        DroneX3Impact,
        DroneX4Impact,
        TankImpact,
        TankImpactVersion2,
        PlayerMainImpact,
        ExplosionSmall,
        ExplosionLarge,
        ArachnoidActivate,
        EnergyShieldImpactMain,
        EnergyShieldImpactElectric,
        RocketLauncherImpact,
        RocketLauncherVersion2,
        ArachnoidActivateVersion2,
        ArachnoidActivateVersion3,
        ArachnoidActivateVersion4,
        TurretImpact,
        ExplosionExtraLarge,
        MetalHitVersion1,
        MetalHitVersion2,
        MetalHitVersion3,
        MetalHitVersion4,
        EnergyShieldImpact,
        EnergyShieldImpactVersion2,
        EnergyShieldImpactVersion3,
        EnergyShieldDown,
        ExplosionHuge,
        //UI
        ButtonClick,
    }

    public enum MusicType
    {
        Menu,
        Gameplay,
        //boss 1
        Boss1Intro,
        Boss1OutroBegin,
        Boss1OutroLoop,
        Boss1Fight1Loop,
        Boss1Fight2Loop,
        Boss1Transmission,
    }
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
                return _instance;
            }
        }
        
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        
        [SerializeField] private List<SfxData> _sfxDataList;
        [SerializeField] private List<MusicData> _musicDataList;
        [SerializeField] private List<AudioClip> _explosions;
        
        [SerializeField] private float _fadeDuration;

        public void SetupMusicDefaultVolume(float volume)
        {
            _musicSource.volume = volume;
        }

        public void SetupSfxDefaultVolume(float volume)
        {
            _sfxSource.volume = volume;
        }
        
        public void PlayMusic(MusicType musicType)
        {
            var musicData = _musicDataList.Find(music => music.MusicType == musicType);
            //fade to 0 switch to clip and play new one
            _musicSource.DOFade(0, _fadeDuration)
                .OnComplete(() =>
                {
                    _musicSource.clip = musicData.Clip;
                    _musicSource.Play();
                    _musicSource.DOFade(1, _fadeDuration);
                });
        }
        
        public void PlayExplosionSound()
        {
            var explosionSound = UnityEngine.Random.value > 0.5f ? SfxType.ExplosionSmall : SfxType.ExplosionLarge;
            PlaySfx(explosionSound);
        }

        public void PlaySfx(SfxType sfxType)
        {
            var sfxData = _sfxDataList?.Find(sfx => sfx.SfxType == sfxType);
            
            if (sfxData == null) return;
            
            _sfxSource?.PlayOneShot(sfxData.Clip);
        }

        public void PlayRocketLauncherSound()
        {
            var rocketLauncherSound = UnityEngine.Random.value > 0.5f ? SfxType.RocketLauncher : SfxType.RocketLauncherVersion2;
            PlaySfx(rocketLauncherSound);
        }

        public void PlayMetalHitSound()
        {
            var hitSoundEffects = new List<SfxType>()
            {
                SfxType.MetalHitVersion1, SfxType.MetalHitVersion2, SfxType.MetalHitVersion3, SfxType.TankImpact,
                SfxType.TankImpactVersion2, SfxType.TurretImpact, SfxType.MetalHitVersion4,
            };
            var index = UnityEngine.Random.Range(0, hitSoundEffects.Count);
            PlaySfx(hitSoundEffects[index]);
        }

        public void PlayEnergyShieldImpactSound()
        {
            var impactSoundEffects = new List<SfxType>()
            {
                SfxType.EnergyShieldImpact, SfxType.EnergyShieldImpactVersion2, SfxType.EnergyShieldImpactVersion3,
            };
            
            var index = UnityEngine.Random.Range(0, impactSoundEffects.Count);
            PlaySfx(impactSoundEffects[index]);
        }

        public void PlayEnergyShieldDownSound()
        {
            PlaySfx(SfxType.EnergyShieldDown);
        }

        public void PlayRandomExplosionSound()
        {
            var index = UnityEngine.Random.Range(0, _explosions.Count);
            var audioClip = _explosions[index];
            _sfxSource?.PlayOneShot(audioClip);
        }

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
    }

    [System.Serializable]
    public class SfxData
    {
        [field: SerializeField] public SfxType SfxType { get; set; }
        [field: SerializeField] public AudioClip Clip { get; private set; }
    }

    [System.Serializable]
    public class MusicData
    {
        [field: SerializeField] public MusicType MusicType { get; set; }
        [field: SerializeField] public AudioClip Clip { get; private set; }
    }
}


