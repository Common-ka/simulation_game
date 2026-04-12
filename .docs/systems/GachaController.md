# GachaController

## Контекст
Отвечает за механику анпакинга посылок с маркетплейсов. Списывает мягкую валюту (SoftCurrency), генерирует лут через математику DropRates и маршрутизирует его:
- Rare+ отправляет в `ShelfManager`.
- Common/Uncommon оставляет в инвентаре. 

Контроллер не взаимодействует с UI, работая на бэкенде.

## Ключевые классы
- `GachaController`

## API

### Свойства
- `public static GachaController Instance { get; private set; }`

### События
- `public static event Action<int> OnLootBoxOpened` — срабатывает после того, как коробка (по `categoryIndex`) была успешно куплена, валюта списана и предмет обработан. Это нужно для мета-систем (добавление стикеров, счетчики).

### Методы
- `public void BuyLoot(int categoryIndex, double cost)` — пытается купить лот. Если у `GameManager` хватает валюты, генерируется случайный предмет на базе таблицы вероятностей (Rare+, Common, Uncommon).

## Основные алгоритмы
- Вызывается метод `TrySpendCurrency` у `GameManager` (который уменьшает SoftCurrency и триггерит обновление `GameSnapshot`).
- Вычисляется редкость через рулетку с вероятностями, заданными через `_dropRates` в инспекторе.
- Если редкость `Common` или `Uncommon`, предмет "закидывается в инвентарь" (пока логируется).
- Если `Rare+`, собирается `ShelfItemData` (для поля EffectType применяется рандом 50/50 между `Flat_IPS` и `Mult_MPC`) и вызывается `ShelfManager.Instance.TryAddItem`. Если слотов нет — предмет игнорируется (остается в инвентаре/логается).
