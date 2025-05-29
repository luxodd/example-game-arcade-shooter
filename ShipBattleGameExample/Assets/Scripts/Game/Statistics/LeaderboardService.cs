using System;
using System.Collections.Generic;
using Luxodd.Game.Scripts.Game.Leaderboard;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Statistics
{
    public class LeaderboardService : MonoBehaviour
    {
        public IReadOnlyCollection<LeaderboardData> CommonLeaderboardData => _commonLeaderboardData;
        public IReadOnlyCollection<LeaderboardData> LeaderboardDataByLevel => _leaderboardDataByLevel;

        public LeaderboardDataResponse LeaderboardData { get; private set; }

        [SerializeField] private WebSocketCommandHandler _webSocketCommandHandler;
        [SerializeField] private MotivatedPhraseDescriptor _motivatedPhraseDescriptor;

        [SerializeField] private TextAsset _defaultLeaderboardDataJson;

        private LevelStatisticData _previousLevelStatisticData;

        private readonly List<LevelStatisticData> _allLevelStatisticData = new List<LevelStatisticData>();
        private readonly List<LeaderboardData> _commonLeaderboardData = new List<LeaderboardData>();
        private readonly List<LeaderboardData> _leaderboardDataByLevel = new List<LeaderboardData>();

        private Action _onLeaderboardRequestSuccessCallback;
        private Action<string> _onLeaderboardRequestFailureCallback;

        public void LogPlayerLevelResult(LevelStatisticData levelStatisticData)
        {
            _previousLevelStatisticData = levelStatisticData;
            _allLevelStatisticData.Add(levelStatisticData);
        }

        public Tuple<LevelStatisticData, string> CompareLevelStatisticData(LevelStatisticData currentLevelStatisticData)
        {
            if (_previousLevelStatisticData == null) return null;

            var difference = GetDifferenceLevelStatisticData(currentLevelStatisticData);
            var phrase = GetMotivatedPhrase(difference);
            return Tuple.Create(difference, phrase);
        }

        public void PullLastLeaderboardData(Action onSuccessCallback, Action<string> onFailureCallback)
        {
            _onLeaderboardRequestSuccessCallback = onSuccessCallback;
            _onLeaderboardRequestFailureCallback = onFailureCallback;
            _webSocketCommandHandler.SendLeaderboardRequestCommand(OnLeaderboardPullLatestDataSuccessHandler,
                OnLeaderboardPullLatestDataFailureHandler);
        }

        private void Awake()
        {
            ParseDefaultLeaderboardData();
        }

        private LevelStatisticData GetDifferenceLevelStatisticData(LevelStatisticData levelStatisticData)
        {
            var previous = _previousLevelStatisticData;
            var scoreDifference = previous.TotalScore == 0
                ? 100
                : ((levelStatisticData.TotalScore - previous.TotalScore) / (float)previous.TotalScore) * 100;
            var enemiesKilledDifference = previous.EnemiesKilled == 0
                ? 100
                : ((levelStatisticData.EnemiesKilled - previous.EnemiesKilled) / (float)previous.EnemiesKilled) * 100;
            var accuracyDifference = previous.Accuracy == 0
                ? 100
                : ((levelStatisticData.Accuracy - previous.Accuracy) / (float)previous.Accuracy) * 100;
            var levelProgressDifference = previous.LevelProgress == 0
                ? 100
                : ((levelStatisticData.LevelProgress - previous.LevelProgress) / (float)previous.LevelProgress) * 100;
            var timeInSecondsDifference = previous.TimeInSeconds == 0
                ? 100
                : ((levelStatisticData.TimeInSeconds - previous.TimeInSeconds) / (float)previous.TimeInSeconds) * 100;

            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(GetDifferenceLevelStatisticData)}] OK, Real Value scoreDifference: {scoreDifference}" +
                $"enemiesKilledDifference: {enemiesKilledDifference}, accuracyDifference: {accuracyDifference}," +
                $"levelProgressDifference: {levelProgressDifference}, timeInSecondsDifference: {timeInSecondsDifference}");

            var difference = LevelStatisticData.Create((int)scoreDifference, (int)enemiesKilledDifference,
                accuracyDifference,
                levelProgressDifference,
                timeInSecondsDifference, DateTime.Now);

            return difference;
        }

        private string GetMotivatedPhrase(LevelStatisticData difference)
        {
            var phrases = new List<string>();
            var phraseData = _motivatedPhraseDescriptor.GetMotivatedPhrase(StatsType.TotalScore);

            phrases.Add(difference.TotalScore > 0
                ? phraseData.GetRandomPhrasesIfBetter()
                : phraseData.GetRandomPhrasesIfWorse());

            phraseData = _motivatedPhraseDescriptor.GetMotivatedPhrase(StatsType.EnemiesKilled);

            phrases.Add(difference.EnemiesKilled > 0
                ? phraseData.GetRandomPhrasesIfBetter()
                : phraseData.GetRandomPhrasesIfWorse());

            phraseData = _motivatedPhraseDescriptor.GetMotivatedPhrase(StatsType.Accuracy);

            phrases.Add(difference.Accuracy > 0
                ? phraseData.GetRandomPhrasesIfBetter()
                : phraseData.GetRandomPhrasesIfWorse());

            phraseData = _motivatedPhraseDescriptor.GetMotivatedPhrase(StatsType.LevelProgress);

            phrases.Add(difference.LevelProgress > 0
                ? phraseData.GetRandomPhrasesIfBetter()
                : phraseData.GetRandomPhrasesIfWorse());

            phraseData = _motivatedPhraseDescriptor.GetMotivatedPhrase(StatsType.TimeTaken);

            phrases.Add(difference.TimeInSeconds > 0
                ? phraseData.GetRandomPhrasesIfBetter()
                : phraseData.GetRandomPhrasesIfWorse());

            var index = UnityEngine.Random.Range(0, phrases.Count);
            return phrases[index];
        }

        private void ParseDefaultLeaderboardData()
        {
            var leaderboardDataJson = _defaultLeaderboardDataJson.text;
            var defaultLeaderboardResponse =
                JsonConvert.DeserializeObject<LeaderboardResponseCommand>(leaderboardDataJson);
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(ParseDefaultLeaderboardData)}] OK, raw: {leaderboardDataJson}, total: {defaultLeaderboardResponse.Data.Leaderboard.Count}");
            LeaderboardData = defaultLeaderboardResponse.Data;
            _commonLeaderboardData.Clear();
        }

        private void OnLeaderboardPullLatestDataSuccessHandler(LeaderboardDataResponse leaderboardDataResponse)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLeaderboardPullLatestDataSuccessHandler)}] OK");
            LeaderboardData = leaderboardDataResponse;
            _onLeaderboardRequestSuccessCallback?.Invoke();
        }

        private void OnLeaderboardPullLatestDataFailureHandler(int statusCode, string errorMessage)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLeaderboardPullLatestDataFailureHandler)}] OK, error: {errorMessage}");
            _onLeaderboardRequestFailureCallback?.Invoke(errorMessage);
        }
    }
}