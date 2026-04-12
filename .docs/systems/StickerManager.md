# StickerManager

## Контекст
Мета-система альбома стикеров. Подписывается на `GachaController.OnLootItemGenerated`, с шансом 5% выпадает стикер. Дубликаты конвертируются в Stardust. При завершении сета — флаг в `RetainedData` и событие `OnSetCompleted`.

Стикеры и сеты **не обнуляются при Престиже** (хранятся в `RetainedData`).

## Ключевые классы
- `StickerManager`

## API

### Свойства
- `public static StickerManager Instance { get; private set; }`

### События
- `public static event Action<string> OnSetCompleted` — передаёт имя категории при завершении сета.

## Внутренние таблицы
- `DustBurnTable` — пыль за дублирующий стикер (по редкости).
- `CraftCostTable` — стоимость ручного крафта (по редкости). **Зарезервирован для будущего UI ручного крафта.**

## Зависимости
- Подписка на `GachaController.OnLootItemGenerated` (событие добавлено в GachaController).
- Чтение/запись `SaveManager.Instance.Data.UnlockedStickerIDs`, `CompletedSetNames`, `CurrencyStardust`.
