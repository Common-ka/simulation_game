# BlackMarketManager

## Контекст
Бэкенд-система Чёрного рынка. Отвечает за:
- Дроп ключей при открытии коробок (шанс 0.05%, кулдаун 2 часа)
- Инкремент `RetainedData.BlackMarketKeys` и сохранение
- Расчёт суммарного IPS-бонуса от артефактов ЧР с хардкапом

UI Чёрного рынка реализуется отдельно.

## Ключевые классы
- `BlackMarketManager` — Singleton MonoBehaviour
- `BlackMarketArtifact` — глобальная модель данных артефакта (`Assets/Scripts/BlackMarket/BlackMarketArtifact.cs`)

## API

### Свойства
- `public static BlackMarketManager Instance { get; private set; }`

### Методы
- `public void AddKey()` — инкрементит `Data.BlackMarketKeys`, вызывает `SaveManager.Save()`
- `public double GetBM_IPSBonus()` — суммирует `EffectValue` по `OwnedArtifactIDs` для артефактов с `EffectType == "+Total_IPS%"`, нормализует в долю (÷100), применяет хардкап: `(300% / 100) * (1 + 0.1 * PrestigeCount)`

## Зависимости
- `GachaController.OnLootBoxOpened` (подписка через `OnEnable`/`OnDisable`)
- `SaveManager.Instance.Data` (`RetainedData`)
- `Resources/GameData/BlackMarketArtifacts.json` (загружается в `Awake`)
