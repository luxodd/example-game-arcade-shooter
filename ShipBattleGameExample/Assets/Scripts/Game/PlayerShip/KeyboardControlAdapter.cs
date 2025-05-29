using Luxodd.Game.HelpersAndUtils.Utils;
using UnityEngine;

namespace Game.PlayerShip
{
    public class KeyboardControlAdapter : MonoBehaviour, IControlAdapter
    {
        public Vector2 MovementVector { get; private set; }
        public bool IsMoving { get; private set; }
        public SimpleEvent PrimaryAttack { get; set; } = new SimpleEvent();
        public SimpleEvent SecondaryAttack { get; set; } = new SimpleEvent();

        [SerializeField] private KeyCode _attackPrimaryKeyCode = KeyCode.Space;
        [SerializeField] private KeyCode _attackSecondaryKeyCode = KeyCode.LeftShift;

        private bool _isInTheGame = false;

        public void InTheGame()
        {
            _isInTheGame = true;
        }

        public void OutTheGame()
        {
            _isInTheGame = false;
        }

        private void Update()
        {
            if (_isInTheGame == false) return;

            MovementVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            IsMoving = MovementVector != Vector2.zero;

            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(_attackPrimaryKeyCode))
            {
                PrimaryAttack.Notify();
            }

            if (Input.GetButtonDown("Fire2") || Input.GetKeyDown(_attackSecondaryKeyCode))
            {
                SecondaryAttack.Notify();
            }
        }
    }
}