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

        [Header("Slot Visual")]
        [SerializeField] private Color _defaultSlotColor = new Color(1f, 0.985f, 0.94f, 1f);
        [SerializeField] private Color _selectedSlotColor = new Color(1f, 0.82f, 0.42f, 1f);
        [SerializeField] private Color _dropCandidateSlotColor = new Color(0.84f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color _dropTargetSlotColor = new Color(1f, 0.9f, 0.45f, 1f);

        [Header("Firestore")]
        [SerializeField] private MergeBoardSlotsSO _mergeBoardFirestore;

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

        [Header("Responsive Layout")]
        [SerializeField] private bool _useResponsiveLayout = true;
        [SerializeField] private RectTransform _layoutRoot;
        [SerializeField] private RectTransform _boardRect;
        [SerializeField] private RectTransform _bottomActionRect;
        [SerializeField] private RectTransform _bottomActionAlignmentRect;
        [SerializeField] private Vector2 _referenceLayoutSize = new(1080f, 2340f);
        [SerializeField] private Vector2 _minimumBoardSize = new(560f, 720f);
        [SerializeField] private Vector2 _maximumBoardSize = new(940f, 1210f);
        [SerializeField] private Vector2 _bottomActionReferenceSize = new(150f, 100f);
        [SerializeField] private Vector2 _bottomActionMinimumSize = new(120f, 78f);
        [SerializeField] private Vector2 _bottomActionMaximumSize = new(150f, 100f);
        [SerializeField] private float _horizontalPadding = 45f;
        [SerializeField] private float _topReservedHeight = 500f;
        [SerializeField] private float _bottomReservedHeight = 240f;
        [SerializeField] private float _boardVerticalOffset = -15f;
        [SerializeField] private float _bottomActionRightPadding = 40f;
        [SerializeField] private float _bottomActionBottomPadding = 70f;
        [SerializeField] private float _bottomActionVerticalOffset = 0f;
        [SerializeField] private float _boardToBottomActionGap = 60f;
        [SerializeField] private float _minimumSlotSize = 72f;
        [SerializeField] private float _maximumSlotSize = 125f;

        public bool IsBoardReady { get; private set; }
        public bool IsServerDataLoaded { get; private set; }

        private readonly Dictionary<int, ItemData> _slotItemDict = new();
        private ItemSlot _selectedSlot;
        private ItemSlot _dragSourceSlot;
        private ItemSlot _dragTargetSlot;
        private bool _isMovingItem;
        private bool _isClearingAllItems;
        private bool _isApplyingResponsiveLayout;
        private readonly Vector3[] _rectCornerBuffer = new Vector3[4];

        public int SlotCount => _width * _height;
        public IReadOnlyDictionary<int, ItemData> SlotItemDict => _slotItemDict;

        public void RefreshResponsiveLayout()
        {
            ApplyResponsiveLayout();
        }

        private const string SlotRootObjectName = "@Slot Root";

        private void Awake()
        {
            ResolveSlotRootReferences();
        }

        private void ResolveSlotRootReferences()
        {
            if (_slotRoot == null)
            {
                Transform slotRootTransform = transform.Find(SlotRootObjectName);

                if (slotRootTransform == null)
                {
                    Transform[] descendants = GetComponentsInChildren<Transform>(true);

                    for (int i = 0; i < descendants.Length; i++)
                    {
                        Transform child = descendants[i];

                        if (child != null && child.name == SlotRootObjectName)
                        {
                            slotRootTransform = child;
                            break;
                        }
                    }
                }

                if (slotRootTransform != null)
                    _slotRoot = slotRootTransform.gameObject;
            }

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
            ApplyResponsiveLayout();
            Init();
            InitSlotData();
            GenerateSlot();
            ApplyResponsiveLayout();

            if (_itemInfoPanel == null)
                _itemInfoPanel = FindFirstObjectByType<BoardItemInfoPanel>();

            if (_itemInfoPanel != null)
                _itemInfoPanel.Init(this);

            ClearSelectedSlot();

            IsBoardReady = true;

            if (MergeBoardItemService.Instance != null)
                MergeBoardItemService.Instance.RegisterBoardSystem(this);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            ApplyResponsiveLayout();
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
                ApplySlotVisual(itemSlot);
            }

            DebugTool.Log($"보드 슬롯 생성 완료 / 총 {_itemSlots.Count}개", DebugType.Board, this);
        }

        private void ApplyResponsiveLayout()
        {
            if (_isApplyingResponsiveLayout)
                return;

            ResolveSlotRootReferences();
            ResolveResponsiveLayoutReferences();

            if (!_useResponsiveLayout)
                return;

            if (_layoutRoot == null || _boardRect == null || _grid == null)
                return;

            Rect layoutRect = _layoutRoot.rect;

            if (layoutRect.width <= 0f || layoutRect.height <= 0f)
                return;

            _isApplyingResponsiveLayout = true;

            try
            {
                float scale = CalculateResponsiveScale(layoutRect.size);
                float boardScale = CalculateResponsiveWidthScale(layoutRect.size);
                float horizontalPadding = Mathf.Max(0f, _horizontalPadding * boardScale);
                float topReservedHeight = Mathf.Max(0f, _topReservedHeight * scale);
                Rect bottomActionBounds = ApplyResponsiveBottomAction(layoutRect, scale);
                float bottomReservedHeight = CalculateBottomReservedHeight(layoutRect, bottomActionBounds, scale);

                float availableWidth = Mathf.Max(1f, layoutRect.width - horizontalPadding * 2f);
                float availableHeight = Mathf.Max(1f, layoutRect.height - topReservedHeight - bottomReservedHeight);
                Vector2 boardSize = CalculateResponsiveBoardSize(availableWidth, availableHeight, boardScale);

                float contentTop = layoutRect.yMax - topReservedHeight;
                float contentBottom = layoutRect.yMin + bottomReservedHeight;
                float desiredCenterY = (contentTop + contentBottom) * 0.5f;
                float verticalOffset = _boardVerticalOffset * scale;

                _boardRect.anchorMin = new Vector2(0.5f, 0.5f);
                _boardRect.anchorMax = new Vector2(0.5f, 0.5f);
                _boardRect.pivot = new Vector2(0.5f, 0.5f);
                _boardRect.sizeDelta = boardSize;
                _boardRect.anchoredPosition = new Vector2(0f, desiredCenterY - layoutRect.center.y + verticalOffset);

                ApplyResponsiveGrid(boardSize, boardScale);
            }
            finally
            {
                _isApplyingResponsiveLayout = false;
            }
        }

        private void ResolveResponsiveLayoutReferences()
        {
            _boardRect ??= transform as RectTransform;

            if (_layoutRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                _layoutRoot = canvas != null ? canvas.transform as RectTransform : transform.parent as RectTransform;
            }

            if (_bottomActionRect == null && _layoutRoot != null)
                _bottomActionRect = FindChildRectTransform(_layoutRoot, "Home Button");

            if (_bottomActionAlignmentRect == null && _layoutRoot != null)
                _bottomActionAlignmentRect = FindChildRectTransform(_layoutRoot, "Selected Item Info");
        }

        private RectTransform FindChildRectTransform(RectTransform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            RectTransform[] children = root.GetComponentsInChildren<RectTransform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                RectTransform child = children[i];

                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }

        private Rect ApplyResponsiveBottomAction(Rect layoutRect, float scale)
        {
            if (_bottomActionRect == null)
                return new Rect(layoutRect.xMin, layoutRect.yMin, 0f, 0f);

            Vector2 size = CalculateResponsiveSize(
                _bottomActionReferenceSize,
                _bottomActionMinimumSize,
                _bottomActionMaximumSize,
                scale);

            float rightPadding = Mathf.Max(0f, _bottomActionRightPadding * scale);
            float bottomPadding = CalculateBottomActionPadding(layoutRect, size, scale);

            _bottomActionRect.anchorMin = new Vector2(1f, 0f);
            _bottomActionRect.anchorMax = new Vector2(1f, 0f);
            _bottomActionRect.pivot = new Vector2(1f, 0f);
            _bottomActionRect.sizeDelta = size;
            _bottomActionRect.anchoredPosition = new Vector2(-rightPadding, bottomPadding);

            float xMax = layoutRect.xMax - rightPadding;
            float yMin = layoutRect.yMin + bottomPadding;
            return new Rect(xMax - size.x, yMin, size.x, size.y);
        }

        private Vector2 CalculateResponsiveSize(Vector2 referenceSize, Vector2 minimumSize, Vector2 maximumSize, float scale)
        {
            Vector2 minSize = new(
                Mathf.Max(1f, minimumSize.x),
                Mathf.Max(1f, minimumSize.y));

            Vector2 maxSize = new(
                Mathf.Max(minSize.x, maximumSize.x),
                Mathf.Max(minSize.y, maximumSize.y));

            return new Vector2(
                Mathf.Clamp(referenceSize.x * scale, minSize.x, maxSize.x),
                Mathf.Clamp(referenceSize.y * scale, minSize.y, maxSize.y));
        }

        private float CalculateBottomActionPadding(Rect layoutRect, Vector2 actionSize, float scale)
        {
            float fallbackPadding = Mathf.Max(0f, _bottomActionBottomPadding * scale);

            if (_bottomActionAlignmentRect == null || _layoutRoot == null)
                return fallbackPadding;

            Rect alignmentRect = GetLocalRectInLayout(_bottomActionAlignmentRect);

            if (alignmentRect.width <= 0f || alignmentRect.height <= 0f)
                return fallbackPadding;

            float targetCenterY = alignmentRect.center.y + _bottomActionVerticalOffset * scale;
            float alignedBottom = targetCenterY - actionSize.y * 0.5f;
            return Mathf.Max(0f, alignedBottom - layoutRect.yMin);
        }

        private Rect GetLocalRectInLayout(RectTransform target)
        {
            target.GetWorldCorners(_rectCornerBuffer);

            Vector3 bottomLeft = _layoutRoot.InverseTransformPoint(_rectCornerBuffer[0]);
            Vector3 topRight = _layoutRoot.InverseTransformPoint(_rectCornerBuffer[2]);

            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        private float CalculateBottomReservedHeight(Rect layoutRect, Rect bottomActionBounds, float scale)
        {
            float reservedHeight = Mathf.Max(0f, _bottomReservedHeight * scale);

            if (_bottomActionRect == null)
                return reservedHeight;

            float actionTopFromBottom = Mathf.Max(0f, bottomActionBounds.yMax - layoutRect.yMin);
            float actionGap = Mathf.Max(0f, _boardToBottomActionGap * scale);
            return Mathf.Max(reservedHeight, actionTopFromBottom + actionGap);
        }

        private Vector2 CalculateResponsiveBoardSize(float availableWidth, float availableHeight, float scale)
        {
            float aspect = _width > 0 && _height > 0
                ? (float)_width / _height
                : 1f;

            float maxWidth = Mathf.Min(availableWidth, Mathf.Max(1f, _maximumBoardSize.x * scale));
            float maxHeight = Mathf.Min(availableHeight, Mathf.Max(1f, _maximumBoardSize.y * scale));
            float width = Mathf.Min(maxWidth, maxHeight * aspect);
            float height = width / aspect;

            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * aspect;
            }

            float minWidth = Mathf.Min(maxWidth, Mathf.Max(1f, _minimumBoardSize.x * scale));
            float minHeight = Mathf.Min(maxHeight, Mathf.Max(1f, _minimumBoardSize.y * scale));

            if (width < minWidth && minWidth / aspect <= maxHeight)
            {
                width = minWidth;
                height = width / aspect;
            }

            if (height < minHeight && minHeight * aspect <= maxWidth)
            {
                height = minHeight;
                width = height * aspect;
            }

            return new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        }

        private void ApplyResponsiveGrid(Vector2 boardSize, float scale)
        {
            if (_grid == null || _width <= 0 || _height <= 0)
                return;

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = _width;
            _grid.spacing = new Vector2(_slotSpacing, _slotSpacing);

            float availableWidth = boardSize.x - _grid.padding.horizontal - _grid.spacing.x * Mathf.Max(0, _width - 1);
            float availableHeight = boardSize.y - _grid.padding.vertical - _grid.spacing.y * Mathf.Max(0, _height - 1);
            float slotSize = Mathf.Min(availableWidth / _width, availableHeight / _height);

            if (float.IsNaN(slotSize) || float.IsInfinity(slotSize) || slotSize <= 0f)
                return;

            float minSlotSize = Mathf.Max(1f, _minimumSlotSize * scale);
            float maxSlotSize = Mathf.Max(minSlotSize, _maximumSlotSize * scale);
            slotSize = Mathf.Clamp(slotSize, minSlotSize, maxSlotSize);

            _slotSize = Mathf.RoundToInt(slotSize);
            _grid.cellSize = new Vector2(slotSize, slotSize);
            _grid.childAlignment = TextAnchor.MiddleCenter;

            if (_grid.transform is RectTransform gridRect)
                LayoutRebuilder.MarkLayoutForRebuild(gridRect);

            float responsiveItemSpacing = Mathf.Min(_itemSpacing * scale, slotSize * 0.4f);
            int itemSize = Mathf.RoundToInt(Mathf.Max(1f, slotSize - responsiveItemSpacing));

            for (int i = 0; i < _itemSlots.Count; i++)
                _itemSlots[i]?.SetItemSize(itemSize);
        }

        private float CalculateResponsiveScale(Vector2 currentSize)
        {
            float referenceWidth = Mathf.Max(1f, _referenceLayoutSize.x);
            float referenceHeight = Mathf.Max(1f, _referenceLayoutSize.y);
            float widthScale = currentSize.x > 0f ? currentSize.x / referenceWidth : 1f;
            float heightScale = currentSize.y > 0f ? currentSize.y / referenceHeight : 1f;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.01f, 1f);
        }

        private float CalculateResponsiveWidthScale(Vector2 currentSize)
        {
            float referenceWidth = Mathf.Max(1f, _referenceLayoutSize.x);
            float widthScale = currentSize.x > 0f ? currentSize.x / referenceWidth : 1f;
            return Mathf.Clamp(widthScale, 0.01f, 1f);
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

        public async Task<ItemSlot> TryAddItemFromQueueAndSelectAsync(ItemData itemData)
        {
            bool result = TryAddItemInternal(itemData, out int changedSlotNumber);

            if (!result)
                return null;

            ItemSlot addedSlot = GetSlot(changedSlotNumber);
            if (addedSlot != null)
                SelectSlot(addedSlot);

            if (!await SaveSlotSafeAsync(changedSlotNumber))
            {
                SetSlotData(changedSlotNumber, ItemData.Empty);

                if (_selectedSlot != null && _selectedSlot.SlotNumber == changedSlotNumber)
                    ClearSelectedSlot();

                return null;
            }

            return addedSlot;
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

        public void BeginSlotDrag(ItemSlot sourceSlot)
        {
            if (sourceSlot == null || !sourceSlot.HasItem)
                return;

            _dragSourceSlot = sourceSlot;
            _dragTargetSlot = null;
            RefreshSlotVisuals();
        }

        public void UpdateSlotDragTarget(ItemSlot sourceSlot, PointerEventData eventData)
        {
            if (_dragSourceSlot == null || sourceSlot != _dragSourceSlot || eventData == null)
                return;

            ItemSlot nextTarget = null;

            if (TryFindNearestSlot(eventData.position, eventData.pressEventCamera, out ItemSlot foundSlot) &&
                foundSlot != _dragSourceSlot)
            {
                nextTarget = foundSlot;
            }

            if (_dragTargetSlot == nextTarget)
                return;

            _dragTargetSlot = nextTarget;
            RefreshSlotVisuals();
        }

        public void EndSlotDrag(ItemSlot sourceSlot)
        {
            if (_dragSourceSlot == null || sourceSlot != _dragSourceSlot)
                return;

            _dragSourceSlot = null;
            _dragTargetSlot = null;
            RefreshSlotVisuals();
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

                Dictionary<int, ItemData> changedSlots = new Dictionary<int, ItemData>
                {
                    { fromSlotNumber, toData.Clone() },
                    { toSlotNumber, fromData.Clone() }
                };

                SetSlotData(toSlotNumber, fromData);
                SetSlotData(fromSlotNumber, toData);

                if (!await SaveSlotsSafeAsync(changedSlots))
                {
                    SetSlotData(fromSlotNumber, fromData);
                    SetSlotData(toSlotNumber, toData);
                    SelectSlot(fromSlot);
                    return false;
                }

                UpdateSelectionAfterMove(toSlot);
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

            ItemData previousData = GetItemData(slotNumber);
            SetSlotData(slotNumber, ItemData.Empty);

            if (!await SaveSlotSafeAsync(slotNumber))
            {
                SetSlotData(slotNumber, previousData);
                return false;
            }

            if (_selectedSlot != null && _selectedSlot.SlotNumber == slotNumber)
                ClearSelectedSlot();

            return true;
        }

        public async Task<bool> ClearSlotIfContainsAsync(int slotNumber, int expectedItemID)
        {
            if (!IsValidSlotNumber(slotNumber) || expectedItemID <= 0)
                return false;

            if (!_slotItemDict.TryGetValue(slotNumber, out ItemData itemData))
                return false;

            if (itemData == null || !itemData.HasItem || itemData.ItemID != expectedItemID)
                return false;

            return await ClearSlotAsync(slotNumber);
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
            Dictionary<int, ItemData> backupData = new Dictionary<int, ItemData>();
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

                backupData[slotNumber] = itemData.Clone();
                SetSlotData(slotNumber, ItemData.Empty);
                changedSlots[slotNumber] = ItemData.Empty;
                consumedCount++;
            }

            if (changedSlots.Count > 0 && !await SaveSlotsSafeAsync(changedSlots))
            {
                for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
                {
                    if (changedSlots.ContainsKey(slotNumber) && backupData.TryGetValue(slotNumber, out ItemData backupItem))
                        SetSlotData(slotNumber, backupItem);
                }

                if (selectedSlotConsumed && _selectedSlot != null)
                    SelectSlot(_selectedSlot);

                return 0;
            }

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

            ItemSlot previousSlot = _selectedSlot;

            _selectedSlot = itemSlot;

            ApplySlotVisual(previousSlot);
            ApplySlotVisual(_selectedSlot);

            if (_itemInfoPanel == null)
                _itemInfoPanel = FindFirstObjectByType<BoardItemInfoPanel>();

            if (_itemInfoPanel != null)
                _itemInfoPanel.Show(itemSlot.ItemData);
        }

        public void ClearSelectedSlot()
        {
            ItemSlot previousSlot = _selectedSlot;
            if (_selectedSlot != null)
                _selectedSlot = null;

            ApplySlotVisual(previousSlot);

            if (_itemInfoPanel != null)
                _itemInfoPanel.Hide();
        }

        private void RefreshSlotVisuals()
        {
            for (int i = 0; i < _itemSlots.Count; i++)
                ApplySlotVisual(_itemSlots[i]);
        }

        private void ApplySlotVisual(ItemSlot itemSlot)
        {
            if (itemSlot == null)
                return;

            if (_dragSourceSlot != null && itemSlot != _dragSourceSlot)
            {
                itemSlot.ChangeBackgroundColor(itemSlot == _dragTargetSlot
                    ? _dropTargetSlotColor
                    : _dropCandidateSlotColor);
                return;
            }

            itemSlot.ChangeBackgroundColor(itemSlot == _selectedSlot
                ? _selectedSlotColor
                : _defaultSlotColor);
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

            ItemData previousData = GetItemData(slotNumber);
            SetSlotData(slotNumber, ItemData.Empty);

            if (!await SaveSlotSafeAsync(slotNumber))
            {
                SetSlotData(slotNumber, previousData);
                SelectSlot(_selectedSlot);
                return false;
            }

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
                FireStoreManager.Instance.TryGetStore(out _mergeBoardFirestore);

            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardSlotsSO가 인스펙터 또는 FireStoreManager에 연결되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            if (!_mergeBoardFirestore.IsReady)
                _mergeBoardFirestore.TryEnsureDatabaseReady();

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("MergeBoardSlotsSO가 아직 준비되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            DebugTool.Log("MergeBoardSlotsSO 연결 완료", DebugType.Board, this);
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
                ApplySlotVisual(itemSlot);
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

        private async Task<bool> SaveSlotSafeAsync(int slotNumber)
        {
            if (!ResolveMergeBoardFirestore())
                return false;

            if (!_slotItemDict.TryGetValue(slotNumber, out ItemData itemData))
                return false;

            ItemData saveData = itemData?.Clone() ?? ItemData.Empty;

            try
            {
                await _mergeBoardFirestore.SaveSlotAsync(slotNumber, saveData);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{slotNumber}번 슬롯 저장 실패 : {exception.Message}", this);
                return false;
            }
        }

        private async Task<bool> SaveSlotsSafeAsync(Dictionary<int, ItemData> changedSlots)
        {
            if (!ResolveMergeBoardFirestore())
                return false;

            try
            {
                await _mergeBoardFirestore.SaveSlotsAsync(changedSlots);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"보드 슬롯 저장 실패 : {exception.Message}", this);
                return false;
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
