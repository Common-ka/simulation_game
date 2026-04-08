# Архитектура Live-Ops для браузерного Idle-тайкуна (Яндекс Игры)

---

## Принцип №1: «Игра — это только рендер. Данные — снаружи»

У тебя уже сделан правильный фундамент: всё хранится в JSON-файлах. Задача Live-Ops — сделать так, чтобы эти JSON **не были захардкожены внутри сборки** Unity, а подтягивались с сервера при каждом запуске.

```
Сборка Unity → запрос к серверу → получает актуальный LootTable.json → запускает игру
```

Это **единственное архитектурное решение**, которое даёт тебе полный LiveOps без перевыкладки игры.

---

## Слой 1: Remote Config (Главный инструмент)

### Что это
Отдельный HTTP-эндпоинт (или Firebase Remote Config / Yandex DataLens), который возвращает JSON с конфигурацией при старте игры.

### Минимальная схема запроса
```
GET https://your-backend.com/config?version=1.4.2&user_id=yx_12345
```

### Что он возвращает
```json
{
  "economy_version": "1.4.2",
  "loot_table_url": "https://cdn.your-site.com/economy/v1.4.2/LootTable.json",
  "categories_url":  "https://cdn.your-site.com/economy/v1.4.2/Categories.json",
  "black_market_url": "https://cdn.your-site.com/economy/v1.4.2/BlackMarketArtifacts.json",
  "events_url": "https://cdn.your-site.com/events/seasonal_spring.json",
  "flags": {
    "prestige_enabled": true,
    "black_market_enabled": true,
    "seasonal_event_active": true
  }
}
```

### Как это работает в Unity (WebGL)
```csharp
IEnumerator LoadRemoteConfig() 
{
    using var req = UnityWebRequest.Get(CONFIG_URL);
    yield return req.SendWebRequest();
    
    var config = JsonConvert.DeserializeObject<RemoteConfig>(req.downloadHandler.text);
    
    // Загружаем актуальные базы по URL из конфига
    yield return LoadEconomyData(config.loot_table_url);
    yield return LoadEconomyData(config.categories_url);
}
```

> **Резервный план:** Если запрос упал — грузим из `/StreamingAssets/` (дефолтные JSON в сборке). Игра всегда запустится.

---

## Слой 2: CDN-хостинг баз данных

### Структура папок на сервере
```
cdn/
  economy/
    v1.0.0/
      LootTable.json          ← базовая версия
      Categories.json
      BlackMarketArtifacts.json
    v1.4.2/
      LootTable.json          ← после ребаланса категорий 7-10
      Categories.json
    v2.0.0/
      LootTable.json          ← сезонное обновление, новые предметы
      Categories.json
      Categories_Season2.json ← новые категории!
  events/
    seasonal_spring.json
    halloween.json
```

### Ключевые правила
1. **Никогда не редактируй старые версии** — только создавай новые папки. Это позволяет откатиться мгновенно.
2. **Версия в Remote Config** — переключение для всех игроков занимает секунды (меняешь одну строку в конфиге).
3. **CDN-кэширование** — файлы отдаются моментально, нет нагрузки на сервер.

---

## Слой 3: Добавление нового контента без обновления сборки

### Новые предметы (LootTable.json)
Просто добавляешь объект в массив и деплоишь новую версию:
```json
{
  "ID": 999,
  "Category": "Антиквариат",
  "Name": "Золотые часы Patek Philippe",
  "Rarity": "Unique",
  "SellPrice": 875000,
  "BoostType": "Flat_IPS",
  "BoostValue": 4629000
}
```
Игра подтянет его при следующем запуске. Никакого билда.

### Новые категории (Categories.json)
Аналогично — просто новый объект:
```json
{ "Name": "Военные реликвии", "UnlockCost": 8000000000000000000000 }
```

> **Критично:** В C# SimulationController уже читает категории динамически (`foreach (var c in categories)`). Добавление категории не ломает код.

### Новые артефакты Черного рынка
`BlackMarketArtifacts.json` — добавляй объекты, спрайты подгружаются по `ID`.

---

## Слой 4: Сезонные события (Live Events)

### Структура `seasonal_spring.json`
```json
{
  "event_id": "spring_2026",
  "name": "Весенняя распродажа",
  "start_timestamp": 1746057600,
  "end_timestamp": 1748649600,
  "bonus_drop_rate_multiplier": 1.5,
  "exclusive_items": [
    {
      "ID": 5001,
      "Category": "Таможенный конфискат",
      "Name": "🌸 Сезонный сувенир",
      "Rarity": "Legendary",
      ...
    }
  ],
  "exclusive_artifacts": [
    {
      "ID": 2001,
      "Name": "Весенний ключ",
      "EffectType": "+Total_IPS%",
      "EffectValue": 8.5
    }
  ]
}
```

Игра проверяет `timestamp` и включает событие без обновления сборки.

---

## Слой 5: Хранилище прогресса (Яндекс Игры)

### Яндекс PlayerData API (уже в SDK)
```javascript
// Сохранение
ysdk.getPlayer().then(player => {
    player.setData({
        epoch: retainedData.epoch,
        prestige_multiplier: retainedData.prestigeMultiplier,
        stickers: retainedData.unlockedStickers,
        completed_sets: retainedData.completedSets,
        artifacts: retainedData.ownedArtifacts
    });
});
```

### Что сохранять (минимальный набор)
```
RetainedData:
  - lifetime_earnings: double
  - prestige_multiplier: double
  - prestige_count: int
  - owned_artifact_ids: int[]    ← только ID, не объекты!
  - unlocked_sticker_ids: int[]
  - completed_set_names: string[]
  - economy_version: string      ← КРИТИЧНО для миграций!
```

> **Почему `economy_version` в сохранении?** Когда ты добавишь 100 новых стикеров в v2.0.0, нужно знать что у игрока из v1.0.0 — иначе его прогресс альбома не совпадёт с новой базой. Это позволяет писать **скрипты миграции**.

---

## Слой 6: A/B-тестирование баланса

### Механика
Remote Config возвращает группу игрока:
```json
{
  "ab_group": "B",
  "economy_version": "1.4.2_aggressive_wall"
}
```

Группа A получает `v1.4.2` (стена), группа B — `v1.4.2_smooth` (мягче). Через неделю смотришь retention по группам и выбираешь победителя.

---

## Слой 7: Аналитика (интеграция с метриками из churn_analysis.md)

Каждый event из `churn_analysis.md` отправляется через Яндекс Метрику:
```javascript
ym(COUNTER_ID, 'reachGoal', 'category_unlocked', {
    category_id: 7,
    epoch: 1,
    time_since_start: 172800
});
```

Пороговые значения мониторишь в дашборде.

---

## Итоговая схема архитектуры

```
┌─────────────────────────────────────────────────┐
│                   ЯНДЕКС ИГРЫ                   │
│                                                  │
│  ┌──────────┐    ┌──────────────────────────┐   │
│  │ Unity    │───▶│  Remote Config Server    │   │
│  │ WebGL    │    │  (твой бэкенд / Firebase)│   │
│  │          │◀───│  Отдает: версию, URL, флаги│  │
│  └────┬─────┘    └──────────────────────────┘   │
│       │                                          │
│       ▼                                          │
│  ┌──────────────────────────────────────────┐   │
│  │              CDN (твой хостинг)          │   │
│  │  LootTable.json   Categories.json        │   │
│  │  BlackMarket.json seasonal_event.json    │   │
│  └──────────────────────────────────────────┘   │
│       │                                          │
│       ▼                                          │
│  ┌──────────────────────────────────────────┐   │
│  │         Яндекс PlayerData API           │   │
│  │  Сохранение RetainedData (мета-прогресс) │   │
│  └──────────────────────────────────────────┘   │
│       │                                          │
│       ▼                                          │
│  ┌──────────────────────────────────────────┐   │
│  │           Яндекс Метрика                │   │
│  │  category_unlocked, prestige_initiated,  │   │
│  │  session_end, sticker_set_completed...   │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

---

## Что делать прямо сейчас (приоритетный порядок)

### Этап 0 (до выкладки): ✅ Уже сделано
- Все данные в JSON — **готово**
- Динамическая загрузка категорий в C# — **готово**
- Архитектура без захардкоженных значений — **готово**

### Этап 1 (перед первым релизом)
1. Загрузить JSON в `StreamingAssets/` как дефолты (резервный план)
2. Настроить Яндекс `player.setData()` для сохранения `RetainedData`
3. Добавить Яндекс Метрику, прописать 3 ключевых события: `category_unlocked`, `prestige_initiated`, `session_end`

### Этап 2 (через 2 недели после релиза)
1. Поднять простой хостинг (GitHub Pages или любой CDN) — именно туда переедут JSON-файлы
2. Сделать `RemoteConfigLoader.cs` — при старте качает конфиг и перезаписывает локальные JSON
3. Добавить версионирование `economy_version` в сохранения

### Этап 3 (первый Live-Op)
1. Добавить 20 новых предметов в `LootTable.json`, задеплоить новую версию на CDN
2. Поменять одну строку в Remote Config — все игроки получат обновление без перевыкладки в Яндекс

---

> [!IMPORTANT]
> Самое важное ограничение Яндекс Игр: **нет своего сервера — используй облако**.
> Firebase Realtime Database (бесплатный tier) прекрасно подходит для Remote Config и хранения флагов сезонных событий. Для CDN — GitHub Pages (бесплатно) или Cloudflare R2 (очень дёшево).
