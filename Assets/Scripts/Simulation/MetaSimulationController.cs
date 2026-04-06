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
        
        // PP = Math.Pow((LifetimeEarnings / 1_000_000_000.0), 0.43)
        double pp = Math.Pow((lifetime / 1000000000.0), 0.38);
        if (double.IsNaN(pp) || pp < 0) pp = 0;

        // PermanentPrestigeMultiplier = 1.0 + (PP * 0.02)
        double multi = 1.0 + (pp * 0.02);

        Debug.Log($"<color=green>ПРЕСТИЖ!</color> Заработано за Эпоху 1: {lifetime:F0}");
        Debug.Log($"Prestige Points (PP): {pp:F2} -> Глобальный Множитель: {multi:F2}x");

        // Сохраняем данные для Эпохи 2
        var carryOverData = new SimulationController.RetainedData
        {
            LifetimeEarnings = lifetime, 
            PermanentPrestigeMultiplier = multi,
            OwnedArtifacts = new List<SimulationController.BlackMarketArtifact>(currentSim.persistentData.OwnedArtifacts),
            UnlockedStickers = new HashSet<double>(currentSim.persistentData.UnlockedStickers),
            CompletedSets = new HashSet<string>(currentSim.persistentData.CompletedSets),
            CurrencyStardust = currentSim.persistentData.CurrencyStardust
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
