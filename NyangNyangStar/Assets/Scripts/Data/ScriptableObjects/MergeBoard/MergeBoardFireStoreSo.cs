using Firebase.Firestore;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    [FirestorePath("Users/{userId}/MergeBoard/{docId}")]
    [CreateAssetMenu(fileName = "MergeBoardFirestoreSO", menuName = "SO/MergeBoard/MergeBoard Firestore SO")]
    public class MergeBoardFirestoreSo : BaseFireStore
    {
        private const int BoardSlotCount = 63;
        private const int SpecialSlotCount = 2;
        private const int BatchLimit = 450;

        public bool IsReady => db != null && !string.IsNullOrEmpty(m_UserId);

        private CollectionReference SlotCollection =>
            db.Collection("Users")
                .Document(m_UserId)
                .Collection("MergeBoard");

        private CollectionReference RewardQueueCollection =>
            db.Collection("Users")
                .Document(m_UserId)
                .Collection("MergeBoardRewardQueue");

        private CollectionReference SpecialItemCollection =>
            db.Collection("Users")
                .Document(m_UserId)
                .Collection("SpecialItemBoard");

        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            InitDataBase(database, userId);
            await CreateEmptyBoardAsync();
            await CreateEmptySpecialBoardAsync();
            await ClearRewardQueueAsync();
        }

        public async Task CreateEmptyBoardAsync()
        {
            Dictionary<int, ItemData> emptyBoard = new Dictionary<int, ItemData>();

            for (int slotNumber = 1; slotNumber <= BoardSlotCount; slotNumber++)
                emptyBoard[slotNumber] = ItemData.Empty;

            await SaveBoardAsync(emptyBoard);
        }

        public Task SaveSlotAsync(int slotNumber, ItemData itemData)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 슬롯 저장을 생략합니다.", DebugType.Board);
                return Task.CompletedTask;
            }

            DocumentReference docRef = SlotCollection.Document(ToSlotDocumentId(slotNumber));
            return docRef.SetAsync(ToSlotDictionary(slotNumber, itemData));
        }

        public async Task SaveSlotsAsync(Dictionary<int, ItemData> changedSlots)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 슬롯 저장을 생략합니다.", DebugType.Board);
                return;
            }

            if (changedSlots == null || changedSlots.Count == 0)
                return;

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            foreach (var pair in changedSlots)
            {
                int slotNumber = pair.Key;
                ItemData itemData = pair.Value;

                DocumentReference docRef = SlotCollection.Document(ToSlotDocumentId(slotNumber));
                batch.Set(docRef, ToSlotDictionary(slotNumber, itemData));

                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        public async Task<Dictionary<int, ItemData>> LoadBoardAsync()
        {
            Dictionary<int, ItemData> boardData = new Dictionary<int, ItemData>();

            for (int slotNumber = 1; slotNumber <= BoardSlotCount; slotNumber++)
                boardData[slotNumber] = ItemData.Empty;

            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보드 로드를 생략합니다.", DebugType.Board);
                return boardData;
            }

            QuerySnapshot snapshot = await SlotCollection.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (!document.Exists)
                    continue;

                int slotNumber = ReadInt(document, "SlotNumber", TryParseDocumentId(document.Id));

                if (slotNumber < 1 || slotNumber > BoardSlotCount)
                    continue;

                boardData[slotNumber] = ToItemData(document);
            }

            return boardData;
        }

        public async Task SaveRewardQueueAsync(IEnumerable<ItemData> rewardQueue)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐 저장을 생략합니다.", DebugType.Board);
                return;
            }

            await ClearRewardQueueAsync();

            if (rewardQueue == null)
                return;

            WriteBatch batch = db.StartBatch();

            int order = 1;
            int operationCount = 0;

            foreach (ItemData itemData in rewardQueue)
            {
                if (itemData == null || !itemData.HasItem)
                    continue;

                string documentId = ToQueueDocumentId(order);
                DocumentReference docRef = RewardQueueCollection.Document(documentId);

                batch.Set(docRef, ToQueueDictionary(order, itemData));

                order++;
                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        public async Task<List<ItemData>> LoadRewardQueueAsync()
        {
            List<(int Order, ItemData Item)> loadedItems = new List<(int Order, ItemData Item)>();

            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐 로드를 생략합니다.", DebugType.Board);
                return new List<ItemData>();
            }

            QuerySnapshot snapshot = await RewardQueueCollection.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (!document.Exists)
                    continue;

                int order = ReadInt(document, "Order", TryParseDocumentId(document.Id));
                ItemData itemData = ToItemData(document);

                if (itemData == null || !itemData.HasItem)
                    continue;

                loadedItems.Add((order, itemData));
            }

            loadedItems.Sort((a, b) => a.Order.CompareTo(b.Order));

            List<ItemData> result = new List<ItemData>();
            for (int i = 0; i < loadedItems.Count; i++)
                result.Add(loadedItems[i].Item.Clone());

            return result;
        }

        public async Task ClearRewardQueueAsync()
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐 초기화를 생략합니다.", DebugType.Board);
                return;
            }

            QuerySnapshot snapshot = await RewardQueueCollection.GetSnapshotAsync();

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                batch.Delete(document.Reference);
                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        public async Task SaveBoardAsync(IReadOnlyDictionary<int, ItemData> boardData)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보드 저장을 생략합니다.", DebugType.Board);
                return;
            }

            await ClearBoardAsync();

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            for (int slotNumber = 1; slotNumber <= BoardSlotCount; slotNumber++)
            {
                ItemData itemData = ItemData.Empty;

                if (boardData != null && boardData.TryGetValue(slotNumber, out ItemData data))
                    itemData = data ?? ItemData.Empty;

                DocumentReference docRef = SlotCollection.Document(ToSlotDocumentId(slotNumber));
                batch.Set(docRef, ToSlotDictionary(slotNumber, itemData));

                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        public async Task ClearBoardAsync()
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보드 초기화를 생략합니다.", DebugType.Board);
                return;
            }

            QuerySnapshot snapshot = await SlotCollection.GetSnapshotAsync();

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                batch.Delete(document.Reference);
                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        public Task CreateEmptySpecialBoardAsync()
        {
            return CreateEmptySpecialBoardAsync(SpecialSlotCount);
        }

        public async Task CreateEmptySpecialBoardAsync(int slotCount)
        {
            Dictionary<int, SpecialItemSlotData> emptyBoard = new Dictionary<int, SpecialItemSlotData>();
            int safeSlotCount = Mathf.Max(1, slotCount);

            for (int slotNumber = 1; slotNumber <= safeSlotCount; slotNumber++)
                emptyBoard[slotNumber] = SpecialItemSlotData.Empty(slotNumber);

            await SaveSpecialBoardAsync(emptyBoard, safeSlotCount);
        }

        public Task SaveSpecialSlotAsync(int slotNumber, SpecialItemSlotData slotData)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 슬롯 저장을 생략합니다.", DebugType.Board);
                return Task.CompletedTask;
            }

            DocumentReference docRef = SpecialItemCollection.Document(ToSpecialSlotDocumentId(slotNumber));
            return docRef.SetAsync(ToSpecialSlotDictionary(slotNumber, slotData));
        }

        public async Task<Dictionary<int, SpecialItemSlotData>> LoadSpecialBoardAsync(int slotCount = SpecialSlotCount)
        {
            int safeSlotCount = Mathf.Max(1, slotCount);
            Dictionary<int, SpecialItemSlotData> specialBoardData = new Dictionary<int, SpecialItemSlotData>();

            for (int slotNumber = 1; slotNumber <= safeSlotCount; slotNumber++)
                specialBoardData[slotNumber] = SpecialItemSlotData.Empty(slotNumber);

            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 보드 로드를 생략합니다.", DebugType.Board);
                return specialBoardData;
            }

            QuerySnapshot snapshot = await SpecialItemCollection.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (!document.Exists)
                    continue;

                int slotNumber = ReadInt(document, "SlotNumber", TryParseDocumentId(document.Id));

                if (slotNumber < 1 || slotNumber > safeSlotCount)
                    continue;

                specialBoardData[slotNumber] = ToSpecialItemSlotData(document, slotNumber);
            }

            return specialBoardData;
        }

        public async Task SaveSpecialBoardAsync(IReadOnlyDictionary<int, SpecialItemSlotData> boardData, int slotCount = SpecialSlotCount)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 보드 저장을 생략합니다.", DebugType.Board);
                return;
            }

            await ClearSpecialBoardAsync();

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;
            int safeSlotCount = Mathf.Max(1, slotCount);

            for (int slotNumber = 1; slotNumber <= safeSlotCount; slotNumber++)
            {
                SpecialItemSlotData slotData = SpecialItemSlotData.Empty(slotNumber);

                if (boardData != null && boardData.TryGetValue(slotNumber, out SpecialItemSlotData data))
                    slotData = data ?? SpecialItemSlotData.Empty(slotNumber);

                DocumentReference docRef = SpecialItemCollection.Document(ToSpecialSlotDocumentId(slotNumber));
                batch.Set(docRef, ToSpecialSlotDictionary(slotNumber, slotData));

                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        public async Task ClearSpecialBoardAsync()
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 보드 초기화를 생략합니다.", DebugType.Board);
                return;
            }

            QuerySnapshot snapshot = await SpecialItemCollection.GetSnapshotAsync();

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                batch.Delete(document.Reference);
                operationCount++;

                if (operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        private Dictionary<string, object> ToSlotDictionary(int slotNumber, ItemData itemData)
        {
            itemData ??= ItemData.Empty;

            return new Dictionary<string, object>
            {
                { "SlotNumber", slotNumber },
                { "HasItem", itemData.HasItem },
                { "ItemID", itemData.ItemID },
                { "ItemName", itemData.ItemName },
                { "ItemLevel", itemData.ItemLevel },
                { "ItemType", itemData.ItemType.ToString() },
                { "AddressableKey", itemData.AddressableKey }
            };
        }

        private Dictionary<string, object> ToQueueDictionary(int order, ItemData itemData)
        {
            itemData ??= ItemData.Empty;

            return new Dictionary<string, object>
            {
                { "Order", order },
                { "HasItem", itemData.HasItem },
                { "ItemID", itemData.ItemID },
                { "ItemName", itemData.ItemName },
                { "ItemLevel", itemData.ItemLevel },
                { "ItemType", itemData.ItemType.ToString() },
                { "AddressableKey", itemData.AddressableKey }
            };
        }

        private Dictionary<string, object> ToSpecialSlotDictionary(int slotNumber, SpecialItemSlotData slotData)
        {
            slotData ??= SpecialItemSlotData.Empty(slotNumber);
            ItemData itemData = slotData.ItemData ?? ItemData.Empty;

            return new Dictionary<string, object>
            {
                { "SlotNumber", slotNumber },
                { "HasItem", slotData.HasItem },
                { "ItemID", itemData.ItemID },
                { "ItemName", itemData.ItemName },
                { "ItemLevel", itemData.ItemLevel },
                { "ItemType", itemData.ItemType.ToString() },
                { "Count", slotData.Count },
                { "AddressableKey", itemData.AddressableKey }
            };
        }

        private ItemData ToItemData(DocumentSnapshot snapshot)
        {
            bool hasItem = ReadBool(snapshot, "HasItem", false);

            if (!hasItem)
                return ItemData.Empty;

            int itemID = ReadInt(snapshot, "ItemID", ReadInt(snapshot, "ItemNumber", 0));
            string itemName = ReadString(snapshot, "ItemName", string.Empty);
            int itemLevel = ReadInt(snapshot, "ItemLevel", 1);
            string itemTypeValue = ReadString(snapshot, "ItemType", ItemType.Common.ToString());
            string addressableKey = ReadString(snapshot, "AddressableKey", ReadString(snapshot, "SpriteKey", string.Empty));

            if (!Enum.TryParse(itemTypeValue, true, out ItemType itemType))
                itemType = ItemType.Common;

            return new ItemData(itemID, itemName, itemLevel, itemType, addressableKey);
        }

        private SpecialItemSlotData ToSpecialItemSlotData(DocumentSnapshot snapshot, int slotNumber)
        {
            bool hasItem = ReadBool(snapshot, "HasItem", false);
            int count = ReadInt(snapshot, "Count", 0);

            if (!hasItem || count <= 0)
                return SpecialItemSlotData.Empty(slotNumber);

            ItemData itemData = ToItemData(snapshot);

            if (itemData == null || !itemData.HasItem)
                return SpecialItemSlotData.Empty(slotNumber);

            return new SpecialItemSlotData(slotNumber, itemData, count);
        }

        private int TryParseDocumentId(string documentId)
        {
            return int.TryParse(documentId, out int value) ? value : 0;
        }

        private int ReadInt(DocumentSnapshot snapshot, string fieldName, int defaultValue)
        {
            Dictionary<string, object> data = snapshot.ToDictionary();

            if (!data.TryGetValue(fieldName, out object value) || value == null)
                return defaultValue;

            if (value is int intValue)
                return intValue;

            if (value is long longValue)
                return (int)longValue;

            if (value is double doubleValue)
                return (int)doubleValue;

            if (value is string stringValue && int.TryParse(stringValue, out int parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private bool ReadBool(DocumentSnapshot snapshot, string fieldName, bool defaultValue)
        {
            Dictionary<string, object> data = snapshot.ToDictionary();

            if (!data.TryGetValue(fieldName, out object value) || value == null)
                return defaultValue;

            if (value is bool boolValue)
                return boolValue;

            if (value is string stringValue && bool.TryParse(stringValue, out bool parsedValue))
                return parsedValue;

            return defaultValue;
        }

        private string ReadString(DocumentSnapshot snapshot, string fieldName, string defaultValue)
        {
            Dictionary<string, object> data = snapshot.ToDictionary();

            if (!data.TryGetValue(fieldName, out object value) || value == null)
                return defaultValue;

            return value.ToString();
        }

        private string ToSlotDocumentId(int slotNumber)
        {
            return slotNumber.ToString("D2");
        }

        private string ToQueueDocumentId(int order)
        {
            return order.ToString("D2");
        }

        private string ToSpecialSlotDocumentId(int slotNumber)
        {
            return slotNumber.ToString("D2");
        }
    }
}
