using System;
using System.Collections;
using System.Collections.Generic;
using Core.Audio;
using Game.Events;
using Game.Weapons;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.PlayerShip
{
    public class PlayerAttackBehaviour : MonoBehaviour
    {
        [SerializeField] private List<PlayerProjectileStartPointHelper> _projectileStartPointList =
            new List<PlayerProjectileStartPointHelper>();

        [SerializeField] private BigBossBombMissileBehaviour _bigBossBombMissileBehaviour;

        private Transform _parentForProjectiles;
        private ProjectileBehaviour _projectilePrefab;
        private PlayerWeaponDescriptor _primaryWeaponDescriptor;

        private PlayerWeaponService _primaryWeaponService;

        private IControlAdapter _controlAdapter;

        private bool _isPrimaryWeaponAttacking;
        private PlayerShipState _shipState;

        private bool IsCanAttack => _shipState is PlayerShipState.Normal or PlayerShipState.Invulnerable;

        public void ProvideDependencies(PlayerWeaponService primaryWeaponService,
            IControlAdapter controlAdapter,
            ISimpleEvent<PlayerShipState> playerShipStateChangedEvent)
        {
            _primaryWeaponService = primaryWeaponService;
            _controlAdapter = controlAdapter;
            _controlAdapter.PrimaryAttack.AddListener(OnPrimaryFire);

            playerShipStateChangedEvent.AddListener(OnPlayerShipStateChanged);
        }

        public void LaunchBigBossBomb(Transform target)
        {
            _bigBossBombMissileBehaviour.SetTarget(target);
            _bigBossBombMissileBehaviour.Activate();
        }

        private void OnDestroy()
        {
            _controlAdapter?.PrimaryAttack.RemoveListener(OnPrimaryFire);
        }

        private void OnPrimaryFire()
        {
            if (IsCanAttack == false) return;

            if (_isPrimaryWeaponAttacking) return;

            _isPrimaryWeaponAttacking = true;
            StartCoroutine(FirePrimaryWeapon());
            CoroutineManager.DelayedAction(_primaryWeaponService.Cooldown, () => _isPrimaryWeaponAttacking = false);
        }

        private void OnSecondaryFire()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(OnSecondaryFire)}] OK");
        }

        private IEnumerator FirePrimaryWeapon()
        {
            var amountCounter = 0;
            var playerShotEvent = new PlayerShotEvent();
            while (amountCounter < _primaryWeaponService.AmountInTime)
            {
                var startPointList = GetProjectileSpawnPoints(_primaryWeaponService.Level);

                foreach (var startPoint in startPointList)
                {
                    var projectile = Instantiate(_primaryWeaponService.ProjectilePrefab, startPoint.position,
                        startPoint.rotation);

                    projectile.transform.SetParent(_primaryWeaponService.ParentForProjectiles);
                    projectile.SetProjectileData(ProjectileOwner.Player, _primaryWeaponService.Damage,
                        _primaryWeaponService.ProjectileSpeed,
                        _primaryWeaponService.AutoDestroyTime, startPoint.position, startPoint.rotation, startPoint.up);

                    EventAggregator.Post(this, playerShotEvent);
                }

                AudioManager.Instance.PlaySfx(SfxType.PlayerMainAttack);
                amountCounter++;
                yield return new WaitForSeconds(_primaryWeaponService.FireRate);
            }
        }

        private List<Transform> GetProjectileSpawnPoints(int weaponLevel)
        {
            var startPointHelper = _projectileStartPointList.Find(startPoints => startPoints.Level == weaponLevel);
            return startPointHelper == null
                ? _projectileStartPointList[^1].StartPointList
                : startPointHelper.StartPointList;
        }

        private void OnPlayerShipStateChanged(PlayerShipState state)
        {
            _shipState = state;
        }
    }

    [Serializable]
    public class PlayerProjectileStartPointHelper
    {
        [field: SerializeField] public int Level { get; set; }
        [field: SerializeField] public List<Transform> StartPointList { get; set; }
    }
}