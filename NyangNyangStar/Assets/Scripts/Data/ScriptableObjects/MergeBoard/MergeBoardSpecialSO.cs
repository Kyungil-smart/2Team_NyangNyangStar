using Firebase.Firestore;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    // 특수 아이템 보드(기본 2칸) 영속화. 컬렉션: Users/{uid}/SpecialItemBoard/{슬롯번호}
    [CreateAssetMenu(fileName = "MergeBoardSpecialSO", menuName = "SO/MergeBoard/MergeBoard Special SO")]
    public class MergeBoardSpecialSO : MergeCollectionStoreBase<SpecialSlotDoc>
    {
        private const int SpecialSlotCount = 2;

        protected override string CollectionName => "SpecialItemBoard";

        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            InitDataBase(database, userId);
            await CreateEmptySpecialBoardAsync();
        }

        public Task CreateEmptySpecialBoardAsync() => CreateEmptySpecialBoardAsync(SpecialSlotCount);

        public async Task CreateEmptySpecialBoardAsync(int slotCount)
        {
            int safeSlotCount = Mathf.Max(1, slotCount);
            var emptyBoard = new Dictionary<int, SpecialItemSlotData>();
            for (int slotNumber = 1; slotNumber <= safeSlotCount; slotNumber++)
                emptyBoard[slotNumber] = SpecialItemSlotData.Empty(slotNumber);

            await SaveSpecialBoardAsync(emptyBoard, safeSlotCount);
        }

        public async Task<Dictionary<int, SpecialItemSlotData>> LoadSpecialBoardAsync(int slotCount = SpecialSlotCount)
        {
            int safeSlotCount = Mathf.Max(1, slotCount);
            var specialBoardData = new Dictionary<int, SpecialItemSlotData>();
            for (int slotNumber = 1; slotNumber <= safeSlotCount; slotNumber++)
                specialBoardData[slotNumber] = SpecialItemSlotData.Empty(slotNumber);

            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 보드 로드를 생략합니다.", DebugType.Board);
                return specialBoardData;
            }

            foreach (SpecialSlotDoc doc in await ReadAllAsync())
            {
                if (doc.SlotNumber < 1 || doc.SlotNumber > safeSlotCount)
                    continue;
                specialBoardData[doc.SlotNumber] = ToSpecialSlotData(doc);
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

            await ClearAsync();

            int safeSlotCount = Mathf.Max(1, slotCount);
            var docs = new List<KeyValuePair<string, SpecialSlotDoc>>();
            for (int slotNumber = 1; slotNumber <= safeSlotCount; slotNumber++)
            {
                SpecialItemSlotData slotData = SpecialItemSlotData.Empty(slotNumber);
                if (boardData != null && boardData.TryGetValue(slotNumber, out SpecialItemSlotData data))
                    slotData = data ?? SpecialItemSlotData.Empty(slotNumber);

                docs.Add(new KeyValuePair<string, SpecialSlotDoc>(DocId(slotNumber), ToDoc(slotNumber, slotData)));
            }

            await WriteManyAsync(docs);
        }

        public Task SaveSpecialSlotAsync(int slotNumber, SpecialItemSlotData slotData)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 슬롯 저장을 생략합니다.", DebugType.Board);
                return Task.CompletedTask;
            }

            return WriteOneAsync(DocId(slotNumber), ToDoc(slotNumber, slotData));
        }

        public Task ClearSpecialBoardAsync() => ClearAsync();

        private static SpecialSlotDoc ToDoc(int slotNumber, SpecialItemSlotData slotData)
        {
            slotData ??= SpecialItemSlotData.Empty(slotNumber);
            ItemData itemData = slotData.ItemData ?? ItemData.Empty;
            return new SpecialSlotDoc
            {
                SlotNumber = slotNumber,
                HasItem = slotData.HasItem,
                ItemID = itemData.ItemID,
                ItemName = itemData.ItemName,
                ItemLevel = itemData.ItemLevel,
                ItemType = itemData.ItemType.ToString(),
                Count = slotData.Count,
                AddressableKey = itemData.AddressableKey
            };
        }

        private static SpecialItemSlotData ToSpecialSlotData(SpecialSlotDoc doc)
        {
            if (!doc.HasItem || doc.Count <= 0)
                return SpecialItemSlotData.Empty(doc.SlotNumber);

            if (!Enum.TryParse(doc.ItemType, true, out ItemType itemType))
                itemType = ItemType.Common;

            ItemData itemData = new ItemData(doc.ItemID, doc.ItemName, doc.ItemLevel, itemType, doc.AddressableKey);
            if (!itemData.HasItem)
                return SpecialItemSlotData.Empty(doc.SlotNumber);

            return new SpecialItemSlotData(doc.SlotNumber, itemData, doc.Count);
        }
    }
}
