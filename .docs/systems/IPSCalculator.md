# IPSCalculator
- **Связанный файл:** `Assets/Scripts/Economy/IPSCalculator.cs`
- **Статус:** ✅ Реализовано
- **Классы:** `IPSCalculator`
- **Особенности:**
  - `GetCurrentIPS(ShelfManager, double, double)`: Рассчитывает пассивный доход в секунду (IPS).
  - Применяет бонусы Престижа и Черного рынка строго аддитивно для предотвращения экспоненциального взрыва экономики.
  - Учитывает только `EffectType == "Flat_IPS"` из предметов `ShelfManager`.
