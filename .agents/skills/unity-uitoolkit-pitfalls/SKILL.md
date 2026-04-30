---
name: unity-uitoolkit-pitfalls
description: Справочник ловушек и ограничений Unity UI Toolkit USS. Вызывается перед написанием любых USS-стилей, чтобы избежать типичных ошибок, связанных с различиями между USS и стандартным CSS.
---

# USS Pitfalls Reference

## Когда использовать
- Перед написанием любого USS-файла.
- При отладке: стили не применяются, элементы не на своих местах.

## Молчаливые поломки (без ошибок в консоли)

Следующие конструкции **ломают весь USS-файл целиком** — парсер отбрасывает все правила, без предупреждений:

- `:last-child`, `:first-child`, `:nth-child()`, `:not()`, `:has()`
- `::before`, `::after`
- `calc()`, `min()`, `max()`, `clamp()`

Поддерживаемые псевдоклассы: `:hover`, `:active`, `:focus`, `:disabled`, `:checked`, `:root`.

## Запрещённые свойства

| Свойство | Замена |
|---|---|
| `gap` | `margin-right` / `margin-bottom` на дочерних |
| `box-shadow` | `text-shadow` (только текст) или спрайт |
| `linear-gradient()` | 9-slice спрайт |
| `display: grid` | `flex-direction` + `flex-wrap` |
| `border: 2px solid #fff` | `border-width: 2px` + `border-color: #fff` |
| `z-index` | Порядок элементов в UXML |

## Паттерн замены `gap`

```css
/* gap: 16px — не поддерживается */
.row > .item { margin-right: 16px; }
.col > .item { margin-bottom: 16px; }
/* НЕ убирай margin у последнего через :last-child — сломаешь файл */
```

## Shorthand-свойства

- `padding: 8px 16px` — работает.
- `padding: 8px 16px 8px 16px` — нестабильно, расписывай по сторонам.
- `border: ...` — shorthand не работает, всегда расписывай.

## Шрифты — обязательная пара

```css
.text {
    -unity-font: var(--font-name);
    -unity-font-definition: var(--font-name);
}
```
Без пары: в Editor отобразится, в Runtime — нет (или наоборот).

## Изображения

Путь всегда через `project://database/`:
```css
:root { --img-icon: url('project://database/Assets/Resources/Icons/ic_name.png'); }
#my-icon {
    background-image: var(--img-icon);
    -unity-background-scale-mode: scale-to-fit;
}
```

## Позиционирование

- `position: absolute` — позиционирует элемент относительно его непосредственного родителя (в USS нет `position: relative`, оно по умолчанию).
- **Скрытая ловушка Absolute + Flex:** Если ты задаешь `position: absolute` элементу, вложенному в flex-контейнер (например, панель делит экран пополам через `flex-grow: 1`), абсолютные `left`/`top` отсчитываются от **границ этой flex-ячейки**, а не от левого верхнего угла экрана!
  - *Ошибка:* Скопировать из Pencil `left: 1033px` (в 1920 экране) для элемента, чья родительская панель уже сдвинута на 960px вправо. В итоге элемент улетит на `960 + 1033 = 1993px` (за экран).
  - *Решение:* Вычитай ширину/высоту всех предшествующих flex-элементов (сдвиг контейнера) из абсолютных координат макета.
- Если родитель имеет `align-items: center` + `justify-content: center`, то `margin-top` отсчитывается от центра, а не от верха. Используй `position: absolute` для pixel-perfect размещения, предварительно пересчитав `left`/`top`.

## Hover и анимации

```css
.btn {
    transition-property: background-color, scale;
    transition-duration: 0.2s;
}
.btn:hover { scale: 1.05 1.05; }
```

## Чеклист перед сохранением USS

- [ ] Нет `:last-child` / `:first-child` / `:nth-child`
- [ ] Нет `calc()` / `min()` / `max()`
- [ ] Нет `gap`, `box-shadow`, `linear-gradient`, shorthand `border`
- [ ] Шрифты: оба `-unity-font` и `-unity-font-definition`
- [ ] Пути к изображениям: `project://database/Assets/...`
- [ ] Нет конфликта центрирования родителя + margin на ребёнке
