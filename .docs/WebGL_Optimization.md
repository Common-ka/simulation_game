# Памятка по оптимизации Unity WebGL
## Unclaimed Assets · Справочник разработчика

> Цель: сборка **< 15 МБ**, стабильные **60 FPS** в браузере, время загрузки **< 5 сек** на среднем соединении.
> Используй этот документ как чеклист перед каждым нятым задачи, где пишешь код или добавляешь ассеты.

---

## 🔴 Критические правила (нарушение = проблема на проде)

### 1. Никаких `find` и `GetComponent` в `Update()`
```csharp
// ❌ ЗАПРЕЩЕНО — вызывается 60 раз в секунду, убивает производительность
void Update() {
    GetComponent<Text>().text = softCurrency.ToString();
    GameObject.Find("HUD").GetComponent<HUDPanel>().Refresh();
}

// ✅ ПРАВИЛЬНО — кэшируем в Awake(), обновляем по событию
private Text _label;
void Awake() { _label = GetComponent<Text>(); }
void OnCurrencyChanged(double val) { _label.text = NumberFormatter.Format(val); }
```

### 2. Текстовые операции — только через StringBuilder или кэш
Конкатенация строк в цикле создаёт GC-мусор → фризы в WebGL.
```csharp
// ❌
label.text = "IPS: " + currentIPS.ToString("F2") + "/сек";

// ✅ NumberFormatter возвращает готовую строку из пула
label.text = NumberFormatter.FormatIPS(currentIPS);
```

### 3. Нет синхронных операций с файлами или сетью
В WebGL нет файловой системы. `File.ReadAllText()` **не работает**. Только через `UnityWebRequest` или `Resources.Load()`.
```csharp
// ❌ Не скомпилируется или упадёт в рантайме
var json = File.ReadAllText("path/to/file.json");

// ✅ Уже реализовано в GameDataLoader через Resources.Load() / UnityWebRequest
```

### 4. Корутины вместо `async/await` для IO
`async/await` в Unity WebGL ненадёжен (особенно с Yandex SDK). Все асинхронные операции — через `IEnumerator` + `StartCoroutine`.
```csharp
// ✅
IEnumerator LoadSprite(string url) {
    using var req = UnityWebRequestTexture.GetTexture(url);
    yield return req.SendWebRequest();
    // обработка результата
}
```

### 5. Никаких `Thread` и `Task.Run()`
WebGL — однопоточный. Любые потоки упадут в рантайме. Тяжёлые вычисления — делить на части через корутины с `yield return null` между итерациями.

---

## 🟠 Ассеты и сборка (влияет на размер < 15 МБ)

### Текстуры
| Параметр | Значение | Почему |
|---|---|---|
| Формат | `WebP` или `PNG` для спрайтов | Лучшее сжатие для web |
| Max Size | 512×512 для иконок предметов | Достаточно для 2D UI |
| Max Size | 1024×1024 для фоновых изображений | Не больше |
| Mip Maps | **Выключено** для UI-спрайтов | UI не масштабируется — mips лишний размер |
| Compression | `Crunch Compression` | Уменьшает размер ещё на 30–50% |

### Спрайтшиты (Atlas)
Все иконки предметов одной категории — **один атлас**. Инструмент: `Sprite Atlas` (встроен в Unity).
```
❌ 50 отдельных PNG → 50 draw calls
✅ 1 Sprite Atlas на категорию → 1 draw call
```
Атласы иконок на Яндекс Облаке — один файл на категорию (уже решено).

### Аудио
| Тип | Формат | Compression |
|---|---|---|
| Музыка (фон) | `.ogg` | `Streaming` (не грузится целиком в RAM) |
| SFX (короткие) | `.wav` → `.ogg` | `Compressed In Memory` |
| Max Size трека | < 1 МБ | Иначе раздует сборку |

### Шрифты
- Используй **один** шрифт.
- Подключай через `TextMeshPro` — он кэширует глифы и работает быстрее стандартного `Text`.
- **Не включай в атлас шрифта** лишние символы.

---

## 🟡 Build Settings (настраивается один раз)

### Player Settings → WebGL
```
Compression Format:    Brotli         ← лучшее сжатие (Яндекс поддерживает)
Exception support:     None           ← убирает ~2 МБ из сборки (только для релиза)
Strip Engine Code:     ✅ включено
Managed Stripping:     Medium          ← High может сломать рефлексию JSON
IL2CPP:                ✅ (не Mono)   ← быстрее в браузере
```

### Что исключить из сборки
В `Project Settings → Graphics`:
- Убрать все неиспользуемые шейдеры из `Always Included Shaders`
- Оставить только: `Sprites-Default`, `UI/Default`, `Particles/Standard`

---

## Производительность UI (UI Toolkit, Unity 6)

### Преимущество UI Toolkit перед Canvas для этого проекта

UI Toolkit использует **retained mode rendering** — перерисовываются только «грязные» (dirty) элементы, а не весь экран. Проблемы Canvas Rebuild здесь нет по умолчанию. Это делает его более предпочтительным для idle-игры, где числа меняются каждую секунду, а фоны — редко.

### Структура UI в проекте (UIDocument)

```
Game (Scene)
└── UIDocument  [PanelSettings, UXML Root]
    ├── hud-container          ← валюта, IPS, прогресс (обновляется 1/сек)
    ├── shop-panel             ← выбор категории и покупка лота
    ├── shelf-panel            ← витрина 5 слотов (статичная, меняется редко)
    ├── roulette-panel         ← анимация выпадения (USS Transitions)
    ├── sticker-album-panel    ← альбом стикеров
    ├── black-market-panel     ← артефакты (hidden по умолчанию)
    ├── prestige-panel         ← расчёт PP (hidden по умолчанию)
    └── offline-reward-panel   ← оффлайн-доход (hidden по умолчанию)
```

### Правила оптимизации UI Toolkit

**1. Скрывай через класс, не через style напрямую**
```csharp
// ❌ — прямое изменение style создаёт лишний layout pass
panel.style.display = DisplayStyle.None;

// ✅ — тогgling CSS-класса: дешевле и читаемее
panel.AddToClassList("hidden");     // в USS: .hidden { display: none; }
panel.RemoveFromClassList("hidden");
```

**2. Не изменяй стили в Update() — только по событию**
```csharp
// ❌ — каждый кадр запускает layout recalculation
void Update() { label.text = currentIPS.ToString(); }

// ✅ — обновляем 1 раз в секунду, из GameTick
void OnGameStateChanged(GameSnapshot snap) {
    _currencyLabel.text = NumberFormatter.Format(snap.SoftCurrency);
    _ipsLabel.text      = NumberFormatter.FormatIPS(snap.CurrentIPS);
}
```

**3. `PickingMode.Ignore` для неинтерактивных элементов**
```csharp
// Фоновые изображения, декорации, разделители
backgroundElement.pickingMode = PickingMode.Ignore;
// Аналог отключения GraphicRaycaster в Canvas
```

**4. Батчи изменений стилей через классы**
```csharp
// ❌ — три отдельных layout pass
element.style.color         = Color.red;
element.style.fontSize      = 24;
element.style.borderWidth   = 2;

// ✅ — один layout pass
element.AddToClassList("error-state"); // всё описано в .uss файле
```

**5. Анимации через USS Transitions (GPU-ускоренные)**
```css
/* В файле .uss */
.roulette-item {
    transition: transform 0.3s ease-out, opacity 0.2s linear;
}
.roulette-item.spinning {
    transform: translateY(-200px);
    opacity: 0;
}
```
Запуск из C#: `element.AddToClassList("spinning")` — анимация стартует автоматически.

**6. Рулетка — Custom Painter для нестандартной графики**
```csharp
// Для кастомной отрисовки (градиенты, кривые, сложные эффекты)
public class RouletteWheel : VisualElement {
    static RouletteWheel() {
        // Регистрируем кастомный рендерер
        CustomStyleRegistry.Register<RouletteWheel>();
    }
    void GenerateVisualContent(MeshGenerationContext ctx) {
        // Рисуем через Painter2D API — аналог Canvas2D в вебе
        var painter = ctx.painter2D;
        painter.Arc(center, radius, startAngle, endAngle);
        painter.Fill();
    }
}
```

**7. Планировщик вместо InvokeRepeating для UI-таймеров**
```csharp
// Таймер обратного отсчёта кулдауна ключа ЧР
_root.schedule.Execute(() => {
    UpdateKeyTimer();
}).Every(1000); // каждую секунду, без MonoBehaviour
```

---

## Оффлайн-прогресс и тяжёлые вычисления

При возврате игрока после 8 часов оффлайна нужно "прогнать" пропущенное время. Делай это **не за один кадр**:

```csharp
IEnumerator ApplyOfflineEarnings(double seconds, double ips) {
    // Не имитируем каждую секунду — просто считаем итог
    double earned = Mathf.Min(seconds, offlineCap) * ips;
    softCurrency += earned;
    
    // Тяжёлый расчёт? Разбиваем на части
    yield return null; // отдаём управление браузеру на 1 кадр
    
    // Обновляем UI
    offlinePanel.Show(earned, seconds);
}
```

---

## Работа с числами (Idle-специфика)

Числа в idle-играх быстро выходят за пределы `int` и `float`:

```csharp
// ❌ float теряет точность уже на 16 миллионах
float currency = 16_777_217f; // = 16 777 216 (ошибка!)

// ✅ double: точность до ~9 * 10^15 — достаточно для всей игры
double currency = 16_777_217.0;
```

**NumberFormatter** — обязателен для всех меток:
```
1 234          → "1 234"
1 234 567      → "1.23M"
1 234 567 890  → "1.23B"
1.23e15        → "1.23Qd"
```

---

## Загрузка (Bootstrap — цель < 5 сек)

| Этап | Время | Оптимизация |
|---|---|---|
| Скачивание сборки | < 3 сек | Brotli + небольшой размер |
| Инициализация Unity | ~0.5 сек | Нельзя ускорить |
| GameDataLoader (local) | < 0.1 сек | JSON в Resources/ |
| GameDataLoader (remote) | < 1 сек | GitHub Pages + HTTP кэш |
| SaveManager (Yandex) | < 0.5 сек | Плагин обрабатывает |
| Preload спрайтшит кат.1 | < 0.5 сек | Yandex Cloud CDN |

**Показывай прогресс-бар** на Bootstrap — даже фейковый (0% → 100% за 3 сек). Игроки с прогрессом ждут дольше.

---

## Работа с памятью

```csharp
// Выгружай неиспользуемые спрайтшиты при смене эпохи или редком событии
Resources.UnloadUnusedAssets();
// Вызывать максимум раз в несколько минут, не в Update

// Не создавай new List/Dictionary внутри методов, которые вызываются часто
// Кэшируй их как поля класса и очищай через .Clear()
```

---

## Чеклист перед каждым PR / тест-запуском

- [ ] Нет `GetComponent` / `Find` в `Update()` или `GameTick()`
- [ ] Все текстуры UI не превышают 512×512
- [ ] Новый Canvas разделён по типу (Static / Dynamic)?
- [ ] Новый звук < 1 МБ и в формате `.ogg`?
- [ ] Все числа используют `double`, не `float`?
- [ ] Новые корутины используют `yield return null` при тяжёлых вычислениях?
- [ ] После добавления ассетов: проверь размер сборки в Build Report
