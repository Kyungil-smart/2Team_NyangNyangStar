using Data.ScriptableObjects.MergeBoard;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

namespace UI.MergeBoard
{
    public class BoardRewardQueue : MonoBehaviour
    {
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private MergeBoardFirestoreSo _mergeBoardFirestore;
        [SerializeField] private List<RewardQueueSlotView> _queueSlotViews = new();
        [SerializeField] private TMP_Text _countText;

        private readonly Queue<ItemData> _rewardQueue = new();
        
        private const int PreviewCount = 3;

        private void Start()
        {
            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                bool isTopSlot = i == 0;
                _queueSlotViews[i].Init(this, isTopSlot);
            }

            RefreshView();
        }

        public async Task EnqueueItemAsync(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
                return;

            _rewardQueue.Enqueue(itemData.Clone());

            RefreshView();

            if (_mergeBoardFirestore != null)
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);
        }

        public async void TryMoveTopItemToBoard()
        {
            if (_rewardQueue.Count <= 0)
                return;

            if (_boardSystem == null)
            {
                DebugTool.Warning("BoardSystem이 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            ItemData itemData = _rewardQueue.Peek();

            bool result = await _boardSystem.TryAddItemAsync(itemData);

            if (!result)
                return;

            _rewardQueue.Dequeue();

            RefreshView();

            if (_mergeBoardFirestore != null)
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);
        }

        private void RefreshView()
        {
            List<ItemData> previewItems = new List<ItemData>(_rewardQueue);

            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                if (i < previewItems.Count && i < PreviewCount)
                    _queueSlotViews[i].SetItem(previewItems[i]);
                else
                    _queueSlotViews[i].SetItem(ItemData.Empty);
            }

            if (_countText != null)
                _countText.text = _rewardQueue.Count.ToString();
        }
    }
}