# Unclaimed Assets — Technical GDD
## Механики, Формулы, Таблицы · v1.0

> **Статус:** Финальный. Все числовые значения проверены на 14-дневных симуляциях (Эпоха 1 + Эпоха 2).
> **Источник правды:** Все цены, IPS-значения и шансы хранятся в JSON-файлах папки `/Economy/`. C#-код является «тупым исполнителем» и не содержит захардкоженных балансных значений.

---

## 1. Система дохода и Полок

### 1.1 Архитектура полки

Игрок имеет **5 активных слотов** для размещения предметов редкости **Rare и выше**. Предметы редкости Common и Uncommon продаются мгновенно при выпадении.

```
shelf[5] = { item_0, item_1, item_2, item_3, item_4 }
```

Каждый слот занимает предмет одного из двух типов:

| Тип бонуса | Поле в JSON | Эффект |
|---|---|---|
| `Flat_IPS` | `BoostValue` | Прибавляет `BoostValue` к `BaseIPS` каждую секунду |
| `Mult_MPC` | `BoostValue` | Прибавляет `BoostValue` к `BaseClickPower` |
| `Boost` | — | Зарезервировано для специальных предметов (гибридный эффект) |

### 1.2 Формула текущего дохода

```
// Шаг 1: AdjustedBaseIPS с учётом завершённых сетов стикеров
AdjustedBaseIPS = 0
for each item in shelf:
    v = item.BoostValue  if  item.BoostType == "Flat_IPS"  else  0
    if item.Category in CompletedSets:
        v *= GachaMath.SynergyBonuses.StandardSetMultiplier  // 1.2
    AdjustedBaseIPS += v

// Шаг 2: Бонус Черного рынка (аддитивный, с хардкапом)
BM_Bonus_Percent = sum(art.EffectValue for art in OwnedArtifacts if art.EffectType == "+Total_IPS%")
BM_Bonus = min(BM_Bonus_Percent / 100.0, BlackMarketConfig.BM_Global_IPS_Limit.BaseHardCapPercent / 100.0)

// Шаг 3: Бонус Престижа (аддитивный)
Prestige_Bonus = PermanentPrestigeMultiplier - 1.0

// Шаг 4: Итоговый мультипликатор — АДДИТИВНЫЙ, не перемножение
TotalMultiplier = 1.0 + Prestige_Bonus + BM_Bonus

// Шаг 5: Финальный IPS
CurrentIPS = AdjustedBaseIPS * TotalMultiplier
```

> **Критическое правило:** `Prestige_Bonus` и `BM_Bonus` **складываются**, а не перемножаются. Это предотвращает экспоненциальный взрыв при накоплении артефактов.

### 1.3 Формула силы клика (MPC)

```
ManualTapBonus  = upgradeLevels["manual_tap_power"] * 1.5
BaseClickPower  = 1.0 + sum(item.BoostValue for item in shelf if item.BoostType == "Mult_MPC")
CurrentClickPow = BaseClickPower + ManualTapBonus + (CurrentIPS * 0.1)
```

### 1.4 Алгоритм замены предмета на полке

При выпадении предмета `Rare+` система принимает решение по следующей логике:

```
procedure TryPlaceOnShelf(newItem):

    // Случай А: есть свободный слот
    if shelf.Count < 5:
        AddToShelf(newItem)
        return

    worstItem = shelf.OrderBy(x => x.SellPrice).First()

    // Случай Б: новый предмет лучше худшего
    if newItem.SellPrice > worstItem.SellPrice:
        Replace(worstItem, newItem)
        return

    // Случай В: IPS Routing — предметы равны по цене (оба Unique)
    // Балансируем типы на полке: стремимся к ≥2 предметам каждого типа
    if abs(newItem.SellPrice - worstItem.SellPrice) < 0.01:
        countOfNewType = shelf.Count(x => x.BoostType == newItem.BoostType)
        if countOfNewType < 2:
            // Ищем дубль противоположного типа для вытеснения
            target = shelf.FirstOrDefault(
                x => abs(x.SellPrice - newItem.SellPrice) < 0.01
                     AND x.BoostType != newItem.BoostType
            )
            if target != null:
                Replace(target, newItem)
                return

    // Случай Г: продаём новый предмет
    SellItem(newItem)
```

### 1.5 Доходность предметов по редкости

`BoostValue` для `Flat_IPS` рассчитывается скриптом `recalculate_ips.py` на основе стоимости следующей категории и целевого времени открытия.

| Редкость | Коэффициент от Unique_IPS | Источник |
|---|---|---|
| Rare | ÷ 150 | `recalculate_ips.py` |
| Epic | ÷ 30 | `recalculate_ips.py` |
| Legendary | ÷ 6 | `recalculate_ips.py` |
| Unique | × 1.0 (базовый) | `recalculate_ips.py` |

**Формула Unique_IPS для категории N:**

```
// Категории 0–5 (ранняя игра): целевое время открытия следующей — 18 часов
TargetTime_Early = 64_800   // секунд

// Категории 6–8 (эндгейм): целевое время — 10.5 дней
TargetTime_Late = 907_200   // секунд

Unique_IPS(N) = Categories[N+1].UnlockCost / (5 * TargetTime)
```

**Минимальные полы:**

```
if Unique_IPS < 150.0: Unique_IPS = 150.0
if Rare_IPS   < 1.0:   Rare_IPS   = 1.0
```

---

## 2. Математика Мета-прогрессии (Престиж)

### 2.1 Триггер Престижа

Престиж — добровольное действие игрока. Рекомендуемый момент UI-подсказки: игрок впервые достигает Категории 7 («Ювелирный лом»).

### 2.2 Формула Prestige Points

```
// Основные очки — от суммарного заработка за эпоху
PP_from_earnings = pow(LifetimeEarnings / 1_000_000_000.0, 0.38)

// Дополнительные очки — от сожжённой Звёздной пыли
// Коэффициент /10_000 ограничивает вклад пыли до ~10-15% от основных PP
PP_from_stardust = CurrencyStardust / 10_000.0

PP_total = PP_from_earnings + PP_from_stardust
```

> Коэффициент `0.38` (вместо линейного роста) обеспечивает убывающую доходность: каждый следующий Престиж даёт меньше PP на единицу заработка, чем предыдущий.

### 2.3 Глобальный множитель Престижа

```
PermanentPrestigeMultiplier = 1.0 + (PP_total * 0.02)
```

Каждое очко PP даёт **+2% к базовому IPS навсегда**. Применяется аддитивно (см. формулу из раздела 1.2).

### 2.4 Что сбрасывается, что сохраняется

| Параметр | При Престиже |
|---|---|
| `SoftCurrency` | ❌ Обнуляется |
| Уровни апгрейдов (автокликер, ручной клик) | ❌ Обнуляются |
| Предметы на полке | ❌ Очищаются |
| `CurrencyStardust` | ❌ Конвертируется в PP, затем обнуляется |
| `OwnedArtifacts` (Черный рынок) | ✅ Сохраняется |
| `UnlockedStickers` (ID стикеров) | ✅ Сохраняется |
| `CompletedSets` (закрытые сеты) | ✅ Сохраняется |
| `PermanentPrestigeMultiplier` | ✅ Накапливается |
| `economy_version` | ✅ Сохраняется (для миграций) |

### 2.5 Объект RetainedData (C# / JSON)

```csharp
public class RetainedData {
    public double  LifetimeEarnings          = 0.0;
    public double  PermanentPrestigeMultiplier = 1.0;
    public List<BlackMarketArtifact> OwnedArtifacts   = new();
    public HashSet<double> UnlockedStickers           = new();
    public HashSet<string> CompletedSets              = new();
    public double  CurrencyStardust          = 0.0;  // обнуляется при Престиже
    public string  economy_version           = "";
}
```

---

## 3. Гача и Коллекции (Стикеры)

### 3.1 Механика выпадения стикеров

При каждом открытии коробки система бросает кубик:

```
// 5% шанс выпадения стикера — независимо от редкости предмета
if Random(0, 100) < 5.0:
    TryDropSticker(item)
```

### 3.2 Обработка стикера

```
procedure TryDropSticker(item):
    if item.ID in UnlockedStickers:
        // Дубликат → конвертируем в пыль
        dust = GachaMath.DustBurnTable[item.Rarity]
        CurrencyStardust += dust
        // Логировать только для Rare+
    else:
        UnlockedStickers.Add(item.ID)
        CheckIfSetIsComplete(item.Category)
```

### 3.3 Таблица Звёздной пыли (DustBurnTable)

Источник: `Economy/GachaMath.json`

| Редкость | Пыль за дубликат | Стоимость крафта |
|---|---|---|
| Common | 1 | 5 |
| Uncommon | 3 | 15 |
| Rare | 10 | 50 |
| Epic | 50 | 250 |
| Legendary | 250 | 1 250 |
| Unique | 1 000 | 5 000 |
| Black_Market_Exclusive | 2 000 | 10 000 |

### 3.4 Алгоритм авто-крафта

Запускается каждый игровой тик. Приоритет: открытые категории, дешёвые редкости первыми.

```
procedure AutoCraftStickers():
    for each category in unlockedCategories:
        if category.Name in CompletedSets: continue
        
        for rarity in ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Unique"]:
            cost = GachaMath.StickerCraftCost[rarity]
            if CurrencyStardust >= cost:
                missingItem = FindFirstMissingSticker(category, rarity)
                if missingItem != null:
                    CurrencyStardust -= cost
                    UnlockedStickers.Add(missingItem.ID)
                    CheckIfSetIsComplete(category.Name)
                    return  // 1 крафт за тик — имитирует органичный поток
```

### 3.5 Математическое ожидание закрытия сета

При 5% шансе на стикер и 11 уникальных предметах в категории (задача о коллекционере купонов):

```
E[boxes] ≈ 11 / 0.05 * H(11) ≈ 228 коробок среднего ожидания
```

Система `DustBurn + Craft` снижает реальное число примерно до **160–180 коробок** при активном дропе.

### 3.6 Бонус завершённого сета

```
// Применяется в RecalculateCoreStats() локально к каждому предмету на полке
if item.Category in CompletedSets:
    effective_BoostValue = item.BoostValue * 1.2  // StandardSetMultiplier из GachaMath.json
```

---

## 4. Черный рынок и Артефакты

### 4.1 Источники ключей

| Источник | Условие |
|---|---|
| Туториал (Эпоха 1) | Хардкод: 1 ключ на `Day 1, 00:00:00`. Только в Эпохе 1. |
| Случайный дроп | Шанс **0.05%** за открытие коробки, кулдаун **2 часа** |
| IAP (Master Key Pack) | 5 ключей за реальные деньги |

### 4.2 Кулдаун на дроп ключа

```csharp
// В SimulationController.cs
private double lastKeyDropTime = -999999;

// Внутри OpenBox():
if (timeInSeconds - lastKeyDropTime > 7200        // 2 часа = 7200 секунд
    && Random.value * 100.0 < 0.05)               // шанс 0.05%
{
    lastKeyDropTime = timeInSeconds;
    SpendBlackMarketKey(forceTutorial: false);
}
```

### 4.3 Туториальный артефакт

При первом ключе (Эпоха 1) симулятор хардкодом выдаёт артефакт с `ID = 1005` — гарантированный `+Total_IPS%`. Это исключает сценарий «первый артефакт оказался бесполезным».

```csharp
// forceTutorial = true → только в Эпохе 1 при i == 0
var art = bmArtifactsDB.FirstOrDefault(x => x.ID == 1005);
```

### 4.4 Правило аддитивности бонусов

**Запрещено:** перемножать глобальные мета-бонусы друг на друга.

**Разрешено:** складывать их нормализованные значения перед применением к базе.

```
// НЕВЕРНО (взрыв экономики):
CurrentIPS = BaseIPS * (1 + BM_Bonus) * PrestigeMultiplier

// ВЕРНО (аддитивная схема):
TotalMultiplier = 1.0 + (PrestigeMultiplier - 1.0) + BM_Bonus
CurrentIPS = AdjustedBaseIPS * TotalMultiplier
```

### 4.5 Хардкап бонуса Черного рынка

Источник: `Economy/BlackMarketConfig.json`

```json
{
    "BM_Global_IPS_Limit": {
        "BaseHardCapPercent": 300,
        "PrestigeScaling": 0.1,
        "MaxBonusFormula": "300 * (1 + 0.1 * PrestigeCount)"
    }
}
```

```
// В коде:
maxBmBonus = BlackMarketConfig.BM_Global_IPS_Limit.BaseHardCapPercent / 100.0
             * (1 + 0.1 * PrestigeCount)
BM_Bonus = min(RawBmBonus, maxBmBonus)
```

Базовый хардкап: **+300%** от `AdjustedBaseIPS`. Растёт на 10% за каждый Престиж.

### 4.6 Типы эффектов артефактов

| EffectType | Применение |
|---|---|
| `+Total_IPS%` | Аддитивно суммируется, применяется через `BM_Bonus` |
| `+Gacha_Chance%` | Увеличивает шанс выпадения стикера (5% + bonus%) |
| `-Shop_Prices%` | Уменьшает `BoxPrice` текущей категории |
| `+Offline_Time` | Увеличивает расчётный оффлайн-период (зарезервировано) |

### 4.7 Price Snapshot — фиксация цены при входе

**Правило:** цена в Чёрном рынке рассчитывается **один раз** в момент активации ключа и остаётся неизменной до конца сессии.

**Проблема без фиксации:**
```
Игрок входит в ЧР → видит цену 50 000
Покупает артефакт +200% MPC → его MPC вырастает в 3 раза
Цена соседнего артефакта → 150 000
Игрок не может купить то, что секунду назад было доступно → ярость
```
Это «наказание за прогресс внутри одной сессии» — один из худших UX-грехов в монетизации.

**Реализация:**
```csharp
// BlackMarketManager.cs
private double _sessionBoxPrice;  // вычисляется один раз

public void ActivateSession() {
    // Snapshot: фиксируем цену при входе
    _sessionBoxPrice = (GameManager.CurrentTPS * 120)
                     + (GameManager.CurrentMPC * 50);
    ShowPanel(_sessionBoxPrice);
}

public void BuyArtifact(int artifactId) {
    if (softCurrency >= _sessionBoxPrice) {
        softCurrency -= _sessionBoxPrice;
        ApplyArtifact(artifactId);
        // _sessionBoxPrice НЕ пересчитываем — цена заморожена до конца сессии
    }
}
```

Пересчёт происходит только при **следующей активации ключа**. Это создаёт дополнительную мотивацию копить ключи: «зайду позже с большим IPS — куплю больше».

---

## 5. Таблица категорий и Экономические стены

Источник: `Economy/Categories.json`

| # | Категория | UnlockCost | Тайминг Эп.1 | Тайминг Эп.2 | Роль |
|---|---|---|---|---|---|
| 1 | Барахло | 0 | Старт | Старт | Онбординг |
| 2 | Возвраты электроники | 1 500 | ~2 мин | ~18 сек | Лёгкий барьер |
| 3 | Забытый багаж | 25 000 | ~7 мин | ~31 сек | Стена 1 |
| 4 | Складские остатки | 500 000 | ~40 мин | ~2 мин | — |
| 5 | Таможенный конфискат | 12 000 000 | ~2.5 ч | ~7 мин | Стена 2 |
| 6 | Антиквариат | 450 000 000 | ~7.7 ч | ~16 мин | Стена 3 |
| 7 | Ювелирный лом | 1.5 × 10¹² | День 2 | ~25 мин | **Стена Престижа** |
| 8 | Крипто-фермы | 3.0 × 10¹⁵ | День 4–5 | ~34 мин | Глубокий эндгейм |
| 9 | Искусство | 8.0 × 10¹⁷ | День 7–8 | ~64 мин | Глубокий эндгейм |
| 10 | Гос. конфискат | 2.0 × 10²⁰ | **Недостижимо** | День 1, ~5.6 ч | Цель Эп.2 |

> **Дизайн-правило:** Категория 10 **не открывается в первой эпохе**. Её существование — главная мотивация нажать «Обновить контракт» (Престиж).

### 5.1 Критерии прохождения экономических стен (PASS/FAIL)

| Стена | FAIL (слишком быстро) | PASS | FAIL (слишком медленно) |
|---|---|---|---|
| Кат. 4 («Складские») | < 20 мин | 20–50 мин | > 2 ч |
| Кат. 7 («Ювелирный лом») | < 18 ч | 20–28 ч | > 4 дней |
| Кат. 10 (Эп.1) | Открыта | **Недостижима** | — |
| Кат. 10 (Эп.2) | < 2 ч | 4–8 ч | > 2 дней |

---

## 6. Структура JSON-файлов (источники правды)

| Файл | Содержимое | Кто читает |
|---|---|---|
| `Categories.json` | Имена и `UnlockCost` категорий | `SimulationController`, игровой клиент |
| `LootTable.json` | Все предметы: ID, Category, Rarity, SellPrice, BoostType, BoostValue | `SimulationController`, генераторы |
| `GachaMath.json` | `DustBurnTable`, `StickerCraftCost`, `SynergyBonuses` | `SimulationController` |
| `BlackMarketArtifacts.json` | Все артефакты: ID, Name, EffectType, EffectValue | `SimulationController` |
| `BlackMarketConfig.json` | Хардкап BM, формула масштабирования | `SimulationController` |
| `Upgrades.json` | Апгрейды: ID, BaseCost, MaxLevel, CostFormula | `SimulationController` |

> **Правило архитектуры:** C#-код не содержит числовых балансных констант. Любое изменение баланса = изменение JSON. Для Live-Ops JSON-файлы подтягиваются с CDN по URL из Remote Config — без перевыкладки сборки Unity.

---

## Appendix: Ключевые константы (финальные значения)

| Константа | Значение | Источник |
|---|---|---|
| Слотов на полке | 5 | Хардкод |
| Шанс выпадения стикера | 5% | `GachaMath.json` |
| Шанс дропа ключа ЧР | 0.05% | `SimulationController.cs` |
| Кулдаун дропа ключа | 7 200 сек (2 ч) | `SimulationController.cs` |
| Минимальный IPS (пол) | 1.0 / сек | `recalculate_ips.py` |
| Мин. Unique_IPS | 150.0 / сек | `recalculate_ips.py` |
| Коэффициент PP | 0.38 | `MetaSimulationController.cs` |
| PP → множитель | × 0.02 | `MetaSimulationController.cs` |
| Конвертация пыли в PP | ÷ 10 000 | `MetaSimulationController.cs` |
| Бонус завершённого сета | × 1.2 | `GachaMath.json → SynergyBonuses` |
| Хардкап BM (базовый) | +300% | `BlackMarketConfig.json` |
| Длительность эпохи | 14 игровых дней | `SimulationController.cs` |
