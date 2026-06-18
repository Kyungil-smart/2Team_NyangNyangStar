using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
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

        private const int GeneralBoardSlotCount = 63;

        private bool _isAddingItem;
        private bool _isConsumingItem;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        protected virtual void Start()
        {
            if (_testReceiveButton != null)
            {
                _testReceiveButton.onClick.RemoveListener(ReceiveRandomTestItem);
                _testReceiveButton.onClick.AddListener(ReceiveRandomTestItem);
            }
            else
            {
                DebugTool.Warning("테스트 아이템 생성 버튼이 연결되지 않았습니다.", DebugType.Board, this);
            }
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
            if (_isAddingItem)
                return false;

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

            _isAddingItem = true;

            try
            {
                return await AddItemAsync(itemData, count);
            }
            finally
            {
                _isAddingItem = false;
            }
        }

        public void AddItem(ItemData itemData, int count = 1)
        {
            _ = AddItemAsync(itemData, count);
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

            if (_boardSystem != null && _boardSystem.IsServerDataLoaded)
                count += _boardSystem.GetItemCountById(itemID);

            if (_rewardQueue != null && _rewardQueue.IsLoaded)
                count += _rewardQueue.GetItemCountById(itemID);

            if (_specialItemBoardSystem != null && _specialItemBoardSystem.IsServerDataLoaded)
                count += _specialItemBoardSystem.GetItemCountById(itemID);

            return count;
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
            await AddItemAsync(itemData, 1);
        }

        public async void ReceiveItem(ItemData itemData, int count)
        {
            await AddItemAsync(itemData, count);
        }

        public void ReceiveRandomTestItem()
        {
            DebugTool.Log("아이템 생성 버튼 클릭됨", DebugType.Board, this);

            if (!CanUseItemService())
                return;

            if (TryReadInputItemID(out int itemID, out bool hasInput))
            {
                AddItemById(itemID);
                return;
            }

            if (hasInput)
                return;

            if (!TryGetRandomCommonItem(out ItemData itemData))
            {
                DebugTool.Warning("생성 가능한 Common 아이템 데이터가 없습니다.", DebugType.Board, this);
                return;
            }

            AddItem(itemData);
        }

        private async Task<bool> AddCommonItemAsync(ItemData itemData, int count)
        {
            ResolveReferences();

            if (_rewardQueue != null && _rewardQueue.IsLoaded)
                return await _rewardQueue.EnqueueItemAsync(itemData, count);

            if (!TryResolveStores())
                return false;

            List<ItemData> queueItems = await _rewardQueueStore.LoadRewardQueueAsync();
            ItemData runtimeItem = CreateRuntimeItem(itemData);

            for (int i = 0; i < count; i++)
                queueItems.Add(runtimeItem.Clone());

            await _rewardQueueStore.SaveRewardQueueAsync(queueItems);
            DebugTool.Log($"보상 큐 직접 저장 완료 / ID:{itemData.ItemID}, Count:{count}", DebugType.Board, this);
            return true;
        }

        private async Task<bool> AddSpecialItemAsync(ItemData itemData, int count)
        {
            ResolveReferences();

            if (_specialItemBoardSystem != null && _specialItemBoardSystem.IsServerDataLoaded)
                return await _specialItemBoardSystem.TryAddSpecialItemAsync(itemData, count);

            if (!TryResolveStores())
                return false;

            Dictionary<int, SpecialItemSlotData> specialBoard = await _specialStore.LoadSpecialBoardAsync();
            int slotNumber = FindSpecialSlotNumberForItem(specialBoard, itemData.ItemID);

            if (slotNumber == -1)
            {
                DebugTool.Warning("특수 아이템 보드 공간이 부족합니다.", DebugType.Board, this);
                return false;
            }

            SpecialItemSlotData currentData = specialBoard[slotNumber];
            int newCount = currentData.HasItem ? currentData.Count + count : count;
            SpecialItemSlotData newSlotData = new SpecialItemSlotData(slotNumber, CreateRuntimeItem(itemData), newCount);

            specialBoard[slotNumber] = newSlotData;
            await _specialStore.SaveSpecialSlotAsync(slotNumber, newSlotData);
            DebugTool.Log($"특수 아이템 직접 저장 완료 / ID:{itemData.ItemID}, Count:{count}", DebugType.Board, this);
            return true;
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
                return false;
            }

            if (LocalDataAccess.Instance?.Game == null || !LocalDataAccess.Instance.Game.IsReady)
            {
                DebugTool.Warning("아이템 데이터 로드 완료 전입니다.", DebugType.Board, this);
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

            if (_boardSlotsStore == null || _rewardQueueStore == null || _specialStore == null)
            {
                DebugTool.Warning("머지보드 Store SO가 인스펙터에 연결되지 않았습니다.", DebugType.Board, this);
                return false;
            }

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

        protected virtual void OnDestroy()
        {
            if (_testReceiveButton != null)
                _testReceiveButton.onClick.RemoveListener(ReceiveRandomTestItem);

            if (Instance == this)
                Instance = null;
        }
    }
}
