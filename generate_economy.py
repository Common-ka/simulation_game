import json
import os

path = 'c:/Users/Daniil/Desktop/Unity Projects/simulation_game/Economy/LootTable.json'

with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Схема инфляционных множителей (цена взлетает, бусты отстают)
cat_multipliers = {
    "Ювелирный лом": {"price": 100, "boost": 5},
    "Крипто-фермы": {"price": 250, "boost": 8},
    "Искусство": {"price": 400, "boost": 12},
    "Гос. конфискат": {"price": 500, "boost": 15}
}

for item in data:
    cat = item['Category']
    if cat in cat_multipliers:
        m = cat_multipliers[cat]
        # Цена коробок/продажи взлетает экспоненциально
        item['SellPrice'] = item['SellPrice'] * m['price']
        
        # Эффективность отстает от инфляции цены
        if 'BoostValue' in item and item['BoostValue'] is not None:
            if item['BoostType'] == 'Flat_IPS':
                item['BoostValue'] = round(item['BoostValue'] * m['boost'])
            elif item['BoostType'] == 'Mult_MPC':
                item['BoostValue'] = round(item['BoostValue'] * m['boost'], 3)

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print("Макро-баланс применен: создана Стена Престижа для поздней игры.")
