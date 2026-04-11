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

        private void Start()
        {
            InvokeRepeating(nameof(GameTick), 0f, _tickInterval);
        }

        private void GameTick()
        {
            _softCurrency += 1;
            OnGameStateChanged?.Invoke(new GameSnapshot { SoftCurrency = _softCurrency });
        }
    }
}
