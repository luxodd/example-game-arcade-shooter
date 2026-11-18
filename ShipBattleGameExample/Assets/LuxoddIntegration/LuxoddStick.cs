using Luxodd.Game.Scripts.Input;
using UnityEngine;

namespace Luxodd.Integration
{
    /// <summary>
    /// The arcade joystick as plugin 1.0.x's ArcadeControls.GetStick() returned it, on plugin
    /// 1.2.0+ (where it is ArcadeControls.GetJoystick()): the same 0.15 dead zone, so a resting
    /// stick reads exactly zero.
    /// </summary>
    public static class LuxoddStick
    {
        public const float DefaultDeadZone = 0.15f;

        public static ArcadeStick Get(float deadZone = DefaultDeadZone)
        {
            Vector2 raw = ArcadeControls.GetJoystick();
            float magnitude = raw.magnitude;
            if (magnitude <= deadZone) return new ArcadeStick(0f, 0f);
            // Rescale so the stick still reaches 1 at full throw.
            Vector2 v = raw.normalized * Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
            return new ArcadeStick(v.x, v.y);
        }
    }
}
