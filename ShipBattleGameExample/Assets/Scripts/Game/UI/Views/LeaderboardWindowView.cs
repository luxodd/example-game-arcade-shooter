using System;
using System.Collections.Generic;
using Core.UI;
using Game.UI.Items;
using Game.UI.Items.Keyboard;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Views
{
    public interface ILeaderboardWindowView : IView
    {
        VirtualKeyboardNavigator KeyboardNavigator { get; }
        void SetCloseButtonClickCallback(Action closeButtonClicked);
        void AddLeaderboardEntry(LeaderboardRowItem leaderboardEntry);
        void AddUserEntry(LeaderboardRowItem leaderboardEntry);
        void ClearLeaderboardEntries();
    }

    public class LeaderboardWindowView : BaseView, ILeaderboardWindowView
    {
        public override ViewType ViewType => ViewType.LeaderboardWindow;
        public VirtualKeyboardNavigator KeyboardNavigator => _keyboardNavigator;

        [SerializeField] private VirtualKeyboardNavigator _keyboardNavigator;
        [SerializeField] private Button _closeButton;

        [SerializeField] private Transform _leaderboardEntryContainer;
        [SerializeField] private Transform _leaderboardParentForUserEntry;

        private Action _closeButtonClicked;

        private List<LeaderboardRowItem> _leaderboardEntries = new List<LeaderboardRowItem>();

        public void SetCloseButtonClickCallback(Action closeButtonClicked)
        {
            _closeButtonClicked = closeButtonClicked;
        }

        public void AddLeaderboardEntry(LeaderboardRowItem leaderboardEntry)
        {
            leaderboardEntry.transform.SetParent(_leaderboardEntryContainer, false);
            _leaderboardEntries.Add(leaderboardEntry);
        }

        public void AddUserEntry(LeaderboardRowItem leaderboardEntry)
        {
            leaderboardEntry.transform.SetParent(_leaderboardParentForUserEntry, false);
            _leaderboardEntries.Add(leaderboardEntry);
            _leaderboardParentForUserEntry.gameObject.SetActive(true);
        }

        public void ClearLeaderboardEntries()
        {
            while (_leaderboardEntries.Count > 0)
            {
                var leaderboardEntry = _leaderboardEntries[0];
                _leaderboardEntries.Remove(leaderboardEntry);
                Destroy(leaderboardEntry.gameObject);
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _keyboardNavigator.Activate();
        }

        protected override void OnHide()
        {
            base.OnHide();
            _keyboardNavigator.Deactivate();
        }

        private void OnCloseButtonClicked()
        {
            _closeButtonClicked?.Invoke();
        }
    }
}