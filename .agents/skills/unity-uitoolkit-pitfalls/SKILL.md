---
name: unity-uitoolkit-pitfalls
description: Справочник ловушек Unity UI Toolkit USS, специфичных для этого проекта. Вызывается перед написанием или отладкой любых USS-стилей.
---

# USS Pitfalls

## Чеклист перед сохранением USS

- [ ] Нет `:last-child` / `:first-child` / `:nth-child` / `:not()` / `::before`
- [ ] Нет `calc()` / `min()` / `max()`
- [ ] Нет `gap`, `box-shadow`, `linear-gradient`, shorthand `border`
- [ ] Нет `right` / `bottom` для абсолютного позиционирования
- [ ] Шрифты: оба `-unity-font` и `-unity-font-definition`
- [ ] Пути к изображениям: `project://database/Assets/...`
- [ ] Класс позиционирования — на `<ui:Instance>`, не на внутреннем корне UXML

## Молчаливые поломки

Эти конструкции ломают **весь USS-файл** без ошибок в консоли:

```
:last-child, :first-child, :nth-child(), :not(), :has()
::before, ::after
calc(), min(), max(), clamp()
```

Поддерживаются: `:hover`, `:active`, `:focus`, `:disabled`, `:checked`, `:root`.

## Запрещённые свойства

| Запрещено | Замена |
|---|---|
| `gap` | `margin-right` / `margin-bottom` на дочерних |
| `box-shadow` | спрайт или `border-color` для имитации |
| `linear-gradient()` | 9-slice спрайт |
| `display: grid` | `flex-direction` + `flex-wrap` |
| `border: 2px solid #fff` | `border-width: 2px;` + `border-color: #fff;` |
| `z-index` | Порядок элементов в UXML |
| `right` / `bottom` | `left` / `top` с абсолютной координатой |
| `padding: 8px 16px 8px 16px` | Расписывай по сторонам |

## Ловушки с примерами

### `gap` → `margin`

```css
❌ .col { gap: 16px; }                        /* Unknown property */
✅ .col > .item { margin-bottom: 16px; }       /* НЕ убирай у последнего через :last-child! */
```

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

### Шрифты — обязательная пара

```css
❌ .text { -unity-font-definition: var(--font-jetbrains-mono); }
/* В Runtime шрифт не отобразится */

✅ .text {
    -unity-font: var(--font-jetbrains-mono);
    -unity-font-definition: var(--font-jetbrains-mono);
}
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

## Анимации

```css
.btn {
    transition-property: background-color, scale;
    transition-duration: 0.2s;
}
.btn:hover { scale: 1.05 1.05; }
```

## Ресурсы

- `Assets/Scripts/UI_Toolkit/USS/Variables.uss` — переменные проекта
- `.docs/project/02-asset-conventions.md` — именование
