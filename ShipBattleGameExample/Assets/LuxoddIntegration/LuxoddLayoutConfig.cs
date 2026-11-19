using UnityEngine;

namespace Luxodd.Integration
{
    /// <summary>This game's layout settings for LuxoddLayout. Arcade shooter is a portrait game (640x860 web window, 1080x1920 UI): use the 9:16 preset. Nothing changes in that slot.</summary>
    internal static class LuxoddLayoutConfig
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure() =>
            LuxoddLayout.Configure(9f / 16f, LuxoddLayout.CameraFit.None, fitCanvases: true);
    }
}
