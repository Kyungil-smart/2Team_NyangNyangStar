using Services.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    [CreateAssetMenu(fileName = "ItemDatabaseSo", menuName = "Data/MergeBoard/Item Database")]
    public class ItemDatabaseSo : SoBase, ISheetParsable
    {
        [SerializeField] private List<ItemData> _items = new();

        public IReadOnlyList<ItemData> Items => _items;

        public bool TryGetRandomItem(out ItemData itemData)
        {
            return TryGetRandomItem(ItemType.General, out itemData);
        }

        public bool TryGetRandomItem(ItemType itemType, out ItemData itemData)
        {
            itemData = null;

            if (_items == null || _items.Count == 0)
                return false;

            List<ItemData> candidates = new List<ItemData>();

            for (int i = 0; i < _items.Count; i++)
            {
                ItemData data = _items[i];

                if (data == null)
                    continue;

                if (data.ItemNumber <= 0)
                    continue;

                if (data.ItemType != itemType)
                    continue;

                candidates.Add(data);
            }

            if (candidates.Count == 0)
                return false;

            int index = Random.Range(0, candidates.Count);
            ItemData selected = candidates[index];

            itemData = new ItemData(
                selected.ItemNumber,
                selected.ItemSprite,
                selected.ItemName,
                selected.ItemLevel,
                selected.ItemType,
                Mathf.Max(1, selected.Amount),
                selected.SpriteKey
            );

            return true;
        }

        public override void Init()
        {
        }

        public void ClearData()
        {
        }

        public void SetData(string[] cols)
        {
        }

        public void PrintData()
        {
        }
    }
}