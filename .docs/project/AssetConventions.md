# Правила именования (Asset Conventions)
## Unclaimed Assets · Справочник разработчика

Этот документ устанавливает строгие правила именования файлов, классов и ассетов. Так как проект пишется совместно с ИИ-агентом, соблюдение этих правил — единственный способ избежать хаоса, дублирования и потери связей между скриптами и UI через месяц разработки.

---

## 1. Общие правила (Золотой стандарт Unity)

- **Директории (Папки):** `PascalCase` (например, `Scripts/`, `UI_Toolkit/`, `GameData/`).
- **Скрипты (C#):** `PascalCase` (например, `GameManager.cs`, `SavesExtension.cs`).
- **Префабы (Prefab):** `PascalCase` (например, `CoinSpawnNode.prefab`).
- **Интерфейсы (C#):** Начинаются с `I` + `PascalCase` (например, `ILootGenerator.cs`).
- **Внутренние переменные (C#):** `_camelCase` для приватных, `PascalCase` для публичных свойств, `camelCase` для параметров методов.

---

## 2. Именование UI Toolkit (Самое важное для ИИ)

В UI Toolkit используется веб-подход (HTML/CSS), поэтому правила отличаются от C#. ИИ-агент должен строго следовать им при генерации разметки.

### Файлы разметки и стилей
- **UXML-файлы:** `PascalCase` + суффикс `Panel` или `Component` 
  *(Пример: `HUDPanel.uxml`, `ItemCard.uxml`)*
- **USS-файлы:** `PascalCase` 
  *(Пример: `Common.uss`, `BlackMarket.uss`, `Variables.uss`)*

### Внутри UXML (Атрибуты `name` и `class`)
Всегда используем **`kebab-case`** (строчные буквы через дефис). Это веб-стандарт, он хорошо читается и исключает ошибки регистра при поиске через `root.Q<VisualElement>("my-element")`.

- **`name` (ID элемента, уникальный):**
  - Панели: `name="shop-panel"`, `name="hud-panel"`
  - Кнопки: `name="btn-buy-upgrade"`, `name="btn-watch-ad"`
  - Тексты/Лейблы: `name="lbl-soft-currency"`, `name="lbl-ips-value"`
  - Контейнеры: `name="inventory-grid"`, `name="roulette-container"`

- **`class` (CSS классы, переиспользуемые):**
  - Состояния: `class="hidden"`, `class="active"`, `class="disabled"`
  - Типы: `class="panel-base"`, `class="text-header"`, `class="btn-primary"`

> **Пример идеального UXML для ИИ:**
> `<ui:Button name="btn-buy-upgrade" class="btn-primary disabled" text="Купить" />`

---

## 3. Спрайты и 2D-Ассеты

Для спрайтов используем **`snake_case`** (строчные буквы через нижнее подчеркивание) с обязательным префиксом типа. Это позволяет быстро фильтровать ассеты в строке поиска Unity.

### Префиксы:
- `ic_` (Icon) — иконки UI (кнопки, валюты). *Пример: `ic_gold_key.png`, `ic_close.png`*
- `bg_` (Background) — фоны экранов или панелей. *Пример: `bg_black_market.png`*
- `item_` (Item) — иконки лута с рулетки. *Пример: `item_broken_phone.png`*
- `fx_` (Effect) — частицы, свечения. *Пример: `fx_glow_legendary.png`*

### Атласы (SpriteSheets для Яндекс Облака)
- Формат: `cat_<id>_sheet.png` *(Пример: `cat_0_sheet.png`, `cat_1_sheet.png`)*.

---

## 4. Звуки и Музыка (AudioClip)

Также используем **`snake_case`** с префиксами.

- `bgm_` (Background Music) — фоновая музыка. *Пример: `bgm_main_theme.mp3`, `bgm_black_market.mp3`*
- `sfx_` (Sound Effect) — звуки UI и геймплея. 
  - *Пример UI: `sfx_btn_click.wav`, `sfx_panel_open.wav`*
  - *Пример Геймплей: `sfx_cash_register.wav`, `sfx_roulette_tick.wav`, `sfx_item_drop_legendary.wav`*

---

## 5. Файлы Данных (JSON)

Названия JSON-файлов пишутся в `PascalCase`, так как они обычно маппятся на C#-классы 1 к 1. Списки (массивы объектов) именуются во множественном числе.

✅ Правильно: 
- `LootTable.json`
- `Upgrades.json`
- `Categories.json`

❌ Неправильно: 
- `loot_table.json` (нарушает стиль C# сериализации)
- `Category.json` (файл содержит массив, лучше `Categories`)

---

## 6. Идентификаторы (ID) в JSON данных

Внутри JSON-файлов для строковых ID (например, ID артефактов или апгрейдев) используем **`snake_case`** (веб-стандарт для JSON значений).

```json
// Upgrades.json пример:
{
  "id": "capacity_boost",
  "base_cost": 1500
}
```

Для покупок IAP в Yandex Console также используем **`snake_case`**:
- `starter_pack`
- `keys_10`
- `vip_gold`

---

## 7. Чек-лист проверки ИИ-агента

Если поручаешь агенту составить новый компонент, скидывай ему этот документ в контекст и проверяй:
1. `name` кнопок начинаются с `btn-`?
2. Лейблы начинаются с `lbl-`?
3. Анимации UI делаются через добавление/снятие USS `class`, а не через корутины?
4. Скрытые панели переключаются через добавление `class="hidden"`, а не через `SetEnabled(false)`?
