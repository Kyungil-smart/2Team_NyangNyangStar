using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UI.MergeBoard
{
    public class BoardRewardQueue : MonoBehaviour
    {
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private MergeBoardFirestoreSo _mergeBoardFirestore;
        [SerializeField] private List<RewardQueueSlotView> _queueSlotViews = new();
        [SerializeField] private TMP_Text _countText;

        [Header("경고 메시지")]
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private float _alertDuration = 1.5f;

        private Coroutine _alertCoroutine;
        private bool _isProcessing;
        private bool _isSavingQueue;
        private bool _queueSaveRequested;

        private readonly Queue<ItemData> _rewardQueue = new();

        private const int PreviewCount = 3;

        public bool IsLoaded { get; private set; }

        private void Start()
        {
            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                if (_queueSlotViews[i] == null)
                    continue;

                bool isTopSlot = i == 0;
                _queueSlotViews[i].Init(this, isTopSlot);
            }

            if (_alertText != null)
                _alertText.gameObject.SetActive(false);

            RefreshView();

            if (MergeBoardItemService.Instance != null)
                MergeBoardItemService.Instance.RegisterRewardQueue(this);
        }

        public Task<bool> EnqueueItemAsync(ItemData itemData)
        {
            return EnqueueItemAsync(itemData, 1);
        }

        public async Task<bool> EnqueueItemAsync(ItemData itemData, int count)
        {
            if (!IsLoaded)
            {
                DebugTool.Warning("보상 큐 서버 데이터 로드 전에는 아이템을 추가할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            if (itemData == null || !itemData.HasItem)
                return false;

            if (itemData.ItemType != Services.Enums.ItemType.Common)
            {
                DebugTool.Warning("보상 큐에는 Common 타입 아이템만 추가할 수 있습니다.", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);

            ItemData runtimeItem = CreateRuntimeItem(itemData);

            if (runtimeItem == null || !runtimeItem.HasItem)
            {
                DebugTool.Warning("보상 큐에 추가할 런타임 아이템 데이터를 만들 수 없습니다.", DebugType.Board, this);
                return false;
            }

            for (int i = 0; i < safeCount; i++)
                _rewardQueue.Enqueue(runtimeItem.Clone());

            RefreshView();

            if (ResolveMergeBoardFirestore())
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);

            return true;
        }

        public async void TryMoveTopItemToBoard()
        {
            if (_isProcessing)
                return;

            if (!IsLoaded)
            {
                DebugTool.Warning("보상 큐 서버 데이터 로드 전에는 아이템을 꺼낼 수 없습니다.", DebugType.Board, this);
                return;
            }

            if (_rewardQueue.Count <= 0)
                return;

            if (_boardSystem == null)
            {
                DebugTool.Warning("BoardSystem이 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            ItemData itemData = _rewardQueue.Peek();

            _isProcessing = true;

            try
            {
                ItemSlot addedSlot = await _boardSystem.TryAddItemFromQueueAndSelectAsync(itemData);

                if (addedSlot == null)
                {
                    ShowAlert("보드판 공간이 부족합니다.");
                    return;
                }

                _rewardQueue.Dequeue();

                RefreshView();
                RequestSaveQueue();

                DebugTool.Log($"보상 큐 Pop 완료 / 남은 개수: {_rewardQueue.Count}", DebugType.Board, this);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public async Task LoadQueueFromServerAsync(bool normalizeDocumentIds = false)
        {
            IsLoaded = false;

            if (!ResolveMergeBoardFirestore())
                return;

            List<ItemData> loadedItems = await _mergeBoardFirestore.LoadRewardQueueAsync();

            _rewardQueue.Clear();

            for (int i = 0; i < loadedItems.Count; i++)
            {
                ItemData itemData = loadedItems[i];

                if (itemData == null || !itemData.HasItem)
                    continue;

                ItemData runtimeItem = CreateRuntimeItem(itemData);

                if (runtimeItem == null || !runtimeItem.HasItem)
                    continue;

                _rewardQueue.Enqueue(runtimeItem);
            }

            RefreshView();

            if (normalizeDocumentIds)
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);

            IsLoaded = true;
            DebugTool.Log("보상 큐 서버 데이터 로드 완료", DebugType.Board, this);
        }

        private bool ResolveMergeBoardFirestore()
        {
            if (_mergeBoardFirestore != null && _mergeBoardFirestore.IsReady)
                return true;

            if (FireStoreManager.Instance == null)
            {
                DebugTool.Warning("FireStoreManager.Instance가 없습니다.", DebugType.Board, this);
                return false;
            }

            if (!FireStoreManager.Instance.IsInitialized)
            {
                DebugTool.Warning("FireStoreManager 초기화가 완료되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 인스펙터에 연결되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 아직 준비되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            DebugTool.Log("MergeBoardFirestoreSO 연결 완료", DebugType.Board, this);
            return true;
        }

        private ItemData CreateRuntimeItem(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
                return ItemData.Empty;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryCreateMergeBoardRuntimeItem(itemData, out ItemData runtimeData))
            {
                return runtimeData;
            }

            return itemData.Clone();
        }

        private void RequestSaveQueue()
        {
            _queueSaveRequested = true;

            if (_isSavingQueue)
                return;

            _ = SaveQueueLoopAsync();
        }

        private async Task SaveQueueLoopAsync()
        {
            _isSavingQueue = true;

            try
            {
                while (_queueSaveRequested)
                {
                    _queueSaveRequested = false;

                    if (!ResolveMergeBoardFirestore())
                        continue;

                    List<ItemData> snapshot = new List<ItemData>(_rewardQueue);
                    await _mergeBoardFirestore.SaveRewardQueueAsync(snapshot);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"보상 큐 저장 실패 : {exception.Message}", this);
            }
            finally
            {
                _isSavingQueue = false;
            }
        }


        public int GetItemCountById(int itemID)
        {
            if (itemID <= 0)
                return 0;

            int count = 0;

            foreach (ItemData itemData in _rewardQueue)
            {
                if (itemData != null && itemData.HasItem && itemData.ItemID == itemID)
                    count++;
            }

            return count;
        }

        public async Task<int> ConsumeItemsByIdAsync(int itemID, int count = 1)
        {
            if (!IsLoaded)
            {
                DebugTool.Warning("보상 큐 서버 데이터 로드 전에는 아이템을 소비할 수 없습니다.", DebugType.Board, this);
                return 0;
            }

            if (itemID <= 0)
                return 0;

            int safeCount = Mathf.Max(1, count);
            int availableCount = GetItemCountById(itemID);

            if (availableCount < safeCount)
            {
                DebugTool.Warning($"보상 큐에 소비할 아이템 수량이 부족합니다. ID:{itemID}, 필요:{safeCount}, 보유:{availableCount}", DebugType.Board, this);
                return 0;
            }

            Queue<ItemData> newQueue = new Queue<ItemData>();
            int consumedCount = 0;

            while (_rewardQueue.Count > 0)
            {
                ItemData itemData = _rewardQueue.Dequeue();

                if (consumedCount < safeCount && itemData != null && itemData.HasItem && itemData.ItemID == itemID)
                {
                    consumedCount++;
                    continue;
                }

                newQueue.Enqueue(itemData);
            }

            while (newQueue.Count > 0)
                _rewardQueue.Enqueue(newQueue.Dequeue());

            RefreshView();

            if (ResolveMergeBoardFirestore())
            {
                List<ItemData> snapshot = new List<ItemData>(_rewardQueue);
                await _mergeBoardFirestore.SaveRewardQueueAsync(snapshot);
            }

            DebugTool.Log($"보상 큐 아이템 소비 완료 / ID:{itemID}, Count:{consumedCount}", DebugType.Board, this);
            return consumedCount;
        }

        public void ShowAlert(string message)
        {
            if (_alertText == null)
            {
                DebugTool.Warning(message, DebugType.Board, this);
                return;
            }

            if (_alertCoroutine != null)
                StopCoroutine(_alertCoroutine);

            _alertCoroutine = StartCoroutine(AlertRoutine(message));
        }

        private IEnumerator AlertRoutine(string message)
        {
            _alertText.text = message;
            _alertText.gameObject.SetActive(true);

            yield return new WaitForSeconds(_alertDuration);

            _alertText.gameObject.SetActive(false);
            _alertCoroutine = null;
        }

        private void RefreshView()
        {
            List<ItemData> previewItems = new List<ItemData>(_rewardQueue);

            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                if (_queueSlotViews[i] == null)
                    continue;

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
