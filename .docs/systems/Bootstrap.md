# Bootstrap System

## Статус
✅ Реализовано (заглушки)

## Ключевые классы
- `UnclaimedAssets.Bootstrap.BootstrapController` — `Assets/Scripts/Bootstrap/BootstrapController.cs`

## Назначение
Точка входа игры. Запускается в `Bootstrap.unity` (Build Index 0). Последовательно выполняет шаги инициализации через корутину, затем грузит `Game.unity` (Build Index 1).

## API / Последовательность

| # | Метод | Статус | Описание |
|---|---|---|---|
| 1 | `LoadGameData()` | 🟡 Заглушка | Будет загружать JSON через `GameDataLoader` |
| 2 | `LoadSaveData()` | 🟡 Заглушка | Будет читать сохранения через `SaveManager` |
| 3 | `PreloadSpriteSheet()` | 🟡 Заглушка | Будет грузить спрайтшит через `SpriteSheetLoader` |
| 4 | `InitYandexSDK()` | 🟡 Заглушка | Будет инициализировать Yandex SDK (PluginYG2) |

## Константы
- `GameSceneName = "Game"` — имя целевой сцены

## Паттерн
`IEnumerator Start()` — Unity запускает как корутину автоматически. Шаги выполняются последовательно через `yield return`.

## Зависимости (будущие)
- `GameDataLoader` (Economy)
- `SaveManager` (Core)
- `SpriteSheetLoader` (Icons)
- PluginYG2 (Yandex SDK)
