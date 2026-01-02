using Luxodd.Game;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.Input;
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

            var stick = ArcadeControls.GetStick();
            MovementVector = stick.Vector;
            IsMoving = MovementVector != Vector2.zero;

            if (ArcadeControls.GetButtonDown(ArcadeButtonColor.Black) || Input.GetKeyDown(_attackPrimaryKeyCode))
            {
                PrimaryAttack.Notify();
            }

            if (ArcadeControls.GetButtonDown(ArcadeButtonColor.Red)|| Input.GetKeyDown(_attackSecondaryKeyCode))
            {
                SecondaryAttack.Notify();
            }
        }
    }
}