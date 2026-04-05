using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    // --- Data Models ---
    [Serializable]
    public class UpgradeData {
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

    [Serializable]
    public class LootItem {
        public int ID;
        public string Category;
        public string Name;
        public string Rarity;
        public double SellPrice;
        public string BoostType;
        public double BoostValue;
    }

    public class CategoryData {
        public string Name;
        public double UnlockCost;
        public double BoxPrice;
    }

    // --- State Variables ---
    [Header("Simulation State")]
    public double softCurrency = 0;
    public double baseIPS = 0;
    public double currentIPS = 0;
    public double baseClickPower = 1;
    public float autoCPS = 0f;

    private int timeInSeconds = 0;
    private int unlockedCategoryIndex = 0;

    [Header("Config")]
    public string overrideEconomyFolderPath = ""; // Если пусто, ищет в папке Economy в корне проекта (рядом с Assets)
    public int maxUnpacksPerSecond = 5; // Имитация скорости игрока (во избежание EV > 1 бесконечных циклов)

    // --- DB Data ---
    private List<UpgradeData> upgrades;
    private List<LootItem> lootTable;
    private List<CategoryData> categories;
    
    // --- Cached Logic Data ---
    private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
    private List<LootItem> shelf = new List<LootItem>(); // Макс 5 предметов
    private StreamWriter logWriter;
    private Dictionary<string, List<LootItem>> lootLookup = new Dictionary<string, List<LootItem>>();

    // Checkpoints Check
    private bool cp100k = false;
    private bool cp1M = false;
    private bool cp1B = false;

    private void Start()
    {
        string projRoot = string.IsNullOrEmpty(overrideEconomyFolderPath) 
                            ? Path.Combine(Application.dataPath, "../Economy") 
                            : overrideEconomyFolderPath;
                            
        LoadJSON(projRoot);
        InitCategoriesAndCache();

        string logFolderPath = Path.Combine(Application.dataPath, "../Logs");
        if (!Directory.Exists(logFolderPath)) Directory.CreateDirectory(logFolderPath);
        logWriter = new StreamWriter(Path.Combine(logFolderPath, $"SimulationLog_{DateTime.Now:MM-dd_HH-mm-ss}.txt"), false);

        LogEvent("=== ИНИЦИАЛИЗАЦИЯ И ЗАПУСК СИМУЛЯЦИИ ===");
        StartCoroutine(RunSimulation());
    }

    private void LoadJSON(string path)
    {
        string upgradesJson = File.ReadAllText(Path.Combine(path, "Upgrades.json"));
        string lootJson = File.ReadAllText(Path.Combine(path, "LootTable.json"));

        upgrades = JsonHelper.FromJson<UpgradeData>(upgradesJson).ToList();
        lootTable = JsonHelper.FromJson<LootItem>(lootJson).ToList();
    }

    private void InitCategoriesAndCache()
    {
        string[] catsLayout = { "Барахло", "Возвраты электроники", "Забытый багаж", "Складские остатки", 
                                "Таможенный конфискат", "Антиквариат", "Ювелирный лом", "Крипто-фермы", 
                                "Искусство", "Гос. конфискат" };
        double[] unlockCosts = { 0, 1500, 25000, 500000, 12000000, 450000000, 15000000000, 600000000000, 30000000000000, 2000000000000000 };

        categories = new List<CategoryData>();
        for (int i = 0; i < catsLayout.Length; i++) {
            string cName = catsLayout[i];
            var commons = lootTable.Where(x => x.Category == cName && x.Rarity == "Common").ToList();
            if (commons.Count > 0) {
                // Математически: CommonDropPrice = 0.2 * BoxPrice -> BoxPrice = CommonDropPrice * 5
                categories.Add(new CategoryData { Name = cName, UnlockCost = unlockCosts[i], BoxPrice = commons[0].SellPrice * 5.0 });
            }
        }

        // Cache Items for blazing-fast random pulls in memory without GC allocation spikes
        string[] rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Unique" };
        foreach (var c in categories) {
            foreach (var r in rarities) {
                lootLookup[$"{c.Name}_{r}"] = lootTable.Where(x => x.Category == c.Name && x.Rarity == r).ToList();
            }
        }
    }

    private IEnumerator RunSimulation()
    {
        int totalIterations = 14 * 24 * 60 * 60; // 14 дней

        for (int i = 0; i < totalIterations; i++)
        {
            timeInSeconds = i;
            UpdateBotState();

            // Чтобы редактор Unity не фризнул, пропуск кадра каждый 1 час ин-гейм времени
            if (i > 0 && i % 3600 == 0)
            {
                yield return null; 
            }
        }

        LogEvent("=== СИМУЛЯЦИЯ ЗАВЕРШЕНА ===");
        logWriter?.Close();
        Debug.Log("Simulation complete. Check Logs folder for details.");
    }

    private void UpdateBotState()
    {
        // 1. Пассивный доход
        softCurrency += currentIPS;

        // 2. Клики (ручные и авто)
        float manualCPS = (timeInSeconds < 1800) ? 5f : 0f;
        float totalCPS = manualCPS + autoCPS;

        if (totalCPS > 0)
        {
            double clickDamage = baseClickPower;
            double tickGain = manualCPS * clickDamage;

            // Обработка логики процента от силы авто-клика
            if (autoCPS > 0) {
                double autoMultiplier = 1.0 + (0.1 * GetUpgradeLevel("auto_clicker_power"));
                tickGain += autoCPS * (clickDamage * autoMultiplier);
            }
            softCurrency += tickGain;
        }

        // 3. Апгрейды (Жадный режим)
        bool upgradeBought = true;
        while (upgradeBought)
        {
            upgradeBought = false;
            // Пытаемся купить начиная с самого приоритетного
            if (TryBuyUpgrade("rusty_autoclicker")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("overdrive_module")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("auto_clicker_cps")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("auto_clicker_power")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("manual_tap_power")) { upgradeBought = true; continue; }
        }

        // 4. Открытие категорий
        for (int i = unlockedCategoryIndex + 1; i < categories.Count; i++)
        {
            if (softCurrency >= categories[i].UnlockCost)
            {
                unlockedCategoryIndex = i;
                LogEvent($"Открытие новой категории коробок: {categories[i].Name}");
            }
        }

        // 5. Анпакинг (покупка коробок высшей доступной категории)
        var targetCat = categories[unlockedCategoryIndex];
        int unpacksThisSecond = 0;
        while (softCurrency >= targetCat.BoxPrice && unpacksThisSecond < maxUnpacksPerSecond)
        {
            softCurrency -= targetCat.BoxPrice;
            OpenBox(targetCat);
            unpacksThisSecond++;
        }

        CheckCheckpoints();

        // End of the day summary
        if (timeInSeconds > 0 && timeInSeconds % 86400 == 0) {
            int d = (timeInSeconds / 86400);
            LogEvent($"-- ИТОГИ ДНЯ {d} -- SoftCurrency: {softCurrency:F0} | IPS: {currentIPS:F2} | ClickPow: {baseClickPower:F2}");
        }
    }

    private void OpenBox(CategoryData cat)
    {
        float p = UnityEngine.Random.value * 100f;
        string rarity = "Common";
        if (p < 55f) rarity = "Common";
        else if (p < 80f) rarity = "Uncommon";
        else if (p < 92f) rarity = "Rare";
        else if (p < 98f) rarity = "Epic";
        else if (p < 99.8f) rarity = "Legendary";
        else rarity = "Unique";

        List<LootItem> possible = lootLookup[$"{cat.Name}_{rarity}"];
        if (possible.Count > 0)
        {
            var item = possible[UnityEngine.Random.Range(0, possible.Count)];
            ProcessLoot(item);
        }
    }

    private void ProcessLoot(LootItem item)
    {
        // Неликвид продается сразу
        if (item.Rarity == "Common" || item.Rarity == "Uncommon")
        {
            softCurrency += item.SellPrice;
            return;
        }

        // Менеджмент полки (Rare+)
        if (shelf.Count < 5)
        {
            shelf.Add(item);
            if (item.Rarity != "Rare") LogEvent($"Установка предмета {item.Rarity} на полку: {item.Name} (+Boost)");
            RecalculateCalculatedStats();
        }
        else
        {
            // Полка заполнена — вычисляем худший предмет на основе агрегатора ценности (SellPrice)
            var worstItem = shelf.OrderBy(x => x.SellPrice).First();

            if (item.SellPrice > worstItem.SellPrice)
            {
                shelf.Remove(worstItem);
                softCurrency += worstItem.SellPrice;
                shelf.Add(item);
                
                if (item.Rarity != "Rare") LogEvent($"Установка {item.Rarity} на полку (замена {worstItem.Name}): {item.Name} (+Boost)");
                RecalculateCalculatedStats();
            }
            else
            {
                softCurrency += item.SellPrice; 
            }
        }
    }

    private bool TryBuyUpgrade(string id)
    {
        var u = upgrades.FirstOrDefault(x => x.UpgradeID == id);
        if (u == null) return false;

        int lvl = GetUpgradeLevel(id);
        if (lvl >= u.MaxLevel) return false;
        if (!string.IsNullOrEmpty(u.RequiredUpgrade) && GetUpgradeLevel(u.RequiredUpgrade) == 0) return false;

        double cost = MathEvaluateCostFormula(u, lvl);

        if (softCurrency >= cost)
        {
            softCurrency -= cost;
            upgradeLevels[id] = lvl + 1;
            LogEvent($"Покупка апгрейда: {u.Name} (Ур. {lvl + 1}) за {cost:F0}");
            RecalculateCalculatedStats();
            return true;
        }
        return false;
    }

    private void RecalculateCalculatedStats()
    {
        double shelfIPS = 0;
        double shelfMPCAdd = 0;

        foreach (var item in shelf)
        {
            if (item.BoostType == "Flat_IPS") shelfIPS += item.BoostValue;
            if (item.BoostType == "Mult_MPC") shelfMPCAdd += item.BoostValue; 
        }

        baseIPS = shelfIPS;
        currentIPS = baseIPS;
        
        // Согласно MacroBalance.md: MPC = Base_Click + (Total_IPS * 0.1)
        double manualTapPower = GetUpgradeLevel("manual_tap_power") * 1.5;
        double pureBaseClick = 1.0 + manualTapPower + shelfMPCAdd;
        
        baseClickPower = pureBaseClick + (currentIPS * 0.1);

        // Расчет авто-клика
        if (GetUpgradeLevel("overdrive_module") > 0) {
            autoCPS = 3.0f + GetUpgradeLevel("auto_clicker_cps");
        } else if (GetUpgradeLevel("rusty_autoclicker") > 0) {
            autoCPS = 0.33f;
        } else {
            autoCPS = 0f;
        }
    }

    private double MathEvaluateCostFormula(UpgradeData u, int level)
    {
        if (u.UpgradeID == "manual_tap_power")
            return u.BaseCost * Math.Pow(1.18, level) * Math.Max(1.0, Math.Floor(level / 50.0) * 1.4);
        
        if (u.UpgradeID == "auto_clicker_cps")
            return u.BaseCost * Math.Pow(1.70, level);
        
        if (u.UpgradeID == "auto_clicker_power")
            return u.BaseCost * Math.Pow(1.20, level) * Math.Max(1.0, Math.Floor(level / 25.0) * 1.5);
            
        return u.BaseCost; // Константа для rusty_autoclicker и overdrive_module
    }

    private int GetUpgradeLevel(string id) => upgradeLevels.ContainsKey(id) ? upgradeLevels[id] : 0;
    
    private void CheckCheckpoints()
    {
        if (!cp100k && softCurrency >= 100000) { cp100k = true; LogEvent("Достижение чекпоинта: 100k софт-валюты"); }
        if (!cp1M && softCurrency >= 1000000) { cp1M = true; LogEvent("Достижение чекпоинта: 1M софт-валюты"); }
        if (!cp1B && softCurrency >= 1000000000) { cp1B = true; LogEvent("Достижение чекпоинта: 1B софт-валюты"); }
    }

    private void LogEvent(string message)
    {
        int day = (timeInSeconds / 86400) + 1;
        int hour = (timeInSeconds % 86400) / 3600;
        int min = (timeInSeconds % 3600) / 60;
        int sec = timeInSeconds % 60;

        string ts = $"[Day {day}, {hour:D2}:{min:D2}:{sec:D2}]";
        string printMsg = $"{ts} {message}";

        Debug.Log(printMsg);
        logWriter?.WriteLine(printMsg);
        logWriter?.Flush(); 
    }

    private void OnDestroy()
    {
        logWriter?.Close();
    }
}

// --- JSON Wrapper for Root Arrays (Standard Engine Fix) ---
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
