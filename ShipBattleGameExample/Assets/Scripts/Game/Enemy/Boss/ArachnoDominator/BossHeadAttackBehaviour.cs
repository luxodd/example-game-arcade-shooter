using System;
using System.Collections;
using System.Collections.Generic;
using Core.Audio;
using DG.Tweening;
using Game.Common;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.Enemy.Boss.ArachnoDominator
{
    public class BossHeadAttackBehaviour : ActivatorBehaviour, ITargetable
    {
        public DroneSpawnerBehaviour DroneSpawner => _droneSpawnerBehaviour;
        [SerializeField] private List<SpriteRenderer> _droneStartPointSprites = new List<SpriteRenderer>();
        
        [SerializeField] private Collider2D _headCollider2D;
        
        [SerializeField] private float _hideAnimationDuration = 0.5f;
        [SerializeField] private Ease _hideEase;

        [SerializeField] private DroneSpawnerBehaviour _droneSpawnerBehaviour;
        
        private bool _isHeadAttackingActivated = false;
        private int _completedAnimation = 0;
        
        public Transform Target { get; private set; }
        public void SetTarget(Transform target)
        {
            Target = target;
            _droneSpawnerBehaviour.SetTarget(target);
        }
        
        private void Awake()
        {
            _headCollider2D.enabled = false;
            _droneSpawnerBehaviour.FirstSpawnEvent.AddListener(OnFirstSpawnHandler);
        }

        private void OnDestroy()
        {
            _droneSpawnerBehaviour?.FirstSpawnEvent.RemoveListener(OnFirstSpawnHandler);
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnActivate)}] OK");
            
            if (_isHeadAttackingActivated)
            {
                ActivateHeadAttackingFast();
            }
            else
            {
                StartCoroutine(ActivateHeadAttacking());
            }
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _droneSpawnerBehaviour.Deactivate();
        }

        [ContextMenu("Activate")]
        private void TestActivate()
        {
            Activate();
        }

        [ContextMenu("Deactivate")]
        private void TestDeactivate()
        {
            
        }

        private IEnumerator ActivateHeadAttacking()
        {
            var counter = 0;
            _completedAnimation = 0;
            while (counter < _droneStartPointSprites.Count)
            {
                var spriteRenderer = _droneStartPointSprites[counter];
                spriteRenderer.DOFade(0f, _hideAnimationDuration)
                    .SetEase(_hideEase)
                    .OnComplete(OnHideAnimationCompleted);
                counter++;
                yield return new WaitForSeconds(_hideAnimationDuration*0.5f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        private void ActivateHeadAttackingFast()
        {
            _droneStartPointSprites.ForEach(sprite => sprite.gameObject.SetActive(false));
            _completedAnimation = _droneStartPointSprites.Count;
            _isHeadAttackingActivated = true;
            
        }
        
        private void OnHideAnimationCompleted()
        {
            _completedAnimation++;
            _isHeadAttackingActivated = _completedAnimation >= _droneStartPointSprites.Count;
            if (_isHeadAttackingActivated)
            {
                
                PrepareDroneAttacking();
            }
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnHideAnimationCompleted)}] OK, completed animation: {_completedAnimation}");
        }

        private void PrepareDroneAttacking()
        {
            AudioManager.Instance.PlayMusic(MusicType.Boss1Fight2Loop);
            
            _droneSpawnerBehaviour.Activate();
        }
        
        private void OnFirstSpawnHandler()
        {
            _headCollider2D.enabled = true;
        }
    }
}
