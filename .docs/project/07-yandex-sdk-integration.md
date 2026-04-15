# Yandex SDK — Интеграция (PluginYG2)
## Unclaimed Assets · Справочник разработчика

> **Плагин:** [PluginYG2](https://assetstore.unity.com/packages/tools/integration/plugin-your-games-2-0-302343) — бесплатный, не официальный продукт Яндекс, но поддерживается в партнёрстве.
> **Namespace:** `using YG;` — в начале каждого скрипта, который обращается к SDK.
> **Главный класс:** `YG2` — через него вызывается всё.
> **Документация:** https://max-games.ru/plugin-yg/doc/ · **Чат:** https://t.me/pluginYG2

---

## Установка и настройка

1. Скачать с Unity Asset Store → импортировать в проект
2. Плагин автоматически предложит установить оптимальные настройки под WebGL — **соглашаться**
3. Открыть окно настроек плагина (меню `PluginYG → Settings`)
4. Выбрать платформу: **YandexGames**
5. Модули подключаются отдельно через встроенный менеджер версий

### Обязательные модули для проекта

| Модуль | Для чего |
|---|---|
| `Saves` | Облачные сохранения через Яндекс |
| `InterstitialAdv` | Полноэкранная реклама |
| `RewardedAdv` | Реклама с наградой |
| `Purchases` | IAP (покупка Токенов, VIP, ключей) |
| `Player` | Авторизация, имя/аватар игрока |

### Настройки проекта под WebGL (плагин ставит автоматически)

```
Color Space:          Gamma       (не Linear — не работает на iOS)
Auto Graphic API:     Включено
Compression:          Brotli      (Яндекс поддерживает)
DSP Buffer Size:      настроить   (убирает посторонние шумы)
Audio Load Type:      Decompress On Load (иначе на мобильных показывается Media Player)
```

---

## Сохранения (SavesYG)

### Как работает
Плагин хранит сейв в статическом поле `YG2.saves`. Это объект класса `SavesYG`, который ты **расширяешь** через `partial class`. Данные автоматически загружаются при старте.

### Определение полей сейва

```csharp
// Файл: Assets/Scripts/Core/SavesExtension.cs
using YG;

namespace YG {
    public partial class SavesYG {

        // --- Основная экономика ---
        public double  SoftCurrency             = 0.0;
        public double  CurrencyStardust          = 0.0;
        public int     BlackMarketKeys           = 0;
        public int     HardCurrency              = 0;           // Токены (ЯМ)

        // --- Прогресс ---
        public int     UnlockedCategoryIndex     = 0;           // текущая открытая категория (0–9)
        public double  LifetimeEarnings          = 0.0;
        public int     PrestigeCount             = 0;
        public double  PermanentPrestigeMultiplier = 1.0;

        // --- Коллекции ---
        public int[]   UnlockedStickerIDs        = new int[0];
        public string[] CompletedSetNames        = new string[0];

        // --- Черный рынок ---
        public int[]   OwnedArtifactIDs          = new int[0];

        // --- Апгрейды ---
        public int[]   UpgradeLevels             = new int[0];  // индекс = ID апгрейда

        // --- Полка ---
        public int[]   ShelfItemIDs              = new int[0];  // -1 = пустой слот

        // --- Временные метки (для anti-cheat) ---
        public long    LastOnlineTimestamp        = 0L;          // Unix сек, UTC
        public long    LastKeyDropTimestamp       = 0L;

        // --- VIP ---
        public bool    IsVipSilver               = false;
        public bool    IsVipGold                 = false;
        public long    VipExpiryTimestamp        = 0L;

        // --- Мета ---
        public string  EconomyVersion            = "1.0";       // для миграций
        public bool    IsTutorialComplete        = false;
        public bool    IsTutorialKeyGiven        = false;
    }
}
```

### Чтение и запись

```csharp
using YG;

// Чтение — всегда напрямую
double coins   = YG2.saves.SoftCurrency;
int    keys    = YG2.saves.BlackMarketKeys;

// Запись — изменить + вызвать SaveProgress
YG2.saves.SoftCurrency += earned;
YG2.saves.LastOnlineTimestamp = serverTime;
YG2.SaveProgress(); // ← обязательно после каждого изменения, которое нужно сохранить
```

### События загрузки

```csharp
void OnEnable() {
    // Срабатывает когда данные загружены из облака (при старте игры)
    YG2.onGetSDKData += OnSaveLoaded;

    // Срабатывает если у игрока нет сохранений (первый запуск)
    YG2.onDefaultSaves += OnFirstLaunch;
}

void OnDisable() {
    YG2.onGetSDKData -= OnSaveLoaded;
    YG2.onDefaultSaves -= OnFirstLaunch;
}

void OnSaveLoaded() {
    // Здесь запускаем Bootstrap — данные точно готовы
    BootstrapController.Instance.Initialize();
}

void OnFirstLaunch() {
    // Первый запуск — дать туториальный ключ
    YG2.saves.IsTutorialKeyGiven = false;
    YG2.saves.IsTutorialComplete = false;
}
```

### Правила работы с сейвом

- **Никогда не сохранять в `Update()`** — только по событию (покупка, тик, Престиж)
- **SaveProgress не более 1 раза в 10 секунд** — Яндекс имеет rate limit
- `IsTutorialKeyGiven` — использовать как guard от повторной выдачи ключа
- `EconomyVersion` — увеличивать при изменении схемы, добавить логику миграции

---

## Реклама

### Interstitial (полноэкранная, непропускаемая)

```csharp
// Показать — игра автоматически лагнет/замёрзнет на время рекламы
YG2.InterstitialAdvShow();

// Подписаться на события
YG2.RewardedAdvOpened  += OnAdOpened;   // реклама открылась
YG2.RewardedAdvClosed  += OnAdClosed;   // реклама закрылась (без награды)
YG2.RewardedAdvError   += OnAdError;    // ошибка показа
```

**Когда показывать (правила проекта):**
```csharp
// AdsManager.cs
private long _lastInterstitialTime = 0;
private const int INTERSTITIAL_INTERVAL_SEC = 300; // 5 минут

public void TryShowInterstitial() {
    long now = ServerTimeProvider.Current;
    if (now - _lastInterstitialTime >= INTERSTITIAL_INTERVAL_SEC) {
        _lastInterstitialTime = now;
        YG2.InterstitialAdvShow();
    }
}

// Вызывается при загрузке игры (всегда) и по таймеру GameTick
```

### Rewarded (реклама с наградой)

```csharp
// Показать с тегом — по тегу определяем какую награду дать
YG2.RewardedAdvShow("offline_x2");
YG2.RewardedAdvShow("reroll_roulette");
YG2.RewardedAdvShow("black_market_slot");
YG2.RewardedAdvShow("offline_time_plus");
YG2.RewardedAdvShow("stardust_bonus");
YG2.RewardedAdvShow("xray_box");         // Рентген коробки

// Подписка на результат
void OnEnable() {
    YG2.RewardedAdvReward += OnRewardEarned;
    YG2.RewardedAdvClosed += OnRewardClosed; // закрыл БЕЗ просмотра
    YG2.RewardedAdvError  += OnRewardError;
}

void OnRewardEarned(string tag) {
    switch (tag) {
        case "offline_x2":       ApplyOfflineDoubleBonus();  break;
        case "reroll_roulette":  RoulettePanel.RerollFree(); break;
        case "xray_box":         GachaController.ActivateXRay(); break;
        // ...
    }
}
```

**UX-правило:** Всегда показывать выбор — реклама ИЛИ Токены:
```csharp
// RewardedOfferUI.cs
public void ShowOffer(string rewardTag, int hardCurrencyCost) {
    _currentTag  = rewardTag;
    _hcCost      = hardCurrencyCost;
    // Показать панель с двумя кнопками
    panel.RemoveFromClassList("hidden");
}

public void OnWatchAdClicked()  => YG2.RewardedAdvShow(_currentTag);
public void OnPayHCClicked()    { SpendHC(_hcCost); ApplyReward(_currentTag); }
```

---

## IAP (Покупки)

### Определение продуктов

Продукты настраиваются в окне PluginYG → Purchases. Пример ID:

| ID | Тип | Описание |
|---|---|---|
| `starter_pack` | non-consumable | Starter Pack (одноразово) |
| `keys_small` | consumable | 3 ключа |
| `keys_medium` | consumable | 7 ключей + 200 ЯМ |
| `keys_large` | consumable | 20 ключей + 500 ЯМ |
| `hc_100` | consumable | 100 Токенов |
| `hc_500` | consumable | 500 Токенов |
| `hc_1500` | consumable | 1 500 Токенов |
| `hc_4000` | consumable | 4 000 Токенов |
| `vip_silver` | subscription | VIP Silver (месяц) |
| `vip_gold` | subscription | VIP Gold (месяц) |

### Покупка и обработка

```csharp
// Инициировать покупку
YG2.Purchase("keys_small");

// Подписка на результат
void OnEnable() {
    YG2.PurchaseSuccess += OnPurchaseSuccess;
    YG2.PurchaseFailed  += OnPurchaseFailed;
}

void OnPurchaseSuccess(string id) {
    switch (id) {
        case "keys_small":
            YG2.saves.BlackMarketKeys += 3;
            YG2.SaveProgress();
            break;
        case "vip_silver":
            YG2.saves.IsVipSilver = true;
            YG2.saves.VipExpiryTimestamp = ServerTimeProvider.Current + 30 * 86400L;
            YG2.SaveProgress();
            break;
        // ...
    }
    ShopPanel.RefreshUI();
}
```

---

## Геймплейные события (Game Ready API)

Яндекс использует эти сигналы для аналитики и рекомендаций:

```csharp
// Вызвать в момент начала геймплея (после загрузки Bootstrap)
YG2.GameplayStart();

// Вызвать при паузе (открытие рекламы, попап настроек)
// Плагин делает это автоматически при показе рекламы — вручную только для кастомных пауз
YG2.GameplayStop();

// Проверить текущее состояние
bool isPlaying = YG2.isGameplaying;
```

---

## Инициализационная последовательность (Bootstrap)

```
Сцена Bootstrap загружается
        ↓
YG2.onGetSDKData ← ожидаем это событие
        ↓
Данные сохранений готовы → SaveManager.Initialize()
        ↓
FetchServerTime() → AntiCheat.ValidateOfflineDelta()
        ↓
GameDataLoader.Load() → загружаем JSON (local или remote)
        ↓
YG2.GameplayStart()  ← сигналим Яндексу
        ↓
Показываем Interstitial при первом входе (AdsManager.ShowOnLoad)
        ↓
Загружаем сцену Game или активируем UIDocument
```

```csharp
// BootstrapController.cs
using YG;

public class BootstrapController : MonoBehaviour {

    void Start() {
        YG2.onGetSDKData  += OnSDKReady;
        YG2.onDefaultSaves += OnFirstLaunch;
    }

    void OnSDKReady() {
        StartCoroutine(InitSequence());
    }

    IEnumerator InitSequence() {
        yield return StartCoroutine(AntiCheat.FetchServerTime());
        yield return StartCoroutine(GameDataLoader.Load());
        SaveManager.Initialize();
        YG2.GameplayStart();
        AdsManager.ShowOnLoadInterstitial();
        SceneManager.LoadScene("Game");
    }
}
```

---

## Тестирование в редакторе

PluginYG2 симулирует все SDK-функции прямо в Unity Editor:
- Сохранения работают через PlayerPrefs (локально)
- Реклама — симулируется диалогом в Editor
- IAP — можно симулировать покупки в настройках плагина

**Для тестирования билда с реальным SDK:**
В файле `WebGLTemplates/YandexGames/index.html` изменить функцию `IsLocalHost` чтобы SDK подключался при локальном запуске.

**Developer Build:**
В настройках плагина → Template → включить `Developer Build`. В углу экрана будет отображаться номер билда — удобно убедиться что на сервере актуальная версия.

---

## Чеклист перед релизом

- [ ] Выключить `Developer Build` в настройках плагина
- [ ] Code Optimization в Build Settings → `Runtime Speed with LTO`
- [ ] Проверить все IAP-продукты в консоли Яндекс Игр
- [ ] Протестировать Rewarded Ad на реальном билде (симулятор в редакторе не показывает реальную задержку)
- [ ] Проверить что `YG2.GameplayStart()` вызывается ровно один раз при входе
- [ ] Убедиться что `SaveProgress()` не вызывается в `Update()`
