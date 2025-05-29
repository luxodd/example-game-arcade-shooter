using System.Collections.Generic;
using UnityEngine;

namespace Game.Settings
{
    [CreateAssetMenu(menuName = "Create/Settings/Default Game Settings", fileName = "DefaultGameSettings", order = 0)]
    public class DefaultGameSettings : ScriptableObject
    {
        [field: SerializeField] public float MusicVolume { get; set; }
        [field: SerializeField] public float SfxVolume { get; set; }

        [field: SerializeField] public int CreditsCount { get; set; }
        [field: SerializeField] public int CreditsForGame { get; set; }
        [field: SerializeField] public int CreditsForContinueGame { get; set; }

        [field: SerializeField] public List<int> CreditsForDeposit { get; set; }
    }
}