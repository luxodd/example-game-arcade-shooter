using Luxodd.Game;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.Input;
using UnityEngine;

namespace Game.PlayerShip
{
    public class KeyboardControlAdapter : MonoBehaviour, IControlAdapter
    {
        // Arcade joystick through plugin 1.2.0's ArcadeControls.GetJoystick(), with the 0.15 dead zone
        // (rescaled to reach 1 at full throw) and no axis inversion that 1.0.x's GetStick() gave in
        // player builds (its config asset sits in an Editor folder, so builds used the defaults).
        private static ArcadeStick ReadArcadeStick()
        {
            Vector2 raw = ArcadeControls.GetJoystick();
            float magnitude = raw.magnitude;
            if (magnitude <= 0.15f) return new ArcadeStick(0f, 0f);
            Vector2 v = raw.normalized * Mathf.Clamp01((magnitude - 0.15f) / 0.85f);
            return new ArcadeStick(v.x, v.y);
        }

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

            var stick = ReadArcadeStick();
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