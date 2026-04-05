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

    [Serializable]
    public class CategoryData {
        public string Name;
        public double UnlockCost;
        public double BoxPrice;
    }

    [Serializable]
    public class BlackMarketArtifact {
        public double ID;
        public string Name;
        public string Rarity;
        public string EffectType;
        public double EffectValue;
    }

    [Serializable]
    public class BMConfigModel {
        public BMLimit BM_Global_IPS_Limit;
    }

    [Serializable]
    public class BMLimit {
        public double BaseHardCapPercent;
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
    private List<BlackMarketArtifact> bmArtifactsDB;
    private BMConfigModel bmConfig;
    
    // --- Cached Logic Data ---
    private Dictionary<string, double> upgradeLevels = new Dictionary<string, double>();
    private List<LootItem> shelf = new List<LootItem>(); 
    public List<BlackMarketArtifact> ownedArtifacts = new List<BlackMarketArtifact>(); 
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
        upgrades = JsonConvert.DeserializeObject<List<UpgradeData>>(File.ReadAllText(Path.Combine(path, "Upgrades.json")));
        lootTable = JsonConvert.DeserializeObject<List<LootItem>>(File.ReadAllText(Path.Combine(path, "LootTable.json")));
        categories = JsonConvert.DeserializeObject<List<CategoryData>>(File.ReadAllText(Path.Combine(path, "Categories.json")));
        bmArtifactsDB = JsonConvert.DeserializeObject<List<BlackMarketArtifact>>(File.ReadAllText(Path.Combine(path, "BlackMarketArtifacts.json")));
        bmConfig = JsonConvert.DeserializeObject<BMConfigModel>(File.ReadAllText(Path.Combine(path, "BlackMarketConfig.json")));
    }

    private void InitCategoriesAndCache()
    {
        foreach (var c in categories) {
            var commons = lootTable.Where(x => x.Category == c.Name && x.Rarity == "Common").ToList();
            if (commons.Count > 0) c.BoxPrice = commons[0].SellPrice * 5.0;
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

            if (i == 0) {
                LogEvent("ТУТОРИАЛ: Получен стартовый ключ Черного рынка!");
                SpendBlackMarketKey(true);
            }

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
        // Механика случайного дропа ключей (шанс условно 0.05%)
        if (UnityEngine.Random.value * 100.0 < 0.05) 
        {
            LogEvent("Глобальный дроп: Выпал Ключ Черного Рынка! Тратим...");
            SpendBlackMarketKey(false);
        }

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
            // Находим худший предмет
            var worstItem = shelf.OrderBy(x => x.SellPrice).First();
            bool replace = false;
            LootItem targetToReplace = worstItem;

            if (item.SellPrice > worstItem.SellPrice)
            {
                replace = true;
            }
            // УЗЕЛ IPS ROUTING: Если предметы равны по цене (оба Unique), балансируем статы!
            else if (Math.Abs(item.SellPrice - worstItem.SellPrice) < 0.01)
            {
                int myTypeCount = shelf.Count(x => x.BoostType == item.BoostType);
                // Стремимся держать хотя бы 2 предмета каждого типа на полке
                if (myTypeCount < 2) 
                {
                    var oppositeTarget = shelf.FirstOrDefault(x => Math.Abs(x.SellPrice - item.SellPrice) < 0.01 && x.BoostType != item.BoostType);
                    if (oppositeTarget != null)
                    {
                        targetToReplace = oppositeTarget;
                        replace = true;
                    }
                }
            }

            if (replace)
            {
                RemoveItemFromShelfAndRoute(targetToReplace);
                softCurrency += targetToReplace.SellPrice; 
                
                AddItemToShelfAndRoute(item);
                if (item.Rarity != "Rare") LogEvent($"Установка {item.Rarity} на полку (замена {targetToReplace.Name}): {item.Name} (+{item.BoostType})");
            }
            else
            {
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
        // Черный рынок: накапливаем множители грязного дохода
        double bmIpsModifiers = 0;
        foreach (var art in ownedArtifacts) {
            if (art.EffectType == "+Total_IPS%") {
                bmIpsModifiers += art.EffectValue;
            }
        }
        
        double percentMulti = bmIpsModifiers / 100.0;
        double maxPercentMulti = bmConfig != null && bmConfig.BM_Global_IPS_Limit != null 
                                 ? bmConfig.BM_Global_IPS_Limit.BaseHardCapPercent / 100.0 
                                 : 3.0;

        if (percentMulti > maxPercentMulti) percentMulti = maxPercentMulti;

        // Итоговый доход = BaseIPS * (1 + Сумма(Total_IPS_Artifacts / 100))
        currentIPS = baseIPS * (1.0 + percentMulti);
        
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

    private void SpendBlackMarketKey(bool forceTutorial)
    {
        if (bmArtifactsDB == null || bmArtifactsDB.Count == 0) return;

        if (forceTutorial) {
            // Жестко вытягиваем артефакт со свойством +Total_IPS% (ID 1005: Теневой Артефакт #5)
            var art = bmArtifactsDB.FirstOrDefault(x => x.ID == 1005);
            if (art != null) {
                ownedArtifacts.Add(art);
                LogEvent($"Черный рынок: куплен туториал-артефакт {art.Name} ({art.EffectType} {art.EffectValue})");
                RecalculateCoreStats();
            }
        } else {
            // Случайный мидгейм-дроп
            var art = bmArtifactsDB[UnityEngine.Random.Range(0, bmArtifactsDB.Count)];
            ownedArtifacts.Add(art);
            LogEvent($"Черный рынок: случайная покупка {art.Name} ({art.EffectType} {art.EffectValue})");
            RecalculateCoreStats();
        }
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
