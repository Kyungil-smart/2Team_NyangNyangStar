using Core.Managers;
using Services.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Data.ScriptableObjects.MergeBoard
{
    [CreateAssetMenu(fileName = "ItemDatabaseSo", menuName = "Data/MergeBoard/Item Database")]
    public class ItemDatabaseSo : SoBase, ISheetParsable
    {
        [Header("아이템 데이터")]
        [Tooltip("아이템ID, 아이템 이름, 아이템 레벨, 아이템 타입, Addressable Key 순서로 파싱됩니다.")]
        [SerializeField] private List<ItemData> _items = new();

        private readonly Dictionary<int, ItemData> _itemDict = new();
        private readonly Dictionary<int, AsyncOperationHandle<Sprite>> _spriteHandles = new();

        public IReadOnlyList<ItemData> Items => _items;
        public int DataCount => _items?.Count ?? 0;
        public bool IsSpriteLoaded { get; private set; }

        private void OnEnable()
        {
            RebuildItemDictionary();
        }

        public override void Init()
        {
            ClearData();
        }

        public void ClearData()
        {
            ReleaseSpriteHandles();
            _items.Clear();
            _itemDict.Clear();
            IsSpriteLoaded = false;
        }

        public void SetData(string[] cols)
        {
            if (cols == null || cols.Length < 5)
            {
                DebugTool.Warning("[ItemDatabaseSo] 컬럼 수가 부족합니다. 필요 컬럼: ItemID, ItemName, ItemLevel, ItemType, AddressableKey", DebugType.Data, this);
                return;
            }

            if (!TryParseInt(GetColumn(cols, 0), out int itemID) || itemID <= 0)
            {
                DebugTool.Warning($"[ItemDatabaseSo] 잘못된 ItemID 입니다. 값: {GetColumn(cols, 0)}", DebugType.Data, this);
                return;
            }

            string itemName = GetColumn(cols, 1);

            if (string.IsNullOrEmpty(itemName))
            {
                DebugTool.Warning($"[ItemDatabaseSo] ItemID {itemID}의 아이템 이름이 비어있습니다.", DebugType.Data, this);
                return;
            }

            int itemLevel = TryParseInt(GetColumn(cols, 2), out int parsedLevel)
                ? Mathf.Max(1, parsedLevel)
                : 1;

            ItemType itemType = ConvertItemType(GetColumn(cols, 3));

            if (itemType == ItemType.None)
            {
                DebugTool.Warning($"[ItemDatabaseSo] ItemID {itemID}의 ItemType이 유효하지 않습니다. 값: {GetColumn(cols, 3)}", DebugType.Data, this);
                return;
            }

            string addressableKey = GetColumn(cols, 4);

            ItemData itemData = new ItemData(
                itemID,
                itemName,
                itemLevel,
                itemType,
                addressableKey);

            AddOrUpdateItem(itemData);
        }

        public IEnumerator LoadItemSpritesCoroutine(Action onComplete = null)
        {
            IsSpriteLoaded = false;
            ReleaseSpriteHandles();
            RebuildItemDictionaryIfNeeded();

            if (_items == null || _items.Count == 0)
            {
                IsSpriteLoaded = true;
                onComplete?.Invoke();
                yield break;
            }

            int pendingCount = 0;

            for (int i = 0; i < _items.Count; i++)
            {
                ItemData itemData = _items[i];

                if (itemData == null || !itemData.HasItem)
                    continue;

                if (string.IsNullOrEmpty(itemData.AddressableKey))
                    continue;

                pendingCount++;
                ItemData capturedItemData = itemData;

                GameManager.Addressable.LoadSprite(
                    capturedItemData.AddressableKey,
                    (sprite, handle) =>
                    {
                        capturedItemData.SetSprite(sprite);
                        _spriteHandles[capturedItemData.ItemID] = handle;
                        pendingCount--;
                    },
                    failedKey =>
                    {
                        DebugTool.Warning($"[ItemDatabaseSo] {failedKey} Sprite 로드 실패", DebugType.Addressable, this);
                        pendingCount--;
                    });
            }

            while (pendingCount > 0)
                yield return null;

            IsSpriteLoaded = true;
            DebugTool.Log($"[ItemDatabaseSo] 아이템 Sprite 로드 완료", DebugType.Addressable, this);
            onComplete?.Invoke();
        }

        public bool TryGetItemById(int itemID, out ItemData itemData)
        {
            itemData = null;

            if (itemID <= 0)
                return false;

            RebuildItemDictionaryIfNeeded();

            if (!_itemDict.TryGetValue(itemID, out ItemData foundData))
                return false;

            if (foundData == null || !foundData.HasItem)
                return false;

            itemData = foundData.Clone();
            return true;
        }

        public bool TryGetItemById(int itemID, int count, out ItemData itemData)
        {
            return TryGetItemById(itemID, out itemData);
        }

        public bool TryGetRandomItem(out ItemData itemData)
        {
            return TryGetRandomItem(ItemType.Common, out itemData);
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

                if (data.ItemID <= 0)
                    continue;

                if (data.ItemType != itemType)
                    continue;

                candidates.Add(data);
            }

            if (candidates.Count == 0)
                return false;

            int index = UnityEngine.Random.Range(0, candidates.Count);
            itemData = candidates[index].Clone();
            return true;
        }

        public ItemData CreateRuntimeItem(ItemData sourceData)
        {
            if (sourceData == null || !sourceData.HasItem)
                return ItemData.Empty;

            ItemData runtimeData = sourceData.Clone();

            RebuildItemDictionaryIfNeeded();

            if (_itemDict.TryGetValue(sourceData.ItemID, out ItemData originData) && originData != null)
            {
                if (originData.ItemSprite != null)
                    runtimeData.SetSprite(originData.ItemSprite);
            }

            return runtimeData;
        }

        public void PrintData()
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine($"[ItemDatabaseSo 로드 완료] 총 {DataCount}개");

            for (int i = 0; i < _items.Count; i++)
            {
                ItemData itemData = _items[i];

                if (itemData == null)
                    continue;

                log.AppendLine(
                    $"ID: {itemData.ItemID}, " +
                    $"Name: {itemData.ItemName}, " +
                    $"Level: {itemData.ItemLevel}, " +
                    $"Type: {itemData.ItemType}, " +
                    $"Key: {itemData.AddressableKey}, " +
                    $"Sprite: {(itemData.ItemSprite != null ? itemData.ItemSprite.name : "null")}");
            }

            DebugTool.Log(log.ToString(), DebugType.Data, this);
        }

        private void AddOrUpdateItem(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
                return;

            RebuildItemDictionaryIfNeeded();

            if (_itemDict.ContainsKey(itemData.ItemID))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i] == null)
                        continue;

                    if (_items[i].ItemID != itemData.ItemID)
                        continue;

                    _items[i] = itemData;
                    _itemDict[itemData.ItemID] = itemData;
                    return;
                }
            }

            _items.Add(itemData);
            _itemDict[itemData.ItemID] = itemData;
        }

        private void RebuildItemDictionaryIfNeeded()
        {
            if (_itemDict.Count == _items.Count)
                return;

            RebuildItemDictionary();
        }

        private void RebuildItemDictionary()
        {
            _itemDict.Clear();

            if (_items == null)
                return;

            for (int i = 0; i < _items.Count; i++)
            {
                ItemData itemData = _items[i];

                if (itemData == null || !itemData.HasItem)
                    continue;

                if (itemData.ItemID <= 0)
                    continue;

                _itemDict[itemData.ItemID] = itemData;
            }
        }

        private void ReleaseSpriteHandles()
        {
            foreach (AsyncOperationHandle<Sprite> handle in _spriteHandles.Values)
            {
                if (!handle.IsValid())
                    continue;

                GameManager.Addressable.Release(handle);
            }

            _spriteHandles.Clear();
        }

        private string GetColumn(string[] cols, int index)
        {
            if (cols == null)
                return string.Empty;

            if (index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }

        private bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        private ItemType ConvertItemType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ItemType.None;

            string trimmedValue = value.Trim();

            if (int.TryParse(trimmedValue, out int numericValue))
            {
                if (Enum.IsDefined(typeof(ItemType), numericValue))
                    return (ItemType)numericValue;

                return ItemType.None;
            }

            if (Enum.TryParse(trimmedValue, true, out ItemType itemType))
                return itemType;

            switch (trimmedValue)
            {
                case "일반":
                case "common":
                    return ItemType.Common;

                case "특수":
                case "special":
                    return ItemType.Special;

                case "없음":
                case "none":
                    return ItemType.None;

                default:
                    return ItemType.None;
            }
        }
    }
}
