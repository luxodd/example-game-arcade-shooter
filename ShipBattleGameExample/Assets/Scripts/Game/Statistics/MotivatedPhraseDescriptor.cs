using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Statistics
{
    public enum StatsType
    {
        TotalScore = 0,
        EnemiesKilled = 1,
        Accuracy = 2,
        LevelProgress = 3,
        TimeTaken = 4,
    }

    [CreateAssetMenu(menuName = "Create/Statistic/Motivated Phrases Descriptor", fileName = "MotivatedPhraseDescriptor",
        order = 0)]
    public class MotivatedPhraseDescriptor : ScriptableObject
    {
        [SerializeField] private List<MotivatedPhraseData> _motivatedPhrases = new List<MotivatedPhraseData>();

        public MotivatedPhraseData GetMotivatedPhrase(StatsType statType)
        {
            var result = _motivatedPhrases.Find(phrase => phrase.Stats == statType);
            return result;
        }
    }

    [Serializable]
    public class MotivatedPhraseData
    {
        [field: SerializeField] public StatsType Stats { get; private set; }
        [field: SerializeField] public List<string> PhrasesIfBetter { get; private set; }
        [field: SerializeField] public List<string> PhrasesIfWorse { get; private set; }

        public string GetRandomPhrasesIfBetter()
        {
            var index = UnityEngine.Random.Range(0, PhrasesIfBetter.Count);
            var phrase = PhrasesIfBetter[index];
            return phrase;
        }

        public string GetRandomPhrasesIfWorse()
        {
            var index = UnityEngine.Random.Range(0, PhrasesIfBetter.Count);
            var phrase = PhrasesIfWorse[index];
            return phrase;
        }
    }
}