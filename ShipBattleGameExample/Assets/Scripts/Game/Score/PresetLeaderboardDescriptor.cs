using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Score
{
    [CreateAssetMenu(menuName = "Create/Preset Leaderboard Descriptor", fileName = "PresetLeaderboardDescriptor",
        order = 0)]
    public class PresetLeaderboardDescriptor : ScriptableObject
    {
        [field: SerializeField] public List<LeaderboardDescriptor> LeaderboardDescriptorList { get; set; }
    }

    [Serializable]
    public class LeaderboardDescriptor
    {
        [field: SerializeField] public int Rank { get; private set; }
        [field: SerializeField] public int Score { get; private set; }
        [field: SerializeField] public string UserName { get; private set; }
        [field: SerializeField] public string Level { get; private set; }
    }
}