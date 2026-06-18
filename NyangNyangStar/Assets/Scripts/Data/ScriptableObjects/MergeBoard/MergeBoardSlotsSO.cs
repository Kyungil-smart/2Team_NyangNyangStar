using Firebase.Firestore;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    // 머지 보드 슬롯(기본 63칸) 영속화. 컬렉션: Users/{uid}/MergeBoard/{슬롯번호}
    [CreateAssetMenu(fileName = "MergeBoardSlotsSO", menuName = "SO/MergeBoard/MergeBoard Slots SO")]
    public class MergeBoardSlotsSO : MergeCollectionStoreBase<MergeSlotDoc>
    {
        private const int BoardSlotCount = 63;

        protected override string CollectionName => "MergeBoard";

        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            InitDataBase(database, userId);
            await CreateEmptyBoardAsync();
        }

        public async Task CreateEmptyBoardAsync()
        {
            Dictionary<int, ItemData> emptyBoard = new Dictionary<int, ItemData>();
            for (int slotNumber = 1; slotNumber <= BoardSlotCount; slotNumber++)
                emptyBoard[slotNumber] = ItemData.Empty;

            await SaveBoardAsync(emptyBoard);
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

            foreach (MergeSlotDoc doc in await ReadAllAsync())
            {
                if (doc.SlotNumber < 1 || doc.SlotNumber > BoardSlotCount)
                    continue;
                boardData[doc.SlotNumber] = ToItemData(doc);
            }

            return boardData;
        }

        public async Task SaveBoardAsync(IReadOnlyDictionary<int, ItemData> boardData)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보드 저장을 생략합니다.", DebugType.Board);
                return;
            }

            await ClearAsync();

            var docs = new List<KeyValuePair<string, MergeSlotDoc>>();
            for (int slotNumber = 1; slotNumber <= BoardSlotCount; slotNumber++)
            {
                ItemData itemData = ItemData.Empty;
                if (boardData != null && boardData.TryGetValue(slotNumber, out ItemData data))
                    itemData = data ?? ItemData.Empty;

                docs.Add(new KeyValuePair<string, MergeSlotDoc>(DocId(slotNumber), ToDoc(slotNumber, itemData)));
            }

            await WriteManyAsync(docs);
        }

        public Task SaveSlotAsync(int slotNumber, ItemData itemData)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 슬롯 저장을 생략합니다.", DebugType.Board);
                return Task.CompletedTask;
            }

            return WriteOneAsync(DocId(slotNumber), ToDoc(slotNumber, itemData));
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

            var docs = new List<KeyValuePair<string, MergeSlotDoc>>();
            foreach (KeyValuePair<int, ItemData> pair in changedSlots)
                docs.Add(new KeyValuePair<string, MergeSlotDoc>(DocId(pair.Key), ToDoc(pair.Key, pair.Value)));

            await WriteManyAsync(docs);
        }

        public Task ClearBoardAsync() => ClearAsync();

        private static MergeSlotDoc ToDoc(int slotNumber, ItemData itemData)
        {
            itemData ??= ItemData.Empty;
            return new MergeSlotDoc
            {
                SlotNumber = slotNumber,
                HasItem = itemData.HasItem,
                ItemID = itemData.ItemID,
                ItemName = itemData.ItemName,
                ItemLevel = itemData.ItemLevel,
                ItemType = itemData.ItemType.ToString(),
                AddressableKey = itemData.AddressableKey
            };
        }

        private static ItemData ToItemData(MergeSlotDoc doc)
        {
            if (!doc.HasItem)
                return ItemData.Empty;

            if (!Enum.TryParse(doc.ItemType, true, out ItemType itemType))
                itemType = ItemType.Common;

            return new ItemData(doc.ItemID, doc.ItemName, doc.ItemLevel, itemType, doc.AddressableKey);
        }
    }
}
