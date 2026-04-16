using System;

namespace UnclaimedAssets.Utils
{
    /// <summary>
    /// Статичный утилитарный класс для форматирования больших чисел (K, M, B, T, Q).
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Q" };

        /// <summary>
        /// Форматирует число в строку с суффиксом (например, 1234 -> 1.2K).
        /// </summary>
        /// <param name="value">Число для форматирования.</param>
        /// <returns>Отформатированная строка.</returns>
        public static string Format(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
                return value.ToString();

            double absValue = Math.Abs(value);
            int suffixIndex = 0;

            while (absValue >= 1000 && suffixIndex < Suffixes.Length - 1)
            {
                absValue /= 1000;
                suffixIndex++;
            }

            // Если suffixIndex == 0, форматируем без суффикса
            if (suffixIndex == 0)
            {
                return value.ToString("0.#");
            }

            // Учитываем знак оригинального числа при делении
            double formattedValue = value / Math.Pow(1000, suffixIndex);
            return formattedValue.ToString("0.#") + Suffixes[suffixIndex];
        }
    }
}
