using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class BoardSystem : MonoBehaviour
    {
        [Header("보드 슬롯")]
        [SerializeField] private GameObject _slotRoot;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private List<ItemSlot> _itemSlots = new();
        [SerializeField] Color _selectedSlotColor = new (1f, 0.9f, 0.6f, 1);
        private Color _baseColor = new (1f, 1f, 1f, 1);

        [Header("Firestore")]
        [SerializeField] private MergeBoardFirestoreSo _mergeBoardFirestore;

        [Header("아이템 정보 UI")]
        [SerializeField] private BoardItemInfoPanel _itemInfoPanel;

        [Header("보드 크기")]
        [SerializeField] private int _width = 7;
        [SerializeField] private int _height = 9;

        [Header("슬롯 크기")]
        [SerializeField] private int _slotSize = 135;
        [SerializeField] private int _itemSpacing = 5;

        [Header("슬롯 간격")]
        [SerializeField] private int _slotSpacing = 5;

        public bool IsBoardReady { get; private set; }
        public bool IsServerDataLoaded { get; private set; }

        private readonly Dictionary<int, ItemData> _slotItemDict = new();
        private ItemSlot _selectedSlot;
        private bool _isMovingItem;
        private bool _isClearingAllItems;

        public int SlotCount => _width * _height;
        public IReadOnlyDictionary<int, ItemData> SlotItemDict => _slotItemDict;

        private void Awake()
        {
            if (_slotRoot == null)
                _slotRoot = GameObject.Find("@Slot Root");

            if (_slotRoot == null)
            {
                DebugTool.Warning("@Slot Root를 찾을 수 없습니다.", DebugType.Board, this);
                return;
            }

            if (_grid == null)
                _grid = _slotRoot.GetComponent<GridLayoutGroup>();

            if (_grid == null)
                _grid = _slotRoot.AddComponent<GridLayoutGroup>();
        }

        private void Start()
        {
            Init();
            InitSlotData();
            GenerateSlot();

            if (_itemInfoPanel == null)
                _itemInfoPanel = FindFirstObjectByType<BoardItemInfoPanel>();

            if (_itemInfoPanel != null)
                _itemInfoPanel.Init(this);

            ClearSelectedSlot();

            IsBoardReady = true;

            if (MergeBoardItemService.Instance != null)
                MergeBoardItemService.Instance.RegisterBoardSystem(this);
        }

        private void Init()
        {
            if (_grid == null)
            {
                DebugTool.Warning("GridLayoutGroup이 없습니다.", DebugType.Board, this);
                return;
            }

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = _width;
            _grid.cellSize = new Vector2(_slotSize, _slotSize);
            _grid.spacing = new Vector2(_slotSpacing, _slotSpacing);
        }

        private void InitSlotData()
        {
            _slotItemDict.Clear();

            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
                _slotItemDict[slotNumber] = ItemData.Empty;
        }

        private void GenerateSlot()
        {
            _itemSlots.Clear();

            if (_slotPrefab == null)
            {
                DebugTool.Warning("Slot Prefab이 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            int itemSize = _slotSize - _itemSpacing;
            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                GameObject slot = Instantiate(_slotPrefab, _slotRoot.transform, false);
                slot.name = $"Slot_{slotNumber}";

                ItemSlot itemSlot = slot.GetComponent<ItemSlot>();

                if (itemSlot == null)
                {
                    DebugTool.Warning($"{slot.name}에 ItemSlot이 없습니다.", DebugType.Board, this);
                    continue;
                }

                _itemSlots.Add(itemSlot);
                itemSlot.Init(this, slotNumber, ItemData.Empty, itemSize);
            }

            DebugTool.Log($"보드 슬롯 생성 완료 / 총 {_itemSlots.Count}개", DebugType.Board, this);
        }

        public bool TryAddItem(ItemData itemData)
        {
            bool result = TryAddItemInternal(itemData, out int changedSlotNumber);

            if (result)
                _ = SaveSlotSafeAsync(changedSlotNumber);

            return result;
        }

        public async Task<bool> TryAddItemFromQueueAsync(ItemData itemData)
        {
            ItemSlot addedSlot = await TryAddItemFromQueueAndSelectAsync(itemData);
            return addedSlot != null;
        }

        public Task<ItemSlot> TryAddItemFromQueueAndSelectAsync(ItemData itemData)
        {
            bool result = TryAddItemInternal(itemData, out int changedSlotNumber);

            if (!result)
                return Task.FromResult<ItemSlot>(null);

            ItemSlot addedSlot = GetSlot(changedSlotNumber);
            if (addedSlot != null)
                SelectSlot(addedSlot);

            // 큐 아이템을 보드에 넣는 순간 로컬 보드 상태는 이미 변경되었다.
            // Firestore 저장을 기다리면 모바일에서 SaveSlotAsync가 지연될 때
            // BoardRewardQueue의 Dequeue까지 도달하지 못해 큐가 Pop되지 않는 문제가 발생한다.
            SaveSlotFireAndForget(changedSlotNumber);

            return Task.FromResult(addedSlot);
        }

        public Task<bool> TryAddItemAsync(ItemData itemData)
        {
            return TryAddItemFromQueueAsync(itemData);
        }

        private bool TryAddItemInternal(ItemData itemData, out int changedSlotNumber)
        {
            changedSlotNumber = -1;

            if (!IsServerDataLoaded)
            {
                DebugTool.Warning("보드 서버 데이터 로드 전에는 아이템을 배치할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            if (itemData == null || !itemData.HasItem)
                return false;

            if (itemData.ItemType != ItemType.Common)
            {
                DebugTool.Warning("일반 보드에는 Common 타입 아이템만 배치할 수 있습니다.", DebugType.Board, this);
                return false;
            }

            int emptySlotNumber = FindFirstEmptyGeneralSlot();

            if (emptySlotNumber == -1)
            {
                DebugTool.Warning("보드판 공간이 부족합니다.", DebugType.Board, this);
                return false;
            }

            SetSlotData(emptySlotNumber, itemData.Clone());
            changedSlotNumber = emptySlotNumber;
            return true;
        }

        private int FindFirstEmptyGeneralSlot()
        {
            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                if (!_slotItemDict.TryGetValue(slotNumber, out ItemData itemData))
                    continue;

                if (itemData == null || !itemData.HasItem)
                    return slotNumber;
            }

            return -1;
        }

        public void HandleDragEnd(ItemSlot fromSlot, PointerEventData eventData)
        {
            if (fromSlot == null || eventData == null)
                return;

            if (!TryFindNearestSlot(eventData.position, eventData.pressEventCamera, out ItemSlot targetSlot))
                return;

            MoveOrSwapItem(fromSlot, targetSlot);
        }

        public async void MoveOrSwapItem(ItemSlot fromSlot, ItemSlot toSlot)
        {
            await MoveOrSwapItemAsync(fromSlot, toSlot);
        }

        private async Task<bool> MoveOrSwapItemAsync(ItemSlot fromSlot, ItemSlot toSlot)
        {
            if (_isMovingItem)
                return false;

            if (fromSlot == null || toSlot == null)
                return false;

            if (fromSlot == toSlot)
                return false;

            if (!fromSlot.HasItem)
                return false;

            int fromSlotNumber = fromSlot.SlotNumber;
            int toSlotNumber = toSlot.SlotNumber;

            if (!IsValidSlotNumber(fromSlotNumber) || !IsValidSlotNumber(toSlotNumber))
                return false;

            _isMovingItem = true;

            try
            {
                ItemData fromData = fromSlot.ItemData.Clone();
                ItemData toData = toSlot.HasItem ? toSlot.ItemData.Clone() : ItemData.Empty;

                SetSlotData(toSlotNumber, fromData);
                SetSlotData(fromSlotNumber, toData);

                Dictionary<int, ItemData> changedSlots = new Dictionary<int, ItemData>
                {
                    { fromSlotNumber, _slotItemDict[fromSlotNumber].Clone() },
                    { toSlotNumber, _slotItemDict[toSlotNumber].Clone() }
                };

                UpdateSelectionAfterMove(toSlot);
                await SaveSlotsSafeAsync(changedSlots);
                return true;
            }
            finally
            {
                _isMovingItem = false;
            }
        }

        private void UpdateSelectionAfterMove(ItemSlot movedSlot)
        {
            SelectSlot(movedSlot);
        }

        private bool TryFindNearestSlot(Vector2 screenPosition, Camera eventCamera, out ItemSlot nearestSlot)
        {
            nearestSlot = null;

            RectTransform rootRect = _slotRoot != null
                ? _slotRoot.transform as RectTransform
                : null;

            if (rootRect == null)
                return false;

            if (!RectTransformUtility.RectangleContainsScreenPoint(rootRect, screenPosition, eventCamera))
                return false;

            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _itemSlots.Count; i++)
            {
                ItemSlot slot = _itemSlots[i];

                if (slot == null || slot.RectTransform == null)
                    continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(slot.RectTransform, screenPosition, eventCamera))
                {
                    nearestSlot = slot;
                    return true;
                }

                Vector3 worldCenter = slot.RectTransform.TransformPoint(slot.RectTransform.rect.center);
                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
                float distance = (screenCenter - screenPosition).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = slot;
                }
            }

            return nearestSlot != null;
        }

        public async Task<bool> ClearSlotAsync(int slotNumber)
        {
            if (!IsValidSlotNumber(slotNumber))
                return false;

            SetSlotData(slotNumber, ItemData.Empty);
            await SaveSlotSafeAsync(slotNumber);

            if (_selectedSlot != null && _selectedSlot.SlotNumber == slotNumber)
                ClearSelectedSlot();

            return true;
        }

        public ItemData GetItemData(int slotNumber)
        {
            if (!IsValidSlotNumber(slotNumber))
                return ItemData.Empty;

            return _slotItemDict.TryGetValue(slotNumber, out ItemData itemData)
                ? itemData.Clone()
                : ItemData.Empty;
        }


        public int GetItemCountById(int itemID)
        {
            if (itemID <= 0)
                return 0;

            int count = 0;

            foreach (var pair in _slotItemDict)
            {
                ItemData itemData = pair.Value;

                if (itemData != null && itemData.HasItem && itemData.ItemID == itemID)
                    count++;
            }

            return count;
        }

        public async Task<int> ConsumeItemsByIdAsync(int itemID, int count = 1)
        {
            if (!IsServerDataLoaded)
            {
                DebugTool.Warning("보드 서버 데이터 로드 전에는 아이템을 소비할 수 없습니다.", DebugType.Board, this);
                return 0;
            }

            if (itemID <= 0)
                return 0;

            int safeCount = Mathf.Max(1, count);
            int availableCount = GetItemCountById(itemID);

            if (availableCount < safeCount)
            {
                DebugTool.Warning($"보드에 소비할 아이템 수량이 부족합니다. ID:{itemID}, 필요:{safeCount}, 보유:{availableCount}", DebugType.Board, this);
                return 0;
            }

            Dictionary<int, ItemData> changedSlots = new Dictionary<int, ItemData>();
            int consumedCount = 0;
            bool selectedSlotConsumed = false;

            for (int slotNumber = 1; slotNumber <= SlotCount && consumedCount < safeCount; slotNumber++)
            {
                if (!_slotItemDict.TryGetValue(slotNumber, out ItemData itemData))
                    continue;

                if (itemData == null || !itemData.HasItem || itemData.ItemID != itemID)
                    continue;

                if (_selectedSlot != null && _selectedSlot.SlotNumber == slotNumber)
                    selectedSlotConsumed = true;

                SetSlotData(slotNumber, ItemData.Empty);
                changedSlots[slotNumber] = ItemData.Empty;
                consumedCount++;
            }

            if (changedSlots.Count > 0)
                await SaveSlotsSafeAsync(changedSlots);

            if (selectedSlotConsumed)
                ClearSelectedSlot();

            DebugTool.Log($"보드 아이템 소비 완료 / ID:{itemID}, Count:{consumedCount}", DebugType.Board, this);
            return consumedCount;
        }

        public void SelectSlot(ItemSlot itemSlot)
        {
            if (itemSlot == null || !itemSlot.HasItem)
            {
                ClearSelectedSlot();
                return;
            }

            if(_selectedSlot != null)
                _selectedSlot.ChangeBackgroundColor(_baseColor);

            _selectedSlot = itemSlot;
            
            _selectedSlot.ChangeBackgroundColor(_selectedSlotColor);

            if (_itemInfoPanel == null)
                _itemInfoPanel = FindFirstObjectByType<BoardItemInfoPanel>();

            if (_itemInfoPanel != null)
                _itemInfoPanel.Show(itemSlot.ItemData);
        }

        public void ClearSelectedSlot()
        {
            if (_selectedSlot != null)
                _selectedSlot.ChangeBackgroundColor(_baseColor);

            _selectedSlot = null;

            if (_itemInfoPanel != null)
                _itemInfoPanel.Hide();
        }

        public async Task<bool> SellSelectedItemAsync()
        {
            if (_selectedSlot == null || !_selectedSlot.HasItem)
            {
                DebugTool.Warning("판매할 아이템이 선택되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            int slotNumber = _selectedSlot.SlotNumber;

            if (!IsValidSlotNumber(slotNumber))
                return false;

            SetSlotData(slotNumber, ItemData.Empty);
            await SaveSlotSafeAsync(slotNumber);
            ClearSelectedSlot();

            return true;
        }


        public async Task<bool> ClearAllItemsAsync()
        {
            if (_isClearingAllItems)
                return false;

            if (!IsServerDataLoaded)
            {
                DebugTool.Warning("보드 서버 데이터 로드 전에는 전체 삭제를 할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            if (!ResolveMergeBoardFirestore())
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않아 보드 전체 삭제를 저장할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            Dictionary<int, ItemData> backupData = new Dictionary<int, ItemData>();
            foreach (var pair in _slotItemDict)
                backupData[pair.Key] = pair.Value?.Clone() ?? ItemData.Empty;

            _isClearingAllItems = true;

            try
            {
                for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
                    _slotItemDict[slotNumber] = ItemData.Empty;

                RefreshAllSlotUI();
                ClearSelectedSlot();

                await _mergeBoardFirestore.SaveBoardAsync(_slotItemDict);

                DebugTool.Log("일반 보드 전체 아이템 삭제 완료", DebugType.Board, this);
                return true;
            }
            catch (Exception exception)
            {
                _slotItemDict.Clear();
                foreach (var pair in backupData)
                    _slotItemDict[pair.Key] = pair.Value?.Clone() ?? ItemData.Empty;

                RefreshAllSlotUI();
                Debug.LogError($"일반 보드 전체 아이템 삭제 저장 실패 : {exception.Message}", this);
                return false;
            }
            finally
            {
                _isClearingAllItems = false;
            }
        }

        public async Task LoadBoardFromServerAsync(bool normalizeDocumentIds = false)
        {
            IsServerDataLoaded = false;

            if (!ResolveMergeBoardFirestore())
                return;

            Dictionary<int, ItemData> loadedData = await _mergeBoardFirestore.LoadBoardAsync();
            ApplyBoardData(loadedData);

            if (normalizeDocumentIds)
                await _mergeBoardFirestore.SaveBoardAsync(_slotItemDict);

            IsServerDataLoaded = true;
            DebugTool.Log("보드 서버 데이터 로드 완료", DebugType.Board, this);
        }

        public async Task CreateEmptyBoardOnServerAsync()
        {
            if (!ResolveMergeBoardFirestore())
                return;

            await _mergeBoardFirestore.CreateEmptyBoardAsync();
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

        private void ApplyBoardData(Dictionary<int, ItemData> boardData)
        {
            InitSlotData();

            if (boardData != null)
            {
                foreach (var pair in boardData)
                {
                    if (!IsValidSlotNumber(pair.Key))
                        continue;

                    _slotItemDict[pair.Key] = CreateRuntimeItem(pair.Value);
                }
            }

            RefreshAllSlotUI();
            ClearSelectedSlot();

            LogBoardDataSummary();
        }

        private void SetSlotData(int slotNumber, ItemData itemData)
        {
            if (!IsValidSlotNumber(slotNumber))
                return;

            ItemData safeItemData = CreateRuntimeItem(itemData);
            _slotItemDict[slotNumber] = safeItemData;

            ItemSlot itemSlot = GetSlot(slotNumber);
            if (itemSlot != null)
                itemSlot.SetItemData(safeItemData);
        }

        private void RefreshAllSlotUI()
        {
            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                ItemSlot itemSlot = GetSlot(slotNumber);

                if (itemSlot == null)
                    continue;

                ItemData itemData = _slotItemDict.TryGetValue(slotNumber, out ItemData data)
                    ? data
                    : ItemData.Empty;

                itemSlot.SetItemData(itemData);
            }
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

        private ItemSlot GetSlot(int slotNumber)
        {
            int index = slotNumber - 1;

            if (index < 0 || index >= _itemSlots.Count)
                return null;

            return _itemSlots[index];
        }

        private bool IsValidSlotNumber(int slotNumber)
        {
            return slotNumber >= 1 && slotNumber <= SlotCount;
        }

        private void SaveSlotFireAndForget(int slotNumber)
        {
            _ = SaveSlotSafeAsync(slotNumber);
        }

        private async Task SaveSlotSafeAsync(int slotNumber)
        {
            if (!ResolveMergeBoardFirestore())
                return;

            if (!_slotItemDict.TryGetValue(slotNumber, out ItemData itemData))
                return;

            ItemData saveData = itemData?.Clone() ?? ItemData.Empty;

            try
            {
                await _mergeBoardFirestore.SaveSlotAsync(slotNumber, saveData);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{slotNumber}번 슬롯 저장 실패 : {exception.Message}", this);
            }
        }

        private async Task SaveSlotsSafeAsync(Dictionary<int, ItemData> changedSlots)
        {
            if (!ResolveMergeBoardFirestore())
                return;

            try
            {
                await _mergeBoardFirestore.SaveSlotsAsync(changedSlots);
            }
            catch (Exception exception)
            {
                Debug.LogError($"보드 슬롯 저장 실패 : {exception.Message}", this);
            }
        }
        
        private void LogBoardDataSummary()
        {
            int totalSlotCount = _width * _height;
            int itemCount = 0;
            int spriteLoadedCount = 0;
            int missingSpriteCount = 0;

            foreach (var pair in _slotItemDict)
            {
                ItemData itemData = pair.Value;

                if (itemData == null || !itemData.HasItem)
                    continue;

                itemCount++;

                if (itemData.ItemSprite != null)
                    spriteLoadedCount++;
                else
                    missingSpriteCount++;
            }

            DebugTool.Log(
                $"보드 데이터 적용 완료 / 전체 슬롯:{totalSlotCount}, 아이템:{itemCount}, Sprite 있음:{spriteLoadedCount}, Sprite 없음:{missingSpriteCount}",
                DebugType.Board,
                this);
        }
    }
}
