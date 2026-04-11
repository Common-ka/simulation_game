# Bootstrap System

## Статус
✅ Шаг 1 реализован (GameDataLoader), остальные — заглушки

## Ключевые классы
- `UnclaimedAssets.Bootstrap.BootstrapController` — `Assets/Scripts/Bootstrap/BootstrapController.cs`

## Назначение
Точка входа игры. Запускается в `Bootstrap.unity` (Build Index 0). Последовательно выполняет шаги инициализации через корутину, затем грузит `Game.unity` (Build Index 1).

## API / Последовательность

| # | Вызов | Статус | Описание |
|---|---|---|---|
| 1 | `_gameDataLoader.LoadAsync()` | ✅ Реализовано | Загружает JSON через `GameDataLoader` |
| 2 | `LoadSaveData()` | 🟡 Заглушка | Будет читать сохранения через `SaveManager` |
| 3 | `PreloadSpriteSheet()` | 🟡 Заглушка | Будет грузить спрайтшит через `SpriteSheetLoader` |
| 4 | `InitYandexSDK()` | 🟡 Заглушка | Будет инициализировать Yandex SDK (PluginYG2) |

## SerializeField
- `[SerializeField] GameDataLoader _gameDataLoader` — назначается в Inspector

## Константы
- `GameSceneName = "Game"` — имя целевой сцены

## Зависимости
- `GameDataLoader` (Economy) ✅
- `SaveManager` (Core) 🟡
- `SpriteSheetLoader` (Icons) 🟡
- PluginYG2 (Yandex SDK) 🟡
