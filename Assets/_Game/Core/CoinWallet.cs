using System;
using UnityEngine;

namespace HexWords.Core
{
    /// <summary>
    /// Soft-currency wallet for Coins.
    /// Persisted via PlayerPrefs. Default starting balance = 250.
    /// </summary>
    public class CoinWallet
    {
        private const string PrefKey      = "HexWords.Coins";
        private const string FirstRunKey  = "HexWords.CoinsInitialized";
        private const int    StartBalance = 250;

        private static CoinWallet _instance;
        public static CoinWallet Instance => _instance ??= new CoinWallet();

        public int Balance { get; private set; }

        public event Action<int> BalanceChanged;

        private CoinWallet()
        {
            if (PlayerPrefs.GetInt(FirstRunKey, 0) == 0)
            {
                Balance = StartBalance;
                PlayerPrefs.SetInt(PrefKey, Balance);
                PlayerPrefs.SetInt(FirstRunKey, 1);
                PlayerPrefs.Save();
            }
            else
            {
                Balance = PlayerPrefs.GetInt(PrefKey, StartBalance);
            }
        }

        /// <summary>Adds coins and fires BalanceChanged.</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            Persist();
            BalanceChanged?.Invoke(Balance);
        }

        /// <summary>
        /// Attempts to spend coins. Returns true and deducts if sufficient balance.
        /// Returns false and does nothing if insufficient.
        /// </summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Balance < amount) return false;

            Balance -= amount;
            Persist();
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        private void Persist()
        {
            PlayerPrefs.SetInt(PrefKey, Balance);
            PlayerPrefs.Save();
        }

        // Editor / test helper — resets wallet to default state
        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.DeleteKey(FirstRunKey);
            PlayerPrefs.Save();
            _instance = null;
        }
    }
}
