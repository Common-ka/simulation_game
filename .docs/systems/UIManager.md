# UIManager

Менеджер для управления отображением панелей интерфейса.

## Статус
✅ Реализовано

## Ключевые классы
- `UIManager` (Singleton MonoBehaviour)

## API
- `Show(string panelName)`: Убирает класс `hidden` у элемента.
- `Hide(string panelName)`: Добавляет класс `hidden` элементу.
- `ShowOnly(string panelName)`: Скрывает все закэшированные панели и показывает только указанную.

## Зависимости
- `UIDocument`: Корневой документ UI Toolkit.
- `hidden` (USS класс): Ожидается наличие класса `.hidden { display: none; }` в подключенных стилях.
