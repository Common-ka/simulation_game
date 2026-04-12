# IPSCalculator
- **Связанный файл:** `Assets/Scripts/Economy/IPSCalculator.cs`
- **Статус:** ✅ Реализовано
- **Классы:** `IPSCalculator`
- **Особенности:**
  - `GetCurrentIPS(ShelfManager, double, double, List<string>)`: Рассчитывает пассивный доход в секунду (IPS). Принимает список `CompletedSetNames` для применения бонуса за завершенные сеты.
  - Применяет множитель `×1.2` к компоненту "Flat_IPS" тех предметов, чья категория находится в переданном списке `CompletedSetNames`.
  - Применяет бонусы Престижа и Черного рынка строго аддитивно для предотвращения экспоненциального взрыва экономики.
  - Учитывает только `EffectType == "Flat_IPS"` из предметов `ShelfManager`.
