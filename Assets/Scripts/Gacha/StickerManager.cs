using System;
using System.Collections.Generic;
using UnityEngine;
using UnclaimedAssets.Economy;

namespace UnclaimedAssets.Gacha
{
    public class StickerManager : MonoBehaviour
    {
        public static StickerManager Instance { get; private set; }

        public static event Action<string> OnSetCompleted;

        [SerializeField] private float _stickerDropChance = 0.05f;

        private static readonly Dictionary<ItemRarity, int> DustBurnTable = new Dictionary<ItemRarity, int>
        {
            { ItemRarity.Common,    1    },
            { ItemRarity.Uncommon,  3    },
            { ItemRarity.Rare,      10   },
            { ItemRarity.Epic,      50   },
            { ItemRarity.Legendary, 250  },
            { ItemRarity.Unique,    1000 }
        };

        private static readonly Dictionary<ItemRarity, double> CraftCostTable = new Dictionary<ItemRarity, double>
        {
            { ItemRarity.Common,    5    },
            { ItemRarity.Uncommon,  15   },
            { ItemRarity.Rare,      50   },
            { ItemRarity.Epic,      250  },
            { ItemRarity.Legendary, 1250 },
            { ItemRarity.Unique,    5000 }
        };


        private RetainedData Data => SaveManager.Instance.Data;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GachaController.OnLootItemGenerated += HandleLootGenerated;
        }

        private void OnDisable()
        {
            GachaController.OnLootItemGenerated -= HandleLootGenerated;
        }

        private void HandleLootGenerated(ShelfItemData item)
        {
            if (UnityEngine.Random.value >= _stickerDropChance)
                return;

            if (Data.UnlockedStickerIDs.Contains(item.ItemID))
            {
                int dust = DustBurnTable[item.Rarity];
                Data.CurrencyStardust += dust;
                Debug.Log($"[StickerManager] Duplicate sticker (ID:{item.ItemID}, Rarity:{item.Rarity}). Dust +{dust}. Total: {Data.CurrencyStardust}");
            }
            else
            {
                HandleNewSticker(item.ItemID, item.Rarity, item.Category);
            }
        }

        private void HandleNewSticker(int itemId, ItemRarity rarity, string category)
        {
            Data.UnlockedStickerIDs.Add(itemId);
            Debug.Log($"[StickerManager] New sticker unlocked! ID:{itemId}, Category:{category}");
            CheckSetCompletion(category);
        }

        private void CheckSetCompletion(string category)
        {
            if (Data.CompletedSetNames.Contains(category))
                return;

            // TODO: Replace with full set completion check using LootTable data
            bool hasAnySticker = Data.UnlockedStickerIDs.Count > 0;
            if (!hasAnySticker)
                return;

            Data.CompletedSetNames.Add(category);
            OnSetCompleted?.Invoke(category);
            Debug.Log($"[StickerManager] Set completed: {category}! Multiplier 1.2x applied.");
        }

    }
}
