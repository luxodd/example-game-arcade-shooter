using Game.PlayerShip;
using Game.Weapons;
using UnityEngine;

namespace Game.ForTest
{
    public class TestSceneActivator : MonoBehaviour
    {
        [SerializeField] private PlayerShipBehaviour _playerShipBehaviour;
        [SerializeField] private KeyboardControlAdapter _keyboardControlAdapter;
        [SerializeField] private PlayerWeaponDescriptor _playerWeaponDescriptor;
        [SerializeField] private ProjectileResourceProvider _projectileResourceProvider;

        [SerializeField] private Transform _parentForProjectiles;

        [SerializeField] private float _defaultLevelSpeed;

        private PlayerWeaponService _playerWeaponService;

        private void Awake()
        {
            PreparePlayerWeaponService();
            PreparePlayerShipBehaviour();
            _keyboardControlAdapter.InTheGame();
        }

        private void Start()
        {
            ActivatePlayerShip();
        }

        private void PreparePlayerShipBehaviour()
        {
            _playerShipBehaviour.ProvideDependencies(_keyboardControlAdapter, _playerWeaponService, _defaultLevelSpeed);
        }

        private void PreparePlayerWeaponService()
        {
            _playerWeaponService = new PlayerWeaponService(_playerWeaponDescriptor, _projectileResourceProvider,
                _parentForProjectiles);
        }

        private void ActivatePlayerShip()
        {
            _playerShipBehaviour.Activate();
        }
    }
}