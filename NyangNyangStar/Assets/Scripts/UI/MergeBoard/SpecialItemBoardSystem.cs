using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class SpecialItemBoardSystem : MonoBehaviour
    {
        [Header("특수 아이템 슬롯")]
        [SerializeField] private GameObject _slotRoot;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private List<SpecialItemSlotView> _specialSlotViews = new();

        [Header("Firestore")]
        [SerializeField] private MergeBoardSpecialSO _mergeBoardFirestore;

        [Header("아이템 정보 UI")]
        [SerializeField] private BoardItemInfoPanel _itemInfoPanel;

        [Header("슬롯 설정")]
        [SerializeField] private int _slotCount = 2;
        [SerializeField] private int _slotSize = 135;
        [SerializeField] private int _slotSpacing = 5;

        private readonly Dictionary<int, SpecialItemSlotData> _specialSlotDict = new();
        private SpecialItemSlotView _selectedSlot;
        private bool _isSelling;
        private bool _isClearingAllItems;

        public bool IsBoardReady { get; private set; }
        public bool IsServerDataLoaded { get; private set; }
        public int SlotCount => Mathf.Max(1, _slotCount);

        private void Awake()
        {
            if (_slotRoot == null)
                _slotRoot = GameObject.Find("@Special Slot Root");

            if (_slotRoot == null)
            {
                DebugTool.Warning("@Special Slot Root를 찾을 수 없습니다.", DebugType.Board, this);
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
            GenerateSlots();

            if (_itemInfoPanel == null)
                _itemInfoPanel = FindFirstObjectByType<BoardItemInfoPanel>();

            IsBoardReady = true;

            if (MergeBoardItemService.Instance != null)
                MergeBoardItemService.Instance.RegisterSpecialItemBoardSystem(this);
        }

        private void Init()
        {
            if (_grid == null)
            {
                DebugTool.Warning("특수 아이템 GridLayoutGroup이 없습니다.", DebugType.Board, this);
                return;
            }

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = SlotCount;
            _grid.cellSize = new Vector2(_slotSize, _slotSize);
            _grid.spacing = new Vector2(_slotSpacing, _slotSpacing);
        }

        private void InitSlotData()
        {
            _specialSlotDict.Clear();

            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
                _specialSlotDict[slotNumber] = SpecialItemSlotData.Empty(slotNumber);
        }

        private void GenerateSlots()
        {
            _specialSlotViews.Clear();

            if (_slotPrefab == null)
            {
                DebugTool.Warning("특수 아이템 Slot Prefab이 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            if (_slotRoot == null)
            {
                DebugTool.Warning("특수 아이템 Slot Root가 없습니다.", DebugType.Board, this);
                return;
            }

            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                GameObject slot = Instantiate(_slotPrefab, _slotRoot.transform, false);
                slot.name = $"SpecialSlot_{slotNumber}";

                SpecialItemSlotView slotView = slot.GetComponent<SpecialItemSlotView>();

                if (slotView == null)
                {
                    DebugTool.Warning($"{slot.name}에 SpecialItemSlotView가 없습니다.", DebugType.Board, this);
                    continue;
                }

                _specialSlotViews.Add(slotView);
                slotView.Init(this, slotNumber);
            }
        }

        public async Task<bool> TryAddSpecialItemAsync(ItemData itemData, int count = 1)
        {
            if (!IsServerDataLoaded)
            {
                DebugTool.Warning("특수 아이템 보드 서버 데이터 로드 전에는 아이템을 추가할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            if (itemData == null || !itemData.HasItem)
                return false;

            if (itemData.ItemType != ItemType.Special)
            {
                DebugTool.Warning("특수 아이템 보드에는 Special 타입 아이템만 추가할 수 있습니다.", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);
            int slotNumber = FindSlotNumberForItem(itemData.ItemID);

            if (slotNumber == -1)
            {
                DebugTool.Warning("특수 아이템 보드 공간이 부족합니다.", DebugType.Board, this);
                return false;
            }

            SpecialItemSlotData currentData = _specialSlotDict[slotNumber];
            int newCount = currentData.HasItem ? currentData.Count + safeCount : safeCount;
            SpecialItemSlotData newSlotData = new SpecialItemSlotData(slotNumber, CreateRuntimeItem(itemData), newCount);

            SetSlotData(slotNumber, newSlotData);
            await SaveSlotSafeAsync(slotNumber);
            return true;
        }


        public int GetItemCountById(int itemID)
        {
            if (itemID <= 0)
                return 0;

            foreach (var pair in _specialSlotDict)
            {
                SpecialItemSlotData slotData = pair.Value;

                if (slotData != null && slotData.HasItem && slotData.ItemData.ItemID == itemID)
                    return slotData.Count;
            }

            return 0;
        }

        public async Task<int> ConsumeItemsByIdAsync(int itemID, int count = 1)
        {
            if (!IsServerDataLoaded)
            {
                DebugTool.Warning("특수 아이템 보드 서버 데이터 로드 전에는 아이템을 소비할 수 없습니다.", DebugType.Board, this);
                return 0;
            }

            if (itemID <= 0)
                return 0;

            int safeCount = Mathf.Max(1, count);
            int slotNumber = FindSlotNumberForExistingItem(itemID);

            if (slotNumber == -1)
            {
                DebugTool.Warning($"소비할 특수 아이템을 찾을 수 없습니다. ID:{itemID}", DebugType.Board, this);
                return 0;
            }

            SpecialItemSlotData currentData = _specialSlotDict[slotNumber];

            if (currentData.Count < safeCount)
            {
                DebugTool.Warning($"특수 아이템 수량이 부족합니다. ID:{itemID}, 필요:{safeCount}, 보유:{currentData.Count}", DebugType.Board, this);
                return 0;
            }

            int newCount = currentData.Count - safeCount;
            SpecialItemSlotData newSlotData = newCount <= 0
                ? SpecialItemSlotData.Empty(slotNumber)
                : new SpecialItemSlotData(slotNumber, currentData.ItemData, newCount);

            SetSlotData(slotNumber, newSlotData);
            await SaveSlotSafeAsync(slotNumber);

            if (_selectedSlot != null && _selectedSlot.SlotNumber == slotNumber)
            {
                if (newSlotData.HasItem)
                {
                    if (_itemInfoPanel != null)
                        _itemInfoPanel.Show(newSlotData.ItemData, SellSelectedItemAsync);
                }
                else
                {
                    ClearSelectedSlot();
                }
            }

            DebugTool.Log($"특수 아이템 소비 완료 / ID:{itemID}, Count:{safeCount}", DebugType.Board, this);
            return safeCount;
        }

        public void SelectSlot(SpecialItemSlotView slotView)
        {
            if (slotView == null || !slotView.HasItem)
            {
                ClearSelectedSlot();
                return;
            }

            _selectedSlot = slotView;

            if (_itemInfoPanel == null)
                _itemInfoPanel = FindFirstObjectByType<BoardItemInfoPanel>();

            if (_itemInfoPanel != null)
                _itemInfoPanel.Show(slotView.SlotData.ItemData, SellSelectedItemAsync);
        }

        public void ClearSelectedSlot()
        {
            _selectedSlot = null;

            if (_itemInfoPanel != null)
                _itemInfoPanel.Hide();
        }

        public async Task<bool> SellSelectedItemAsync()
        {
            if (_isSelling)
                return false;

            if (_selectedSlot == null || !_selectedSlot.HasItem)
            {
                DebugTool.Warning("판매할 특수 아이템이 선택되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            int slotNumber = _selectedSlot.SlotNumber;

            if (!IsValidSlotNumber(slotNumber))
                return false;

            SpecialItemSlotData currentData = _specialSlotDict[slotNumber];

            if (!currentData.HasItem)
                return false;

            _isSelling = true;

            try
            {
                int newCount = currentData.Count - 1;
                SpecialItemSlotData newSlotData = newCount <= 0
                    ? SpecialItemSlotData.Empty(slotNumber)
                    : new SpecialItemSlotData(slotNumber, currentData.ItemData, newCount);

                SetSlotData(slotNumber, newSlotData);
                await SaveSlotSafeAsync(slotNumber);

                if (newSlotData.HasItem)
                {
                    if (_itemInfoPanel != null)
                        _itemInfoPanel.Show(newSlotData.ItemData, SellSelectedItemAsync);
                }
                else
                {
                    ClearSelectedSlot();
                }

                return true;
            }
            finally
            {
                _isSelling = false;
            }
        }


        public async Task<bool> ClearAllItemsAsync()
        {
            if (_isClearingAllItems)
                return false;

            if (!IsServerDataLoaded)
            {
                DebugTool.Warning("특수 아이템 보드 서버 데이터 로드 전에는 전체 삭제를 할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            if (!ResolveMergeBoardFirestore())
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않아 특수 아이템 전체 삭제를 저장할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            Dictionary<int, SpecialItemSlotData> backupData = new Dictionary<int, SpecialItemSlotData>();
            foreach (var pair in _specialSlotDict)
                backupData[pair.Key] = pair.Value?.Clone() ?? SpecialItemSlotData.Empty(pair.Key);

            _isClearingAllItems = true;

            try
            {
                for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
                    _specialSlotDict[slotNumber] = SpecialItemSlotData.Empty(slotNumber);

                RefreshAllSlotUI();
                ClearSelectedSlot();

                await _mergeBoardFirestore.SaveSpecialBoardAsync(_specialSlotDict, SlotCount);

                DebugTool.Log("특수 아이템 보드 전체 삭제 완료", DebugType.Board, this);
                return true;
            }
            catch (Exception exception)
            {
                _specialSlotDict.Clear();
                foreach (var pair in backupData)
                    _specialSlotDict[pair.Key] = pair.Value?.Clone() ?? SpecialItemSlotData.Empty(pair.Key);

                RefreshAllSlotUI();
                Debug.LogError($"특수 아이템 보드 전체 삭제 저장 실패 : {exception.Message}", this);
                return false;
            }
            finally
            {
                _isClearingAllItems = false;
            }
        }

        public async Task LoadSpecialBoardFromServerAsync(bool normalizeDocumentIds = false)
        {
            IsServerDataLoaded = false;

            if (!ResolveMergeBoardFirestore())
                return;

            Dictionary<int, SpecialItemSlotData> loadedData = await _mergeBoardFirestore.LoadSpecialBoardAsync(SlotCount);
            ApplySpecialBoardData(loadedData);

            if (normalizeDocumentIds)
                await _mergeBoardFirestore.SaveSpecialBoardAsync(_specialSlotDict, SlotCount);

            IsServerDataLoaded = true;
            DebugTool.Log("특수 아이템 보드 서버 데이터 로드 완료", DebugType.Board, this);
        }

        public async Task CreateEmptySpecialBoardOnServerAsync()
        {
            if (!ResolveMergeBoardFirestore())
                return;

            await _mergeBoardFirestore.CreateEmptySpecialBoardAsync(SlotCount);
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
                DebugTool.Warning("MergeBoardSpecialSO가 인스펙터 또는 FireStoreManager에 연결되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            if (!_mergeBoardFirestore.IsReady)
                _mergeBoardFirestore.TryEnsureDatabaseReady();

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("MergeBoardSpecialSO가 아직 준비되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            DebugTool.Log("MergeBoardSpecialSO 연결 완료", DebugType.Board, this);
            return true;
        }

        private int FindSlotNumberForItem(int itemID)
        {
            int emptySlotNumber = -1;

            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                if (!_specialSlotDict.TryGetValue(slotNumber, out SpecialItemSlotData slotData))
                    continue;

                if (slotData.HasItem && slotData.ItemData.ItemID == itemID)
                    return slotNumber;

                if (!slotData.HasItem && emptySlotNumber == -1)
                    emptySlotNumber = slotNumber;
            }

            return emptySlotNumber;
        }


        private int FindSlotNumberForExistingItem(int itemID)
        {
            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                if (!_specialSlotDict.TryGetValue(slotNumber, out SpecialItemSlotData slotData))
                    continue;

                if (slotData.HasItem && slotData.ItemData.ItemID == itemID)
                    return slotNumber;
            }

            return -1;
        }

        private void ApplySpecialBoardData(Dictionary<int, SpecialItemSlotData> specialBoardData)
        {
            InitSlotData();

            if (specialBoardData != null)
            {
                foreach (var pair in specialBoardData)
                {
                    if (!IsValidSlotNumber(pair.Key))
                        continue;

                    _specialSlotDict[pair.Key] = CreateRuntimeSlotData(pair.Key, pair.Value);
                }
            }

            RefreshAllSlotUI();
            ClearSelectedSlot();
        }

        private void SetSlotData(int slotNumber, SpecialItemSlotData slotData)
        {
            if (!IsValidSlotNumber(slotNumber))
                return;

            SpecialItemSlotData safeSlotData = CreateRuntimeSlotData(slotNumber, slotData);
            _specialSlotDict[slotNumber] = safeSlotData;

            SpecialItemSlotView slotView = GetSlot(slotNumber);
            if (slotView != null)
                slotView.SetSlotData(safeSlotData);
        }

        private void RefreshAllSlotUI()
        {
            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                SpecialItemSlotView slotView = GetSlot(slotNumber);

                if (slotView == null)
                    continue;

                SpecialItemSlotData slotData = _specialSlotDict.TryGetValue(slotNumber, out SpecialItemSlotData data)
                    ? data
                    : SpecialItemSlotData.Empty(slotNumber);

                slotView.SetSlotData(slotData);
            }
        }

        private SpecialItemSlotData CreateRuntimeSlotData(int slotNumber, SpecialItemSlotData slotData)
        {
            if (slotData == null || !slotData.HasItem)
                return SpecialItemSlotData.Empty(slotNumber);

            return new SpecialItemSlotData(slotNumber, CreateRuntimeItem(slotData.ItemData), slotData.Count);
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

        private SpecialItemSlotView GetSlot(int slotNumber)
        {
            int index = slotNumber - 1;

            if (index < 0 || index >= _specialSlotViews.Count)
                return null;

            return _specialSlotViews[index];
        }

        private bool IsValidSlotNumber(int slotNumber)
        {
            return slotNumber >= 1 && slotNumber <= SlotCount;
        }

        private async Task SaveSlotSafeAsync(int slotNumber)
        {
            if (!ResolveMergeBoardFirestore())
                return;

            try
            {
                await _mergeBoardFirestore.SaveSpecialSlotAsync(slotNumber, _specialSlotDict[slotNumber]);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
