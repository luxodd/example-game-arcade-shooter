using UnityEngine;

namespace Luxodd.Game.Scripts.Runtime.UI
{
    [CreateAssetMenu(
        menuName = "Luxodd Unity Plugin/UI/Arcade Control Legend Config",
        fileName = "ArcadeControlLegendConfig")]
    public sealed class ArcadeControlLegendConfig : ScriptableObject
    {
        [Header("Gameplay button actions")]
        [SerializeField] private string _blackAction;
        [SerializeField] private string _redAction;
        [SerializeField] private string _greenAction;
        [SerializeField] private string _yellowAction;
        [SerializeField] private string _blueAction;
        [SerializeField] private string _purpleAction;

        public string BlackAction => _blackAction;
        public string RedAction => _redAction;
        public string GreenAction => _greenAction;
        public string YellowAction => _yellowAction;
        public string BlueAction => _blueAction;
        public string PurpleAction => _purpleAction;
    }
}
