---
name: pencil-to-uitoolkit
description: Переводит дизайн-макеты из Pencil (.pen) в верстку Unity UI Toolkit (UXML + USS). Вызывается, когда задача требует сверстать UI-компонент по макету из Pencil-файла.
---

# Pencil → UI Toolkit

## Чеклист

- [ ] 1. Снять метрики из Pencil (`batch_get` + `get_screenshot`).
- [ ] 2. Прочитать `MainScreen.uxml` — определить родительский контейнер.
- [ ] 3. Выбрать стратегию позиционирования.
- [ ] 4. Пересчитать координаты Pencil → USS.
- [ ] 5. Нарисовать текстовую схему иерархии.
- [ ] 6. Создать UXML по шаблону.
- [ ] 7. Прочитать `unity-uitoolkit-pitfalls`. Создать USS.
- [ ] 8. Проверить интеграцию в `MainScreen.uxml` (класс на `<ui:Instance>`!).
- [ ] 9. `read_console` → ошибки парсинга.
- [ ] 10. Скриншот Pencil + скриншот Unity → визуальное сравнение.

## Снятие метрик

```
mcp_pencil_batch_get(filePath, nodeIds: ["<id>"], readDepth: 3)
mcp_pencil_get_screenshot(filePath, nodeId: "<id>")
```

Снимать: `width`, `height`, `x`, `y`, `padding`, `gap`, `layout`, `fill`, `stroke`, `effect`, шрифт.

**Обязательно** сделай `get_screenshot` целевого узла до написания кода.

## Стратегия позиционирования

| Случай | Стратегия |
|---|---|
| Элементы в ряд/колонку | Flexbox |
| Фиксированные позиции поверх фона | `position: absolute` |
| Плавающий оверлей (SideBar, Popup) | `position: absolute` на `<ui:Instance>` в MainScreen |

## Пересчёт координат

```
USS_left = Pencil_X - Parent_offset_X
USS_top  = Pencil_Y - Parent_offset_Y
```

### Пример: плавающий оверлей

Если компонент — прямой ребёнок корневого `MainScreen`, то `Parent_offset = (0, 0)`:
```
Pencil: x=1660, y=150  →  USS: left: 1660px; top: 150px;
```

### Пример: элемент внутри flex-контейнера

Экран 1920px делится пополам. Правая панель начинается с X=960:
```
Pencil: x=1033  →  USS: left = 1033 - 960 = 73px;
❌ left: 1033px → элемент на 960+1033 = 1993px (за экраном)
```

## Ловушки

### TemplateContainer

`<ui:Instance>` создаёт скрытый `TemplateContainer` между родителем и корнем UXML.

```xml
❌ НЕПРАВИЛЬНО — класс на внутреннем элементе (TemplateContainer остаётся flex)
<ui:Instance template="SideBarPanel" />
<!-- SideBarPanel.uxml: -->
<ui:VisualElement class="side-bar-panel"> ← absolute не поможет

✅ ПРАВИЛЬНО — класс на Instance (= на TemplateContainer)
<ui:Instance template="SideBarPanel" class="side-bar-panel" />
<!-- SideBarPanel.uxml: -->
<ui:VisualElement name="side-bar-panel"> ← без класса позиционирования
```

### Порядок в иерархии

Плавающие элементы — **первыми** в `MainScreen`, до HUD/Content/Footer. Иначе `space-between` сдвинет `TemplateContainer`.

```xml
<ui:VisualElement name="MainScreen" class="main-screen">
    <ui:Instance template="SideBarPanel" class="side-bar-panel" />  ← первый
    <ui:Instance template="HUDPanel" />
    ...
</ui:VisualElement>
```

## UXML-шаблон

**Пути к стилям всегда `../USS/`!** Файлы UXML и USS лежат в соседних папках.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ui:UXML
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:ui="UnityEngine.UIElements"
    xsi:noNamespaceSchemaLocation="../../../../UIElementsSchema/UIElements.xsd"
>
    <ui:Style src="../USS/Variables.uss" />
    <ui:Style src="../USS/Common.uss" />
    <ui:Style src="../USS/<Component>.uss" />

    <ui:VisualElement name="<root-name>">
        <!-- контент -->
    </ui:VisualElement>
</ui:UXML>
```

```
❌ src="Variables.uss"         → стили не загрузятся, элементы невидимы
✅ src="../USS/Variables.uss"  → корректный относительный путь
```

Именование: kebab-case. Кнопки `btn-*`, лейблы `lbl-*`, иконки `img-*`.

## Схема иерархии

Перед UXML — текстовая схема:
```
panel (col, center)
├── card-1 (col) → icon + label + timer-badge(abs)
├── card-2 (col) → icon + label + timer-badge(abs)
└── card-3 (col) → icon + label + timer-badge(abs)
```

## Визуальная проверка

```
mcp_unityMCP_read_console(count: 20)
mcp_pencil_get_screenshot(filePath, nodeId: "<screenId>")
mcp_unityMCP_manage_camera(action: "screenshot", include_image: true)
```

## Ресурсы

- `.agents/skills/unity-uitoolkit-pitfalls/SKILL.md` — читать перед USS
- `.docs/project/02-asset-conventions.md` — именование
- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные
