using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UI.FindMoongchi;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class MergeBoardItemService : MonoBehaviour
    {
        public static MergeBoardItemService Instance { get; private set; }

        [Header("보드 시스템")]
        [SerializeField] private BoardSystem _boardSystem;

        [Header("특수 아이템 보드")]
        [SerializeField] private SpecialItemBoardSystem _specialItemBoardSystem;

        [Header("보상 큐")]
        [SerializeField] private BoardRewardQueue _rewardQueue;

        [Header("아이템 데이터베이스")]
        [SerializeField] private ItemDatabaseSo _itemDatabase;

        [Header("머지보드 Firestore")]
        [SerializeField] private MergeBoardSlotsSO _boardSlotsStore;
        [SerializeField] private MergeBoardRewardQueueSO _rewardQueueStore;
        [SerializeField] private MergeBoardSpecialSO _specialStore;

        [Header("테스트 아이템 생성")]
        [SerializeField] private TMP_InputField _itemIdInputField;
        [SerializeField] private Button _testReceiveButton;

        [Header("Resource Cost")]
        [Min(0)]
        [SerializeField] private int _generateItemEnergyCost = 5;

        [Header("Runtime Wait")]
        [Min(0.1f)]
        [SerializeField] private float _runtimeDataWaitTimeoutSeconds = 5f;

        [Header("Runtime Diagnostics")]
        [SerializeField] private bool _enableRuntimeDiagnostics = true;

        private const int GeneralBoardSlotCount = 63;

        private readonly SemaphoreSlim _addItemSemaphore = new(1, 1);
        private readonly Dictionary<int, int> _serverItemCountCache = new();

        private bool _isAddingItem;
        private bool _isConsumingItem;
        private bool _hasServerItemCountCache;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
            RuntimeLog("Awake 완료");
        }

        protected virtual void OnEnable()
        {
            BindTestReceiveButton(logMissing: false);
        }

        protected virtual void Start()
        {
            BindTestReceiveButton(logMissing: true);
        }

        protected virtual void OnDisable()
        {
            UnbindTestReceiveButton();
        }

        private void BindTestReceiveButton(bool logMissing)
        {
            ResolveTestReceiveButton();

            if (_testReceiveButton != null)
            {
                _testReceiveButton.onClick.RemoveListener(ReceiveRandomTestItem);
                _testReceiveButton.onClick.AddListener(ReceiveRandomTestItem);

                if (logMissing)
                    RuntimeLog($"아이템 생성 버튼 바인딩 완료: {_testReceiveButton.name}");
            }
            else
            {
                if (logMissing)
                {
                    DebugTool.Warning("테스트 아이템 생성 버튼이 연결되지 않았습니다.", DebugType.Board, this);
                    RuntimeWarning("테스트 아이템 생성 버튼이 연결되지 않았습니다. 인스펙터의 Test Receive Button 연결을 확인하세요.");
                }
            }
        }

        private void UnbindTestReceiveButton()
        {
            if (_testReceiveButton != null)
                _testReceiveButton.onClick.RemoveListener(ReceiveRandomTestItem);
        }

        public void RegisterBoardSystem(BoardSystem boardSystem)
        {
            _boardSystem = boardSystem;
        }

        public void RegisterSpecialItemBoardSystem(SpecialItemBoardSystem specialItemBoardSystem)
        {
            _specialItemBoardSystem = specialItemBoardSystem;
        }

        public void RegisterRewardQueue(BoardRewardQueue rewardQueue)
        {
            _rewardQueue = rewardQueue;
        }

        public void RegisterItemDatabase(ItemDatabaseSo itemDatabase)
        {
            _itemDatabase = itemDatabase;
        }

        public void AddItemById(int itemID, int count = 1)
        {
            _ = AddItemByIdAsync(itemID, count);
        }

        public async Task<bool> AddItemByIdAsync(int itemID, int count = 1)
        {
            if (itemID <= 0)
            {
                DebugTool.Warning($"유효하지 않은 아이템 ID입니다. ID: {itemID}", DebugType.Board, this);
                return false;
            }

            if (!TryGetItemDataById(itemID, out ItemData itemData))
            {
                DebugTool.Warning($"{itemID} ID에 해당하는 아이템 데이터를 찾을 수 없습니다.", DebugType.Board, this);
                return false;
            }

            return await AddItemQueuedAsync(itemData, count);
        }

        public void AddItem(ItemData itemData, int count = 1)
        {
            _ = AddItemQueuedAsync(itemData, count);
        }

        public async Task<bool> AddItemAsync(ItemData itemData, int count = 1)
        {
            if (itemData == null || !itemData.HasItem)
            {
                DebugTool.Warning("유효하지 않은 아이템 데이터입니다.", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);

            if (itemData.ItemType == ItemType.Special)
                return await AddSpecialItemAsync(itemData, safeCount);

            if (itemData.ItemType != ItemType.Common)
            {
                DebugTool.Warning($"지원하지 않는 아이템 타입입니다. Type: {itemData.ItemType}", DebugType.Board, this);
                return false;
            }

            return await AddCommonItemAsync(itemData, safeCount);
        }

        public void ConsumeItemById(int itemID, int count = 1)
        {
            _ = ConsumeItemByIdAsync(itemID, count);
        }

        public async Task<bool> ConsumeItemByIdAsync(int itemID, int count = 1)
        {
            if (_isConsumingItem)
                return false;

            if (itemID <= 0)
            {
                DebugTool.Warning($"유효하지 않은 소비 아이템 ID입니다. ID: {itemID}", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);

            if (!TryGetItemDataById(itemID, out ItemData itemData))
            {
                DebugTool.Warning($"소비할 아이템 데이터를 찾을 수 없습니다. ID: {itemID}", DebugType.Board, this);
                return false;
            }

            _isConsumingItem = true;

            try
            {
                bool result = itemData.ItemType == ItemType.Special
                    ? await ConsumeSpecialItemAsync(itemID, safeCount)
                    : await ConsumeCommonItemAsync(itemID, safeCount);

                if (result)
                    DebugTool.Log($"아이템 소비 완료 / ID:{itemID}, Count:{safeCount}", DebugType.Board, this);

                return result;
            }
            finally
            {
                _isConsumingItem = false;
            }
        }

        public int GetOwnedItemCount(int itemID)
        {
            if (itemID <= 0)
                return 0;

            ResolveReferences();

            int count = 0;
            bool hasLoadedRuntimeInventory = false;

            if (_boardSystem != null && _boardSystem.IsServerDataLoaded)
            {
                count += _boardSystem.GetItemCountById(itemID);
                hasLoadedRuntimeInventory = true;
            }

            if (_rewardQueue != null && _rewardQueue.IsLoaded)
            {
                count += _rewardQueue.GetItemCountById(itemID);
                hasLoadedRuntimeInventory = true;
            }

            if (_specialItemBoardSystem != null && _specialItemBoardSystem.IsServerDataLoaded)
            {
                count += _specialItemBoardSystem.GetItemCountById(itemID);
                hasLoadedRuntimeInventory = true;
            }

            if (!hasLoadedRuntimeInventory && _hasServerItemCountCache &&
                _serverItemCountCache.TryGetValue(itemID, out int cachedCount))
            {
                count = cachedCount;
            }

            return count;
        }

        public async Task EnsureInventoryLoadedAsync()
        {
            ResolveReferences();

            if (_boardSystem != null && !_boardSystem.IsServerDataLoaded)
                await _boardSystem.LoadBoardFromServerAsync();

            if (_rewardQueue != null && !_rewardQueue.IsLoaded)
                await _rewardQueue.LoadQueueFromServerAsync();

            if (_specialItemBoardSystem != null && !_specialItemBoardSystem.IsServerDataLoaded)
                await _specialItemBoardSystem.LoadSpecialBoardFromServerAsync();

            await RefreshServerItemCountCacheAsync();
        }

        public void ReceiveItemById(int itemID)
        {
            AddItemById(itemID, 1);
        }

        public void ReceiveItemById(int itemID, int count)
        {
            AddItemById(itemID, count);
        }

        public async void ReceiveItem(ItemData itemData)
        {
            await AddItemQueuedAsync(itemData, 1);
        }

        public async void ReceiveItem(ItemData itemData, int count)
        {
            await AddItemQueuedAsync(itemData, count);
        }

        public void ReceiveRandomTestItem()
        {
            _ = ReceiveRandomTestItemAsync();
        }

        public async Task<bool> ReceiveRandomTestItemAsync()
        {
            RuntimeLog("아이템 생성 버튼 클릭됨");
            DebugTool.Log("아이템 생성 버튼 클릭됨", DebugType.Board, this);

            if (!CanUseItemService())
                return false;

            if (TryReadInputItemID(out int itemID, out bool hasInput))
            {
                if (!TryGetItemDataById(itemID, out ItemData inputItemData))
                {
                    DebugTool.Warning($"{itemID} ID에 해당하는 아이템 데이터를 찾을 수 없습니다.", DebugType.Board, this);
                    RuntimeWarning($"{itemID} ID에 해당하는 아이템 데이터를 찾을 수 없습니다.");
                    return false;
                }

                return await GenerateItemByEnergyAsync(inputItemData);
            }

            if (hasInput)
                return false;

            if (!TryGetRandomCommonItem(out ItemData itemData))
            {
                DebugTool.Warning("생성 가능한 Common 아이템 데이터가 없습니다.", DebugType.Board, this);
                RuntimeWarning("생성 가능한 Common 아이템 데이터가 없습니다.");
                return false;
            }

            return await GenerateItemByEnergyAsync(itemData);
        }

        public async Task<bool> GenerateItemByEnergyAsync(ItemData itemData, int count = 1)
        {
            if (_isAddingItem)
            {
                RuntimeWarning("이미 아이템 생성 처리 중입니다.");
                return false;
            }

            if (itemData == null || !itemData.HasItem)
            {
                DebugTool.Warning("유효하지 않은 아이템 데이터입니다.", DebugType.Board, this);
                ShowBoardAlert("유효하지 않은 아이템입니다.");
                return false;
            }

            int safeCount = Mathf.Max(1, count);
            int energyCost = Mathf.Max(0, _generateItemEnergyCost) * safeCount;

            _isAddingItem = true;

            await _addItemSemaphore.WaitAsync();

            try
            {
                RuntimeLog($"아이템 생성 처리 시작 / ID:{itemData.ItemID}, Count:{safeCount}, EnergyCost:{energyCost}, Type:{itemData.ItemType}");

                if (!CanUseItemService())
                    return false;

                if (!await WaitForTargetStoreReadyAsync(itemData))
                    return false;

                if (!await TrySpendGenerateEnergyAsync(energyCost))
                    return false;

                bool added;

                try
                {
                    added = await AddItemAsync(itemData, safeCount);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception, this);
                    RuntimeWarning($"아이템 추가 중 예외가 발생해 에너지를 환불합니다. ID:{itemData.ItemID}, Refund:{energyCost}");
                    await RefundGenerateEnergyAsync(energyCost);
                    return false;
                }

                if (!added)
                {
                    RuntimeWarning($"아이템 추가 실패로 에너지 환불 처리 / ID:{itemData.ItemID}, Count:{safeCount}, Refund:{energyCost}");
                    await RefundGenerateEnergyAsync(energyCost);
                    return false;
                }

                try
                {
                    await NotifyFindMoongchiEnergySpentAsync(energyCost);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception, this);
                    RuntimeWarning("아이템 생성은 완료됐지만 FindMoongchi 에너지 사용 진행도 갱신에 실패했습니다.");
                }

                RuntimeLog($"아이템 생성 완료 / ID:{itemData.ItemID}, Count:{safeCount}");
                return true;
            }
            finally
            {
                _addItemSemaphore.Release();
                _isAddingItem = false;
            }
        }

        private async Task<bool> AddItemQueuedAsync(ItemData itemData, int count)
        {
            await _addItemSemaphore.WaitAsync();

            try
            {
                return await AddItemAsync(itemData, count);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }
            finally
            {
                _addItemSemaphore.Release();
            }
        }

        private async Task<bool> AddCommonItemAsync(ItemData itemData, int count)
        {
            ResolveReferences();

            if (_rewardQueue != null)
            {
                if (!_rewardQueue.IsLoaded && !await WaitForRewardQueueLoadedAsync())
                    return false;

                if (_rewardQueue.IsLoaded)
                {
                    bool queued = await _rewardQueue.EnqueueItemAsync(itemData, count);

                    if (queued)
                        RuntimeLog($"보상 큐 추가 완료 / ID:{itemData.ItemID}, Count:{count}");
                    else
                        RuntimeWarning($"보상 큐 추가 실패 / ID:{itemData.ItemID}, Count:{count}");

                    return queued;
                }
            }

            if (!TryResolveStores())
                return false;

            List<ItemData> queueItems = await _rewardQueueStore.LoadRewardQueueAsync();
            ItemData runtimeItem = CreateRuntimeItem(itemData);

            for (int i = 0; i < count; i++)
                queueItems.Add(runtimeItem.Clone());

            await _rewardQueueStore.SaveRewardQueueAsync(queueItems);
            DebugTool.Log($"보상 큐 직접 저장 완료 / ID:{itemData.ItemID}, Count:{count}", DebugType.Board, this);
            RuntimeLog($"보상 큐 직접 저장 완료 / ID:{itemData.ItemID}, Count:{count}");
            return true;
        }

        private async Task<bool> AddSpecialItemAsync(ItemData itemData, int count)
        {
            ResolveReferences();

            if (_specialItemBoardSystem != null)
            {
                if (!_specialItemBoardSystem.IsServerDataLoaded && !await WaitForSpecialBoardLoadedAsync())
                    return false;

                if (_specialItemBoardSystem.IsServerDataLoaded)
                    return await _specialItemBoardSystem.TryAddSpecialItemAsync(itemData, count);
            }

            if (!TryResolveStores())
                return false;

            Dictionary<int, SpecialItemSlotData> specialBoard = await _specialStore.LoadSpecialBoardAsync();
            int slotNumber = FindSpecialSlotNumberForItem(specialBoard, itemData.ItemID);

            if (slotNumber == -1)
            {
                DebugTool.Warning("특수 아이템 보드 공간이 부족합니다.", DebugType.Board, this);
                RuntimeWarning("특수 아이템 보드 공간이 부족합니다.");
                return false;
            }

            SpecialItemSlotData currentData = specialBoard[slotNumber];
            int newCount = currentData.HasItem ? currentData.Count + count : count;
            SpecialItemSlotData newSlotData = new SpecialItemSlotData(slotNumber, CreateRuntimeItem(itemData), newCount);

            specialBoard[slotNumber] = newSlotData;
            await _specialStore.SaveSpecialSlotAsync(slotNumber, newSlotData);
            DebugTool.Log($"특수 아이템 직접 저장 완료 / ID:{itemData.ItemID}, Count:{count}", DebugType.Board, this);
            RuntimeLog($"특수 아이템 직접 저장 완료 / ID:{itemData.ItemID}, Count:{count}");
            return true;
        }

        private async Task<bool> WaitForTargetStoreReadyAsync(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
                return false;

            if (itemData.ItemType == ItemType.Common)
                return await WaitForRewardQueueLoadedAsync();

            if (itemData.ItemType == ItemType.Special)
                return await WaitForSpecialBoardLoadedAsync();

            return true;
        }

        private async Task<bool> WaitForRewardQueueLoadedAsync()
        {
            ResolveReferences();

            if (_rewardQueue == null || _rewardQueue.IsLoaded)
                return true;

            if (TryResolveStores())
            {
                RuntimeLog("보상 큐가 미로드 상태라 즉시 서버 로드를 시도합니다.");
                await _rewardQueue.LoadQueueFromServerAsync();

                if (_rewardQueue == null || _rewardQueue.IsLoaded)
                    return true;
            }

            float startTime = Time.realtimeSinceStartup;
            float timeout = Mathf.Max(0.1f, _runtimeDataWaitTimeoutSeconds);

            while (_rewardQueue != null && !_rewardQueue.IsLoaded && Time.realtimeSinceStartup - startTime < timeout)
            {
                await Task.Yield();
                ResolveReferences();
            }

            if (_rewardQueue == null || _rewardQueue.IsLoaded)
                return true;

            DebugTool.Warning("보상 큐 서버 데이터 로드 전에는 아이템을 생성할 수 없습니다.", DebugType.Board, this);
            RuntimeWarning("보상 큐 서버 데이터 로드 전에는 아이템을 생성할 수 없습니다.");
            ShowBoardAlert("보상 큐 로드 중입니다.");
            return false;
        }

        private async Task<bool> WaitForSpecialBoardLoadedAsync()
        {
            ResolveReferences();

            if (_specialItemBoardSystem == null || _specialItemBoardSystem.IsServerDataLoaded)
                return true;

            if (TryResolveStores())
            {
                RuntimeLog("특수 아이템 보드가 미로드 상태라 즉시 서버 로드를 시도합니다.");
                await _specialItemBoardSystem.LoadSpecialBoardFromServerAsync();

                if (_specialItemBoardSystem == null || _specialItemBoardSystem.IsServerDataLoaded)
                    return true;
            }

            float startTime = Time.realtimeSinceStartup;
            float timeout = Mathf.Max(0.1f, _runtimeDataWaitTimeoutSeconds);

            while (_specialItemBoardSystem != null && !_specialItemBoardSystem.IsServerDataLoaded && Time.realtimeSinceStartup - startTime < timeout)
            {
                await Task.Yield();
                ResolveReferences();
            }

            if (_specialItemBoardSystem == null || _specialItemBoardSystem.IsServerDataLoaded)
                return true;

            DebugTool.Warning("특수 아이템 보드 서버 데이터 로드 전에는 아이템을 생성할 수 없습니다.", DebugType.Board, this);
            RuntimeWarning("특수 아이템 보드 서버 데이터 로드 전에는 아이템을 생성할 수 없습니다.");
            ShowBoardAlert("보드 데이터 로드 중입니다.");
            return false;
        }

        private async Task<bool> TrySpendGenerateEnergyAsync(int energyCost)
        {
            if (energyCost <= 0)
                return true;

            bool spent = await PlayerResourceManager.Instance.TrySpendEnergyAsync(energyCost);

            if (!spent)
            {
                DebugTool.Warning(
                    $"아이템 생성에 필요한 에너지가 부족합니다. 필요:{energyCost}, 보유:{PlayerResourceManager.Instance.Energy}",
                    DebugType.Board,
                    this);
                RuntimeWarning($"아이템 생성에 필요한 에너지가 부족합니다. 필요:{energyCost}, 보유:{PlayerResourceManager.Instance.Energy}");
                ShowBoardAlert("에너지가 부족합니다.");
            }
            else
            {
                RuntimeLog($"아이템 생성 에너지 소비 완료 / Cost:{energyCost}, Remain:{PlayerResourceManager.Instance.Energy}");
            }

            return spent;
        }

        private static Task<bool> RefundGenerateEnergyAsync(int energyCost)
        {
            if (energyCost <= 0)
                return Task.FromResult(true);

            return PlayerResourceManager.Instance.AddEnergyAsync(
                energyCost,
                refreshFromServer: false,
                trackTotal: false);
        }

        private async Task NotifyFindMoongchiEnergySpentAsync(int energyCost)
        {
            if (energyCost <= 0)
                return;

            FindMoongchiProgressController progressController = ResolveFindMoongchiProgressController();

            if (progressController == null)
            {
                DebugTool.Warning("FindMoongchiProgressController를 찾지 못해 에너지 사용 진행도를 갱신하지 못했습니다.", DebugType.FindMoongchi, this);
                return;
            }

            if (!progressController.IsProgressReady && !await progressController.EnsureLoadedAsync())
            {
                DebugTool.Warning("뭉치를 찾아라 진행 데이터가 준비되지 않아 에너지 사용 진행도를 갱신하지 못했습니다.", DebugType.FindMoongchi, this);
                return;
            }

            await progressController.NotifyEnergySpentAsync(energyCost);
        }

        private static FindMoongchiProgressController ResolveFindMoongchiProgressController()
        {
            FindMoongchiProgressController activeController = FindFirstObjectByType<FindMoongchiProgressController>();

            if (activeController != null)
                return activeController;

            FindMoongchiProgressController[] controllers = Resources.FindObjectsOfTypeAll<FindMoongchiProgressController>();

            for (int i = 0; i < controllers.Length; i++)
            {
                FindMoongchiProgressController controller = controllers[i];

                if (controller != null && controller.gameObject.scene.IsValid())
                    return controller;
            }

            return null;
        }

        private async Task<bool> ConsumeCommonItemAsync(int itemID, int count)
        {
            ResolveReferences();

            if (_boardSystem != null && _boardSystem.IsServerDataLoaded &&
                _rewardQueue != null && _rewardQueue.IsLoaded)
            {
                int availableCount = _boardSystem.GetItemCountById(itemID) + _rewardQueue.GetItemCountById(itemID);

                if (availableCount < count)
                {
                    DebugTool.Warning($"소비할 Common 아이템 수량이 부족합니다. ID:{itemID}, 필요:{count}, 보유:{availableCount}", DebugType.Board, this);
                    return false;
                }

                int remainingCount = count;
                int consumedFromBoard = await _boardSystem.ConsumeItemsByIdAsync(itemID, remainingCount);
                remainingCount -= consumedFromBoard;

                if (remainingCount > 0)
                {
                    int consumedFromQueue = await _rewardQueue.ConsumeItemsByIdAsync(itemID, remainingCount);
                    remainingCount -= consumedFromQueue;
                }

                return remainingCount <= 0;
            }

            if (!TryResolveStores())
                return false;

            Dictionary<int, ItemData> boardData = await _boardSlotsStore.LoadBoardAsync();
            List<ItemData> queueItems = await _rewardQueueStore.LoadRewardQueueAsync();

            int boardCount = CountBoardItems(boardData, itemID);
            int queueCount = CountListItems(queueItems, itemID);
            int available = boardCount + queueCount;

            if (available < count)
            {
                DebugTool.Warning($"소비할 Common 아이템 수량이 부족합니다. ID:{itemID}, 필요:{count}, 보유:{available}", DebugType.Board, this);
                return false;
            }

            int remaining = count;
            Dictionary<int, ItemData> changedSlots = new Dictionary<int, ItemData>();

            for (int slotNumber = 1; slotNumber <= GeneralBoardSlotCount && remaining > 0; slotNumber++)
            {
                if (!boardData.TryGetValue(slotNumber, out ItemData slotItem))
                    continue;

                if (slotItem == null || !slotItem.HasItem || slotItem.ItemID != itemID)
                    continue;

                boardData[slotNumber] = ItemData.Empty;
                changedSlots[slotNumber] = ItemData.Empty;
                remaining--;
            }

            if (remaining > 0)
                remaining = RemoveItemsFromList(queueItems, itemID, remaining);

            if (changedSlots.Count > 0)
                await _boardSlotsStore.SaveSlotsAsync(changedSlots);

            if (queueCount > 0)
                await _rewardQueueStore.SaveRewardQueueAsync(queueItems);

            return remaining <= 0;
        }

        private async Task<bool> ConsumeSpecialItemAsync(int itemID, int count)
        {
            ResolveReferences();

            if (_specialItemBoardSystem != null && _specialItemBoardSystem.IsServerDataLoaded)
                return await _specialItemBoardSystem.ConsumeItemsByIdAsync(itemID, count) == count;

            if (!TryResolveStores())
                return false;

            Dictionary<int, SpecialItemSlotData> specialBoard = await _specialStore.LoadSpecialBoardAsync();
            int slotNumber = FindSpecialSlotNumberForItem(specialBoard, itemID, requireExisting: true);

            if (slotNumber == -1)
            {
                DebugTool.Warning($"소비할 특수 아이템을 찾을 수 없습니다. ID:{itemID}", DebugType.Board, this);
                return false;
            }

            SpecialItemSlotData currentData = specialBoard[slotNumber];

            if (currentData.Count < count)
            {
                DebugTool.Warning($"소비할 특수 아이템 수량이 부족합니다. ID:{itemID}, 필요:{count}, 보유:{currentData.Count}", DebugType.Board, this);
                return false;
            }

            int newCount = currentData.Count - count;
            SpecialItemSlotData newSlotData = newCount <= 0
                ? SpecialItemSlotData.Empty(slotNumber)
                : new SpecialItemSlotData(slotNumber, CreateRuntimeItem(currentData.ItemData), newCount);

            await _specialStore.SaveSpecialSlotAsync(slotNumber, newSlotData);
            return true;
        }

        private bool CanUseItemService()
        {
            ResolveReferences();

            if (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
            {
                DebugTool.Warning("Firestore 초기화가 완료되지 않았습니다. 로그인 후 다시 시도하세요.", DebugType.Board, this);
                RuntimeWarning("Firestore 초기화가 완료되지 않았습니다. 로그인 후 다시 시도하세요.");
                ShowBoardAlert("서버 데이터 로드 중입니다.");
                return false;
            }

            if (LocalDataAccess.Instance?.Game == null || !LocalDataAccess.Instance.Game.IsReady)
            {
                DebugTool.Warning("아이템 데이터 로드 완료 전입니다.", DebugType.Board, this);
                RuntimeWarning("아이템 데이터 로드 완료 전입니다.");
                ShowBoardAlert("아이템 데이터 로드 중입니다.");
                return false;
            }

            return true;
        }

        private void ResolveReferences()
        {
            if (_boardSystem == null)
                _boardSystem = FindFirstObjectByType<BoardSystem>();

            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

            if (_rewardQueue == null)
                _rewardQueue = FindFirstObjectByType<BoardRewardQueue>();
        }

        private void ResolveTestReceiveButton()
        {
            if (_testReceiveButton != null)
                return;

            Button[] childButtons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < childButtons.Length; i++)
            {
                Button button = childButtons[i];

                if (button == null)
                    continue;

                string buttonName = button.name.ToLowerInvariant();

                if (buttonName.Contains("item") &&
                    (buttonName.Contains("create") || buttonName.Contains("generate") || buttonName.Contains("receive") || buttonName.Contains("test")))
                {
                    _testReceiveButton = button;
                    return;
                }

                if (button.name.Contains("아이템") && button.name.Contains("생성"))
                {
                    _testReceiveButton = button;
                    return;
                }
            }
        }

        private bool TryResolveStores()
        {
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

            if (_boardSlotsStore == null)
                FireStoreManager.Instance.TryGetStore(out _boardSlotsStore);

            if (_rewardQueueStore == null)
                FireStoreManager.Instance.TryGetStore(out _rewardQueueStore);

            if (_specialStore == null)
                FireStoreManager.Instance.TryGetStore(out _specialStore);

            if (_boardSlotsStore == null || _rewardQueueStore == null || _specialStore == null)
            {
                DebugTool.Warning("머지보드 Store SO가 인스펙터 또는 FireStoreManager에 연결되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            if (!_boardSlotsStore.IsReady)
                _boardSlotsStore.TryEnsureDatabaseReady();

            if (!_rewardQueueStore.IsReady)
                _rewardQueueStore.TryEnsureDatabaseReady();

            if (!_specialStore.IsReady)
                _specialStore.TryEnsureDatabaseReady();

            if (!_boardSlotsStore.IsReady || !_rewardQueueStore.IsReady || !_specialStore.IsReady)
            {
                DebugTool.Warning("머지보드 Store SO가 아직 준비되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            return true;
        }

        private bool TryReadInputItemID(out int itemID, out bool hasInput)
        {
            itemID = 0;
            hasInput = false;

            if (_itemIdInputField == null)
                return false;

            string input = _itemIdInputField.text?.Trim();

            if (string.IsNullOrEmpty(input))
                return false;

            hasInput = true;

            if (!int.TryParse(input, out itemID) || itemID <= 0)
            {
                DebugTool.Warning($"아이템 ID 입력값이 올바르지 않습니다. 입력값: {input}", DebugType.Board, this);
                return false;
            }

            return true;
        }

        private bool TryGetRandomCommonItem(out ItemData itemData)
        {
            itemData = null;

            if (_itemDatabase != null && _itemDatabase.TryGetRandomItem(ItemType.Common, out itemData))
                return true;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetRandomMergeBoardItem(ItemType.Common, out itemData))
            {
                return true;
            }

            return false;
        }

        private bool TryGetItemDataById(int itemID, out ItemData itemData)
        {
            itemData = null;

            if (_itemDatabase != null && _itemDatabase.TryGetItemById(itemID, out itemData))
                return true;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemID, out itemData))
                return true;

            return false;
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

        private int FindSpecialSlotNumberForItem(Dictionary<int, SpecialItemSlotData> specialBoard, int itemID, bool requireExisting = false)
        {
            int emptySlotNumber = -1;

            foreach (var pair in specialBoard)
            {
                SpecialItemSlotData slotData = pair.Value;

                if (slotData != null && slotData.HasItem && slotData.ItemData.ItemID == itemID)
                    return pair.Key;

                if (!requireExisting && (slotData == null || !slotData.HasItem) && emptySlotNumber == -1)
                    emptySlotNumber = pair.Key;
            }

            return requireExisting ? -1 : emptySlotNumber;
        }

        private int CountBoardItems(Dictionary<int, ItemData> boardData, int itemID)
        {
            int count = 0;

            foreach (var pair in boardData)
            {
                ItemData itemData = pair.Value;

                if (itemData != null && itemData.HasItem && itemData.ItemID == itemID)
                    count++;
            }

            return count;
        }

        private int CountListItems(List<ItemData> items, int itemID)
        {
            int count = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ItemData itemData = items[i];

                if (itemData != null && itemData.HasItem && itemData.ItemID == itemID)
                    count++;
            }

            return count;
        }

        
        // 아이템 캐시를 새로 만들기
        // 기존 값을 초기화 
        private async Task RefreshServerItemCountCacheAsync()
        {
            
            _serverItemCountCache.Clear();
            _hasServerItemCountCache = false;

            if (!TryResolveStores())
                return;

            Dictionary<int, ItemData> boardData = await _boardSlotsStore.LoadBoardAsync();
            AddBoardItemsToCountCache(boardData);

            List<ItemData> queueItems = await _rewardQueueStore.LoadRewardQueueAsync();
            AddListItemsToCountCache(queueItems);

            Dictionary<int, SpecialItemSlotData> specialBoard = await _specialStore.LoadSpecialBoardAsync();
            AddSpecialBoardItemsToCountCache(specialBoard);

            _hasServerItemCountCache = true;
        }

        private void AddBoardItemsToCountCache(Dictionary<int, ItemData> boardData)
        {
            if (boardData == null)
                return;

            foreach (var pair in boardData)
                AddItemToCountCache(pair.Value, 1);
        }

        private void AddListItemsToCountCache(List<ItemData> items)
        {
            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
                AddItemToCountCache(items[i], 1);
        }

        private void AddSpecialBoardItemsToCountCache(Dictionary<int, SpecialItemSlotData> specialBoard)
        {
            if (specialBoard == null)
                return;

            foreach (var pair in specialBoard)
            {
                SpecialItemSlotData slotData = pair.Value;

                if (slotData == null || !slotData.HasItem)
                    continue;

                AddItemToCountCache(slotData.ItemData, slotData.Count);
            }
        }

        private void AddItemToCountCache(ItemData itemData, int count)
        {
            if (itemData == null || !itemData.HasItem || itemData.ItemID <= 0 || count <= 0)
                return;

            _serverItemCountCache.TryGetValue(itemData.ItemID, out int currentCount);
            _serverItemCountCache[itemData.ItemID] = currentCount + count;
        }

        private int RemoveItemsFromList(List<ItemData> items, int itemID, int removeCount)
        {
            int remaining = removeCount;

            for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemData itemData = items[i];

                if (itemData == null || !itemData.HasItem || itemData.ItemID != itemID)
                    continue;

                items.RemoveAt(i);
                remaining--;
            }

            return remaining;
        }

        private void ShowBoardAlert(string message)
        {
            ResolveReferences();

            if (_rewardQueue != null)
                _rewardQueue.ShowAlert(message);
            else
            {
                DebugTool.Warning(message, DebugType.Board, this);
                RuntimeWarning(message);
            }
        }

        private void RuntimeLog(string message)
        {
            if (!_enableRuntimeDiagnostics)
                return;

            Debug.Log($"[MergeBoardItemService] {message}", this);
        }

        private void RuntimeWarning(string message)
        {
            if (!_enableRuntimeDiagnostics)
                return;

            Debug.LogWarning($"[MergeBoardItemService] {message}", this);
        }

        protected virtual void OnDestroy()
        {
            UnbindTestReceiveButton();

            if (Instance == this)
                Instance = null;
        }
    }
}
