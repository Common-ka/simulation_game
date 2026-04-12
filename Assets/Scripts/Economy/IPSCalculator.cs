using System;
using UnityEngine;

namespace UnclaimedAssets.Economy
{
    /// <summary>
    /// Отвечает за математический расчет текущего пассивного дохода в секунду (IPS).
    /// </summary>
    public static class IPSCalculator
    {
        /// <summary>
        /// Рассчитывает текущий пассивный доход в секунду.
        /// Учитывает предметы на полке и глобальные мультипликаторы (Престиж, Черный рынок),
        /// применяя их аддитивно.
        /// </summary>
        /// <param name="shelf">Ссылка на ShelfManager для получения предметов с полки.</param>
        /// <param name="prestigeMultiplier">Множитель престижа (например, 1.0 = нет престижа, 1.02 = 1 PP).</param>
        /// <param name="blackMarketBonus">Глобальный бонус от Черного рынка в долях единицы (например, 0.5 = 50%).</param>
        /// <returns>Итоговое значение IPS.</returns>
        public static double GetCurrentIPS(ShelfManager shelf, double prestigeMultiplier, double blackMarketBonus)
        {
            if (shelf == null)
                return 0.0;

            double adjustedBaseIPS = 0.0;

            // Шаг 1: Суммируем EffectValue со всех предметов с полки, у которых EffectType == "Flat_IPS"
            var items = shelf.GetItems();
            foreach (var item in items)
            {
                if (item != null && item.EffectType == "Flat_IPS")
                {
                    adjustedBaseIPS += item.EffectValue;
                }
            }

            // Шаг 2: Вычисляем бонус Престижа
            double prestigeBonus = prestigeMultiplier - 1.0;

            // Шаг 3: Итоговый мультипликатор — АДДИТИВНЫЙ
            double totalMultiplier = 1.0 + prestigeBonus + blackMarketBonus;

            // Шаг 4: Возвращаем результат
            return adjustedBaseIPS * totalMultiplier;
        }
    }
}
