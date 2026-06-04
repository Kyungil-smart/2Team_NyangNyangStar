using Services.Enums;
using System;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    [Serializable]
    public class ItemData
    {
        [SerializeField] private bool _hasItem;
        [SerializeField] private int _itemID;
        [SerializeField] private Sprite _itemSprite;
        [SerializeField] private string _itemName;
        [SerializeField] private int _itemLevel;
        [SerializeField] private ItemType _itemType;
        [SerializeField] private string _addressableKey;

        public bool HasItem => _hasItem || (_itemID > 0 && _itemType != ItemType.None);
        public int ItemID => _itemID;
        public Sprite ItemSprite => _itemSprite;
        public string ItemName => _itemName;
        public int ItemLevel => _itemLevel;
        public ItemType ItemType => _itemType;
        public string AddressableKey => _addressableKey;

        public static ItemData Empty => new ItemData();

        private ItemData()
        {
            _hasItem = false;
            _itemID = 0;
            _itemSprite = null;
            _itemName = string.Empty;
            _itemLevel = 0;
            _itemType = ItemType.None;
            _addressableKey = string.Empty;
        }

        public ItemData(
            int itemID,
            string itemName,
            int itemLevel,
            ItemType itemType,
            string addressableKey = "")
        {
            _hasItem = true;
            _itemID = itemID;
            _itemSprite = null;
            _itemName = itemName;
            _itemLevel = itemLevel;
            _itemType = itemType;
            _addressableKey = addressableKey;
        }

        public ItemData(
            int itemID,
            Sprite itemSprite,
            string itemName,
            int itemLevel,
            ItemType itemType,
            string addressableKey = "")
        {
            _hasItem = true;
            _itemID = itemID;
            _itemSprite = itemSprite;
            _itemName = itemName;
            _itemLevel = itemLevel;
            _itemType = itemType;
            _addressableKey = addressableKey;
        }

        public ItemData Clone()
        {
            return (ItemData)MemberwiseClone();
        }
    }
}
