using System.Collections.Generic;
using Luxodd.Game.HelpersAndUtils.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Player
{
    public enum MoneyType
    {
        Credits,
    }

    public class WalletService : MonoBehaviour
    {
        public IFloatReadOnlyProperty Credits => _credits;

        private FloatProperty _credits = new FloatProperty();

        private Dictionary<MoneyType, int> _money = new Dictionary<MoneyType, int>();

        public void AddCredits(float amount)
        {
            _credits.SetValue(_credits.Value + amount);
        }

        public void SetCredits(float amount)
        {
            _credits.SetValue(amount);
        }

        public void SpendCredits(float amount)
        {
            var remaining = _credits.Value - amount;
            if (remaining < 0)
                remaining = 0;

            _credits.SetValue(remaining);
            SaveData();
        }

        public bool CanSpendCredits(int amount)
        {
            return _credits.Value - amount >= 0;
        }

        private void Awake()
        {
            LoadData();
        }

        private void SaveData()
        {
            var storageData = GetStorageData();
            var saveData = JsonConvert.SerializeObject(storageData);
            PlayerPrefs.SetString(nameof(WalletService), saveData);
        }

        private void LoadData()
        {
            var saveData = PlayerPrefs.GetString(nameof(WalletService), null);
            if (string.IsNullOrEmpty(saveData)) return;

            var loadedData = JsonConvert.DeserializeObject<WalletStorageData>(saveData);
            if (loadedData == null) return;

            _credits.SetValue(loadedData.CreditsCount);
        }

        private WalletStorageData GetStorageData()
        {
            return new WalletStorageData()
            {
                CreditsCount = _credits.Value,
            };
        }
    }

    public class WalletStorageData
    {
        public float CreditsCount { get; set; }
    }
}