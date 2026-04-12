# 🧠 Индекс Игровых Систем (LTM)

Этот файл — точка входа (Index) в долговременную память (Long-Term Memory) для всех ИИ-агентов, работающих над кодовой базой.

Если ты агент (Воркер, Програмптер или Ревьювер) и твоя задача затрагивает какую-либо механику — найди её в списке ниже и прочитай её Markdown-файл, чтобы понять текущее состояние API.

**Если ты добавил новую систему — добавь ссылку на неё сюда!**

## Системы

## Bootstrap
- Файл: `.docs/systems/Bootstrap.md`
- Статус: ✅ Шаг 1 реализован (GameDataLoader), остальные — заглушки
- Ключевые классы: `BootstrapController`

## GameDataLoader
- Файл: `.docs/systems/GameDataLoader.md`
- Статус: ✅ Реализовано
- Ключевые классы: `GameDataLoader`
- API: `LoadAsync()`, свойства `Categories`, `GachaMath`, `BlackMarketConfig`, `Upgrades`

## GameManager
- Файл: `.docs/systems/GameManager.md`
- Статус: ✅ MVP реализован (тик + событие)
- Ключевые классы: `GameManager`
- API: `OnGameStateChanged: Action<GameSnapshot>` (static event), `GameSnapshot.SoftCurrency`

## SaveManager
- Файл: `.docs/systems/SaveManager.md`
- Статус: ✅ Реализовано (Фолбэк локальное сохранение)
- Ключевые классы: `SaveManager`, `RetainedData`
- API: `SaveManager.Instance`, `Load()`, `Save()`, `Data` (свойство)

## ShelfManager
- Файл: `.docs/systems/ShelfManager.md`
- Статус: ✅ Реализовано
- Ключевые классы: `ShelfManager`, `ShelfItemData`
- API: `ShelfManager.Instance`, `OnShelfUpdated`, `TryAddItem()`, `RemoveItem()`, `ClearShelf()`
