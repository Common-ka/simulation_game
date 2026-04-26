# UI Foundation (UI Toolkit)

Этот документ описывает базовую стилистику и общие CSS (USS) файлы для интерфейса проекта.

## Статус
✅ Базовые файлы стилей созданы. 
⚠️ КРИТИЧНО: Иерархия HUDPanel очищена от лишних данных (lvl, статы), строго согласно дизайну Gameplay.

## Особенности верстки (Compatibility)
- **Gap vs Margin**: В текущей версии проекта свойство `gap` в USS не поддерживается (Unknown property). Для создания отступов между элементами следует использовать `margin` на дочерних элементах.
- **Шрифт**: Для корректного рендеринга текста всегда указывайте пару свойств: `-unity-font` и `-unity-font-definition`.
- **Иконки**: Пути к ассетам указываются в Variables.uss через `project://database/Assets/...`.

## Ключевые файлы
- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные (:root) для цветов, шрифтов и редкости.
- `Assets/Scripts/UI_Toolkit/USS/Common.uss` — общие классы (.hidden, .btn-primary) и редкость.
- `Assets/Scripts/UI_Toolkit/USS/MainScreen.uss` — корневой файл для импорта стилей и базового стиля `MainScreen.uxml`.
- `Assets/Scripts/UI_Toolkit/USS/HUD.uss` — изолированные стили для верхней панели с валютами.
- `Assets/Scripts/UI_Toolkit/USS/Shelf.uss` — изолированные стили для витрины игровой зоны.
- `Assets/Scripts/UI_Toolkit/USS/Roulette.uss` — изолированные стили для магазина гачи и рулеток.
- `Assets/Scripts/UI_Toolkit/USS/Navigation.uss` — изолированные стили для нижней навигационной панели.
- `Assets/Scripts/UI_Toolkit/USS/SideBar.uss` — изолированные стили для плавающих предложений (офферов).
- `Assets/Scripts/UI_Toolkit/UXML/MainScreen.uxml` — корневой файл (сборщик экрана через `<ui:Instance>`).
- `Assets/Scripts/UI_Toolkit/UXML/HUDPanel.uxml` — шаблон верхней панели с валютами (не скрывается).
- `Assets/Scripts/UI_Toolkit/UXML/ShelfPanel.uxml` — шаблон центральной игровой зоны (скрывается через `UIManager`).
- `Assets/Scripts/UI_Toolkit/UXML/MarketPanel.uxml` — шаблон магазина гачи (скрывается через `UIManager`).
- `Assets/Scripts/UI_Toolkit/UXML/FooterPanel.uxml` — шаблон нижней навигации (не скрывается).
- `Assets/Scripts/UI_Toolkit/UXML/SideBarPanel.uxml` — шаблон с плавающими офферами (не скрывается, всегда поверх).

## Использование
1. Всегда используйте данные классы и переменные (через `var(--name)`) вместо хардкода стилей внутри `.uxml`. 
2. Каждый новый экран (окно) должен создаваться изолированно в виде отдельного `*Panel.uxml` шаблона, к `#id` которого в дальнейшем подключаются C# контроллеры.
3. Собранные шаблоны импортируются в корневой `MainScreen.uxml` для отображения `UIDocument` на сцене.
