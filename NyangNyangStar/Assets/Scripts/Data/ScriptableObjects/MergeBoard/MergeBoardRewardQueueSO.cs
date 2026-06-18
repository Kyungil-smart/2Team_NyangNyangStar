using Firebase.Firestore;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    // 머지 보드 보상 큐(순서 보존) 영속화. 컬렉션: Users/{uid}/MergeBoardRewardQueue/{순번}
    [CreateAssetMenu(fileName = "MergeBoardRewardQueueSO", menuName = "SO/MergeBoard/MergeBoard Reward Queue SO")]
    public class MergeBoardRewardQueueSO : MergeCollectionStoreBase<RewardQueueDoc>
    {
        protected override string CollectionName => "MergeBoardRewardQueue";

        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            InitDataBase(database, userId);
            await ClearRewardQueueAsync();
        }

        public async Task<List<ItemData>> LoadRewardQueueAsync()
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐 로드를 생략합니다.", DebugType.Board);
                return new List<ItemData>();
            }

            var loaded = new List<(int Order, ItemData Item)>();
            foreach (RewardQueueDoc doc in await ReadAllAsync())
            {
                if (!doc.HasItem)
                    continue;
                loaded.Add((doc.Order, ToItemData(doc)));
            }

            loaded.Sort((a, b) => a.Order.CompareTo(b.Order));

            var result = new List<ItemData>();
            foreach (var entry in loaded)
                result.Add(entry.Item.Clone());

            return result;
        }

        public async Task SaveRewardQueueAsync(IEnumerable<ItemData> rewardQueue)
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐 저장을 생략합니다.", DebugType.Board);
                return;
            }

            await ClearAsync();

            if (rewardQueue == null)
                return;

            var docs = new List<KeyValuePair<string, RewardQueueDoc>>();
            int order = 1;
            foreach (ItemData itemData in rewardQueue)
            {
                if (itemData == null || !itemData.HasItem)
                    continue;
                docs.Add(new KeyValuePair<string, RewardQueueDoc>(DocId(order), ToDoc(order, itemData)));
                order++;
            }

            await WriteManyAsync(docs);
        }

        public Task ClearRewardQueueAsync()
        {
            if (!IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐 초기화를 생략합니다.", DebugType.Board);
                return Task.CompletedTask;
            }

            return ClearAsync();
        }

        private static RewardQueueDoc ToDoc(int order, ItemData itemData)
        {
            itemData ??= ItemData.Empty;
            return new RewardQueueDoc
            {
                Order = order,
                HasItem = itemData.HasItem,
                ItemID = itemData.ItemID,
                ItemName = itemData.ItemName,
                ItemLevel = itemData.ItemLevel,
                ItemType = itemData.ItemType.ToString(),
                AddressableKey = itemData.AddressableKey
            };
        }

        private static ItemData ToItemData(RewardQueueDoc doc)
        {
            if (!doc.HasItem)
                return ItemData.Empty;

            if (!Enum.TryParse(doc.ItemType, true, out ItemType itemType))
                itemType = ItemType.Common;

            return new ItemData(doc.ItemID, doc.ItemName, doc.ItemLevel, itemType, doc.AddressableKey);
        }
    }
}
