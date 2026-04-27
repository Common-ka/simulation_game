# 📋 План Milestone M3: UI-Скелет (UI Toolkit)

> **Дата создания:** 2026-04-16
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
- ✅ `UIManager` — логика переключения панелей (скрытие/показ через классы)
- ✅ `UI_Foundation` — базовые стили (`Variables.uss`, `Common.uss`)
- ✅ `BootstrapController` — заглушка под инициализацию
- ✅ **Task M3.3 (Visual UI Skeleton):** Созданы модульные шаблоны интерфейса `MainScreen.uxml` с инстансами `HUDPanel.uxml`, `ShelfPanel.uxml`, `MarketPanel.uxml` и `FooterPanel.uxml`. 

**Что отсутствует:**
- ❌ C#-контроллеры привязки логики (Binding) к этим уже готовым UXML файлам (`HUDPanel.cs`, `ShelfPanel.cs`, `MarketPanel.cs`, `FooterPanel.cs`).
- ❌ Связь кнопок с `UIManager` для навигации.
- ❌ Сцена `Game.unity` не настроена, в ней нет привязанного компонента `UIDocument` c собранными контроллерами UI Toolkit.

**Вывод:** Интерфейсная верстка (Mockup) собрана, бэкенд есть. Нужно их правильно "склеить" (привязать данные к `#id` элементов) без нарушения Event-Driven архитектуры.

---

## ⚙️ Архитектурный фильтр (Event-Driven анализ)

| Контроллер | Owner Events | Подписчики (UI) |
|---|---|---|
| `HUDPanel.cs` | `GameManager.OnGameStateChanged` | Подписывается на `OnGameStateChanged`, читает `SoftCurrency`, `IPS` и др. |
| `MarketPanel.cs` | Клик по кнопке "Buy" | Вызывает `GachaController.BuyLoot()`. Подписыв. на `OnGameStateChanged` (чтобы блокировать кнопку по деньгам). |
| `ShelfPanel.cs` | `ShelfManager.OnShelfUpdated` | Обновляет слоты на основе вызванного события. |
| `FooterPanel.cs`| Клики по вкладкам | Вызывает `UIManager.Instance.Show(panelName)` |

**Запрещено:** 
1. Воссоздавать или перезаписывать `*.uxml` файлы из папки `UXML`. Они УЖЕ есть! Мы пишем только код.
2. Прямые вызовы из бизнес-логики в UI-классы (UI должен сам слушать события).

---

## 🎨 Декомпозиция задач (UI Toolkit Верстка 1:1 по макету Pencil)

> **Внимание Промптеру:** Текущая верстка черновых UXML не годится (барахло). Прежде чем писать C# Binding, необходимо воссоздать визуальную часть в UI Builder с точностью до пикселя, используя переданные метрики из Pencil.

---

---

---

### Task M3.3.3 — Точечная сборка ShelfPanel (Витрина) ❌
**Цель:** Верстка полки и слотов для игровых предметов.
- Создать правильную сетку 5 слотов с учетом фонового изображения (`UP3d6`).
- Метрики общего бокса: 555x533. Использовать тени для объема.
> **Промпт для Промптера:** ТЗ на верстку `ShelfPanel.uxml` (+ `Shelf.uss`), чтобы слоты ровно ложились на картинку полки. Настроить Flexbox для содержимого каждого слота.

---

### Task M3.3.4 — Точечная сборка SideBarPanel (Офферы) ❌
**Цель:** Панель быстрых предложений и таймеров.
- Строгий `flex-direction: vertical`, gap 16, width 220px. Абсолютное позиционирование для всего блока по правой стороне.
> **Промпт для Промптера:** ТЗ на верстку кнопок офферов в `SideBarPanel.uxml` (+ `SideBar.uss`) со скруглениями углов, таймерами поверх прозрачных темных фонов.

---

### Task M3.3.5 — Точечная сборка FooterPanel (Нижняя навигация) ❌
**Цель:** Подвал для переключения вкладок.
- Высота 143px, горизонтальный Flex. Выровнять иконки и тексты кнопок.
> **Промпт для Промптера:** ТЗ на верстку меню навигации `FooterPanel.uxml`. Кнопки (Upgrades, Inventory, Gacha, Sticker Album) должны заполнять всю ширину равномерно (`flex-grow: 1`).

---

### Task M3.3.6 — Адаптив и Валидация в MainScreen ❌
**Цель:** Интегрировать компоненты в сборщик с фоновой картинкой экрана.
- Проверить скейлинг `MainScreen` при 16:9, добавив базовую картинку фона на весь экран.
- Убедиться, что Flexbox (column layout) работает, панели не распадаются.
> **Промпт для Промптера:** ТЗ на финальную верстку сборщика `MainScreen.uxml`. Вставить фон `Z8DL2` и проверить UI Toolkit Panel Settings, чтобы UI рендерился адаптивно на всех экранах (Reference Resolution). Обязательная визуальная валидация (render_ui_screenshot).

---

## 📦 Декомпозиция задач (C# Binding)

### Task M3.4 — HUD Component Logic (HUDPanel.cs) ❌

**Цель:** Привязать игровые данные (валюты) к существующей панели `HUDPanel.uxml`.

**Архитектурные ограничения:**
- Создаем скрипт `HUDPanel.cs` (Monobehaviour).
- Во время `Start()` он находит в прикрепленном объекте (или глобальном `UIDocument`) корневой элемент HUD, и получает ссылки на лейблы валют (Soft Currency, IPS, Stardust, Keys) по их `#id`.
- Подписывается на `GameManager.OnGameStateChanged`. При срабатывании - обновляет UI тексты. Для форматирования денег использует написанный ранее `NumberFormatter.Format()`. 
- Отписывается в `OnDestroy()`.

---

> 📢 **Промпт для Промптера (Task M3.4):**
> 
> Создай ТЗ на разработку скрипта логики `HUDPanel.cs`. UXML шаблон уже создан ранее, трогать его не надо. Скрипт (MonoBehaviour) должен найти элементы HUD по их `#id`. Подписаться на `GameManager.OnGameStateChanged` и обновлять тексты SoftCurrency (через `NumberFormatter`), IPS (через `IPSCalculator`), а также доставать Stardust и Keys из `SaveManager.Instance.Data`. Обязательно сделать отписку в `OnDestroy`.
> Воркер обновляет `INDEX.md`.

---

### Task M3.5 — Footer Navigation Logic (FooterPanel.cs) ❌

**Цель:** Реализовать навигацию между вкладками через подвал.

**Архитектурные ограничения:**
- Создаем скрипт `FooterPanel.cs` (MonoBehaviour).
- Находит кнопки вкладок по `#id`.
- При клике на кнопки вызывает `UIManager.Instance.Show("MarketPanel")`, `UIManager.Instance.Show("UpgradesPanel")` и т.д.

---

> 📢 **Промпт для Промптера (Task M3.5):**
> 
> Создай ТЗ на разработку скрипта навигации `FooterPanel.cs`. Верстка кнопок уже есть в UXML-шаблоне. В скрипте надо зарегистрировать коллбеки на клики кнопок вкладок (`clicked += ...`) и вызывать через `UIManager.Instance.Show` соответствующие названия панелей (например, `MarketPanel`, `UpgradesPanel` и т.д.).
> Воркер обновляет `INDEX.md`.

---

### Task M3.6 — Market & Gacha Logic (MarketPanel.cs) ❌

**Цель:** Привязать магазин и рулетку к `MarketPanel.uxml`.

**Архитектурные ограничения:**
- Создаем `MarketPanel.cs`.
- Скрипт находит кнопку покупки (Гачи) по её `#id`.
- Слушает `GameManager.OnGameStateChanged`, чтобы проверять `SoftCurrency >= Price`. Если денег меньше - кнопка покупки получает состояние "Disabled". 
- На клик покупки вызывает `GachaController.Instance.BuyLoot(categoryId)`.
- Скрытием/показом этой панели управляет `UIManager`, поэтому скрипт только биндит данные и клики.

---

> 📢 **Промпт для Промптера (Task M3.6):**
> 
> Создай ТЗ для скрипта `MarketPanel.cs`. Верстка гачи и магазина уже есть в UXML. В скрипте: по подписке на смену стейта проверять состояние баланса, блокировать/разблокировать кнопку "BuyLoot" (через `SetEnabled` или CSS). При клике вызывать `GachaController.Instance.BuyLoot()`. Подписка/отписка строго по событиям. 
> Воркер обновляет `INDEX.md`.

---

### Task M3.7 — Shelf Vis Logic (ShelfPanel.cs) ❌

**Цель:** Визуализация 5 полок инвентаря игрока (`ShelfPanel.uxml`).

**Архитектурные ограничения:**
- Создаем `ShelfPanel.cs`.
- Находит все 5 слотов по `#id`.
- Подписка на `ShelfManager.OnShelfUpdated`. Когда событие генерируется, оно возвращает актуальный инвентарь (или словарю можно получить через синглтон). Обновляем в слотах текст (названия, IPS) и задаём нужный CSS класс редкости (из `Variables.uss`).

---

> 📢 **Промпт для Промптера (Task M3.7):**
> 
> Создай ТЗ для контроллера `ShelfPanel.cs`. Витрина полок уже создана в UXML. Подписка в скрипте на `ShelfManager.OnShelfUpdated`. По событию обходить 5 слотов на экране и менять для каждого текст и класс цвета (добавление класса `.card-rare`, удаление старого). 
> Воркер обновляет `INDEX.md`.

---

### Task M3.8 — Интеграция в Scene (Bootstrap → Game) ❌

**Цель:** Оживить проект в рабочей сцене Unity. 

**Архитектурные ограничения:**
- В `Game.unity` завести компонент `UIDocument` c загруженным `MainScreen.uxml`.
- Создать GameObject (или раскидать на сам UIDocument) с компонентами `HUDPanel`, `ShelfPanel`, `FooterPanel`, `MarketPanel`. Каждый должен получить ссылку на корневой `UIDocument`.
- Настроить `BootstrapController.cs` для правильного цикла: `GameDataLoader.LoadAsync()` -> `SaveManager.Instance.Load()` -> `SceneManager.LoadScene("Game")`. Это обеспечит инициализацию менеджеров ДО того, как загрузится интерфейс.

---

> 📢 **Промпт для Промптера (Task M3.8):**
> 
> Создай ТЗ для настройки сцены `Game.unity`. Собрать сцену так, чтобы там был `UIDocument` (с прилинкованным `MainScreen.uxml`) и все созданные контроллеры (`HUDPanel`, `ShelfPanel`, `MarketPanel`, `FooterPanel`). Доработать скрипт `BootstrapController.cs` (что сейчас в загрузочной сцене Load), чтобы он вызывал инициализацию Data, загрузку сейва, а потом асинхронно переходил в `Game.unity`.
> Воркер обновляет `INDEX.md`.
