using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Items;
using Game.UI.Views;
using Luxodd.Game.Scripts.Game.Leaderboard;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class LeaderboardWindowHandler : MonoBehaviour
    {
        private const string CloseKey = "Close";

        [SerializeField] private LeaderboardRowItem _leaderboardRowItemPrefab;
        private ILeaderboardWindowView _leaderboardWindowView;

        private Action _closeButtonClickCallback;
        private int _counter = 0;

        public void PrepareView(ILeaderboardWindowView leaderboardWindow)
        {
            _leaderboardWindowView = leaderboardWindow;
            _leaderboardWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeyboardKeySubmit);
        }

        public void ShowLeaderboard()
        {
            _leaderboardWindowView.Show();
        }

        public void HideLeaderboard()
        {
            _leaderboardWindowView.Hide();
        }

        public void SetCloseButtonClickCallback(Action action)
        {
            _closeButtonClickCallback = action;
            _leaderboardWindowView.SetCloseButtonClickCallback(OnCloseButtonClickHandler);
        }

        public void PrepareLeaderboardEntries(LeaderboardDataResponse leaderboardDataResponse)
        {
            const int DefaultEntriesAmount = 20;
            var userEntryData = leaderboardDataResponse.CurrentUserData;
            var leaderboardList = new List<LeaderboardData>();
            if (leaderboardDataResponse.Leaderboard is { Count: > 0 })
            {
                leaderboardList.AddRange(leaderboardDataResponse.Leaderboard);
            }

            leaderboardList.Add(userEntryData);
            leaderboardList = leaderboardList.OrderByDescending(data => data.TotalScore).ToList();
            if (leaderboardList.Count > DefaultEntriesAmount && leaderboardList[^1] != userEntryData)
            {
                leaderboardList.RemoveAt(leaderboardList.Count - 1);
            }

            var counter = 0;
            foreach (var leaderboardData in leaderboardList)
            {
                var leaderboardRowItem = Instantiate(_leaderboardRowItemPrefab);
                leaderboardRowItem.SetFullLeaderboardData(leaderboardData.Rank, leaderboardData.PlayerName,
                    leaderboardData.TotalScore);
                leaderboardRowItem.SetUserEntry(leaderboardData == userEntryData);

                if (counter >= DefaultEntriesAmount)
                {
                    _leaderboardWindowView.AddUserEntry(leaderboardRowItem);
                }
                else
                {
                    _leaderboardWindowView.AddLeaderboardEntry(leaderboardRowItem);
                }

                counter++;
            }
        }

        private void OnVirtualKeyboardKeySubmit(string stringValue)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnVirtualKeyboardKeySubmit)}] OK, key:{stringValue}");
            switch (stringValue)
            {
                case CloseKey:
                    OnCloseButtonClickHandler();
                    break;
            }
        }

        private void OnCloseButtonClickHandler()
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnCloseButtonClickHandler)}] OK, counter: {_counter}");
            if (_counter > 0) return;
            _counter++;

            _closeButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}