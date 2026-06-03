using Services.Enums;
using System;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    [Serializable]
    public class ItemData
    {
        [SerializeField] private bool _hasItem;
        [SerializeField] private int _itemNumber;
        [SerializeField] private Sprite _itemSprite;
        [SerializeField] private string _itemName;
        [SerializeField] private int _itemLevel;
        [SerializeField] private ItemType _itemType;
        [SerializeField] private int _amount;
        [SerializeField] private string _spriteKey;

        public bool HasItem =>
            _hasItem || (_itemNumber > 0 && _itemType != ItemType.None && _amount > 0);
        public int ItemNumber => _itemNumber;
        public Sprite ItemSprite => _itemSprite;
        public string ItemName => _itemName;
        public int ItemLevel => _itemLevel;
        public ItemType ItemType => _itemType;
        public int Amount => _amount;
        public string SpriteKey => _spriteKey;

        public static ItemData Empty => new ItemData();

        private ItemData()
        {
            _hasItem = false;
            _itemNumber = 0;
            _itemSprite = null;
            _itemName = string.Empty;
            _itemLevel = 0;
            _itemType = ItemType.None;
            _amount = 0;
            _spriteKey = string.Empty;
        }

        public ItemData(
            int itemNumber,
            string itemName,
            int itemLevel,
            ItemType itemType,
            int amount = 1,
            string spriteKey = "")
        {
            _hasItem = true;
            _itemNumber = itemNumber;
            _itemSprite = null;
            _itemName = itemName;
            _itemLevel = itemLevel;
            _itemType = itemType;
            _amount = Mathf.Max(1, amount);
            _spriteKey = spriteKey;
        }

        public ItemData(
            int itemNumber,
            Sprite itemSprite,
            string itemName,
            int itemLevel,
            ItemType itemType,
            int amount = 1,
            string spriteKey = "")
        {
            _hasItem = true;
            _itemNumber = itemNumber;
            _itemSprite = itemSprite;
            _itemName = itemName;
            _itemLevel = itemLevel;
            _itemType = itemType;
            _amount = Mathf.Max(1, amount);
            _spriteKey = spriteKey;
        }

        public ItemData Clone()
        {
            return (ItemData)MemberwiseClone();
        }

        public ItemData CloneWithAmount(int amount)
        {
            ItemData clone = Clone();
            clone._amount = Mathf.Max(0, amount);
            clone._hasItem = clone._amount > 0;

            if (!clone._hasItem)
            {
                clone._itemNumber = 0;
                clone._itemSprite = null;
                clone._itemName = string.Empty;
                clone._itemLevel = 0;
                clone._itemType = ItemType.None;
                clone._spriteKey = string.Empty;
            }

            return clone;
        }
    }
}