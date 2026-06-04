using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

        [Header("Firestore")]
        [SerializeField] private MergeBoardFirestoreSo _mergeBoardFirestore;

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

            IsBoardReady = true;

            if (BoardItemReceiver.Instance != null)
                BoardItemReceiver.Instance.RegisterBoardSystem(this);
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

            int itemSize = _slotSize - _itemSpacing;
            StringBuilder log = new StringBuilder();

            log.AppendLine("[보드 슬롯 생성]");

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

                log.AppendLine($"{slotNumber} 번 슬롯 생성");
            }

            log.AppendLine("슬롯 생성 완료");
            DebugTool.Log($"{log}", DebugType.Board, this);
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
            bool result = TryAddItemInternal(itemData, out int changedSlotNumber);

            if (!result)
                return false;

            await SaveSlotSafeAsync(changedSlotNumber);
            return true;
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

        public async void MoveOrSwapItem(ItemSlot fromSlot, ItemSlot toSlot)
        {
            if (fromSlot == null || toSlot == null)
                return;

            if (fromSlot == toSlot)
                return;

            if (!fromSlot.HasItem)
                return;

            int fromSlotNumber = fromSlot.SlotNumber;
            int toSlotNumber = toSlot.SlotNumber;

            if (!IsValidSlotNumber(fromSlotNumber) || !IsValidSlotNumber(toSlotNumber))
                return;

            ItemData fromData = fromSlot.ItemData.Clone();
            ItemData toData = toSlot.HasItem ? toSlot.ItemData.Clone() : ItemData.Empty;

            SetSlotData(toSlotNumber, fromData);
            SetSlotData(fromSlotNumber, toData);

            Dictionary<int, ItemData> changedSlots = new Dictionary<int, ItemData>
            {
                { fromSlotNumber, _slotItemDict[fromSlotNumber].Clone() },
                { toSlotNumber, _slotItemDict[toSlotNumber].Clone() }
            };

            await SaveSlotsSafeAsync(changedSlots);
        }

        public async Task<bool> ClearSlotAsync(int slotNumber)
        {
            if (!IsValidSlotNumber(slotNumber))
                return false;

            SetSlotData(slotNumber, ItemData.Empty);
            await SaveSlotSafeAsync(slotNumber);
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

        public async Task LoadBoardFromServerAsync(bool normalizeDocumentIds = false)
        {
            IsServerDataLoaded = false;

            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보드 데이터를 불러올 수 없습니다.", DebugType.Board, this);
                return;
            }

            Dictionary<int, ItemData> loadedData = await _mergeBoardFirestore.LoadBoardAsync();
            ApplyBoardData(loadedData);

            if (normalizeDocumentIds)
                await _mergeBoardFirestore.SaveBoardAsync(_slotItemDict);

            IsServerDataLoaded = true;
            DebugTool.Log("보드 서버 데이터 로드 완료", DebugType.Board, this);
        }

        public async Task CreateEmptyBoardOnServerAsync()
        {
            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            await _mergeBoardFirestore.CreateEmptyBoardAsync();
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

                    _slotItemDict[pair.Key] = pair.Value?.Clone() ?? ItemData.Empty;
                }
            }

            RefreshAllSlotUI();
        }

        private void SetSlotData(int slotNumber, ItemData itemData)
        {
            if (!IsValidSlotNumber(slotNumber))
                return;

            ItemData safeItemData = itemData?.Clone() ?? ItemData.Empty;
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

        private async Task SaveSlotSafeAsync(int slotNumber)
        {
            if (_mergeBoardFirestore == null)
                return;

            try
            {
                await _mergeBoardFirestore.SaveSlotAsync(slotNumber, _slotItemDict[slotNumber]);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{slotNumber}번 슬롯 저장 실패 : {exception.Message}", this);
            }
        }

        private async Task SaveSlotsSafeAsync(Dictionary<int, ItemData> changedSlots)
        {
            if (_mergeBoardFirestore == null)
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
    }
}
