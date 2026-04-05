import json
import random

path = 'c:/Users/Daniil/Desktop/Unity Projects/simulation_game/Economy/LootTable.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Collect base values for Rare
base_ips = {}
base_mpc = {}

for item in data:
    if item['Rarity'] == 'Rare':
        cat = item['Category']
        btype = item.get('BoostType')
        bval = item.get('BoostValue')
        if btype == 'Flat_IPS' and bval:
            base_ips.setdefault(cat, []).append(bval)
        elif btype == 'Mult_MPC' and bval:
            base_mpc.setdefault(cat, []).append(bval)

# Average base
avg_base_ips = {}
avg_base_mpc = {}
for cat in set(i['Category'] for i in data):
    if cat in base_ips and base_ips[cat]:
        avg_base_ips[cat] = sum(base_ips[cat]) / len(base_ips[cat])
    else:
        avg_base_ips[cat] = 1 # fallback

    if cat in base_mpc and base_mpc[cat]:
        avg_base_mpc[cat] = sum(base_mpc[cat]) / len(base_mpc[cat])
    else:
        avg_base_mpc[cat] = 0.01 # fallback

multipliers = {
    'Rare': 1,
    'Epic': 5,
    'Legendary': 25,
    'Unique': 150
}

new_data = []

for item in data:
    rarity = item['Rarity']
    
    if rarity in multipliers:
        cat = item['Category']
        mult = multipliers[rarity]
        
        # Разделяем предмет на 2 идентичных клона (один Flat_IPS, другой Mult_MPC),
        # чтобы в пуле базы данных всегда присутствовали оба варианта для каждой редкости.
        jitter1 = random.uniform(0.95, 1.05)
        jitter2 = random.uniform(0.95, 1.05)
        
        # Клон Flat_IPS
        ips_item = dict(item)
        ips_item['BoostType'] = 'Flat_IPS'
        val_ips = avg_base_ips[cat] * mult * jitter1
        ips_item['BoostValue'] = round(val_ips) if val_ips > 10 else round(val_ips, 2)
        
        # Клон Mult_MPC
        mpc_item = dict(item)
        mpc_item['BoostType'] = 'Mult_MPC'
        val_mpc = avg_base_mpc[cat] * mult * jitter2
        mpc_item['BoostValue'] = round(val_mpc, 3)
        mpc_item['ID'] = item['ID'] + 100000 # Чтобы ID не конфликтовали
        
        # Немного меняем SellPrice у одного из них чтобы не было 100% совпадения, но на уровне рандома
        # Хотя лучше оставить одинаковым, чтобы бот не выбрасывал Flat_IPS ради Mult_MPC
        
        new_data.append(ips_item)
        new_data.append(mpc_item)
    else:
        # Common / Uncommon оставляем как есть
        new_data.append(item)

# Пересчет ID для надежности и устранения дубликатов
for idx, item in enumerate(new_data):
    item['ID'] = idx + 1

with open(path, 'w', encoding='utf-8') as f:
    json.dump(new_data, f, ensure_ascii=False, indent=4)

print("Разделение на пары 50/50 завершено. Теперь каждый предмет Rare+ существует в двух вариациях.")
