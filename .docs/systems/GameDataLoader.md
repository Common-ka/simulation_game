# GameDataLoader System

## Статус
✅ Реализовано

## Ключевые классы
- `UnclaimedAssets.Economy.GameDataLoader` — `Assets/Scripts/Economy/GameDataLoader.cs`

## Назначение
`MonoBehaviour`. Загружает 4 JSON-файла из `Resources/GameData/` через `Resources.Load<TextAsset>`. Вызывается из `BootstrapController` через `StartCoroutine`.

## Публичное API

| Член | Тип | Описание |
|---|---|---|
| `LoadAsync()` | `IEnumerator` | Загружает все конфиги. Вызвать через `StartCoroutine`. |
| `Categories` | `CategoryData[]` | Список категорий с `Name` и `UnlockCost` (double). |
| `GachaMath` | `GachaMathData` | Пити-механика, таблицы пыли, стоимость крафта, бонусы синергии. |
| `BlackMarketConfig` | `BlackMarketConfigData` | Правила мультипликаторов, лимиты IPS. |
| `Upgrades` | `UpgradeData[]` | Список апгрейдов с формулами. |

## Модели данных (ключевые поля)

### CategoryData
- `Name: string`, `UnlockCost: double`

### GachaMathData
- `PityMechanism: PityMechanismData` — ExpectedBoxesForSet, HardPityThresholdBoxes
- `DustBurnTable: RarityIntTable` — по редкостям (Common..Black_Market_Exclusive)
- `StickerCraftCost: RarityIntTable`
- `SynergyBonuses: SynergyBonusData`

### BlackMarketConfigData
- `BM_Global_IPS_Limit: BMGlobalIpsLimit` — BaseHardCapPercent, PrestigeScaling

### UpgradeData
- `UpgradeID: string`, `BaseCost: double`, `CostFormula: string`, `EffectFormula: string`, `MaxLevel: int`, `RequiredUpgrade: string`

## Ограничения
- Только **локальное** чтение (`Resources.Load`). Remote-режим — задача M8.
- Парсинг через **`Newtonsoft.Json`** (`JsonConvert.DeserializeObject<T>()`) — пакет установлен в проекте.
- При отсутствии файла — `Debug.LogError`, не исключение.
