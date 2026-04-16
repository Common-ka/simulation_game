# UI Foundation (UI Toolkit)

Этот документ описывает базовую стилистику и общие CSS (USS) файлы для интерфейса проекта.

## Статус
✅ Базовые файлы стилей созданы.

## Ключевые файлы
- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные (:root) для цветов, шрифтов и редкости.
- `Assets/Scripts/UI_Toolkit/USS/Common.uss` — общие классы (.hidden, .btn-primary) и редкость.
- `Assets/Scripts/UI_Toolkit/USS/MainScreen.uss` — стили верстки и позиционирования `MainScreen.uxml`.
- `Assets/Scripts/UI_Toolkit/UXML/MainScreen.uxml` — корневой файл (сборщик экрана через `<ui:Instance>`).
- `Assets/Scripts/UI_Toolkit/UXML/HUDPanel.uxml` — шаблон верхней панели с валютами (не скрывается).
- `Assets/Scripts/UI_Toolkit/UXML/ShelfPanel.uxml` — шаблон центральной игровой зоны (скрывается через `UIManager`).
- `Assets/Scripts/UI_Toolkit/UXML/MarketPanel.uxml` — шаблон магазина гачи (скрывается через `UIManager`).
- `Assets/Scripts/UI_Toolkit/UXML/FooterPanel.uxml` — шаблон нижней навигации (не скрывается).

## Использование
1. Всегда используйте данные классы и переменные (через `var(--name)`) вместо хардкода стилей внутри `.uxml`. 
2. Каждый новый экран (окно) должен создаваться изолированно в виде отдельного `*Panel.uxml` шаблона, к `#id` которого в дальнейшем подключаются C# контроллеры.
3. Собранные шаблоны импортируются в корневой `MainScreen.uxml` для отображения `UIDocument` на сцене.
