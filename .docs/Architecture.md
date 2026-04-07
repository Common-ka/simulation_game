# Unclaimed Assets — Архитектура проекта
## Technical Architecture Document · v1.0

> **Контекст:** Соло-разработка. Код пишет ИИ-агент. Дедлайн — 1 августа.
> Все решения приняты с учётом приоритетов: скорость разработки > идеальная архитектура.

---

## Структура сцен

```
Bootstrap.unity  →  Game.unity
```

**Bootstrap** — сплэшскрин. Единственная задача: показать лого, загрузить данные, перейти в Game.

**Game** — одна сцена на весь геймплей. Переключение контента через Show/Hide панелей. WebGL не фризит, IPS продолжает капать при любой открытой панели.

---

## Структура папок `Assets/`

```
Assets/
├── Scenes/
│   ├── Bootstrap.unity
│   └── Game.unity
│
├── Scripts/
│   ├── Bootstrap/
│   │   └── BootstrapController.cs      ← точка входа, orchestrates loading
│   │
│   ├── Core/
│   │   ├── GameManager.cs              ← главный тик-контроллер
│   │   ├── SaveManager.cs              ← обёртка над Yandex player.setData()
│   │   └── OfflineEarningsCalculator.cs
│   │
│   ├── Economy/
│   │   ├── GameDataLoader.cs           ← загрузка JSON (local / remote)
│   │   ├── EconomyService.cs           ← единственный источник данных о предметах
│   │   ├── ShelfManager.cs             ← логика 5 слотов
│   │   ├── IPSCalculator.cs            ← формула дохода
│   │   └── PrestigeManager.cs          ← расчёт PP, сброс, перенос данных
│   │
│   ├── Gacha/
│   │   ├── GachaController.cs          ← покупка лота, выбор редкости, дроп
│   │   ├── RouletteAnimator.cs         ← анимация рулеток (1–10 штук)
│   │   └── StickerManager.cs           ← альбом, пыль, авто-крафт
│   │
│   ├── BlackMarket/
│   │   └── BlackMarketManager.cs       ← ключи, кулдаун, артефакты
│   │
│   ├── Monetization/
│   │   ├── AdsManager.cs               ← rewarded / interstitial обёртка
│   │   └── IAPManager.cs               ← покупки через Yandex SDK плагин
│   │
│   ├── Icons/
│   │   ├── SpriteSheetLoader.cs        ← загрузка спрайтшита с Yandex Cloud
│   │   └── IconCache.cs                ← кэш загруженных спрайтов в памяти
│   │
│   ├── Analytics/
│   │   └── AnalyticsManager.cs         ← события Yandex Метрики
│   │
│   ├── UI/
│   │   ├── UIManager.cs                ← Show/Hide панелей, стек навигации
│   │   ├── Panels/
│   │   │   ├── HUDPanel.cs             ← верхняя полоса: валюта, IPS
│   │   │   ├── ShopPanel.cs            ← выбор и покупка лота (1/5/10)
│   │   │   ├── ShelfPanel.cs           ← витрина 5 слотов
│   │   │   ├── RoulettePanel.cs        ← экран рулетки (1–10 рулеток)
│   │   │   ├── StickerAlbumPanel.cs    ← альбом стикеров по категориям
│   │   │   ├── BlackMarketPanel.cs     ← ночной режим, артефакты
│   │   │   ├── PrestigePanel.cs        ← расчёт PP, превью множителя
│   │   │   ├── OfflineRewardPanel.cs   ← расчёт и показ оффлайн-дохода
│   │   │   └── SettingsPanel.cs
│   │   └── Components/
│   │       ├── RouletteWheel.cs        ← один виджет рулетки (reusable)
│   │       ├── ItemCard.cs             ← карточка предмета (иконка + название)
│   │       ├── ShelfSlot.cs            ← один слот на витрине
│   │       └── NumberLabel.cs          ← форматирование 1.2M / 3.4B
│   │
│   └── Utils/
│       ├── NumberFormatter.cs
│       └── CoroutineRunner.cs          ← запуск корутин из non-MonoBehaviour
│
├── Resources/
│   ├── GameData/                       ← JSON-файлы (fallback при remote)
│   │   ├── Categories.json
│   │   ├── LootTable.json
│   │   ├── GachaMath.json
│   │   ├── BlackMarketArtifacts.json
│   │   ├── BlackMarketConfig.json
│   │   └── Upgrades.json
│   └── Icons/
│       └── placeholder.png             ← заглушка до загрузки иконки
│
└── StreamingAssets/
    └── remote_config.json              ← флаг local/remote + URL-адреса
```

---

## Последовательность инициализации (Bootstrap)

```
Bootstrap.unity запускается
    │
    ▼
BootstrapController.Start()
    │
    ├─ 1. GameDataLoader.Load()
    │      ├── Читает remote_config.json из StreamingAssets
    │      ├── Если mode = "local"  → читает JSON из Resources/GameData/
    │      └── Если mode = "remote" → скачивает JSON с GitHub Pages (UnityWebRequest)
    │                                   Fallback: при ошибке берёт из Resources/
    │
    ├─ 2. SaveManager.Load()
    │      └── Читает RetainedData через Yandex SDK player.getData()
    │          Fallback: читает из localStorage (делает плагин автоматически)
    │
    ├─ 3. SpriteSheetLoader.Preload(category=1)
    │      └── Качает спрайтшит первой категории с Yandex Cloud
    │          Placeholder висит пока не загружено
    │
    ├─ 4. Yandex SDK Init (через плагин)
    │
    └─ 5. SceneManager.LoadScene("Game")
```

---

## GameManager — главный тик-контроллер

Запускается один раз в `Game.unity`. Держит в себе весь активный игровой стейт.

```csharp
// Тик каждую секунду (InvokeRepeating)
private void GameTick()
{
    // 1. Начислить IPS
    softCurrency += IPSCalculator.GetCurrentIPS(shelfManager, prestigeData, artifacts);
    lifetimeEarnings += ...;

    // 2. Попытаться купить апгрейд (если автопокупка включена)
    upgradeManager.TryAutoBuy();

    // 3. Попытаться открыть следующую категорию
    categoryManager.TryUnlockNext(softCurrency);

    // 4. Авто-крафт стикеров
    stickerManager.AutoCraft();

    // 5. Обновить UI (событие)
    OnGameStateChanged?.Invoke(GetSnapshot());
}
```

**Правило:** `GameManager` не знает про UI. UI подписывается на события через `Action` / `UnityEvent`.

---

## GameDataLoader — local / remote переключение

```json
// StreamingAssets/remote_config.json
{
    "mode": "local",
    "remote_base_url": "https://common-ka.github.io/unclaimed-assets-data/",
    "economy_version": "1.0.0"
}
```

После релиза: меняешь `"mode": "local"` → `"mode": "remote"` в репозитории. Все игроки начинают получать JSON с GitHub Pages. **Без перевыкладки сборки.**

```
mode = "local"   → Resources/GameData/*.json  (внутри сборки)
mode = "remote"  → https://github-pages-url/v1.x.x/*.json
                   → при сбои: fallback на local
```

---

## UI-навигация (панели)

UIManager управляет стеком панелей:

```
[HUD] — всегда видим поверх всего

Основной экран:
  ShopPanel (выбор и покупка лота)
  ShelfPanel (витрина)

Открываются поверх основного:
  RoulettePanel          ← блокирует ShopPanel, НЕ блокирует IPS
  StickerAlbumPanel
  BlackMarketPanel
  PrestigePanel
  OfflineRewardPanel     ← первый экран после возвращения
  SettingsPanel
```

**Частичная блокировка во время рулетки:**

| Действие | Во время рулетки |
|---|---|
| IPS начисление | ✅ продолжается |
| Покупка следующего лота | ✅ разрешена |
| Открытие Черного рынка | ❌ заблокировано |
| Нажатие Престижа | ❌ заблокировано |
| Кнопка «+шанс дропа» (оффер) | ✅ специально разрешена |

---

## Система иконок (Yandex Cloud)

**Структура на Yandex Cloud:**
```
unclaimed-assets-icons/
  category_1.png   ← спрайтшит: все иконки категории «Барахло» (Common→Unique)
  category_2.png
  ...
  category_10.png
```

**Логика загрузки:**
```
При открытии категории N:
    if IconCache.Has(N):
        return cached sprite
    else:
        показываем placeholder.png
        SpriteSheetLoader.Load(N) → Yandex Cloud
        при успехе → кэшируем + обновляем UI
```

Категории 1 и 2 грузятся в Bootstrap (до входа в Game). Остальные — лениво, при первом открытии категории.

---

## SaveManager

Автосохранение:
- Каждые **30 секунд** (тихо)
- При **Престиже**
- При **покупке IAP**
- При **закрытии вкладки** (`OnApplicationFocus(false)`)

```csharp
// Что сохраняется (RetainedData)
{
    "lifetime_earnings": double,
    "prestige_multiplier": double,
    "prestige_count": int,
    "soft_currency": double,
    "owned_artifact_ids": int[],
    "unlocked_sticker_ids": int[],
    "completed_set_names": string[],
    "upgrade_levels": { "id": level },
    "unlocked_category_index": int,
    "last_online_timestamp": long,     // для оффлайн-расчёта
    "offline_cap_seconds": int,        // 7200 base, растёт с апгрейдами/IAP
    "economy_version": string
}
```

---

## Оффлайн-прогресс

```
При входе в игру:
    delta = Now - last_online_timestamp
    capped_delta = min(delta, offline_cap_seconds)
    offline_earned = CurrentIPS * capped_delta
    softCurrency += offline_earned

    Показать OfflineRewardPanel:
        "Вы отсутствовали X ч Y мин. Заработано: Z"
        [Забрать]  [x2 за рекламу]
```

**Расширение лимита оффлайна:**

| Тип | Лимит | Способ получения |
|---|---|---|
| Базовый | 2 ч (7 200 сек) | По умолчанию |
| Буст (разовый) | +2 ч на одну сессию | За Rewarded Video |
| Навсегда (+2 ч) | 4 ч (14 400 сек) | IAP |
| Навсегда (+6 ч) | 8 ч (28 800 сек) | IAP |

---

## Analytics (Yandex Метрика)

Все события из `churn_analysis.md` отправляются через `AnalyticsManager`:

```csharp
AnalyticsManager.Track("category_unlocked", new() {
    { "category_id", 7 },
    { "epoch", 1 },
    { "time_since_start", timeInSeconds }
});
```

`AnalyticsManager` — тонкая обёртка над `ym(COUNTER_ID, 'reachGoal', ...)` через JSInterop.

---

## Милстоуны до 1 августа

| # | Блок | Что входит | Приоритет |
|---|---|---|---|
| M1 | Ядро и данные | GameDataLoader, GameManager (тик), SaveManager, Bootstrap | 🔴 Крит. |
| M2 | Кор-луп | ShelfManager, IPSCalculator, GachaController (без анимации) | 🔴 Крит. |
| M3 | UI-скелет | UIManager, HUD, ShopPanel, ShelfPanel | 🔴 Крит. |
| M4 | Рулетка | RoulettePanel + RouletteAnimator, частичная блокировка | 🟠 Важно |
| M5 | Мета | PrestigeManager, StickerManager, BlackMarketManager | 🟠 Важно |
| M6 | Монетизация | AdsManager, IAPManager, OfflineRewardPanel | 🟠 Важно |
| M7 | Иконки | SpriteSheetLoader, IconCache, Yandex Cloud setup | 🟡 Нужно |
| M8 | Полировка | Анимации, эффекты, звук, Remote Config | 🟡 Нужно |
| M9 | Интеграция | Analytics, финальный баланс, QA | 🟡 Нужно |

---

## Ключевые архитектурные правила

1. **GameManager не знает про UI.** Только события (`Action<GameSnapshot>`).
2. **EconomyService — единственный источник данных.** Никаких `JsonConvert` в случайных местах.
3. **Нет захардкоженных чисел в C#.** Всё из JSON через EconomyService.
4. **SaveManager — единственное место записи прогресса.** Никаких прямых вызовов SDK из других классов.
5. **AnalyticsManager — единственное место отправки событий.** Единый формат, легко отключить.
