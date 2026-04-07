import json
import random

cats_path = 'c:/Users/Daniil/Desktop/Unity Projects/simulation_game/Economy/Categories.json'
loot_path = 'c:/Users/Daniil/Desktop/Unity Projects/simulation_game/Economy/LootTable.json'

with open(cats_path, 'r', encoding='utf-8') as f:
    categories = json.load(f)

with open(loot_path, 'r', encoding='utf-8') as f:
    loot_table = json.load(f)

# Подготовим маппинг цен открытия следующей категории
unlock_targets = {}
num_cats = len(categories)

for i in range(num_cats):
    cat_name = categories[i]['Name']
    
    if i < num_cats - 1:
        next_cost = categories[i+1]['UnlockCost']
    else:
        # Для последней 9-й категории экстраполируем (скачок был 83.3x)
        last_jump = categories[i]['UnlockCost'] / categories[i-1]['UnlockCost'] if categories[i-1]['UnlockCost'] > 0 else 100
        next_cost = categories[i]['UnlockCost'] * last_jump

    # Определяем время окупаемости 5-тью Юник-предметами
    target_time_seconds = 64800 if i <= 5 else 907200
    
    unique_ips = next_cost / (5 * target_time_seconds)
    
    # Искусственный пол: Rare не может давать меньше 1.0 IPS, следовательно Unique не меньше 150
    unique_ips = max(unique_ips, 150.0)
    
    unlock_targets[cat_name] = unique_ips

print("Расчет базовых значений Unique IPS:")
for k, v in unlock_targets.items():
    print(f" - {k}: {v:,.2f} IPS")

# Множители деградации по редкости (Unique = 1.0)
rarity_ratios = {
    'Rare': 1 / 150.0,
    'Epic': 1 / 30.0,
    'Legendary': 1 / 6.0,
    'Unique': 1.0
}

# Обновляем таблицу лута
updates_count = 0
for item in loot_table:
    if item.get('BoostType') == 'Flat_IPS':
        cat = item['Category']
        rarity = item['Rarity']
        
        if rarity in rarity_ratios and cat in unlock_targets:
            base_val = unlock_targets[cat] * rarity_ratios[rarity]
            
            # Небольшой случайный разброс
            jitter = random.uniform(0.95, 1.05)
            final_val = base_val * jitter
            
            # Жесткий пол
            final_val = max(1.0, final_val)
            
            # Округление для красивых чисел
            if final_val > 100:
                final_val = float(round(final_val))
            else:
                final_val = round(final_val, 2)
                
            item['BoostValue'] = final_val
            updates_count += 1

with open(loot_path, 'w', encoding='utf-8') as f:
    json.dump(loot_table, f, ensure_ascii=False, indent=4)

print(f"\nОбновлено {updates_count} предметов типа Flat_IPS.")
