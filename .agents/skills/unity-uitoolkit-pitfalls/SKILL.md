---
name: unity-uitoolkit-pitfalls
description: Справочник ловушек Unity UI Toolkit USS, специфичных для этого проекта. Вызывается перед написанием или отладкой любых USS-стилей.
---

# USS Pitfalls

## ⛔ Чеклист перед КАЖДЫМ сохранением USS

**Пройди КАЖДЫЙ пункт. Нарушение любого = ворнинг или молчаливая поломка.**

- [ ] Нет `:last-child` / `:first-child` / `:nth-child` / `:not()` / `::before`
- [ ] Нет `calc()` / `min()` / `max()`
- [ ] **Нет `gap`** → заменяй на `margin` у дочерних элементов
- [ ] Нет `box-shadow`, `linear-gradient()`
- [ ] Нет shorthand `border` → расписывай `border-width` + `border-color`
- [ ] **Нет shorthand `padding: X Y`** → расписывай по сторонам
- [ ] **Нет shorthand `flex: X Y Z`** → расписывай `flex-grow`, `flex-shrink`, `flex-basis`
- [ ] Нет `right` / `bottom` для абсолютного позиционирования
- [ ] Шрифты: `-unity-font-definition` (в некоторых версиях нужна и `-unity-font`)
- [ ] Пути к изображениям: `project://database/Assets/...`
- [ ] Класс позиционирования — на `<ui:Instance>`, не на внутреннем корне UXML
- [ ] **Ноль ворнингов в Unity Console** после сохранения

## Молчаливые поломки

Эти конструкции ломают **весь USS-файл** без ошибок в консоли:

```
:last-child, :first-child, :nth-child(), :not(), :has()
::before, ::after
calc(), min(), max(), clamp()
```

Поддерживаются: `:hover`, `:active`, `:focus`, `:disabled`, `:checked`, `:root`.

## Запрещённые свойства

| Запрещено | Замена | Почему |
|---|---|---|
| `gap: 16px` | `margin-right: 16px` / `margin-bottom: 16px` на дочерних | Unknown property warning |
| `box-shadow` | спрайт или `border-color` для имитации | Не существует |
| `linear-gradient()` | 9-slice спрайт | Не существует |
| `display: grid` | `flex-direction` + `flex-wrap` | Не существует |
| `border: 2px solid #fff` | `border-width: 2px;` + `border-color: #fff;` | Шортхенд не поддерживается |
| `z-index` | Порядок элементов в UXML | Не существует |
| `right` / `bottom` | `left` / `top` с абсолютной координатой | Ненадёжен при flex-grow |
| `padding: 8px 16px` | `padding-top: 8px;` `padding-right: 16px;` ... | **Может работать, но ненадёжно** |
| `flex: 1 1 0` | `flex-grow: 1;` `flex-shrink: 1;` `flex-basis: 0;` | **Шортхенд может не парситься** |

## Ловушки с примерами

### ⛔ `gap` → `margin` (КРИТИЧНО)

**Это самая частая ошибка.** `gap` вызывает ворнинг `Unknown property` и полностью игнорируется.

```css
❌ .footer-nav { gap: 12px; }
/* Warning: Unknown property 'gap' (did you mean 'top'?) */

✅ .footer-nav > .nav-button { margin-left: 6px; margin-right: 6px; }
/* Или используй margin только с одной стороны: */
✅ .col > .item { margin-bottom: 16px; }
/* НЕ убирай у последнего через :last-child — этот селектор не работает! */
```

### Равномерное распределение (N кнопок в ряд)

```css
❌ .btn { flex-grow: 1; padding: 24px 48px; }
/* Кнопки РАЗНОГО размера: текст "SHOP" vs "STICKER ALBUM" */

✅ .btn {
    flex-grow: 1;
    flex-shrink: 1;
    flex-basis: 0;
    min-width: 0;       /* Без этого flex-basis: 0 не работает */
    overflow: hidden;   /* Предотвращает раздувание контентом */
    padding-left: 8px;
    padding-right: 8px;
}
```

**Правило:** `flex-basis: 0` + `min-width: 0` — единственный надёжный способ получить одинаковую ширину у flex-детей с разным контентом.

### `right` → `left`

```css
❌ .panel { position: absolute; right: 40px; }
/* right ненадёжен при flex-grow родителе */

✅ .panel { position: absolute; left: 1660px; }
/* Расчёт: 1920 - 220(ширина) - 40(отступ) = 1660 */
```

### Shorthand `border`

```css
❌ .card { border: 3px solid #FE53BB; }

✅ .card {
    border-width: 3px;
    border-color: #FE53BB;
}
```

### Shorthand `padding`

```css
❌ .panel { padding: 8px 16px; }
/* Может работать, но непредсказуемо в разных версиях Unity */

✅ .panel {
    padding-top: 8px;
    padding-right: 16px;
    padding-bottom: 8px;
    padding-left: 16px;
}
```

### Шрифты — обязательная пара

```css
❌ .text { -unity-font-definition: var(--font-jetbrains-mono); }
/* В некоторых версиях Runtime шрифт не отобразится */

✅ .text {
    -unity-font: var(--font-jetbrains-mono);
    -unity-font-definition: var(--font-jetbrains-mono);
}
```

### Шрифты — НЕ копируй из Pencil

```css
❌ /* Pencil показал fontFamily: "Inter" → */ font-family: Inter;
/* Шрифт Inter НЕ установлен в проекте! */

✅ /* Всегда используй переменные из Variables.uss: */
-unity-font-definition: var(--font-russo-one);    /* заголовки */
-unity-font-definition: var(--font-jetbrains-mono); /* данные */
```

### Изображения — путь через `project://database/`

```css
❌ background-image: url('Assets/Icons/ic_coin.png');
✅ background-image: url('project://database/Assets/Icons/ic_coin.png');
```

### TemplateContainer

```xml
❌ Класс на внутреннем элементе — TemplateContainer остаётся flex-элементом
<ui:Instance template="Panel" />
<!-- Panel.uxml: <ui:VisualElement class="my-panel"> -->

✅ Класс на Instance — позиционирование применяется к TemplateContainer
<ui:Instance template="Panel" class="my-panel" />
<!-- Panel.uxml: <ui:VisualElement name="my-panel"> -->
```

### Absolute внутри Flex

```
❌ Абсолютный элемент внизу flex-списка с space-between → улетает к футеру
✅ Плавающие панели — первыми в иерархии MainScreen (Parent_offset = 0,0)
```

## Текст: white-space

```css
/* Если текст разной длины в flex-кнопках — запрети перенос */
.nav-button__title {
    white-space: nowrap;
}
```

## Анимации

```css
.btn {
    transition-property: background-color, scale;
    transition-duration: 0.2s;
}
.btn:hover { scale: 1.05 1.05; }
```

## Ресурсы

- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные проекта (шрифты, цвета, иконки)
- `.docs/project/02-asset-conventions.md` — именование
