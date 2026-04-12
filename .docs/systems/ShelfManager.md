# ShelfManager

**Статус:** ✅ Реализовано
**Контекст:** Ядро экономики, отвечает за пассивный доход, где лежат предметы на полке. Поддерживает строго 5 слотов. Исключительно Event-driven подход, не зависит от UI.

## Ключевые классы
- `ShelfManager`
- `ShelfItemData` (модель данных предмета на полке)
- `ItemRarity` (enum редкостей)

## API ShelfManager
- `static ShelfManager Instance` - Синглтон.
- `const int MAX_SLOTS = 5` - Константа максимального числа предметов на полке.
- `event Action OnShelfUpdated` - Триггерится при любом изменении состояния полок.
- `IReadOnlyList<ShelfItemData> GetItems()` - Возвращает текущий список предметов.
- `bool TryAddItem(ShelfItemData item)` - Добавляет предмет на полку (Common и Uncommon отклоняются). Если нет места - возвращает `false`.
- `bool RemoveItem(ShelfItemData item)` - Удалить конкретный предмет с полки.
- `void ClearShelf()` - Очистить полку (вызывается, например, при престиже).
- `void LoadSavedItems(List<ShelfItemData> savedItems)` - Применение данных из сохранения к полке.
