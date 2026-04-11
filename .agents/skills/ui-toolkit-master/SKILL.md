---
name: building-unity-ui
description: Строит и рефакторит UI-экраны и компоненты в Unity. Вызывается, когда нужно создать, обновить или сверстать интерфейс с помощью UXML, USS и UI Toolkit.
---

# Building Unity UI Toolkit Interfaces

## Когда использовать этот скилл
- Пользователь просит сверстать интерфейс, окно, попап или экран.
- Нужно обновить стили USS или структуру UXML.
- Требуется привязать C# контроллер к UI элементам в Unity.

## Чеклист (Workflow)
- [ ] 1. Изучить структуру окна (какие кнопки, списки нужны).
- [ ] 2. Сверстать `.uxml`, используя BEM нотацию.
- [ ] 3. Настроить в `.uss` адаптивную верстку (только Flexbox).
- [ ] 4. Создать C# класс контроллера, реализовав `Q<T>`-биндинги в `OnEnable` и отписки в `OnDisable`.
- [ ] 5. Убедиться, что скрипт НЕ содержит логики экономики или геймплея.

## Инструкции

### 1. UXML (Структура)
- Основа — `VisualElement`.
- Для списков используй `ListView` / `ScrollView`.
- **НЕ** спамь инлайн-стилями. Идентификаторы пиши в `camelCase` или `kebab-case`.

### 2. USS (Стилизация)
- Вся сетка строится на `flex-direction`, `align-items`, `justify-content`.
- Избегай `position: absolute`, кроме полноэкранных оверлеев.
- Используй `%` для тянущихся элементов.

### 3. C# Controller (Шаблон)
```csharp
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GenericPanelUI : MonoBehaviour
{
    private const string BuyButtonName = "buy-button";
    
    private Button _buyButton;
    private UIDocument _doc;

    private void Awake() => _doc = GetComponent<UIDocument>();

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _buyButton = root.Q<Button>(BuyButtonName);
        
        if (_buyButton != null) _buyButton.clicked += OnBuyClicked;
    }

    private void OnDisable()
    {
        // КРИТИЧЕСКИ ВАЖНО: Отписка для предотвращения Memory Leaks
        if (_buyButton != null) _buyButton.clicked -= OnBuyClicked;
    }

    private void OnBuyClicked()
    {
        // Только отправка ивентов - никакой логики расчетов!
    }
}
```

### Антипаттерны (Чего делать нельзя)
- Использование `Update()` для проверки активна ли кнопка.
- Прямая завязка в UI: `player.Money -= 10`. UI только отправляет события!
- Хардкод строковых ключей внутри метода вместо констант.

