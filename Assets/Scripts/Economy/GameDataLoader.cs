using System.Collections;
using Newtonsoft.Json;
using UnityEngine;

namespace UnclaimedAssets.Economy
{
    // ─── Data Models ──────────────────────────────────────────────────────────

    public class CategoryData
    {
        public string Name;
        public double UnlockCost;
    }

    public class PityMechanismData
    {
        public int ExpectedBoxesForSet;
        public float ExpectedDustPerBox;
        public int HardPityThresholdBoxes;
    }

    public class RarityIntTable
    {
        public int Common;
        public int Uncommon;
        public int Rare;
        public int Epic;
        public int Legendary;
        public int Unique;
        public int Black_Market_Exclusive;
    }

    public class SynergyBonusData
    {
        public float StandardSetMultiplier;
        public float BlackMarketSetMultiplier;
    }

    public class GachaMathData
    {
        public PityMechanismData PityMechanism;
        public RarityIntTable DustBurnTable;
        public RarityIntTable StickerCraftCost;
        public SynergyBonusData SynergyBonuses;
    }

    public class BMGlobalIpsLimit
    {
        public float BaseHardCapPercent;
        public float PrestigeScaling;
        public string MaxBonusFormula;
        public string AppliedFormula;
    }

    public class BlackMarketConfigData
    {
        public string MultiplierHandlingRules;
        public BMGlobalIpsLimit BM_Global_IPS_Limit;
    }

    public class UpgradeData
    {
        public string UpgradeID;
        public string Name;
        public string Description;
        public double BaseCost;
        public string CostFormula;
        public string EffectFormula;
        public bool DependsOnIPS;
        public string RequiredUpgrade;
        public int MaxLevel;
    }

    // ─── Loader ───────────────────────────────────────────────────────────────

    public class GameDataLoader : MonoBehaviour
    {
        public CategoryData[] Categories { get; private set; }
        public GachaMathData GachaMath { get; private set; }
        public BlackMarketConfigData BlackMarketConfig { get; private set; }
        public UpgradeData[] Upgrades { get; private set; }

        public IEnumerator LoadAsync()
        {
            Categories = Load<CategoryData[]>("GameData/Categories");
            GachaMath = Load<GachaMathData>("GameData/GachaMath");
            BlackMarketConfig = Load<BlackMarketConfigData>("GameData/BlackMarketConfig");
            Upgrades = Load<UpgradeData[]>("GameData/Upgrades");

            Debug.Log("[GameDataLoader] Done.");
            yield return null;
        }

        private T Load<T>(string resourcePath) where T : class
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[GameDataLoader] File not found: Resources/{resourcePath}");
                return null;
            }
            return JsonConvert.DeserializeObject<T>(asset.text);
        }
    }
}
