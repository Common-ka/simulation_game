# Вертикальное центрирование MarketPanel, ShelfPanel, SideBarPanel

## Контекст
Панели `HUDPanel` (шапка) и `FooterPanel` (подвал) расположены корректно — прибиты к верху и низу. Три центральные панели — `MarketPanel`, `ShelfPanel` и `SideBarPanel` — должны быть **всегда отцентрированы по оси Y** (ровно посередине между HUD и Footer), но сейчас их дочерние элементы используют `position: absolute` с хардкоженным `top`, из-за чего они не реагируют на flexbox-центрирование родителя.

## Связанная документация
- `AGENTS.md` — глобальные запреты.
- `.docs/project/09-ui-screens-guidelines.md` — UI-гайдлайны (при необходимости).

## Архитектурные требования
- **НЕ менять UXML-файлы** — все изменения только в USS.
- Запрещено использовать `gap` (не поддерживается в Unity UI Toolkit).
- Пиксельные размеры элементов (`width`, `height`) — **не трогать**.
- Горизонтальное расположение (X-позиционирование) — **не трогать** (оно уже правильное).

## Что нужно сделать

### 1. `MainScreen.uss` — `.middle-area` (контейнер трёх панелей)
Убрать inline-стили из `MainScreen.uxml` (строка 25) и перенести их в `.middle-area` класс в `MainScreen.uss`. Добавить `align-items: center;` для вертикального центрирования содержимого. `.middle-area` уже имеет `flex-grow: 1` — это растягивает её на всё доступное пространство между HUD и Footer.

**MainScreen.uxml, строка 25** — убрать inline `style`, оставить только class:
```xml
<ui:VisualElement class="middle-area">
```

**MainScreen.uss** — добавить правило:
```css
.middle-area {
    flex-direction: row;
    flex-grow: 1;
    padding: 0 40px;
    align-items: center; /* Вертикальное центрирование дочерних элементов */
}
```

### 2. `Shelf.uss` — `.shelf-container`
Убрать `position: absolute`, `left: 73px` и `top: 111px`. Shelf должен центрироваться flexbox'ом родителя.

**Было:**
```css
.shelf-container {
    position: absolute;
    left: 73px;
    top: 111px;
    ...
}
```

**Стало:**
```css
.shelf-container {
    /* position/left/top УДАЛЕНЫ — центрируется flexbox'ом родителя */
    width: 586px;
    height: 539px;
    ...остальные свойства без изменений...
}
```

### 3. `Roulette.uss` — `.lot-menu` и `.action-area`
Убрать `position: absolute` и фиксированные `left`/`top`. Вместо этого — позиционировать их через flexbox внутри `.market-panel-root`.

**`.market-panel-root`** — добавить flex-выравнивание:
```css
.market-panel-root {
    flex-grow: 1;
    flex-direction: row;
    align-items: center; /* Центрирование по Y */
    background-color: rgba(0, 0, 0, 0.5);
}
```

**`.lot-menu`** — убрать `position: absolute`, `left`, `top`. Заменить на:
```css
.lot-menu {
    width: 350px;
    padding: 16px;
    margin-right: 24px; /* Отступ от action-area */
    ...остальные стили без изменений...
}
```

**`.action-area`** — убрать `position: absolute`, `left`, `top`. Заменить на:
```css
.action-area {
    align-items: center;
}
```

### 4. `SideBar.uss` — `.side-bar-panel`
SideBar уже имеет `position: absolute` и расположен по правому краю. Его вертикальное центрирование нужно сделать через классический CSS-трик: `top: 50%; translate: 0 -50%`.

**Было:**
```css
.side-bar-panel {
    position: absolute;
    left: 1660px;
    top: 150px;
    ...
}
```

**Стало:**
```css
.side-bar-panel {
    position: absolute;
    right: 40px;          /* Привязка к правому краю вместо хардкода left */
    top: 50%;
    translate: 0 -50%;    /* Сдвиг вверх на половину собственной высоты */
    width: 220px;
    height: 808px;
    ...остальные свойства без изменений...
}
```

> ⚠️ **Если `translate` не поддерживается в вашей версии Unity** — используй альтернативу: убери `height: 808px`, задай `top: 0; bottom: 0; justify-content: center;` чтобы растянуть панель на всю высоту и центрировать карточки внутри.

## Критерии приёмки
- [ ] `MarketPanel` (lot-menu + action-area) всегда по центру Y в middle-area.
- [ ] `ShelfPanel` (shelf-container) всегда по центру Y в middle-area.
- [ ] `SideBarPanel` (offer-карточки) всегда по центру Y экрана.
- [ ] Горизонтальное расположение панелей не изменилось.
- [ ] Размеры элементов (ширина/высота) не изменились.
- [ ] При запуске в разных разрешениях (1920x1080, 1366x768) панели остаются по центру Y.
- [ ] Нет inline-стилей в `MainScreen.uxml` для `.middle-area`.
- [ ] Компиляция без ошибок.
