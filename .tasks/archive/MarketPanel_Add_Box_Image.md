# MarketPanel_Add_Box_Image

## Контекст
Необходимо добавить изображение ящика (`box.png`) под кнопку "Open Lot" и индикатор цены в панели `MarketPanel.uxml`. 
В данный момент кнопка и цена находятся в блоке `action-area` и центрируются по вертикали. Нужно, чтобы изображение располагалось под ними.

## Связанная документация (ОПТИМИЗАЦИЯ ТОКЕНОВ)
- `.docs/project/09-ui-screens-guidelines.md`
- `.docs/project/02-asset-conventions.md`

## Архитектурные требования
- Соблюдать паттерны UI Toolkit: добавить элемент в `MarketPanel.uxml` и вынести стилизацию в `Roulette.uss`.
- Путь к изображению: `Assets/Resources/Items/box.png`.
- В USS для установки картинки рекомендуется использовать свойство `background-image: url('project://database/Assets/Resources/Items/box.png');` и настроить масштаб (`-unity-background-scale-mode: scale-to-fit`).
- Задать адекватные размеры для ящика (например, 200x200px или пропорционально) и отступ сверху (`margin-top`), чтобы он визуально отделялся от цены.

## Критерии приемки (Acceptance Criteria)
- [+] В `MarketPanel.uxml` внутри `action-area` добавлен новый `<ui:VisualElement>` для отображения ящика (сразу после `price-indicator`).
- [+] В `Roulette.uss` добавлен класс для нового элемента, задающий `background-image`, `width`, `height`, `-unity-background-scale-mode` и `margin-top`.
- [+] Элементы (кнопка "Open Lot", цена и изображение ящика) визуально выровнены по центру.
- [+] Изменения корректно отображаются в UI Builder.
