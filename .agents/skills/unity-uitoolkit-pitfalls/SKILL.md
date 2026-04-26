---
name: unity-uitoolkit-pitfalls
description: Справочник ловушек и ограничений Unity UI Toolkit USS. Вызывается перед написанием любых USS-стилей, чтобы избежать типичных ошибок, связанных с различиями между USS и стандартным CSS.
---

# Unity UI Toolkit — Ловушки USS

## Когда использовать этот скилл
- Перед написанием **любого** USS-файла для Unity UI Toolkit.
- Когда в консоли Unity появляются `warning: Unknown property`.
- Когда визуальный результат не совпадает с ожидаемым (элементы слиплись, нет отступов, шрифт не загрузился).

## Свойства USS, которые НЕ работают

### ❌ `gap` → Используй `margin`
Unity **не поддерживает** `gap`. Консоль покажет: *"Unknown property 'gap' (did you mean 'top'?)"*

```css
/* ❌ СЛОМАЕТСЯ */
.row { gap: 16px; }

/* ✅ ПРАВИЛЬНО — margin на дочерних */
.row > .item { margin-right: 16px; }

/* Для вертикальных списков: */
.column > .item { margin-bottom: 16px; }
```

### ❌ `box-shadow` → Нет замены для контейнеров
`box-shadow` не поддерживается для VisualElement. `text-shadow` работает **только для текста**.

```css
/* ❌ СЛОМАЕТСЯ */
.card { box-shadow: 0 4px 8px rgba(0,0,0,0.3); }

/* ✅ Для текстового свечения: */
.glow-text { text-shadow: 0 0 10px rgba(8, 247, 254, 0.5); }
```

### ❌ `background: linear-gradient()` → Спрайт
USS **не поддерживает** CSS-градиенты. Используй 9-slice спрайт или `background-image` с заготовленной текстурой.

### ❌ `display: grid` → Только Flexbox
USS поддерживает только `flex-direction: row | column | row-reverse | column-reverse` и `flex-wrap: wrap`.

### ❌ Shorthand `border`
```css
/* ❌ НЕ работает */
.card { border: 2px solid white; }

/* ✅ Расписывай отдельно */
.card {
    border-width: 2px;
    border-color: white;
}

/* Для раздельных сторон: */
.card {
    border-left-width: 2px;
    border-right-width: 2px;
    border-top-width: 2px;
    border-bottom-width: 2px;
    border-left-color: white;
    border-right-color: white;
    border-top-color: white;
    border-bottom-color: white;
}
```

### ⚠️ Shorthand `padding` и `margin`
Двухзначный shorthand (`padding: 8px 16px`) **работает**.
Но четырёхзначный (`padding: 8px 16px 8px 16px`) может быть нестабильным — лучше расписывать.

```css
/* ✅ Надёжный вариант */
.block {
    padding-top: 8px;
    padding-right: 16px;
    padding-bottom: 8px;
    padding-left: 16px;
}
```

## Шрифты — Обязательная пара свойств

Для корректной работы шрифтов **необходимо указывать оба свойства**:

```css
.text {
    -unity-font: var(--font-orbitron);            /* Runtime */
    -unity-font-definition: var(--font-orbitron); /* Editor (UI Builder) */
    -unity-font-style: bold;
}
```

Если указать только `-unity-font`, шрифт может не отобразиться в UI Builder.
Если указать только `-unity-font-definition`, шрифт может не сработать в Runtime.

## Иконки и изображения

Изображения назначаются через `background-image`, **не** через отдельный Image-элемент.

```css
/* В Variables.uss: */
:root {
    --img-icon: url('project://database/Assets/Resources/Icons/ic_name.png');
}

/* В компонентном USS: */
#my-icon {
    background-image: var(--img-icon);
    -unity-background-scale-mode: scale-to-fit;
    width: 64px;
    height: 64px;
}
```

Путь **обязательно** начинается с `project://database/`.

## Масштабирование и hover

```css
/* Плавное масштабирование при наведении */
.btn {
    transition-property: background-color, scale;
    transition-duration: 0.2s;
}
.btn:hover {
    scale: 1.05 1.05;  /* Два значения: X Y */
}
```

## Рендеринг UI для проверки

Чтобы сделать скриншот для визуальной проверки, вызови `render_ui` **дважды**:
```
# Первый вызов: инициализирует RenderTexture
manage_ui(action: "render_ui", target: "UI_Root", include_image: true)
# Второй вызов: фактически захватывает кадр
manage_ui(action: "render_ui", target: "UI_Root", include_image: true)
```

## Проверка консоли на USS-ошибки

После сохранения USS-файла **всегда** проверяй консоль:
```
read_console(types: ["warning", "error"], filter_text: "USS")
read_console(types: ["warning", "error"], filter_text: "Unknown property")
```

Любой `Unknown property` = свойство игнорируется Unity, стиль не применится.

## Быстрый чек-лист перед коммитом USS

- [ ] Нет `gap` → используется `margin` на дочерних
- [ ] Нет `box-shadow` на контейнерах
- [ ] Нет `linear-gradient()`
- [ ] Шрифты: указаны и `-unity-font`, и `-unity-font-definition`
- [ ] Пути к изображениям: `project://database/Assets/...`
- [ ] `border` расписан отдельно (`border-width` + `border-color`)
- [ ] Консоль чистая от `Unknown property` warnings
