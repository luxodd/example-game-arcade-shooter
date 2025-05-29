using UnityEngine;

namespace Game.UI.Handlers
{
    public class GameScreenHandler : MonoBehaviour
    {
        private IGameScreenView _gameScreenView;

        public void PrepareGameScreen(IGameScreenView gameScreenView)
        {
            _gameScreenView = gameScreenView;
        }

        public void OnDifficultyValueChanged(float difficulty)
        {
            _gameScreenView.SetDifficulty(difficulty);
        }

        public void OnEnemiesCountChanged(int count, int maxEnemies)
        {
            _gameScreenView.SetEnemiesCount(count, maxEnemies);
        }

        public void OnCooldownValueChanged(float cooldown)
        {
            _gameScreenView.SetCooldownValue(cooldown);
        }

        public void SetResponseText(string text)
        {
            _gameScreenView.SetResponseText(text);
        }

        public void SetPlayerLives(int lives)
        {
            _gameScreenView.SetPlayerLives(lives);
        }

        public void SetPlayerMaximumHealthPoints(int maximumHealthPoints)
        {
            _gameScreenView.SetPlayerMaximumHealthPoints(maximumHealthPoints);
        }

        public void SetPlayerHealthPoints(int healthPoints)
        {
            _gameScreenView.SetPlayerHealthPoints(healthPoints);
        }

        public void SetBossHealthPoints(int bossHealthPoints)
        {
            _gameScreenView.SetBossHealthPoints(bossHealthPoints);
        }

        public void SetBossMaximumHealthPoints(int maximumHealthPoints)
        {
            _gameScreenView.SetBossMaxHealthPoints(maximumHealthPoints);
        }

        public void ShowBossHealthBar()
        {
            _gameScreenView.ShowBossHealthBar();
        }

        public void HideBossHealthBar()
        {
            _gameScreenView.HideBossHealthBar();
        }

        public void ShowGameScreen()
        {
            _gameScreenView.Show();
        }

        public void HideGameScreen()
        {
            _gameScreenView.Hide();
        }

        public void SetPlayerScore(int score)
        {
            _gameScreenView.SetPlayerScore(score);
        }

        public void RecoveryHealthPoints(int healthPoints)
        {
            _gameScreenView.RecoveryHealthPoints(healthPoints);
        }
    }
}