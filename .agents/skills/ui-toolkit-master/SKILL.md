---
name: Unity UI Toolkit Master
description: Используй этот скилл для создания, обновления и рефакторинга любых UI экранов, компонентов и виджетов с помощью UXML, USS и UI Toolkit.
---

# Unity UI Toolkit Master Guideline

Этот скилл определяет строгий пайплайн создания UI для нашего проекта. 
**Связь с базой:** Избегай тяжелой логики в `Update()`. Жестко разделяй бизнес-логику (Simulation Core) и отображение (UI). 

## 🏗 1. UXML (Структура и Верстка)
- Основа всего — `VisualElement`.
- **Нейминг:** Используй BEM(Block Element Modifier)-подобную нотацию для классов (например, `main-menu`, `main-menu__button`, `main-menu__button--active`). Идентификаторы (имена) пиши в формате `camelCase` или `kebab-case`.
- Для динамических или длинных списков используй `ListView` или `ScrollView`, не спамь сотнями элементов вручную.

## 🎨 2. USS (Стилизация)
- **Flexbox:** Вся сетка строится строго на Flexbox (`flex-direction`, `align-items`, `justify-content`).
- **Позиционирование:** Избегай `position: absolute`, кроме крайних случаев (например, попапы верхнего уровня или индикаторы "парения").
- **Адаптивность:** Корневые контейнеры должны иметь `width: 100%; height: 100%;`, чтобы тянуться за размером экрана.
- **Никаких инлайн-стилей:** Не задавай стили (`style.backgroundColor` и т.д.) через C# код, кроме случаев динамической анимации или прогресс-баров. Вся статика должна лежать в `.uss` файле.

## ⚙️ 3. C# (Контроллеры UI)
- В C# классе элементы получаются исключительно в `OnEnable()` через метод `Q<T>("element-name")`.
- Строго выноси строковые ключи элементов в константы.
- **ОБЯЗАТЕЛЬНО** отписывайся от событий в `OnDisable()`, чтобы предотвратить утечки памяти.

```csharp
// Правильный шаблон базового UI контроллера
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ShopPanelUI : MonoBehaviour
{
    private const string BuyButtonName = "buy-button";
    
    private Button _buyButton;
    private UIDocument _doc;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        
        // 1. Поиск элементов
        _buyButton = root.Q<Button>(BuyButtonName);
        
        // 2. Подписка
        if (_buyButton != null)
            _buyButton.clicked += OnBuyClicked;
    }

    private void OnDisable()
    {
        // 3. Жесткая отписка!
        if (_buyButton != null)
            _buyButton.clicked -= OnBuyClicked;
    }

    private void OnBuyClicked()
    {
        // Делегирование в бизнес-логику (эвенты или синглтоны GameManager)
        Debug.Log("Buy action triggered");
    }
}
```

## 🚫 4. Антипаттерны (ЧЕГО ДЕЛАТЬ НЕЛЬЗЯ - TRIGGERS FOR REVIEWER)
- `[ ]` Использование `Update()` для проверки состояний кнопок или обновлений текстов. Используй паттерн "Observer" (события от ядра).
- `[ ]` Хардкод строк `root.Q("myBtn")` прямо в методе (выноси в константы).
- `[ ]` Бизнес-логика внутри UI. Контроллер UI **не должен** вычитать экономику (`playerMoney -= 100`) — он должен только отправить эвент `ShopEvents.BuyItem.Invoke(itemId)`, а ядро экономики само разберется.

## 🛠 Как применять
Когда `Worker` делает задачу по UI, он берет этот код как стартовый шаблон. Когда проверяет `Reviewer`, он бракует код, если видит забытую отписку в `OnDisable()` или нарушение MVC-разбаловки.
