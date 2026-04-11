using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[Serializable]
public class RetainedData
{
    // 1. МЕТАДАННЫЕ И СИСТЕМНОЕ
    public string EconomyVersion = "1.0";
    public long LastOnlineTimestamp = 0L;
    public bool IsTutorialComplete = false;
    public bool IsTutorialKeyGiven = false;

    // 2. ВАЛЮТЫ И ЭКОНОМИКА
    public double SoftCurrency = 0.0;
    public double CurrencyStardust = 0.0;
    public int HardCurrency = 0;
    public int BlackMarketKeys = 0;

    // 3. ПРОГРЕСС И ПРЕСТИЖ
    public int UnlockedCategoryIndex = 0;
    public double LifetimeEarnings = 0.0;
    public int PrestigeCount = 0;
    public double PermanentPrestigeMultiplier = 1.0;

    // 4. КОЛЛЕКЦИИ (Постоянные)
    public List<int> UnlockedStickerIDs = new List<int>();
    public List<string> CompletedSetNames = new List<string>();

    // 5. ИНВЕНТАРЬ И ПОЛКИ (Сбрасываемые)
    public List<int> ShelfItemIDs = new List<int>();

    // 6. ЧЁРНЫЙ РЫНОК
    public List<int> OwnedArtifactIDs = new List<int>();
    public long LastKeyDropTimestamp = 0L;

    // 7. УЛУЧШЕНИЯ (Сбрасываемые)
    public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();

    // 8. МОНЕТИЗАЦИЯ (VIP)
    public bool IsVipSilver = false;
    public bool IsVipGold = false;
    public long VipExpiryTimestamp = 0L;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private const string SAVE_KEY = "idle_fallback_save";
    private const float AUTO_SAVE_INTERVAL = 30f;
    
    public RetainedData Data { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Load();
    }

    private void Start()
    {
        InvokeRepeating(nameof(Save), AUTO_SAVE_INTERVAL, AUTO_SAVE_INTERVAL);
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    Data = JsonConvert.DeserializeObject<RetainedData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] Failed to deserialize save data, creating new. Error: {e.Message}");
                    Data = new RetainedData();
                }
            }
            else
            {
                Data = new RetainedData();
            }
        }
        else
        {
            Data = new RetainedData();
        }

        if (Data == null)
            Data = new RetainedData();
    }

    public void Save()
    {
        if (Data == null) return;
        
        try
        {
            string json = JsonConvert.SerializeObject(Data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to serialize save data. Error: {e.Message}");
        }
    }
}
