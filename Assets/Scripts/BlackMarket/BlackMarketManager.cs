using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnclaimedAssets.Gacha;

public class BlackMarketManager : MonoBehaviour
{
    public static BlackMarketManager Instance { get; private set; }

    private const long KEY_DROP_COOLDOWN_SECONDS = 7200L;
    private const float KEY_DROP_CHANCE_PERCENT = 0.05f;

    private const float DEFAULT_HARD_CAP_PERCENT = 300f;
    private const float DEFAULT_PRESTIGE_SCALING = 0.1f;

    private List<BlackMarketArtifact> _artifactsDB = new List<BlackMarketArtifact>();

    private RetainedData Data => SaveManager.Instance.Data;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var asset = Resources.Load<TextAsset>("GameData/BlackMarketArtifacts");
        if (asset != null)
            _artifactsDB = JsonConvert.DeserializeObject<List<BlackMarketArtifact>>(asset.text) ?? new List<BlackMarketArtifact>();
        else
            Debug.LogError("[BlackMarketManager] BlackMarketArtifacts.json not found in Resources/GameData/");
    }

    private void OnEnable()
    {
        GachaController.OnLootBoxOpened += HandleLootBoxOpened;
    }

    private void OnDisable()
    {
        GachaController.OnLootBoxOpened -= HandleLootBoxOpened;
    }

    private void HandleLootBoxOpened(int categoryIndex)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - Data.LastKeyDropTimestamp < KEY_DROP_COOLDOWN_SECONDS) return;
        if (UnityEngine.Random.value * 100f >= KEY_DROP_CHANCE_PERCENT) return;

        Data.LastKeyDropTimestamp = now;
        AddKey();
        Debug.Log("[BlackMarketManager] Найден ключ ЧР! Дроп из коробки.");
    }

    public void AddKey()
    {
        Data.BlackMarketKeys++;
        SaveManager.Instance.Save();
    }

    public double GetBM_IPSBonus()
    {
        double rawBonusPercent = 0.0;
        foreach (int id in Data.OwnedArtifactIDs)
        {
            foreach (var artifact in _artifactsDB)
            {
                if (artifact.ID == id && artifact.EffectType == "+Total_IPS%")
                {
                    rawBonusPercent += artifact.EffectValue;
                    break;
                }
            }
        }

        double rawBonus = rawBonusPercent / 100.0;
        double hardCap = (DEFAULT_HARD_CAP_PERCENT / 100.0) * (1.0 + DEFAULT_PRESTIGE_SCALING * Data.PrestigeCount);
        return Math.Min(rawBonus, hardCap);
    }
}
