using UnityEngine;

namespace Luxodd.Game.Scripts.Input
{
    public static class ArcadeControls
    {
        private static IArcadeControlsBackend _backend;
        private static ArcadeInputMappingConfig _mappingConfig;

        public static void SetInputMappingConfig(ArcadeInputMappingConfig config)
        {
            _mappingConfig = config;
            _backend?.SetInputMappingConfig(config);
        }

        public static void RegisterBackend(IArcadeControlsBackend backend)
        {
            _backend = backend;

            if (_backend == null)
            {
                return;
            }

            _backend.SetInputMappingConfig(_mappingConfig);
        }

        public static bool GetButton(ArcadeButtonColor buttonColor)
        {
            if (TryGetButtonFromBackend(buttonColor, out bool isPressed))
            {
                return isPressed;
            }

            return GetButtonLegacy(buttonColor);
        }

        public static bool GetButtonDown(ArcadeButtonColor buttonColor)
        {
            if (TryGetButtonDownFromBackend(buttonColor, out bool wasPressedThisFrame))
            {
                return wasPressedThisFrame;
            }

            return GetButtonDownLegacy(buttonColor);
        }

        public static bool GetButtonUp(ArcadeButtonColor buttonColor)
        {
            if (TryGetButtonUpFromBackend(buttonColor, out bool wasReleasedThisFrame))
            {
                return wasReleasedThisFrame;
            }

            return GetButtonUpLegacy(buttonColor);
        }

        public static Vector2 GetJoystick()
        {
            if (TryGetJoystickFromBackend(out Vector2 gamepadJoystick))
            {
                return gamepadJoystick;
            }

            return GetJoystickLegacy();
        }

        private static bool TryGetButtonFromBackend(ArcadeButtonColor buttonColor, out bool result)
        {
            IArcadeControlsBackend backend = _backend;
            if (backend == null || !backend.IsAvailable)
            {
                result = false;
                return false;
            }

            return backend.TryGetButton(buttonColor, out result);
        }

        private static bool TryGetButtonDownFromBackend(ArcadeButtonColor buttonColor, out bool result)
        {
            IArcadeControlsBackend backend = _backend;
            if (backend == null || !backend.IsAvailable)
            {
                result = false;
                return false;
            }

            return backend.TryGetButtonDown(buttonColor, out result);
        }

        private static bool TryGetButtonUpFromBackend(ArcadeButtonColor buttonColor, out bool result)
        {
            IArcadeControlsBackend backend = _backend;
            if (backend == null || !backend.IsAvailable)
            {
                result = false;
                return false;
            }

            return backend.TryGetButtonUp(buttonColor, out result);
        }

        private static bool TryGetJoystickFromBackend(out Vector2 result)
        {
            IArcadeControlsBackend backend = _backend;
            if (backend == null || !backend.IsAvailable)
            {
                result = default;
                return false;
            }

            return backend.TryGetJoystick(out result);
        }

        private static bool GetButtonLegacy(ArcadeButtonColor buttonColor)
        {
            return ArcadeInput.GetButton(buttonColor);
        }

        private static bool GetButtonDownLegacy(ArcadeButtonColor buttonColor)
        {
            return ArcadeInput.GetButtonDown(buttonColor);
        }

        private static bool GetButtonUpLegacy(ArcadeButtonColor buttonColor)
        {
            return ArcadeInput.GetButtonUp(buttonColor);
        }

        private static Vector2 GetJoystickLegacy()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float horizontal = UnityEngine.Input.GetAxisRaw("Horizontal");
            float vertical = UnityEngine.Input.GetAxisRaw("Vertical");

            return new Vector2(horizontal, vertical);
#else
            return Vector2.zero;
#endif
        }
    }
}
