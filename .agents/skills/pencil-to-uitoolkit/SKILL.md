---
name: pencil-to-uitoolkit
description: Переводит дизайн-макеты из Pencil (.pen) в верстку Unity UI Toolkit (UXML + USS). Вызывается, когда задача требует собрать UI-компонент по макету из Pencil-файла.
---

# Pencil → UI Toolkit

## Когда использовать
- Задача требует сверстать UXML/USS по макету из `.pen`-файла.
- Формулировки: «верстка по макету», «1-к-1 с дизайном».

## Чеклист

- [ ] 1. Снять метрики из Pencil (`batch_get` + `get_screenshot`).
- [ ] 2. Выбрать стратегию позиционирования (Flex vs Absolute).
- [ ] 3. Пересчитать координаты Pencil → USS.
- [ ] 4. Нарисовать текстовую схему иерархии.
- [ ] 5. Создать UXML.
- [ ] 6. Прочитать скилл `unity-uitoolkit-pitfalls`. Создать USS.
- [ ] 7. `read_console` → проверить ошибки парсинга.
- [ ] 8. Скриншот Pencil + скриншот Unity → визуальное сравнение.

## Снятие метрик

```
mcp_pencil_batch_get(filePath, nodeIds: ["<id>"], readDepth: 3)
mcp_pencil_get_screenshot(filePath, nodeId: "<id>")
```

Снимать: `width`, `height`, `x`, `y`, `padding`, `gap`, `layout`, `fill`, `stroke`, `effect`, шрифт.

## Стратегия позиционирования

| Случай | Стратегия |
|---|---|
| Элементы в ряд/колонку (список, строка кнопок) | Flexbox |
| Элементы на фиксированных позициях поверх фона | `position: absolute` |
| Привязка к краю родителя | `position: absolute` |

## Пересчёт координат

Pencil хранит `x, y` как абсолютные координаты от (0,0) экрана.
USS позиционирует относительно родительского контейнера.

```
USS_top  = Pencil_Y - Parent_offset_Y
USS_left = Pencil_X - Parent_offset_X
```

Пример: элемент `y: 224`, вложен в контейнер после HUD (137px высотой):
```
top = 224 - 137 = 87px
```

Если родитель центрирует детей (`align-items: center`), `margin-top` считается от центра — используй `position: absolute`.

## Схема иерархии

Перед написанием UXML — текстовая схема:
```
panel (row, space-between)
├── zone-left (row)
│   ├── block-a (row) → icon + value
│   └── block-b (row) → [item1] [item2]
└── zone-right (row, flex-end)
    └── btn-action
```

## UXML-шаблон

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

    <ui:VisualElement name="<root>" class="<root-class>">
        <!-- ... -->
    </ui:VisualElement>
</ui:UXML>
```

Именование: kebab-case. Кнопки `btn-*`, лейблы `lbl-*`, иконки `img-*`.

## USS — архитектура

```
Variables.uss   → переменные (цвета, шрифты)
Common.uss      → общие классы (.panel, .card)
<Component>.uss → стили компонента
```

Замена `gap` → `margin` на дочерних. Без `:last-child` (сломает файл).

## Визуальная проверка

```
mcp_unityMCP_read_console(count: 20)
mcp_pencil_get_screenshot(filePath, nodeId: "<screenId>")
mcp_unityMCP_manage_camera(action: "screenshot", include_image: true)
```

Сравнить вертикальное положение, размеры блоков, бордеры.

## Ресурсы
- `.agents/skills/unity-uitoolkit-pitfalls/SKILL.md` — читать перед USS
- `.docs/project/02-asset-conventions.md` — именование
- `.docs/project/09-ui-screens-guidelines.md` — экраны
- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные
