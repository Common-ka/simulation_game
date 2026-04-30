---
name: pencil-to-uitoolkit
description: Переводит дизайн-макеты из Pencil (.pen) в верстку Unity UI Toolkit (UXML + USS). Вызывается, когда задача требует сверстать UI-компонент по макету из Pencil-файла.
---

# Pencil → UI Toolkit

## Чеклист

- [ ] 1. Снять метрики из Pencil (`batch_get` + `get_screenshot`).
- [ ] 2. **Прочитать `Variables.uss`** — сверить шрифты и цвета (см. «Валидация шрифтов»).
- [ ] 3. Проверить наличие необходимых ассетов (иконки, спрайты). Если нет — генерировать/добавить **до начала верстки**.
- [ ] 4. Прочитать `MainScreen.uxml` — определить родительский контейнер.
- [ ] 5. Выбрать стратегию позиционирования.
- [ ] 6. Пересчитать координаты Pencil → USS.
- [ ] 7. Нарисовать текстовую схему иерархии.
- [ ] 8. Создать UXML по шаблону.
- [ ] 9. **Прочитать `unity-uitoolkit-pitfalls` ЦЕЛИКОМ**. Создать USS, сверяясь с чеклистом pitfalls.
- [ ] 10. Проверить интеграцию в `MainScreen.uxml` (класс на `<ui:Instance>`!).
- [ ] 11. `read_console` → ошибки парсинга. **Ноль ворнингов = ОК.**
- [ ] 12. Скриншот Pencil + скриншот Unity → визуальное сравнение.

## Валидация шрифтов и цветов

**НИКОГДА не копируй `fontFamily` из Pencil напрямую!** Дизайнер часто оставляет заглушки (Inter, Arial, Roboto). Единственный источник правды — `Variables.uss`.

```
❌ Pencil: fontFamily: "Inter" → USS: font-family: Inter
   Агент слепо копирует шрифт из макета

✅ Pencil: fontFamily: "Inter" → Проверяем Variables.uss:
   --font-russo-one → заголовки
   --font-jetbrains-mono → данные/подзаголовки
```

**Алгоритм:**
1. Извлечь `fontFamily`, `fontSize`, `fill` из Pencil-узла.
2. Открыть `Variables.uss` → найти соответствующую переменную шрифта.
3. Если точного маппинга нет — спросить пользователя.

## Проверка ассетов до верстки

Перед началом кода убедись, что ВСЕ ресурсы уже существуют:

```
list_dir("Assets/Resources/Icons/")
list_dir("Assets/Fonts/")
```

Если иконок нет → сгенерируй и скопируй **ДО** написания UXML/USS. Не начинай верстку с несуществующими ассетами.

## Снятие метрик

```
mcp_pencil_batch_get(filePath, nodeIds: ["<id>"], readDepth: 3)
mcp_pencil_get_screenshot(filePath, nodeId: "<id>")
```

Снимать: `width`, `height`, `x`, `y`, `padding`, `layout`, `fill`, `stroke`, `effect`.

**Обязательно** сделай `get_screenshot` целевого узла до написания кода.

**ИГНОРИРУЙ из Pencil:** `fontFamily`, `gap` — эти данные не переносятся напрямую.

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

## Равномерное распределение кнопок

Для N кнопок в ряд (например, навигация):

```css
❌ .btn { flex-grow: 1; }
   /* Кнопки разного размера — текст влияет на ширину */

✅ .btn {
    flex-grow: 1;
    flex-shrink: 1;
    flex-basis: 0;
    min-width: 0;
    overflow: hidden;
}
```

`flex-basis: 0` + `min-width: 0` — ЕДИНСТВЕННЫЙ способ гарантировать одинаковую ширину.

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

**Ноль ворнингов в консоли = обязательное условие завершения.**

## Ресурсы

- `.agents/skills/unity-uitoolkit-pitfalls/SKILL.md` — читать перед USS
- `.docs/project/02-asset-conventions.md` — именование
- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные (ЕДИНСТВЕННЫЙ источник правды для шрифтов и цветов)
