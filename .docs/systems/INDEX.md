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

## IPSCalculator
- Файл: `.docs/systems/IPSCalculator.md`
- Статус: ✅ Реализовано
- Ключевые классы: `IPSCalculator`
- API: `GetCurrentIPS()`

## GachaController
- Файл: `.docs/systems/GachaController.md`
- Статус: ✅ Реализовано
- Ключевые классы: `GachaController`
- API: `GachaController.Instance`, `BuyLoot()`, `OnLootBoxOpened`, `OnLootItemGenerated` events

## StickerManager
- Файл: `.docs/systems/StickerManager.md`
- Статус: ✅ Реализовано
- Ключевые классы: `StickerManager`
- API: `StickerManager.Instance`, `OnSetCompleted`, `AutoCraft()`

## BlackMarketManager
- Файл: `.docs/systems/BlackMarketManager.md`
- Статус: ✅ Реализовано (backend-логика)
- Ключевые классы: `BlackMarketManager`, `BlackMarketArtifact`
- API: `BlackMarketManager.Instance`, `AddKey()`, `GetBM_IPSBonus()`

## Utils (Утилиты)
- Файл: (Отсутствует, описано в INDEX.md)
- Статус: ✅ Реализовано
- Ключевые классы: `NumberFormatter`
- API: `NumberFormatter.Format(double value)` - форматирование чисел до Q (квадриллионов) с одним знаком после запятой.

## UI Foundation
- Файл: `.docs/systems/UI_Foundation.md`
- Статус: ✅ Реализовано
- Описание: Переменные и утилитарные классы USS (Variables.uss, Common.uss).

## UIManager
- Файл: `.docs/systems/UIManager.md`
- Статус: ✅ Реализовано
- Ключевые классы: `UIManager`
- API: `Show(string)`, `Hide(string)`, `ShowOnly(string)`


