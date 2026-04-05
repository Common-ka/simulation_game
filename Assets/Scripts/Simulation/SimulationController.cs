using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public class SimulationController : MonoBehaviour
{
    // --- Data Models (Newtonsoft.Json) ---
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
        public double MaxLevel; 
    }

    [Serializable]
    public class LootItem {
        public double ID;
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
    public double softCurrency = 0.0;
    public double baseIPS = 0.0;
    public double currentIPS = 0.0;
    public double baseClickPower = 1.0; 
    public double currentClickPower = 1.0;
    public double autoCPS = 0.0; 

    private double timeInSeconds = 0;
    private int unlockedCategoryIndex = 0;

    [Header("Config")]
    public string overrideEconomyFolderPath = ""; 
    public double maxUnpacksPerSecond = 5; 

    // --- DB Data ---
    private List<UpgradeData> upgrades;
    private List<LootItem> lootTable;
    private List<CategoryData> categories;
    
    // --- Cached Logic Data ---
    private Dictionary<string, double> upgradeLevels = new Dictionary<string, double>();
    private List<LootItem> shelf = new List<LootItem>(); 
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

        upgrades = JsonConvert.DeserializeObject<List<UpgradeData>>(upgradesJson);
        lootTable = JsonConvert.DeserializeObject<List<LootItem>>(lootJson);
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
                categories.Add(new CategoryData { Name = cName, UnlockCost = unlockCosts[i], BoxPrice = commons[0].SellPrice * 5.0 });
            }
        }

        string[] rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Unique" };
        foreach (var c in categories) {
            foreach (var r in rarities) {
                lootLookup[$"{c.Name}_{r}"] = lootTable.Where(x => x.Category == c.Name && x.Rarity == r).ToList();
            }
        }
    }

    private IEnumerator RunSimulation()
    {
        double totalIterations = 14.0 * 24.0 * 60.0 * 60.0; // Исключительно double во избежание int overflow

        for (double i = 0; i < totalIterations; i++)
        {
            timeInSeconds = i;
            UpdateBotState();

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
        // --- БЛОК 0: Фарминг (Начисление пассивного и клик-дохода) ---
        softCurrency += currentIPS;

        double manualCPS = (timeInSeconds < 1800) ? 5.0 : 0.0;
        double totalCPS = manualCPS + autoCPS;

        if (totalCPS > 0)
        {
            double tickGain = manualCPS * currentClickPower;

            if (autoCPS > 0) {
                double autoMultiplier = 1.0 + (0.1 * GetUpgradeLevel("auto_clicker_power"));
                tickGain += autoCPS * (currentClickPower * autoMultiplier);
            }
            softCurrency += tickGain;
        }

        // --- БЛОК 1: ПРИОРИТЕТ 1 - Проверка апгрейдов ---
        bool upgradeBought = true;
        while (upgradeBought)
        {
            upgradeBought = false;
            // Покупка жадная: пока хватает денег, скупаем всё
            if (TryBuyUpgrade("rusty_autoclicker")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("overdrive_module")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("auto_clicker_cps")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("auto_clicker_power")) { upgradeBought = true; continue; }
            if (TryBuyUpgrade("manual_tap_power")) { upgradeBought = true; continue; }
        }

        // --- БЛОК 2: ПРИОРИТЕТ 2 - Доступна ли Категория N+1? (Строгий анлок) ---
        if (unlockedCategoryIndex + 1 < categories.Count)
        {
            var nextCat = categories[unlockedCategoryIndex + 1];
            if (softCurrency >= nextCat.UnlockCost)
            {
                softCurrency -= nextCat.UnlockCost; // Обязательная транзакция разблокировки
                unlockedCategoryIndex++;
                LogEvent($"Открытие новой категории коробок: {nextCat.Name} (Списано: {nextCat.UnlockCost:F0})");
            }
        }

        // --- БЛОК 3: ПРИОРИТЕТ 3 - Покупка Лотов текущей категории ---
        var targetCat = categories[unlockedCategoryIndex];
        double unpacksThisSecond = 0;
        while (softCurrency >= targetCat.BoxPrice && unpacksThisSecond < maxUnpacksPerSecond)
        {
            softCurrency -= targetCat.BoxPrice;
            OpenBox(targetCat);
            unpacksThisSecond++;
        }

        CheckCheckpoints();

        if (timeInSeconds > 0 && timeInSeconds % 86400 == 0) {
            double d = (timeInSeconds / 86400);
            LogEvent($"-- ИТОГИ ДНЯ {d} -- SoftCurrency: {softCurrency:F0} | IPS: {currentIPS:F2} | ClickPow: {currentClickPower:F2}");
        }
    }

    private void OpenBox(CategoryData cat)
    {
        double p = UnityEngine.Random.value * 100.0;
        string rarity = "Common";
        if (p < 55.0) rarity = "Common";
        else if (p < 80.0) rarity = "Uncommon";
        else if (p < 92.0) rarity = "Rare";
        else if (p < 98.0) rarity = "Epic";
        else if (p < 99.8) rarity = "Legendary";
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

        // Установка на полку (Rare+)
        if (shelf.Count < 5)
        {
            AddItemToShelfAndRoute(item);
            if (item.Rarity != "Rare") LogEvent($"Установка предмета {item.Rarity} на полку: {item.Name} (+Boost)");
        }
        else
        {
            // Сравнение с худшим
            var worstItem = shelf.OrderBy(x => x.SellPrice).First();

            if (item.SellPrice > worstItem.SellPrice)
            {
                RemoveItemFromShelfAndRoute(worstItem);
                softCurrency += worstItem.SellPrice; // Продаем изгнанный худший предмет
                
                AddItemToShelfAndRoute(item);
                if (item.Rarity != "Rare") LogEvent($"Установка {item.Rarity} на полку (замена {worstItem.Name}): {item.Name} (+Boost)");
            }
            else
            {
                // Новый оказался хуже — продаем его
                softCurrency += item.SellPrice; 
            }
        }
    }

    // --- IPS ROUTING ALGORITHM ---
    private void AddItemToShelfAndRoute(LootItem item)
    {
        shelf.Add(item);
        
        // Строгая маршрутизация прибавки
        if (item.BoostType == "Flat_IPS") {
            baseIPS += item.BoostValue;
        } 
        else if (item.BoostType == "Mult_MPC") {
            baseClickPower += item.BoostValue;
        }
        
        RecalculateCoreStats();
    }

    private void RemoveItemFromShelfAndRoute(LootItem item)
    {
        shelf.Remove(item);
        
        // Вычитаем бусты при продаже
        if (item.BoostType == "Flat_IPS") {
            baseIPS -= item.BoostValue;
            if (baseIPS < 0.0001) baseIPS = 0; // Защита от микропогрешностей
        } 
        else if (item.BoostType == "Mult_MPC") {
            baseClickPower -= item.BoostValue;
            if (baseClickPower < 1.0) baseClickPower = 1.0; 
        }
        
        RecalculateCoreStats();
    }

    private void RecalculateCoreStats()
    {
        // baseIPS является агрегатором прямых бустов от Flat_IPS. 
        currentIPS = baseIPS;
        
        double manualTapPower = GetUpgradeLevel("manual_tap_power") * 1.5;
        // baseClickPower держит в себе "Mult_MPC" прибавки от полок (согласно графу)
        double pureBaseClick = baseClickPower + manualTapPower;
        
        currentClickPower = pureBaseClick + (currentIPS * 0.1);

        if (GetUpgradeLevel("overdrive_module") > 0) {
            autoCPS = 3.0 + GetUpgradeLevel("auto_clicker_cps");
        } else if (GetUpgradeLevel("rusty_autoclicker") > 0) {
            autoCPS = 0.33;
        } else {
            autoCPS = 0;
        }
    }

    private bool TryBuyUpgrade(string id)
    {
        var u = upgrades.FirstOrDefault(x => x.UpgradeID == id);
        if (u == null) return false;

        double lvl = GetUpgradeLevel(id);
        if (lvl >= u.MaxLevel) return false;
        if (!string.IsNullOrEmpty(u.RequiredUpgrade) && GetUpgradeLevel(u.RequiredUpgrade) == 0) return false;

        double cost = MathEvaluateCostFormula(u, lvl);

        if (double.IsInfinity(cost) || double.IsNaN(cost)) return false;

        // Транзакция
        if (softCurrency >= cost)
        {
            softCurrency -= cost;
            upgradeLevels[id] = lvl + 1;
            LogEvent($"Покупка апгрейда: {u.Name} (Ур. {lvl + 1}) за {cost:F0}");
            RecalculateCoreStats();
            return true;
        }
        return false;
    }

    private double MathEvaluateCostFormula(UpgradeData u, double level)
    {
        if (u.UpgradeID == "manual_tap_power")
            return u.BaseCost * Math.Pow(1.18, level) * Math.Max(1.0, Math.Floor(level / 50.0) * 1.4);
        
        if (u.UpgradeID == "auto_clicker_cps")
            return u.BaseCost * Math.Pow(1.70, level);
        
        if (u.UpgradeID == "auto_clicker_power")
            return u.BaseCost * Math.Pow(1.20, level) * Math.Max(1.0, Math.Floor(level / 25.0) * 1.5);
            
        return u.BaseCost;
    }

    private double GetUpgradeLevel(string id) => upgradeLevels.ContainsKey(id) ? upgradeLevels[id] : 0;
    
    private void CheckCheckpoints()
    {
        if (!cp100k && softCurrency >= 100000) { cp100k = true; LogEvent("Достижение чекпоинта: 100k софт-валюты"); }
        if (!cp1M && softCurrency >= 1000000) { cp1M = true; LogEvent("Достижение чекпоинта: 1M софт-валюты"); }
        if (!cp1B && softCurrency >= 1000000000) { cp1B = true; LogEvent("Достижение чекпоинта: 1B софт-валюты"); }
    }

    private void LogEvent(string message)
    {
        double day = Math.Floor(timeInSeconds / 86400) + 1;
        double hour = Math.Floor((timeInSeconds % 86400) / 3600);
        double min = Math.Floor((timeInSeconds % 3600) / 60);
        double sec = timeInSeconds % 60;

        string ts = $"[Day {day}, {hour:00}:{min:00}:{sec:00}]";
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
