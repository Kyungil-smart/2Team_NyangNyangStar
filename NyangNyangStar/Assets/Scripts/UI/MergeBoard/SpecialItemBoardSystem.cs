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
        [SerializeField] private MergeBoardFirestoreSo _mergeBoardFirestore;

        [Header("슬롯 설정")]
        [SerializeField] private int _slotCount = 2;
        [SerializeField] private int _slotSize = 135;
        [SerializeField] private int _slotSpacing = 5;

        private readonly Dictionary<int, SpecialItemSlotData> _specialSlotDict = new();

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

            IsBoardReady = true;

            if (BoardItemReceiver.Instance != null)
                BoardItemReceiver.Instance.RegisterSpecialItemBoardSystem(this);
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
                slotView.Init(slotNumber);
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
            SpecialItemSlotData newSlotData = new SpecialItemSlotData(slotNumber, itemData, newCount);

            SetSlotData(slotNumber, newSlotData);
            await SaveSlotSafeAsync(slotNumber);
            return true;
        }

        public async Task LoadSpecialBoardFromServerAsync(bool normalizeDocumentIds = false)
        {
            IsServerDataLoaded = false;

            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 특수 아이템 보드 데이터를 불러올 수 없습니다.", DebugType.Board, this);
                return;
            }

            Dictionary<int, SpecialItemSlotData> loadedData = await _mergeBoardFirestore.LoadSpecialBoardAsync(SlotCount);
            ApplySpecialBoardData(loadedData);

            if (normalizeDocumentIds)
                await _mergeBoardFirestore.SaveSpecialBoardAsync(_specialSlotDict, SlotCount);

            IsServerDataLoaded = true;
            DebugTool.Log("특수 아이템 보드 서버 데이터 로드 완료", DebugType.Board, this);
        }

        public async Task CreateEmptySpecialBoardOnServerAsync()
        {
            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            await _mergeBoardFirestore.CreateEmptySpecialBoardAsync(SlotCount);
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

        private void ApplySpecialBoardData(Dictionary<int, SpecialItemSlotData> specialBoardData)
        {
            InitSlotData();

            if (specialBoardData != null)
            {
                foreach (var pair in specialBoardData)
                {
                    if (!IsValidSlotNumber(pair.Key))
                        continue;

                    _specialSlotDict[pair.Key] = pair.Value?.Clone() ?? SpecialItemSlotData.Empty(pair.Key);
                }
            }

            RefreshAllSlotUI();
        }

        private void SetSlotData(int slotNumber, SpecialItemSlotData slotData)
        {
            if (!IsValidSlotNumber(slotNumber))
                return;

            SpecialItemSlotData safeSlotData = slotData?.Clone() ?? SpecialItemSlotData.Empty(slotNumber);
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
            if (_mergeBoardFirestore == null)
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
