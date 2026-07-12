using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
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
        [SerializeField] private SpecialItemBoardSystem _specialItemBoardSystem;
        [SerializeField] private MergeBoardRewardQueueSO _mergeBoardFirestore;
        [SerializeField] private List<RewardQueueSlotView> _queueSlotViews = new();
        [SerializeField] private TMP_Text _countText;

        [Header("경고 메시지")]
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private float _alertDuration = 1.5f;

        private Coroutine _alertCoroutine;
        private bool _isProcessing;

        private readonly Queue<ItemData> _rewardQueue = new();

        private const int PreviewCount = 3;

        public bool IsLoaded { get; private set; }

        private void OnEnable()
        {
            ClearAlert();
        }

        private void OnDisable()
        {
            ClearAlert();
        }

        private void Start()
        {
            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                if (_queueSlotViews[i] == null)
                    continue;

                bool isTopSlot = i == 0;
                _queueSlotViews[i].Init(this, isTopSlot);
            }

            ClearAlert();

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

            if (itemData.ItemType != ItemType.Common && itemData.ItemType != ItemType.Special)
            {
                DebugTool.Warning($"보상 큐에는 Common 또는 Special 타입 아이템만 추가할 수 있습니다. Type:{itemData.ItemType}", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);
            Queue<ItemData> backupQueue = CreateQueueSnapshot();
            ItemData runtimeItem = CreateRuntimeItem(itemData);

            if (runtimeItem == null || !runtimeItem.HasItem)
            {
                DebugTool.Warning("보상 큐에 추가할 런타임 아이템 데이터를 만들 수 없습니다.", DebugType.Board, this);
                return false;
            }

            for (int i = 0; i < safeCount; i++)
                _rewardQueue.Enqueue(runtimeItem.Clone());

            RefreshView();

            if (!await SaveQueueSnapshotSafeAsync())
            {
                RestoreQueue(backupQueue);
                DebugTool.Warning($"보상 큐 저장 실패로 아이템 추가를 취소했습니다. ID:{itemData.ItemID}, Count:{safeCount}", DebugType.Board, this);
                return false;
            }

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

            ItemData itemData = _rewardQueue.Peek();

            _isProcessing = true;

            try
            {
                bool moved = false;
                bool movedToSpecialBoard = false;
                ItemSlot addedCommonSlot = null;

                if (itemData.ItemType == ItemType.Special)
                {
                    moved = await TryMoveTopSpecialItemAsync(itemData);
                    movedToSpecialBoard = moved;
                }
                else if (itemData.ItemType == ItemType.Common)
                {
                    addedCommonSlot = await TryMoveTopCommonItemAsync(itemData);
                    moved = addedCommonSlot != null;
                }
                else
                {
                    DebugTool.Warning($"보상 큐에서 처리할 수 없는 아이템 타입입니다. ID:{itemData.ItemID}, Type:{itemData.ItemType}", DebugType.Board, this);
                    ShowAlert("처리할 수 없는 아이템입니다.");
                }

                if (!moved)
                    return;

                ItemData movedItem = _rewardQueue.Dequeue();

                RefreshView();

                if (!await SaveQueueSnapshotSafeAsync())
                {
                    RestoreItemToFront(movedItem);
                    RefreshView();

                    bool rolledBack = await RollbackMovedItemAsync(movedItem, addedCommonSlot, movedToSpecialBoard);
                    if (!rolledBack)
                        DebugTool.Warning($"보상 큐 저장 실패 후 보드 롤백도 실패했습니다. ID:{movedItem.ItemID}", DebugType.Board, this);

                    ShowAlert("저장 실패로 이동을 취소했습니다.");
                    return;
                }

                DebugTool.Log($"보상 큐 Pop 완료 / ID:{itemData.ItemID}, Type:{itemData.ItemType}, 남은 개수: {_rewardQueue.Count}", DebugType.Board, this);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task<ItemSlot> TryMoveTopCommonItemAsync(ItemData itemData)
        {
            if (_boardSystem == null)
                _boardSystem = FindFirstObjectByType<BoardSystem>();

            if (_boardSystem == null)
            {
                DebugTool.Warning("BoardSystem이 연결되지 않았습니다.", DebugType.Board, this);
                ShowAlert("보드가 준비되지 않았습니다.");
                return null;
            }

            ItemSlot addedSlot = await _boardSystem.TryAddItemFromQueueAndSelectAsync(itemData);

            if (addedSlot == null)
            {
                ShowAlert("보드판 공간이 부족합니다.");
                return null;
            }

            return addedSlot;
        }

        private async Task<bool> TryMoveTopSpecialItemAsync(ItemData itemData)
        {
            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

            if (_specialItemBoardSystem == null)
            {
                DebugTool.Warning("SpecialItemBoardSystem이 연결되지 않았습니다.", DebugType.Board, this);
                ShowAlert("특수 아이템 보드가 준비되지 않았습니다.");
                return false;
            }

            if (!_specialItemBoardSystem.IsServerDataLoaded)
                await _specialItemBoardSystem.LoadSpecialBoardFromServerAsync();

            if (!_specialItemBoardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning("특수 아이템 보드 서버 데이터 로드 전에는 아이템을 넣을 수 없습니다.", DebugType.Board, this);
                ShowAlert("특수 아이템 보드 로드 중입니다.");
                return false;
            }

            bool added = await _specialItemBoardSystem.TryAddSpecialItemAsync(itemData, 1);

            if (!added)
            {
                ShowAlert("특수 아이템 슬롯 공간이 부족합니다.");
                return false;
            }

            return true;
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
                FireStoreManager.Instance.TryGetStore(out _mergeBoardFirestore);

            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardRewardQueueSO가 인스펙터 또는 FireStoreManager에 연결되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            if (!_mergeBoardFirestore.IsReady)
                _mergeBoardFirestore.TryEnsureDatabaseReady();

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("MergeBoardRewardQueueSO가 아직 준비되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            DebugTool.Log("MergeBoardRewardQueueSO 연결 완료", DebugType.Board, this);
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

        private async Task<bool> SaveQueueSnapshotSafeAsync()
        {
            try
            {
                if (!ResolveMergeBoardFirestore())
                    return false;

                List<ItemData> snapshot = new List<ItemData>(_rewardQueue);
                await _mergeBoardFirestore.SaveRewardQueueAsync(snapshot);
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"보상 큐 저장 실패 : {exception.Message}", this);
                return false;
            }
        }

        private Queue<ItemData> CreateQueueSnapshot()
        {
            Queue<ItemData> snapshot = new Queue<ItemData>();

            foreach (ItemData itemData in _rewardQueue)
                snapshot.Enqueue(itemData?.Clone() ?? ItemData.Empty);

            return snapshot;
        }

        private void RestoreQueue(Queue<ItemData> snapshot)
        {
            _rewardQueue.Clear();

            if (snapshot != null)
            {
                while (snapshot.Count > 0)
                    _rewardQueue.Enqueue(snapshot.Dequeue());
            }

            RefreshView();
        }

        private void RestoreItemToFront(ItemData itemData)
        {
            Queue<ItemData> restoredQueue = new Queue<ItemData>();

            if (itemData != null && itemData.HasItem)
                restoredQueue.Enqueue(itemData.Clone());

            while (_rewardQueue.Count > 0)
                restoredQueue.Enqueue(_rewardQueue.Dequeue());

            while (restoredQueue.Count > 0)
                _rewardQueue.Enqueue(restoredQueue.Dequeue());
        }

        private async Task<bool> RollbackMovedItemAsync(ItemData itemData, ItemSlot addedCommonSlot, bool movedToSpecialBoard)
        {
            if (itemData == null || !itemData.HasItem)
                return false;

            if (itemData.ItemType == ItemType.Common && _boardSystem != null && addedCommonSlot != null)
                return await _boardSystem.ClearSlotIfContainsAsync(addedCommonSlot.SlotNumber, itemData.ItemID);

            if (itemData.ItemType == ItemType.Special && movedToSpecialBoard)
            {
                if (_specialItemBoardSystem == null)
                    _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

                if (_specialItemBoardSystem == null)
                    return false;

                int consumedCount = await _specialItemBoardSystem.ConsumeItemsByIdAsync(itemData.ItemID, 1);
                return consumedCount == 1;
            }

            return false;
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

            Queue<ItemData> backupQueue = CreateQueueSnapshot();
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

            if (!await SaveQueueSnapshotSafeAsync())
            {
                RestoreQueue(backupQueue);
                return 0;
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

            if (!isActiveAndEnabled)
            {
                ClearAlert();
                DebugTool.Warning(message, DebugType.Board, this);
                return;
            }

            ClearAlert();
            _alertCoroutine = StartCoroutine(AlertRoutine(message));
        }

        public void ClearAlert()
        {
            if (_alertCoroutine != null)
            {
                StopCoroutine(_alertCoroutine);
                _alertCoroutine = null;
            }

            if (_alertText == null)
                return;

            _alertText.text = string.Empty;
            _alertText.gameObject.SetActive(false);
        }

        private IEnumerator AlertRoutine(string message)
        {
            _alertText.text = message;
            _alertText.gameObject.SetActive(true);

            yield return new WaitForSeconds(_alertDuration);

            ClearAlert();
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
