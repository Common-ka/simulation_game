using System;
using UnityEngine;
using UnclaimedAssets.Core;
using UnclaimedAssets.Economy;

namespace UnclaimedAssets.Gacha
{
    public class GachaController : MonoBehaviour
    {
        public static GachaController Instance { get; private set; }

        public static event Action<int> OnLootBoxOpened;
        public static event Action<ShelfItemData> OnLootItemGenerated;

        [Serializable]
        public struct DropRate
        {
            public ItemRarity Rarity;
            [Tooltip("Probability in percentage (e.g., 55.0 for 55%)")]
            public float Probability;
        }

        [SerializeField]
        private DropRate[] _dropRates = new DropRate[]
        {
            new DropRate { Rarity = ItemRarity.Common, Probability = 55.0f },
            new DropRate { Rarity = ItemRarity.Uncommon, Probability = 25.0f },
            new DropRate { Rarity = ItemRarity.Rare, Probability = 12.0f },
            new DropRate { Rarity = ItemRarity.Epic, Probability = 6.0f },
            new DropRate { Rarity = ItemRarity.Legendary, Probability = 1.8f },
            new DropRate { Rarity = ItemRarity.Unique, Probability = 0.2f }
        };

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

        public void BuyLoot(int categoryIndex, double cost)
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("GachaController: GameManager not found.");
                return;
            }

            if (!gameManager.TrySpendCurrency(cost))
            {
                Debug.LogWarning("GachaController: Not enough currency to buy loot.");
                return;
            }

            ItemRarity droppedRarity = GetRandomRarity();
            int itemId = ProcessLoot(categoryIndex, droppedRarity);

            var lootData = new ShelfItemData
            {
                ItemID = itemId,
                Rarity = droppedRarity,
                Category = categoryIndex.ToString()
            };
            OnLootItemGenerated?.Invoke(lootData);
            OnLootBoxOpened?.Invoke(categoryIndex);
        }

        private ItemRarity GetRandomRarity()
        {
            float total = 0f;
            foreach (var drop in _dropRates)
            {
                total += drop.Probability;
            }

            float randomValue = UnityEngine.Random.Range(0f, total);
            float current = 0f;

            foreach (var drop in _dropRates)
            {
                current += drop.Probability;
                if (randomValue <= current)
                {
                    return drop.Rarity;
                }
            }

            return ItemRarity.Common; 
        }

        private int ProcessLoot(int categoryIndex, ItemRarity rarity)
        {
            // Dummy ID for now, since we don't have explicit LootTable matching logic here
            int dummyItemID = UnityEngine.Random.Range(1000, 9999);

            if (rarity == ItemRarity.Common || rarity == ItemRarity.Uncommon)
            {
                // In future: Price is fetched from Assets/Resources/GameData/LootTable.json
                Debug.Log($"GachaController: Rolled {rarity} item (ID: {dummyItemID}). Added to Inventory.");
                return dummyItemID;
            }
            else
            {
                // Rare, Epic, Legendary, Unique
                ShelfItemData shelfItem = new ShelfItemData
                {
                    ItemID = dummyItemID,
                    Rarity = rarity,
                    Category = categoryIndex.ToString(),
                    EffectType = UnityEngine.Random.value > 0.5f ? "Flat_IPS" : "Mult_MPC",
                    EffectValue = 1.0 // Placeholder value, would be calculated or fetched from LootTable
                };

                if (ShelfManager.Instance != null)
                {
                    bool added = ShelfManager.Instance.TryAddItem(shelfItem);
                    if (added)
                    {
                        Debug.Log($"GachaController: Rolled {rarity} item. Added to Shelf.");
                    }
                    else
                    {
                        // Slots are full, handle without error or auto-deletion
                        Debug.Log($"GachaController: Rolled {rarity} item, but shelf is full. Keeping in inventory or discarding.");
                    }
                }
            }
            return dummyItemID;
        }
    }
}
