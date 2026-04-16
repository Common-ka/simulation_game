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

### Task M3.3 — Main Screen Skeleton (Visual Only) ❌

**Цель:** Собрать единый визуальный каркас (Root UI) с плейсхолдерами для HUD, витрины, магазина и навигации, чтобы сразу увидеть всю композицию экрана. Без C# логики.

**Архитектурные ограничения:**
- Только UXML и USS (использование Flexbox, абсолютного позиционирования шапки и подвала).
- Создаем файл `Assets/Scripts/UI_Toolkit/UXML/MainScreen.uxml` и стили `MainScreen.uss` (подключает `Variables.uss` и `Common.uss`).
- Включает 4 зоны (согласно 09-ui-screens-guidelines.md):
  1. Шапка (HUD): SoftCurrency, IPS, HardCurrency, Stardust, Keys.
  2. Геймплей: Центральная витрина на 5 слотов (заглушки).
  3. Магазин-Геймплей: Блок текущей категории и массивная кнопка "Крутить".
  4. Подвал: 4 кнопки навигации (Upgrades, Gacha, Shop, Black Market).
- Каждому логическому элементу задать уникальный `#id` (например, `#hud-soft-currency`, `#shelf-slot-1`, `#shop-spin-btn`), чтобы в будущем C# скрипты могли к ним привязаться.

---

> 📢 **Промпт для Промптера (Task M3.3):**
> 
> Создай ТЗ на разработку корневого UXML-документа: `Assets/Scripts/UI_Toolkit/UXML/MainScreen.uxml` и стилей `MainScreen.uss` (подключает `Variables.uss` и `Common.uss`).
> Основная задача: сверстать весь экран на плейсхолдерах из цветных блоков и dummy-текста (например, '1.5M', квадраты вместо иконок).
> Разбить на зоны через Flexbox: Header (HUD валют), Middle (Контентная часть - Полка витрины из 5 карточек, ниже Блок покупки Gacha лота), Footer (4 кнопки навигации внизу).
> Установить всем интерактивным и изменяемым элементам уникальные `#id`.
> C# скрипты в этой задаче НЕ писать! Только верстка. Проверка: Открытие UXML в UI Builder должно сразу показывать красивый Layout согласно гайдлайну. Воркер обновляет `INDEX.md`.

---

### Task M3.4 — HUDPanel.cs (Logic Binding) ❌

**Цель:** Вдохнуть жизнь в шапку (HUD) из `MainScreen.uxml`.

**Архитектурные ограничения:**
- `HUDPanel` берет корневой `UIDocument` (задается в инспекторе или инициализируется сценой) и достает элементы по `#id`.
- По событию `GameManager.OnGameStateChanged` читает `GameSnapshot.SoftCurrency`, форматирует через `NumberFormatter`.
- IPS берёт из `IPSCalculator.GetCurrentIPS(...)`.
- Stardust и Keys — из `SaveManager.Data` (у них нет своих ивентов? можно проверять в том же OnGameStateChanged или отдельном).
- Никаких вызовов бизнес-логики, только чтение.

---

> 📢 **Промпт для Промптера (Task M3.4):**
> 
> Создай ТЗ для `Assets/Scripts/UI/Panels/HUDPanel.cs` — MonoBehaviour, который висит на объекте и биндит данные к UI.
> Скрипт получает ссылку на `UIDocument`, ищет элементы HUD по `#id` (валюты, stardust, keys, ips). Добавляет Subscribe на `GameManager.OnGameStateChanged`. Обновляет Label'ы через `NumberFormatter`.
> Воркер обновляет `INDEX.md`.

---

### Task M3.5 — FooterPanel.cs (Logic) ❌

**Цель:** Привязать логику к кнопкам навигации из подвала `MainScreen.uxml`.

**Архитектурные ограничения:**
- `FooterPanel` находит кнопки по `#id`.
- Подписывается на клики `clicked += ...` и вызывает `UIManager.Instance.ShowPanel(name)` (если реализовано скрытие/показ центральной зоны). 
- На текущем этапе можно просто повесить Debug.Log или вызов UIManager.

---

> 📢 **Промпт для Промптера (Task M3.5):**
> 
> Создай ТЗ для `Assets/Scripts/UI/Panels/FooterPanel.cs`.
> Получает `UIDocument`, ищет 4 кнопки футера по `#id`. При клике отправляет вызов в `UIManager.Instance` (или пишет `Debug.Log`).
> Воркер обновляет `INDEX.md`.

---

### Task M3.6 — ShopPanel.cs (Logic Binding) ❌

**Цель:** Вдохнуть жизнь в блок Геймплей-Магазина из `MainScreen.uxml`.

**Архитектурные ограничения:**
- Ищет элементы шоп-блока по `#id`.
- Подписывается на `GameManager.OnGameStateChanged`, чтобы проверять доступность кнопки "Крутить" по деньгам.
- Клик по кнопке вызывает `GachaController.Instance.BuyLoot(categoryIndex)`.

---

> 📢 **Промпт для Промптера (Task M3.6):**
> 
> Создай ТЗ для `Assets/Scripts/UI/Panels/ShopPanel.cs`.
> Получает `UIDocument`, находит элементы шоп-панели. Subscribe на `GameManager.OnGameStateChanged` для проверки `SoftCurrency >= price`. Блокирует кнопку, если денег нет. При клике вызывает `GachaController.Instance.BuyLoot`.
> Воркер обновляет `INDEX.md`.

---

### Task M3.7 — ShelfPanel.cs (Logic Binding) ❌

**Цель:** Заполнить витрину (середина экрана) реальными предметами из `ShelfManager`.

**Архитектурные ограничения:**
- `ShelfPanel.cs` — только подписчик `ShelfManager.OnShelfUpdated`.
- Внутри UXML слоты уже размечены по `#id`. Панель обновляет их содержимое (названия, бусты).
- Цвет иконки меняет через добавление классов из `Common.uss` (напр. `.card-rare`), убирая предыдущие.

---

> 📢 **Промпт для Промптера (Task M3.7):**
> 
> Создай ТЗ для `Assets/Scripts/UI/Panels/ShelfPanel.cs`.
> Получает `UIDocument`, ищет 5 слотов по `#id`. Subscribe на `ShelfManager.OnShelfUpdated`. Когда приходит обновление инвентаря полки, скрипт обновляет Label'ы карточек и CSS-классы редкости.
> Воркер обновляет `INDEX.md`.

---

### Task M3.8 — Сборка сцены Game.unity + Bootstrap → Game ❌

**Цель:** Настроить сцену `Game.unity` и запуск игры.

**Архитектурные ограничения:**
- Порядок в `BootstrapController`: `GameDataLoader.LoadAsync()` → `SaveManager.Load()` → `SceneManager.LoadScene("Game")`.
- GameObject'ы с менеджерами и панелями (HUDPanel, ShopPanel, ShelfPanel, FooterPanel) настроены в сцене со ссылкой на `UIDocument`.

---

> 📢 **Промпт для Промптера (Task M3.8):**
> 
> Создай ТЗ для настройки сцены `Game.unity` и доработки `BootstrapController.cs`.
> `BootstrapController` должен реализовать загрузку данных и переход в сцену "Game".
> В сцене `Game.unity` Воркер должен создать GameObject с компонентом `UIDocument` (с прикрепленным `MainScreen.uxml`) и GameObject'ы контроллеров (`HUDPanel`, `ShelfPanel` и т.д.), которые будут ссылаться на этот UIDocument.
> Воркер обновляет `INDEX.md`.

---
