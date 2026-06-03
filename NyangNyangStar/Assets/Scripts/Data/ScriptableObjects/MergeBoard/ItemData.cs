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

        public bool HasItem => _hasItem;
        public int ItemNumber => _itemNumber;
        public Sprite ItemSprite => _itemSprite;
        public string ItemName => _itemName;
        public int ItemLevel => _itemLevel;
        public ItemType ItemType => _itemType;
        public int Amount => _amount;
        public string SpriteKey => _spriteKey;

        public static ItemData Empty => new ItemData(false, 0, string.Empty, 0, ItemType.None, 0, string.Empty);

        public ItemData(int itemNumber, string itemName, int itemLevel, ItemType itemType, int amount = 1, string spriteKey = "")
        {
            _hasItem = true;
            _itemNumber = itemNumber;
            _itemName = itemName;
            _itemLevel = itemLevel;
            _itemType = itemType;
            _amount = Mathf.Max(1, amount);
            _spriteKey = spriteKey;
        }

        private ItemData(bool hasItem, int itemNumber, string itemName, int itemLevel, ItemType itemType, int amount, string spriteKey)
        {
            _hasItem = hasItem;
            _itemNumber = itemNumber;
            _itemName = itemName;
            _itemLevel = itemLevel;
            _itemType = itemType;
            _amount = Mathf.Max(0, amount);
            _spriteKey = spriteKey;
        }

        public ItemData Clone()
        {
            return new ItemData(_hasItem, _itemNumber, _itemName, _itemLevel, _itemType, _amount, _spriteKey);
        }

        public ItemData CloneWithAmount(int amount)
        {
            return new ItemData(_hasItem, _itemNumber, _itemName, _itemLevel, _itemType, amount, _spriteKey);
        }
    }
}
