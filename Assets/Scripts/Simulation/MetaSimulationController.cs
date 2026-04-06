using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetaSimulationController : MonoBehaviour
{
    private SimulationController currentSim;

    private void Start()
    {
        StartCoroutine(RunMetaSimulation());
    }

    private IEnumerator RunMetaSimulation()
    {
        Debug.Log("=== НАЧАЛО МЕТА-СИМУЛЯЦИИ ===");
        
        // --- ЭПОХА 1 ---
        currentSim = gameObject.AddComponent<SimulationController>();
        var initialData = new SimulationController.RetainedData();
        currentSim.Initialize(1, initialData);

        yield return StartCoroutine(currentSim.RunEpoch());

        // Триггер Престижа
        double lifetime = currentSim.persistentData.LifetimeEarnings;
        double stardustAtPrestige = currentSim.persistentData.CurrencyStardust;

        // PP = (LifetimeEarnings / 1e9)^0.38  +  (TotalStardust / 10000)
        // Коэффициент /10000 даёт пыли ~10-15% от основных очков при типичном запасе 7М пыли
        double ppFromEarnings = Math.Pow((lifetime / 1000000000.0), 0.38);
        double ppFromStardust = stardustAtPrestige / 10000.0;
        if (double.IsNaN(ppFromEarnings) || ppFromEarnings < 0) ppFromEarnings = 0;

        double pp = ppFromEarnings + ppFromStardust;

        // PermanentPrestigeMultiplier = 1.0 + (PP * 0.02)
        double multi = 1.0 + (pp * 0.02);

        Debug.Log($"<color=green>ПРЕСТИЖ!</color> Заработано за Эпоху 1: {lifetime:F0}");
        Debug.Log($"[Prestige] Сконвертировано {stardustAtPrestige:F0} пыли в {ppFromStardust:F2} очков престижа. Текущий запас пыли: 0.");
        Debug.Log($"Prestige Points (PP): {ppFromEarnings:F2} (доход) + {ppFromStardust:F2} (пыль) = {pp:F2} -> Глобальный Множитель: {multi:F2}x");

        // Сохраняем данные для Эпохи 2 — пыль обнуляется!
        var carryOverData = new SimulationController.RetainedData
        {
            LifetimeEarnings = lifetime, 
            PermanentPrestigeMultiplier = multi,
            OwnedArtifacts = new List<SimulationController.BlackMarketArtifact>(currentSim.persistentData.OwnedArtifacts),
            UnlockedStickers = new HashSet<double>(currentSim.persistentData.UnlockedStickers),
            CompletedSets = new HashSet<string>(currentSim.persistentData.CompletedSets),
            CurrencyStardust = 0.0  // Пыль сбрасывается при Престиже
        };

        // Вычищаем старую симуляцию
        DestroyImmediate(currentSim);

        // --- ЭПОХА 2 ---
        Debug.Log("=== СТАРТ ЭПОХИ 2 (С ПРЕСТИЖЕМ) ===");
        currentSim = gameObject.AddComponent<SimulationController>();
        currentSim.Initialize(2, carryOverData);

        yield return StartCoroutine(currentSim.RunEpoch());

        Debug.Log("=== МЕТА-СИМУЛЯЦИЯ ПОЛНОСТЬЮ ЗАВЕРШЕНА ===");
    }
}
