using System.Collections.Generic;
using Core.UI;
using Game.UI.Items;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public interface IGameScreenView : IView
    {
        void SetDifficulty(float difficulty);
        void SetEnemiesCount(int count, int maxEnemies);

        void SetResponseText(string text);
        void SetCooldownValue(float cooldown);

        void SetPlayerLives(int lives);
        void SetPlayerScore(int score);

        void SetPlayerMaximumHealthPoints(int maxHP);
        void SetPlayerHealthPoints(int healthPoint);
        void RecoveryHealthPoints(int healthPoint);

        void ShowBossHealthBar();
        void HideBossHealthBar();
        void SetBossHealthPoints(int healthPoint);
        void SetBossMaxHealthPoints(int maxHP);
    }

    public class GameScreenView : BaseView, IGameScreenView
    {
        public override ViewType ViewType => ViewType.GameScreen;

        [SerializeField] private TMP_Text _difficultyText;
        [SerializeField] private TMP_Text _enemiesCountText;

        [SerializeField] private TMP_Text _scoreText;

        [SerializeField] private TMP_Text _responseText;
        [SerializeField] private TMP_Text _spawnCooldownText;

        [SerializeField] private List<GameObject> _playerLives = new List<GameObject>();

        [SerializeField] private HealthPointViewBehaviour _playerHealthPointViewBehaviour;
        [SerializeField] private HealthPointViewBehaviour _bossHealthPointViewBehaviour;

        private string _difficultyFormat;
        private string _enemiesCountFormat;
        private string _spawnCooldownFormat;
        private string _scoreFormat;

        public void SetDifficulty(float difficulty)
        {
            _difficultyText.text = string.Format(_difficultyFormat, difficulty);
        }

        public void SetEnemiesCount(int count, int maxEnemies)
        {
            _enemiesCountText.text = string.Format(_enemiesCountFormat, count, maxEnemies);
        }

        public void SetResponseText(string text)
        {
            _responseText.text = text;
        }

        public void SetCooldownValue(float cooldown)
        {
            _spawnCooldownText.text = string.Format(_spawnCooldownFormat, cooldown);
        }

        public void SetPlayerLives(int lives)
        {
            ApplyPlayerLives(lives);
        }

        public void SetPlayerScore(int score)
        {
            _scoreText.text = string.Format(_scoreFormat, score);
        }

        public void SetPlayerMaximumHealthPoints(int maxHP)
        {
            _playerHealthPointViewBehaviour.Setup(maxHP);
        }

        public void SetPlayerHealthPoints(int healthPoint)
        {
            _playerHealthPointViewBehaviour.SetHP(healthPoint);
        }

        public void ShowBossHealthBar()
        {
            _bossHealthPointViewBehaviour.gameObject.SetActive(true);
        }

        public void HideBossHealthBar()
        {
            _bossHealthPointViewBehaviour.gameObject.SetActive(false);
        }

        public void SetBossHealthPoints(int healthPoint)
        {
            _bossHealthPointViewBehaviour.SetHP(healthPoint);
        }

        public void SetBossMaxHealthPoints(int maxHP)
        {
            _bossHealthPointViewBehaviour.Setup(maxHP);
        }

        public void RecoveryHealthPoints(int healthPoint)
        {
            _playerHealthPointViewBehaviour.RecoveryHealthPoints(healthPoint);
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _difficultyFormat = _difficultyText.text;
            _enemiesCountFormat = _enemiesCountText.text;
            _spawnCooldownFormat = _spawnCooldownText.text;
            _scoreFormat = _scoreText.text;

            HideBossHealthBar();
        }

        private void ApplyPlayerLives(int lives)
        {
            for (int i = 0; i < _playerLives.Count; i++)
            {
                _playerLives[i].SetActive(i < lives);
            }
        }
    }
}