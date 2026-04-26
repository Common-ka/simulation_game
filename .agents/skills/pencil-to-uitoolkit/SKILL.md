---
name: pencil-to-uitoolkit
description: Переводит дизайн-макеты из Pencil (.pen) в верстку Unity UI Toolkit (UXML + USS). Вызывается, когда задача требует собрать UI-компонент по макету из Pencil-файла.
---

# Pencil → UI Toolkit (Конвейер сборки)

## Когда использовать этот скилл
- Задача требует сверстать UXML/USS по макету из `.pen`-файла.
- Нужно точно перенести размеры, отступы, цвета и шрифты из Pencil в UI Toolkit.
- Задача содержит формулировку «верстка по макету» или «1-к-1 с дизайном».

## Чеклист (Workflow)

- [ ] 1. Прочитать ТЗ и понять, какие элементы нужны (строго по ТЗ, ничего лишнего!).
- [ ] 2. Снять метрики из Pencil (размеры, отступы, цвета, шрифты).
- [ ] 3. Спланировать Flexbox-иерархию перед написанием кода.
- [ ] 4. Создать/обновить UXML-файл.
- [ ] 5. Создать/обновить USS-файл.
- [ ] 6. Визуально проверить результат через `render_ui`.
- [ ] 7. Проверить консоль Unity на USS-ошибки через `read_console`.

## Инструкции

### Шаг 1: Снятие метрик из Pencil

Используй MCP Pencil для чтения макета. Нужно получить:

```
# Что снимать из каждого элемента:
- Размеры: width, height (фиксированные / fill_container / auto)
- Позиция: flex-direction, justify-content, align-items
- Отступы: padding (все стороны), margin (между элементами!)
- Визуал: background-color, border-color, border-width, border-radius
- Шрифт: font-family, font-size, font-weight, color
```

Вызови `batch_get` с `readDepth: 3` для целевого фрейма:
```
mcp_pencil_batch_get(filePath, nodeIds: ["<frameId>"], readDepth: 3)
```

Затем вызови `get_screenshot` для визуальной сверки:
```
mcp_pencil_get_screenshot(filePath, nodeId: "<frameId>")
```

### Шаг 2: Планирование Flexbox-иерархии

**КРИТИЧЕСКИ ВАЖНО:** Перед написанием UXML — нарисуй текстовую схему вложенности.

Пример для HUD-панели:
```
hud-panel (row, space-between)
├── zone-left (row, flex-start)
│   ├── money-block (row) → иконка + значение + IPS
│   └── secondary-currencies (row) → [tokens] [stardust] [keys]
└── zone-right (row, flex-end)
    └── btn-profile (circle)
```

Правила планирования:
- Каждый уровень вложенности = отдельный `VisualElement`.
- Никогда не создавай элементы, которых нет в ТЗ! Если в ТЗ нет «имя игрока» — не добавляй его.
- Определи, где `flex-direction: row`, где `column`.
- Определи, кто растягивается (`flex-grow: 1`), а кто фиксирован.

### Шаг 3: Создание UXML

Структура файла:
```xml
<?xml version="1.0" encoding="utf-8"?>
<ui:UXML
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:ui="UnityEngine.UIElements"
    xsi:noNamespaceSchemaLocation="../../../../UIElementsSchema/UIElements.xsd"
>
    <ui:Style src="../USS/Variables.uss" />
    <ui:Style src="../USS/Common.uss" />
    <ui:Style src="../USS/<ComponentName>.uss" />

    <!-- Единственный корневой элемент -->
    <ui:VisualElement name="<root-name>" class="<root-class>">
        <!-- Дочерние элементы -->
    </ui:VisualElement>
</ui:UXML>
```

Правила именования (из `.docs/project/02-asset-conventions.md`):
- `name` атрибуты: **kebab-case** (`name="btn-buy-upgrade"`)
- Кнопки: `btn-*`, Лейблы: `lbl-*`, Иконки: `img-*`
- Контейнеры: описательные (`name="secondary-currencies"`)

### Шаг 4: Создание USS

Архитектура стилей:
```
Variables.uss  → Глобальные переменные (цвета, шрифты, отступы)
Common.uss     → Переиспользуемые классы (.panel, .card, .btn-primary)
<Component>.uss → Стили конкретного компонента
```

**КРИТИЧЕСКИ ВАЖНО — Ограничения USS (НЕ CSS!):**

Unity UI Toolkit использует **подмножество CSS**. Следующие свойства **НЕ ПОДДЕРЖИВАЮТСЯ**:

| ❌ НЕ работает в USS | ✅ Замена |
|---|---|
| `gap` | `margin-right` / `margin-bottom` на дочерних + `:last-child { margin: 0 }` |
| `box-shadow` (для glow) | `text-shadow` (только для текста) или VisualElement с blur |
| `background: linear-gradient(...)` | Нет нативной поддержки, используй 9-slice спрайт |
| `grid` | Только Flexbox (`flex-direction`, `flex-wrap`) |
| Shorthand `margin: 8px 16px` | Расписывай: `margin-top: 8px; margin-right: 16px;` и т.д. |
| Shorthand `padding: 8px 16px` | `padding: 8px 16px;` работает, НО shorthand `border: 1px solid red` — нет |
| `z-index` | Порядок элементов в UXML определяет z-order |

**Паттерн замены `gap`:**
```css
/* НЕПРАВИЛЬНО — Unity выбросит warning: Unknown property 'gap' */
.container {
    flex-direction: row;
    gap: 32px;  /* ❌ */
}

/* ПРАВИЛЬНО — margin на дочерних */
.container > .child {
    margin-right: 32px;
}
```

**Паттерн назначения иконок:**
```css
/* Определяй пути к картинкам через CSS-переменные в Variables.uss */
:root {
    --img-soft-currency: url('project://database/Assets/Resources/Icons/ic_soft_currency.png');
}

/* Применяй в компонентном USS через ID-селектор */
#img-soft-icon {
    background-image: var(--img-soft-currency);
}
```

**Шрифты:**
```css
:root {
    --font-orbitron: url('project://database/Assets/Fonts/Orbitron-Bold.ttf');
}

.hud-value-text {
    -unity-font: var(--font-orbitron);
    -unity-font-definition: var(--font-orbitron);  /* Обязательно оба! */
    -unity-font-style: bold;
}
```

### Шаг 5: Визуальная проверка

После сохранения файлов:

1. Проверь консоль на ошибки USS:
```
mcp_unityMCP_read_console(types: ["warning", "error"], filter_text: "USS")
```

2. Сделай рендер UI:
```
mcp_unityMCP_manage_ui(action: "render_ui", target: "UI_Root", include_image: true)
# Вызови ДВАЖДЫ — первый раз инициализирует RenderTexture, второй захватывает кадр
```

3. Сравни результат со скриншотом из Pencil.

## Ресурсы
- `.docs/project/02-asset-conventions.md` — Правила именования
- `.docs/project/09-ui-screens-guidelines.md` — Список UI-элементов
- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — Глобальные переменные
- `Assets/Scripts/UI_Toolkit/USS/Common.uss` — Общие классы
