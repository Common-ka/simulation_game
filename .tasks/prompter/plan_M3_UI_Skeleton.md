# 📋 План Milestone M3: UI-Скелет (UI Toolkit)

> **Дата создания:** 2026-04-15  
> **Автор:** Planner Agent  
> **Статус:** ✅ Готов к передаче Промптеру

---

## 🔍 Обоснование выбора Milestone

**Что реализовано (M1 + M2 + часть M5 — backend):**
- ✅ `GameDataLoader` — загружает все JSON из Resources
- ✅ `GameManager` — тик, событие `OnGameStateChanged`, `GameSnapshot`
- ✅ `SaveManager` — локальное сохранение `RetainedData`
- ✅ `ShelfManager` — логика 5 слотов
- ✅ `IPSCalculator` — формула дохода
- ✅ `GachaController` — покупка лота, дроп предметов, события
- ✅ `StickerManager` — альбом, пыль (без AutoCraft)
- ✅ `BlackMarketManager` — ключи, бонус IPS
- ✅ `BootstrapController` — заглушка

**Что отсутствует:**
- ❌ `UIManager` — нет вообще
- ❌ Все панели UI (UXML/USS пустые директории, `UI/Panels/` пустая)
- ❌ `UI/Components/` — пустая
- ❌ Числовые форматтеры, утилиты для UI
- ❌ `EconomyService` (упомянут в архитектуре, не создан)
- ❌ Сцена `Game.unity` пока не настроена под реальные компоненты

**Вывод:** Ядро работает, но игру запустить нельзя — нет минимального UI. M3 разблокирует возможность играть в первую версию.

---

## ⚙️ Архитектурный фильтр (Event-Driven анализ)

| Фича | Owner Events | Подписчики |
|---|---|---|
| HUD (валюта, IPS) | `GameManager.OnGameStateChanged` → `GameSnapshot` | `HUDPanel.cs` |
| Кнопка покупки лота | `HUDPanel/ShopPanel` → вызов `GachaController.BuyLoot()` | `GachaController` выбрасывает `OnLootBoxOpened`, `OnLootItemGenerated` |
| Полка (обновление) | `ShelfManager.OnShelfUpdated` | `ShelfPanel.cs` |
| Переключение панелей | `UIManager.ShowPanel(name)` — только навигация, не бизнес-логика | — |

**Запрещено:** `HUDPanel` и `ShopPanel` не знают ничего о `ShelfManager`, `IPSCalculator`, `BlackMarketManager`. Только через события `GameManager` или Subscribe.

---

## 📦 Декомпозиция задач

---

### Task M3.0 — NumberFormatter + Utils (Предварительная задача) 

**Цель:** Создать `NumberFormatter.cs` для форматирования больших чисел (1.2M, 3.4B).

**Архитектурные ограничения:**
- `static class`, никаких зависимостей, никаких MonoBehaviour.
- Поддержать K (тысячи), M (миллионы), B (миллиарды), T (триллионы), Q (квадриллионы).

---

> 📢 **Промпт для Промптера (Task M3.0):**
> 
> Создай ТЗ для `NumberFormatter.cs` — статический утилитный класс в `Assets/Scripts/Utils/`.  
> Метод: `public static string Format(double value) → string`.  
> Форматирование: < 1000 → "123", < 1M → "123.4K", < 1B → "123.4M", < 1T → "1.2B" и т.д. до Q (квадриллион).  
> Без MonoBehaviour, без зависимостей. Протестировать через `Debug.Log` в Bootstrap.  
> Воркер обновляет `INDEX.md`.

---

### Task M3.1 — Design System: Variables.uss + Common.uss 

**Цель:** Создать базовую дизайн-систему: токены цветов, шрифтов, отступов и общие классы (`.hidden`, `.button`, `.panel`, `.card-rare`, `.card-epic` и т.д.)

**Архитектурные ограничения:**
- Никаких пикселей без переменных CSS. Все числа — через `var(--spacing-md)` и т.д.
- Класс `.hidden` обязательно: `display: none;`. UIManager будет переключать через Add/RemoveFromClassList.
- Цветовая схема: тёмная тема (фоны ~#1a1a2e, акценты золото/фиолетовый).

---

> 📢 **Промпт для Промптера (Task M3.1):**
> 
> Создай ТЗ для создания двух файлов:  
> 1. `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — CSS-переменные (цвета, шрифты, радиусы, отступы). Тёмная тема, idle-игровой стиль.  
> 2. `Assets/Scripts/UI_Toolkit/USS/Common.uss` — базовые классы: `.hidden` (display:none), `.btn-primary`, `.card`, `.panel`, цветовые ауры редкостей (`.rarity-rare`, `.rarity-epic`, `.rarity-legendary`, `.rarity-unique`).  
> USS-файлы — только стили, никаких C#. Воркер создаёт оба файла вручную.

---

### Task M3.2 — UIManager.cs ❌

**Цель:** Singleton-MonoBehaviour, управляющий показом/скрытием панелей. Находит корневой `UIDocument` и регистрирует все панели по имени USS-класса.

**Архитектурные ограничения:**
- `UIManager` не знает про бизнес-логику. Только `Show(string panelName)` / `Hide(string panelName)`.
- Скрывает все панели, показывает нужную через Add/RemoveFromClassList("hidden").
- Публичные события: `OnPanelShown: Action<string>` (для аналитики).

---

> 📢 **Промпт для Промптера (Task M3.2):**
> 
> Создай ТЗ для `UIManager.cs` в `Assets/Scripts/UI/`.  
> Класс — Singleton MonoBehaviour. Получает `UIDocument` и кеширует панели по имени (Dictionary).  
> Публичное API: `Show(string), Hide(string), ShowOnly(string)`.  
> Всё переключение через AddToClassList/RemoveFromClassList("hidden").  
> Никакой бизнес-логики. Воркер обновляет `INDEX.md`.

---

### Task M3.3 — HUD.uxml + HUD.uss + HUDPanel.cs ❌

**Цель:** Постоянный верхний HUD с: SoftCurrency, IPS, Stardust, Keys. Подписывается на `GameManager.OnGameStateChanged` и `SaveManager.Data`.

**Architerctural ограничения:**
- `HUDPanel.cs` — подписчик, не производитель событий.
- В `OnGameStateChanged` — читает `GameSnapshot.SoftCurrency`, форматирует через `NumberFormatter`.
- IPS берёт из `IPSCalculator.GetCurrentIPS(...)` через `GameSnapshot` или отдельный тик.
- Stardust и Keys — из `SaveManager.Data` (читать при каждом обновлении снепшота).
- `HUDPanel.cs` не вызывает ничего из Economy-слоя напрямую.

---

> 📢 **Промпт для Промптера (Task M3.3):**
> 
> Создай ТЗ для триады:  
> - `Assets/Scripts/UI_Toolkit/UXML/HUDPanel.uxml` — разметка HUD (верхняя шапка): метки для SoftCurrency, IPS, Stardust, Keys. Кнопки навигации в подвале.  
> - `Assets/Scripts/UI_Toolkit/USS/HUD.uss` — стили HUD, подключает Variables.uss.  
> - `Assets/Scripts/UI/Panels/HUDPanel.cs` — MonoBehaviour, Subscribe на `GameManager.OnGameStateChanged`. Обновляет Label'ы через `NumberFormatter`. Stardust и Keys — из `SaveManager.Instance.Data`.  
> Воркер обновляет `INDEX.md`.

---

### Task M3.4 — ShopPanel: UXML + USS + ShopPanel.cs ❌

**Цель:** Основной экран с категорией (название, цена разблокировки) и кнопкой "Крутить рулетку". Показывает текущую доступную категорию из `GameDataLoader.Categories`.

**Архитектурные ограничения:**
- `ShopPanel.cs` подписывается на `GameManager.OnGameStateChanged` (чтобы знать текущюю валюту и обновлять состояние кнопки "слишком дорого").
- Кнопка "Крутить" вызывает `GachaController.Instance.BuyLoot(categoryIndex)`. Это допустимо — прямой вызов **команды** от UI к единственному ответственному контроллеру.
- Не знает про `ShelfManager`, `IPSCalculator`, `SaveManager`.

---

> 📢 **Промпт для Промптера (Task M3.4):**
> 
> Создай ТЗ для триады:  
> - `Assets/Scripts/UI_Toolkit/UXML/ShopPanel.uxml` — зона текущей категории (иконка-заглушка, название, прогресс-бар до след. категории, цена) + кнопка [Крутить (x1 / x10)].  
> - `Assets/Scripts/UI_Toolkit/USS/Shop.uss` — стили ShopPanel.  
> - `Assets/Scripts/UI/Panels/ShopPanel.cs` — Subscribe на `GameManager.OnGameStateChanged`. Отображает текущую категорию из `GameDataLoader`. Кнопка вызывает `GachaController.Instance.BuyLoot(categoryIndex, 1)`. Кнопка серая если `SoftCurrency < price`.  
> Воркер обновляет `INDEX.md`.

---

### Task M3.5 — ShelfPanel: UXML + USS + ShelfPanel.cs ❌

**Цель:** Витрина из 5 слотов. Каждый слот показывает предмет (иконка-заглушка, название, редкость цветом, BoostValue). Подписывается на `ShelfManager.OnShelfUpdated`.

**Архитектурные ограничения:**
- `ShelfPanel.cs` — только подписчик `ShelfManager.OnShelfUpdated`.
- Не знает про `GameManager`, `GachaController`, `SaveManager`.
- Слоты реализовать как 5 VisualElement-карточек (не prefab), переиспользуемых через обновление текста.

---

> 📢 **Промпт для Промптера (Task M3.5):**
> 
> Создай ТЗ для триады:  
> - `Assets/Scripts/UI_Toolkit/UXML/ShelfPanel.uxml` — 5 слотов-карточек (заглушки). Каждый слот: иконка (Label-заглушка), название, редкость (цветной бейдж), BoostValue.  
> - `Assets/Scripts/UI_Toolkit/USS/Shelf.uss` — сетка 5 слотов + цвета редкостей.  
> - `Assets/Scripts/UI/Panels/ShelfPanel.cs` — Subscribe на `ShelfManager.OnShelfUpdated`. Обновляет 5 карточек. Использует CSS-класс редкости (`.rarity-rare` и т.д.) для окраски.  
> Воркер обновляет `INDEX.md`.

---

### Task M3.6 — Сборка сцены Game.unity + Bootstrap → Game переход ❌

**Цель:** Настроить сцену `Game.unity`: GameObject с `GameManager`, `UIManager`, `UIDocument`. Настроить `BootstrapController` для загрузки данных и перехода в сцену.

**Архитектурные ограничения:**
- Порядок инициализации: `GameDataLoader.LoadAsync()` → `SaveManager.Load()` → `SceneManager.LoadScene("Game")`.
- `UIManager` инициализируется в `Start()` после того как `UIDocument` готов.
- Все Singleton'ы (`ShelfManager`, `GachaController`, `BlackMarketManager`, `SaveManager`) должны быть на GameObject'ах в сцене.

---

> 📢 **Промпт для Промптера (Task M3.6):**
> 
> Создай ТЗ для настройки сцены `Game.unity` и доработки `BootstrapController.cs`.  
> `BootstrapController` должен: 1) запустить `GameDataLoader.LoadAsync()`, 2) вызвать `SaveManager.Instance.Load()`, 3) загрузить сцену "Game" через `SceneManager.LoadSceneAsync`.  
> В сцене `Game.unity` Воркер должен вручную создать GameObject'ы для: `GameManager`, `UIManager`, `ShelfManager`, `GachaController`, `BlackMarketManager`, `SaveManager`.  
> Воркер описывает в ТЗ какие компоненты вешать на какие GameObject'ы.  
> Воркер обновляет `INDEX.md` (Bootstrap, UIManager).

---

## ✅ Чеклист Плэннера перед выдачей

- [✅] Фаза данных (NumberFormatter) выделена отдельно
- [✅] Фаза дизайн-системы (Variables/Common USS) — отдельная задача
- [✅] Каждая задача затрагивает ≤ 3 файлов
- [x] Event-Driven фильтр: ни одна UI-панель не вызывает другой менеджер напрямую (кроме команд)
- [x] В каждой задаче напоминание Воркеру обновить `INDEX.md`
- [x] Нет задач "порефакторь и добавь фичу одновременно"
