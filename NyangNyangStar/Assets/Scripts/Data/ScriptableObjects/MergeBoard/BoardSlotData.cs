using Data.ScriptableObjects.MergeBoard;
using System;
using UnityEngine;

namespace UI.MergeBoard
{
    [Serializable]
    public class BoardSlotData
    {
        [SerializeField] private int _slotNumber;
        [SerializeField] private ItemData _itemData;

        public int SlotNumber => _slotNumber;
        public ItemData ItemData => _itemData;

        public BoardSlotData(int slotNumber, ItemData itemData)
        {
            _slotNumber = slotNumber;
            _itemData = itemData ?? ItemData.Empty;
        }
    }
}