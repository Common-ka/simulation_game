using System;
using UnityEngine;

namespace UnclaimedAssets.Core
{
    public struct GameSnapshot
    {
        public double SoftCurrency;
    }

    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private float _tickInterval = 1f;

        private double _softCurrency;

        public static event Action<GameSnapshot> OnGameStateChanged;

        public bool TrySpendCurrency(double amount)
        {
            if (_softCurrency >= amount)
            {
                _softCurrency -= amount;
                OnGameStateChanged?.Invoke(new GameSnapshot { SoftCurrency = _softCurrency });
                return true;
            }
            return false;
        }

        public void AddCurrency(double amount)
        {
            _softCurrency += amount;
            OnGameStateChanged?.Invoke(new GameSnapshot { SoftCurrency = _softCurrency });
        }

        private void Start()
        {
            InvokeRepeating(nameof(GameTick), 0f, _tickInterval);
        }

        private void GameTick()
        {
            double ips = 0.0;
            if (global::SaveManager.Instance != null && global::SaveManager.Instance.Data != null)
            {
                var shelf = UnclaimedAssets.Economy.ShelfManager.Instance;
                double prestigeMult = global::SaveManager.Instance.Data.PermanentPrestigeMultiplier;
                double blackMarketBonus = global::BlackMarketManager.Instance != null
                    ? global::BlackMarketManager.Instance.GetBM_IPSBonus()
                    : 0.0;
                var completedSets = global::SaveManager.Instance.Data.CompletedSetNames;
                
                ips = UnclaimedAssets.Economy.IPSCalculator.GetCurrentIPS(shelf, prestigeMult, blackMarketBonus, completedSets);
            }
            else
            {
                ips = 1.0;
            }

            _softCurrency += ips;
            OnGameStateChanged?.Invoke(new GameSnapshot { SoftCurrency = _softCurrency });
        }
    }
}
