using System;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    [Serializable]
    public class SpecialItemSlotData
    {
        [SerializeField] private int _slotNumber;
        [SerializeField] private ItemData _itemData;
        [SerializeField] private int _count;

        public int SlotNumber => _slotNumber;
        public ItemData ItemData => _itemData ?? ItemData.Empty;
        public int Count => _count;
        public bool HasItem => ItemData.HasItem && _count > 0;

        public static SpecialItemSlotData Empty(int slotNumber)
        {
            return new SpecialItemSlotData(slotNumber, ItemData.Empty, 0);
        }

        public SpecialItemSlotData(int slotNumber, ItemData itemData, int count)
        {
            _slotNumber = slotNumber;
            _itemData = itemData?.Clone() ?? ItemData.Empty;
            _count = Mathf.Max(0, count);

            if (_count <= 0)
                _itemData = ItemData.Empty;
        }

        public SpecialItemSlotData Clone()
        {
            return new SpecialItemSlotData(_slotNumber, _itemData, _count);
        }

        public SpecialItemSlotData CloneWithCount(int count)
        {
            return new SpecialItemSlotData(_slotNumber, _itemData, count);
        }
    }
}
