using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumItemBoard -> 7x9 슬롯 그리드, 아이템 배치·선택 처리
    public sealed class NyangQuariumRewardQueue : MonoBehaviour
    {
        private const int PreviewCount = 3;

        private readonly Queue<NyangQuariumBoardItem> _rewardQueue = new();
        private readonly List<NyangQuariumRewardQueueSlot> _slotViews = new();

        private NyangQuariumItemBoard _board;
        private TMP_Text _countText;
        private bool _initialized;
        private bool _isMoving;
        private bool _isRestoring;
        private bool _serverQueueLoaded;
        private System.Action<NyangQuariumBoardItem> _onItemAddedToBoard;

        public void Init(NyangQuariumItemBoard board)
        {
            _board = board;

            if (!_initialized)
            {
                BindViews();
                _initialized = true;
            }

            if (!_serverQueueLoaded)
                _ = RestoreQueueFromServerAsync();

            RefreshView();
        }

        public void SetOnItemAddedToBoard(System.Action<NyangQuariumBoardItem> onItemAddedToBoard)
        {
            _onItemAddedToBoard = onItemAddedToBoard;
        }

        public void EnqueueItem(NyangQuariumBoardItem item)
        {
            if (item == null || !item.HasItem)
                return;

            _rewardQueue.Enqueue(new NyangQuariumBoardItem(item.ItemData));
            RefreshView();
            SaveQueueSnapshot();
        }

        public void TryMoveTopItemToBoard()
        {
            if (_isMoving || _rewardQueue.Count <= 0)
                return;

            if (_board == null)
            {
                return;
            }

            _isMoving = true;

            try
            {
                NyangQuariumBoardItem item = _rewardQueue.Peek();
                if (!_board.TryAddItem(item))
                    return;

                _rewardQueue.Dequeue();
                _onItemAddedToBoard?.Invoke(item);
                RefreshView();
                SaveQueueSnapshot();
            }
            finally
            {
                _isMoving = false;
            }
        }

        private void BindViews()
        {
            _slotViews.Clear();

            AddSlotView("ItemSlot3");
            AddSlotView("ItemSlot2");
            AddSlotView("ItemSlot1");

            if (_countText == null)
                _countText = FindCountText();
        }

        private void AddSlotView(string slotName)
        {
            Transform slotTransform = FindChild(slotName);
            if (slotTransform == null)
                return;

            NyangQuariumRewardQueueSlot slotView = slotTransform.GetComponent<NyangQuariumRewardQueueSlot>();
            if (slotView == null)
                slotView = slotTransform.gameObject.AddComponent<NyangQuariumRewardQueueSlot>();

            slotView.Init(this, _slotViews.Count == 0);
            _slotViews.Add(slotView);
        }

        private Transform FindChild(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }

        private TMP_Text FindCountText()
        {
            Transform countRoot = FindChild("ItemCount");
            if (countRoot != null)
                return countRoot.GetComponentInChildren<TMP_Text>(true);

            TMP_Text directText = GetComponentInChildren<TMP_Text>(true);
            if (directText != null)
                return directText;

            return null;
        }

        private void RefreshView()
        {
            List<NyangQuariumBoardItem> previewItems = new(_rewardQueue);

            for (int i = 0; i < _slotViews.Count; i++)
            {
                if (_slotViews[i] == null)
                    continue;

                if (i < previewItems.Count && i < PreviewCount)
                    _slotViews[i].SetItem(previewItems[i]);
                else
                    _slotViews[i].SetItem(NyangQuariumBoardItem.Empty);
            }

            if (_countText == null)
                return;

            int count = _rewardQueue.Count;
            _countText.text = count.ToString();
            _countText.transform.parent?.gameObject.SetActive(count > 0);
            _countText.gameObject.SetActive(count > 0);
        }

        private async Task RestoreQueueFromServerAsync()
        {
            if (_isRestoring)
                return;

            _isRestoring = true;

            try
            {
                NyangQuariumFirestoreSO store = await NyangQuariumFirestoreSO.WaitForReadyAsync();
                if (store == null || !await store.LoadOrCreateFromServerAsync())
                    return;

                IReadOnlyList<ItemData> savedItems = store.GetMergeQuestRewardQueueItems();
                _rewardQueue.Clear();
                _serverQueueLoaded = true;

                if (savedItems != null)
                {
                    for (int i = 0; i < savedItems.Count; i++)
                    {
                        ItemData itemData = savedItems[i];
                        if (itemData == null || !itemData.HasItem)
                            continue;

                        _rewardQueue.Enqueue(new NyangQuariumBoardItem(itemData));
                    }
                }

                RefreshView();
            }
            finally
            {
                _isRestoring = false;
            }
        }

        private async void SaveQueueSnapshot()
        {
            if (_isRestoring)
                return;

            NyangQuariumFirestoreSO store = await NyangQuariumFirestoreSO.WaitForReadyAsync();
            if (store == null)
                return;

            List<ItemData> items = new();
            foreach (NyangQuariumBoardItem item in _rewardQueue)
            {
                if (item == null || !item.HasItem)
                    continue;

                items.Add(item.ItemData?.Clone() ?? ItemData.Empty);
            }

            await store.SaveMergeQuestRewardQueueAsync(items);
        }
    }
}
