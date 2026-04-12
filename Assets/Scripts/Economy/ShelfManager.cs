using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnclaimedAssets.Economy
{
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Unique,
        Black_Market_Exclusive
    }

    [Serializable]
    public class ShelfItemData
    {
        public int ItemID;
        public ItemRarity Rarity;
        
        public string Category;
        public string EffectType; // e.g., "Flat_IPS", "Mult_MPC"
        public double EffectValue;
    }

    /// <summary>
    /// Ядро экономики: Управляет предметами на полке (строго 5 слотов).
    /// </summary>
    public class ShelfManager : MonoBehaviour
    {
        public static ShelfManager Instance { get; private set; }

        public const int MAX_SLOTS = 5;

        private List<ShelfItemData> _items = new List<ShelfItemData>();

        /// <summary>
        /// Вызывается при любом изменении состояния полок (добавление, удаление, очистка).
        /// </summary>
        public event Action OnShelfUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IReadOnlyList<ShelfItemData> GetItems() => _items.AsReadOnly();

        /// <summary>
        /// Логика добавления предмета Rare+: 
        /// Если полка не заполнена — ставим сразу.
        /// Вся остальная сложная логика вытеснения отменена.
        /// </summary>
        public bool TryAddItem(ShelfItemData item)
        {
            if (item == null) 
                return false;

            // Согласно макро-балансу, Common и Uncommon на полку не идут.
            if (item.Rarity == ItemRarity.Common || item.Rarity == ItemRarity.Uncommon)
            {
                return false;
            }

            // Класс поддерживает строго 5 слотов
            if (_items.Count < MAX_SLOTS)
            {
                _items.Add(item);
                OnShelfUpdated?.Invoke();
                return true;
            }

            // Места нет, автоматическое вытеснение отсутствует.
            return false;
        }

        /// <summary>
        /// Ручное удаление предмета с полки
        /// </summary>
        public bool RemoveItem(ShelfItemData item)
        {
            if (_items.Remove(item))
            {
                OnShelfUpdated?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Очистка полки (например, при престиже)
        /// </summary>
        public void ClearShelf()
        {
            if (_items.Count > 0)
            {
                _items.Clear();
                OnShelfUpdated?.Invoke();
            }
        }
        
        /// <summary>
        /// Загрузка состояния полки из сохранений.
        /// Вызывается извне, когда данные из SaveManager загружены и превращены из ItemIDs в ShelfItemData.
        /// </summary>
        public void LoadSavedItems(List<ShelfItemData> savedItems)
        {
            _items.Clear();
            if (savedItems != null)
            {
                foreach(var item in savedItems)
                {
                    if (_items.Count < MAX_SLOTS)
                        _items.Add(item);
                }
            }
            OnShelfUpdated?.Invoke();
        }
    }
}
