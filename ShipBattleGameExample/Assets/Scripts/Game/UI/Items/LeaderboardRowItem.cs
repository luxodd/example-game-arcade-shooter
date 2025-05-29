using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Game.UI.Items
{
    public class LeaderboardRowItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _totalScoreText;
        [SerializeField] private TMP_Text _enemiesKilledText;
        [SerializeField] private TMP_Text _accuracyText;
        [SerializeField] private TMP_Text _timeStampText;
        [SerializeField] private TMP_Text _totalLevelsText;
        [SerializeField] private TMP_Text _levelText;
        
        [SerializeField] private Color _commonColor;
        [SerializeField] private Color _userColor;

        private List<TMP_Text> _texts = new List<TMP_Text>();
        
        public void SetFullLeaderboardData(int rank, string playerName, int totalScore)
        {
            _rankText.text = rank.ToString();
            _playerNameText.text = playerName;
            _totalScoreText.text = totalScore.ToString();
            
            _levelText.gameObject.SetActive(false);
            _totalLevelsText.gameObject.SetActive(true);
            
            // var customDateTimeFormat = "HH:mm\nMM/dd/yy";
            //
            // _timeStampText.text = timeStamp.ToString(customDateTimeFormat);
        }

        public void SetShortLeaderboardData(int rank, string playerName, int level, int totalScore, int enemiesKilled,
            int accuracy, DateTime timeStamp)
        {
            _rankText.text = rank.ToString();
            _playerNameText.text = playerName;
            _totalScoreText.text = totalScore.ToString();
            _enemiesKilledText.text = enemiesKilled.ToString();
            _accuracyText.text = accuracy.ToString();
            _timeStampText.text = timeStamp.ToString(CultureInfo.InvariantCulture);
            _levelText.text = level.ToString();
            
            _levelText.gameObject.SetActive(true);
            _totalLevelsText.gameObject.SetActive(false);
        }

        public void SetUserEntry(bool isUser)
        {
            SetColors(isUser ? _userColor : _commonColor);
        }

        private void Awake()
        {
            var texts = GetComponentsInChildren<TMP_Text>();
            _texts.AddRange(texts);
        }

        private void SetColors(Color color)
        {
            _texts.ForEach(text => text.color = color);
        }
        
        
    }
}
