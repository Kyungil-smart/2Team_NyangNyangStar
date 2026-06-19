using System;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiGamePanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _weekText;
        [SerializeField] private TMP_Text _remainTimeText;
        [SerializeField] private TMP_Text _searchChanceText;
        [SerializeField] private TMP_Text _energyProgressText;

        [Header("Board")]
        [SerializeField] private RectTransform _boardArea;
        [SerializeField] private Transform _tileRoot;
        [SerializeField] private RectTransform _highlightRoot;
        [SerializeField] private RectTransform _targetVisualRoot;
        [SerializeField] private Color _highlightColor = new(1f, 0f, 0f, 0.45f);

        [Header("Tile Auto Generate")]
        [SerializeField] private FindMoongchiTileView _tilePrefab;
        [SerializeField] private int _defaultBoardWidth = FindMoongchiConstants.BoardWidth;
        [SerializeField] private int _defaultBoardHeight = FindMoongchiConstants.BoardHeight;
        [SerializeField] private bool _generateTilesOnInit = true;

        [Header("Targets")]
        [SerializeField] private List<FindMoongchiTargetHintView> _targetHints = new();

        [Header("Found Mark")]
        [SerializeField] private bool _showFoundMark = true;
        [SerializeField] private string _foundMarkSpriteKey = FindMoongchiSpriteKeys.IconCorrect;
        [SerializeField] private bool _fitFoundMarkToTarget = true;
        [SerializeField] private Vector2 _foundMarkSize = new(90f, 90f);
        [SerializeField] private float _foundMarkScale = 0.75f;
        [SerializeField] private Vector2 _foundMarkMinSize = new(48f, 48f);
        [SerializeField] private Vector2 _foundMarkMaxSize = new(180f, 180f);
        [SerializeField] private Vector2 _foundMarkOffset = Vector2.zero;

        [Header("Tools")]
        [SerializeField] private List<FindMoongchiToolSlotView> _toolSlots = new();

        private readonly List<FindMoongchiTileView> _tileViews = new();
        private readonly List<Image> _highlightImages = new();
        private readonly List<TargetVisualEntry> _targetVisualEntries = new();
        private readonly HashSet<int> _revealedTiles = new();

        private readonly int[] _toolItemIds = new int[FindMoongchiConstants.ToolSlotCount];

        private FindMoongchiToolSlotView _currentToolSlot;
        private int _currentPreviewTileIndex = -1;
        private int _currentBoardWidth = FindMoongchiConstants.BoardWidth;
        private int _currentBoardHeight = FindMoongchiConstants.BoardHeight;

        private int CurrentTileCount => Mathf.Max(0, _currentBoardWidth * _currentBoardHeight);

        public event Action OnBackButtonClicked;
        public event Action<int, int> OnToolDropped;

        public void Init()
        {
            DebugTool.Log("[FindMoongchiGamePanel] 초기화 시작", DebugType.FindMoongchi, this);

            EnsureToolItemIdsInitialized();

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            ResolveToolSlots();
            BindToolSlots();

            if (_generateTilesOnInit)
                EnsureTiles(_defaultBoardWidth, _defaultBoardHeight);

            DebugTool.Log(
                $"[FindMoongchiGamePanel] 초기화 완료: 타일={_tileViews.Count}, 보드={_currentBoardWidth}x{_currentBoardHeight}, 도구슬롯={_toolSlots.Count}, 힌트={_targetHints.Count}",
                DebugType.FindMoongchi,
                this);
        }

        private void EnsureToolItemIdsInitialized()
        {
            bool hasValidToolId = false;

            for (int i = 0; i < _toolItemIds.Length; i++)
            {
                if (_toolItemIds[i] > 0)
                {
                    hasValidToolId = true;
                    break;
                }
            }

            if (hasValidToolId)
                return;

            SetToolItemIds(FindMoongchiConstants.DefaultToolItemIds);
        }

        public void SetToolItemIds(IReadOnlyList<int> toolItemIds)
        {
            IReadOnlyList<int> defaults = FindMoongchiConstants.DefaultToolItemIds;

            for (int i = 0; i < FindMoongchiConstants.ToolSlotCount; i++)
            {
                int value = toolItemIds != null && i < toolItemIds.Count ? toolItemIds[i] : 0;

                if (value <= 0)
                    value = defaults[i];

                _toolItemIds[i] = value;
            }

            DebugTool.Log(
                $"[FindMoongchiGamePanel] 도구 ID 적용: Row={_toolItemIds[0]}, Column={_toolItemIds[1]}, Square4x4={_toolItemIds[2]}",
                DebugType.FindMoongchi,
                this);
        }

        public void SetData(FindMoongchiGameViewData data, bool animateNewReveals = false)
        {
            if (data == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] SetData 데이터가 null입니다.", DebugType.FindMoongchi, this);
                return;
            }

            int boardWidth = data.BoardWidth > 0 ? data.BoardWidth : _defaultBoardWidth;
            int boardHeight = data.BoardHeight > 0 ? data.BoardHeight : _defaultBoardHeight;

            EnsureTiles(boardWidth, boardHeight);
            Canvas.ForceUpdateCanvases();

            DebugTool.Log(
                $"[FindMoongchiGamePanel] 데이터 적용: 보드={boardWidth}x{boardHeight}, 주차={data.CurrentWeek}, 탐색기회={data.SearchChance}, 공개타일={data.RevealedTileIndices.Count}, 도구={data.Tools.Count}, 힌트={data.TargetHints.Count}, 목표이미지={data.TargetVisuals.Count}",
                DebugType.FindMoongchi,
                this);

            SetText(_weekText, $"{data.CurrentWeek}주차");
            SetText(_remainTimeText, data.RemainTimeText);
            SetText(_searchChanceText, data.SearchChance.ToString());
            SetText(_energyProgressText, $"{data.EnergySpendProgress}/{data.EnergySpendTarget}");

            HashSet<int> previousRevealedTiles = new(_revealedTiles);

            _revealedTiles.Clear();
            foreach (int tileIndex in data.RevealedTileIndices)
            {
                if (tileIndex < 0 || tileIndex >= CurrentTileCount)
                    continue;

                _revealedTiles.Add(tileIndex);
            }

            RefreshTargetVisuals(data.TargetVisuals);
            RefreshTiles(animateNewReveals, previousRevealedTiles);
            RefreshTools(data.Tools);
            RefreshTargetHints(data.TargetHints);
            ClearHighlight();
        }

        public void RevealTiles(IEnumerable<int> tileIndices, bool animate = true)
        {
            if (tileIndices == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] 공개할 타일 목록이 null입니다.", DebugType.FindMoongchi, this);
                return;
            }

            int beforeCount = _revealedTiles.Count;
            List<int> newlyRevealedTiles = new();

            foreach (int tileIndex in tileIndices)
            {
                if (tileIndex < 0 || tileIndex >= CurrentTileCount)
                    continue;

                if (_revealedTiles.Add(tileIndex))
                    newlyRevealedTiles.Add(tileIndex);
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 타일 공개 요청 반영: 이전={beforeCount}, 이후={_revealedTiles.Count}, 신규={newlyRevealedTiles.Count}", DebugType.FindMoongchi, this);

            foreach (int tileIndex in newlyRevealedTiles)
            {
                if (tileIndex < 0 || tileIndex >= _tileViews.Count)
                    continue;

                _tileViews[tileIndex]?.SetRevealed(true, animate);
            }
        }

        public IReadOnlyList<int> GetAffectedTiles(int toolItemId, int tileIndex)
        {
            return CalculateAffectedTiles(toolItemId, tileIndex);
        }

        private void EnsureTiles(int boardWidth, int boardHeight)
        {
            boardWidth = boardWidth > 0 ? boardWidth : _defaultBoardWidth;
            boardHeight = boardHeight > 0 ? boardHeight : _defaultBoardHeight;

            if (_tileRoot == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] TileRoot가 연결되지 않았습니다.", DebugType.FindMoongchi, this);
                return;
            }

            if (_tilePrefab == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] TilePrefab이 연결되지 않았습니다.", DebugType.FindMoongchi, this);
                CacheExistingTiles(boardWidth, boardHeight);
                return;
            }

            _currentBoardWidth = boardWidth;
            _currentBoardHeight = boardHeight;
            int requiredCount = boardWidth * boardHeight;

            ApplyGridLayout(boardWidth);
            CacheExistingTiles(boardWidth, boardHeight);

            int beforePoolCount = _tileViews.Count;

            while (_tileViews.Count < requiredCount)
            {
                FindMoongchiTileView tileView = Instantiate(_tilePrefab, _tileRoot);
                _tileViews.Add(tileView);
            }

            for (int i = 0; i < _tileViews.Count; i++)
            {
                FindMoongchiTileView tileView = _tileViews[i];

                if (tileView == null)
                    continue;

                bool isActive = i < requiredCount;
                tileView.gameObject.SetActive(isActive);

                if (!isActive)
                    continue;

                tileView.name = $"Tile_{i:00}";
                tileView.Init(i);
                tileView.SetRevealed(_revealedTiles.Contains(i));
            }

            if (_tileViews.Count != beforePoolCount || requiredCount != beforePoolCount)
            {
                DebugTool.Log(
                    $"[FindMoongchiGamePanel] 타일 자동 생성/갱신 완료: 보드={boardWidth}x{boardHeight}, 사용={requiredCount}, 풀={_tileViews.Count}, 생성={Mathf.Max(0, _tileViews.Count - beforePoolCount)}",
                    DebugType.FindMoongchi,
                    this);
            }
        }

        private void CacheExistingTiles(int boardWidth, int boardHeight)
        {
            if (_tileRoot == null)
                return;

            if (_tileViews.Count > 0)
                return;

            _currentBoardWidth = boardWidth;
            _currentBoardHeight = boardHeight;

            for (int i = 0; i < _tileRoot.childCount; i++)
            {
                Transform child = _tileRoot.GetChild(i);
                FindMoongchiTileView tileView = child.GetComponent<FindMoongchiTileView>();

                if (tileView == null)
                    continue;

                _tileViews.Add(tileView);
            }

            if (_tileViews.Count > 0)
                DebugTool.Log($"[FindMoongchiGamePanel] 기존 타일 캐싱: {_tileViews.Count}개", DebugType.FindMoongchi, this);
        }

        private void ApplyGridLayout(int boardWidth)
        {
            if (_tileRoot == null)
                return;

            GridLayoutGroup gridLayoutGroup = _tileRoot.GetComponent<GridLayoutGroup>();

            if (gridLayoutGroup == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] TileRoot에 GridLayoutGroup이 없습니다.", DebugType.FindMoongchi, this);
                return;
            }

            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = boardWidth;
        }

        private void ResolveToolSlots()
        {
            if (_toolSlots.Count > 0)
            {
                DebugTool.Log($"[FindMoongchiGamePanel] 인스펙터 도구 슬롯 사용: {_toolSlots.Count}개", DebugType.FindMoongchi, this);
                return;
            }

            FindMoongchiToolSlotView[] foundSlots = GetComponentsInChildren<FindMoongchiToolSlotView>(true);
            _toolSlots.AddRange(foundSlots);
            DebugTool.Log($"[FindMoongchiGamePanel] 자동 검색 도구 슬롯: {_toolSlots.Count}개", DebugType.FindMoongchi, this);
        }

        private void BindToolSlots()
        {
            int bindCount = 0;
            foreach (FindMoongchiToolSlotView slot in _toolSlots)
            {
                if (slot == null)
                    continue;

                slot.OnBeginDragTool -= HandleBeginDragTool;
                slot.OnDragTool -= HandleDragTool;
                slot.OnEndDragTool -= HandleEndDragTool;

                slot.OnBeginDragTool += HandleBeginDragTool;
                slot.OnDragTool += HandleDragTool;
                slot.OnEndDragTool += HandleEndDragTool;
                bindCount++;
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 도구 슬롯 이벤트 바인딩 완료: {bindCount}개", DebugType.FindMoongchi, this);
        }

        private void RefreshTiles(bool animateNewReveals = false, HashSet<int> previousRevealedTiles = null)
        {
            int activeCount = 0;
            int animatedCount = 0;

            for (int i = 0; i < _tileViews.Count; i++)
            {
                FindMoongchiTileView tileView = _tileViews[i];

                if (tileView == null || !tileView.gameObject.activeSelf)
                    continue;

                bool isRevealed = _revealedTiles.Contains(i);
                bool shouldAnimate = animateNewReveals && isRevealed && (previousRevealedTiles == null || !previousRevealedTiles.Contains(i));

                tileView.SetRevealed(isRevealed, shouldAnimate);

                if (shouldAnimate)
                    animatedCount++;

                activeCount++;
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 타일 상태 갱신: 전체={activeCount}, 공개={_revealedTiles.Count}, 연출={animatedCount}", DebugType.FindMoongchi, this);
        }

        private void RefreshTargetVisuals(IReadOnlyList<FindMoongchiTargetVisualViewData> targets)
        {
            if (_targetVisualRoot == null)
            {
                if (targets != null && targets.Count > 0)
                    DebugTool.Warning("[FindMoongchiGamePanel] TargetVisualRoot가 연결되지 않았습니다.", DebugType.FindMoongchi, this);

                return;
            }

            int targetCount = targets?.Count ?? 0;
            EnsureTargetVisualCount(targetCount);

            for (int i = 0; i < _targetVisualEntries.Count; i++)
            {
                TargetVisualEntry entry = _targetVisualEntries[i];

                if (entry == null || entry.GameObject == null)
                    continue;

                bool isActive = i < targetCount;
                entry.GameObject.SetActive(isActive);

                if (!isActive)
                    continue;

                FindMoongchiTargetVisualViewData data = targets[i];
                ApplyTargetVisual(entry, data);
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 목표물 이미지 갱신: {targetCount}개", DebugType.FindMoongchi, this);
        }

        private void EnsureTargetVisualCount(int count)
        {
            int beforeCount = _targetVisualEntries.Count;

            while (_targetVisualEntries.Count < count)
            {
                GameObject go = new GameObject($"TargetVisual_{_targetVisualEntries.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_targetVisualRoot, false);
                go.transform.SetAsLastSibling();

                Image image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;

                GameObject foundMarkGo = new GameObject("FoundMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                foundMarkGo.transform.SetParent(go.transform, false);

                Image foundMarkImage = foundMarkGo.GetComponent<Image>();
                foundMarkImage.raycastTarget = false;
                foundMarkImage.preserveAspect = true;
                foundMarkGo.SetActive(false);

                TargetVisualEntry entry = new TargetVisualEntry(go, image, foundMarkGo, foundMarkImage);
                _targetVisualEntries.Add(entry);
            }

            if (_targetVisualEntries.Count > beforeCount)
                DebugTool.Log($"[FindMoongchiGamePanel] 목표물 이미지 풀 확장: {beforeCount} → {_targetVisualEntries.Count}", DebugType.FindMoongchi, this);
        }

        private void ApplyTargetVisual(TargetVisualEntry entry, FindMoongchiTargetVisualViewData data)
        {
            if (entry == null || entry.Image == null || data == null)
                return;

            entry.GameObject.name = $"TargetVisual_{data.TargetId}_{data.TargetName}";

            if (!TryCalculateTargetBounds(data.CellIndices, out Vector2 center, out Vector2 size))
            {
                entry.GameObject.SetActive(false);
                return;
            }

            RectTransform rectTransform = entry.RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = center;
            rectTransform.sizeDelta = size;

            if (!string.IsNullOrWhiteSpace(data.IconKey))
            {
                entry.Image.enabled = true;

                if (entry.Controller == null)
                    entry.Controller = new UISpriteController(entry.Image);

                entry.Controller.ChangeSprite(data.IconKey);
            }
            else
            {
                entry.Image.sprite = data.Icon;
                entry.Image.enabled = data.Icon != null;
            }

            ApplyFoundMark(entry, data.IsFound);
        }

        private void ApplyFoundMark(TargetVisualEntry entry, bool isFound)
        {
            if (entry == null || entry.FoundMarkGameObject == null)
                return;

            bool shouldShow = _showFoundMark && isFound &&
                              entry.FoundMarkImage != null &&
                              !string.IsNullOrWhiteSpace(_foundMarkSpriteKey);
            entry.FoundMarkGameObject.SetActive(shouldShow);

            if (!shouldShow)
                return;

            RectTransform rectTransform = entry.FoundMarkRectTransform;

            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = _foundMarkOffset;
                rectTransform.sizeDelta = GetFoundMarkSize(entry);
                rectTransform.SetAsLastSibling();
            }

            entry.FoundMarkImage.preserveAspect = true;
            entry.FoundMarkController ??= new UISpriteController(entry.FoundMarkImage);

            if (entry.FoundMarkLoadedKey == _foundMarkSpriteKey && entry.FoundMarkImage.sprite != null)
                return;

            entry.FoundMarkLoadedKey = _foundMarkSpriteKey;
            entry.FoundMarkController.ChangeSprite(_foundMarkSpriteKey);
        }

        private Vector2 GetFoundMarkSize(TargetVisualEntry entry)
        {
            if (!_fitFoundMarkToTarget || entry?.RectTransform == null)
                return _foundMarkSize;

            Vector2 targetSize = entry.RectTransform.rect.size;
            float targetDiameter = Mathf.Min(Mathf.Abs(targetSize.x), Mathf.Abs(targetSize.y));

            if (targetDiameter <= 0f)
                return _foundMarkSize;

            float minDiameter = Mathf.Min(Mathf.Abs(_foundMarkMinSize.x), Mathf.Abs(_foundMarkMinSize.y));
            float maxDiameter = Mathf.Min(Mathf.Abs(_foundMarkMaxSize.x), Mathf.Abs(_foundMarkMaxSize.y));
            float diameter = targetDiameter * Mathf.Max(0f, _foundMarkScale);

            if (maxDiameter > 0f)
                diameter = Mathf.Min(diameter, maxDiameter);

            if (minDiameter > 0f)
                diameter = Mathf.Max(diameter, minDiameter);

            return new Vector2(diameter, diameter);
        }

        private bool TryCalculateTargetBounds(IReadOnlyList<int> cellIndices, out Vector2 center, out Vector2 size)
        {
            center = Vector2.zero;
            size = Vector2.zero;

            if (_targetVisualRoot == null || cellIndices == null || cellIndices.Count == 0)
                return false;

            bool hasValidCell = false;
            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            Vector3[] corners = new Vector3[4];

            foreach (int tileIndex in cellIndices)
            {
                if (tileIndex < 0 || tileIndex >= _tileViews.Count)
                    continue;

                FindMoongchiTileView tileView = _tileViews[tileIndex];
                if (tileView == null || !tileView.gameObject.activeSelf)
                    continue;

                RectTransform tileRect = tileView.transform as RectTransform;
                if (tileRect == null)
                    continue;

                tileRect.GetWorldCorners(corners);

                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 local = _targetVisualRoot.InverseTransformPoint(corners[i]);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }

                hasValidCell = true;
            }

            if (!hasValidCell)
                return false;

            center = (min + max) * 0.5f;
            size = max - min;
            return size.x > 0f && size.y > 0f;
        }

        private void RefreshTools(IReadOnlyList<FindMoongchiToolViewData> tools)
        {
            for (int i = 0; i < _toolSlots.Count; i++)
            {
                FindMoongchiToolViewData data = tools != null && i < tools.Count ? tools[i] : null;
                _toolSlots[i]?.SetData(data);
            }
        }

        private void RefreshTargetHints(IReadOnlyList<FindMoongchiTargetHintViewData> hints)
        {
            for (int i = 0; i < _targetHints.Count; i++)
            {
                FindMoongchiTargetHintViewData data = hints != null && i < hints.Count ? hints[i] : null;
                _targetHints[i]?.SetData(data);
            }
        }

        private void HandleBackButtonClicked()
        {
            DebugTool.Log("[FindMoongchiGamePanel] 뒤로가기 버튼 클릭", DebugType.FindMoongchi, this);
            OnBackButtonClicked?.Invoke();
        }

        private void HandleBeginDragTool(FindMoongchiToolSlotView slot, PointerEventData eventData)
        {
            DebugTool.Log($"[FindMoongchiGamePanel] 도구 드래그 시작: ToolId={slot?.ToolItemId}, Count={slot?.Count}", DebugType.FindMoongchi, this);
            _currentToolSlot = slot;
            _currentPreviewTileIndex = -1;
        }

        private void HandleDragTool(FindMoongchiToolSlotView slot, PointerEventData eventData)
        {
            if (slot == null || eventData == null)
                return;

            if (!TryGetTileIndex(eventData, out int tileIndex))
            {
                _currentPreviewTileIndex = -1;
                ClearHighlight();
                return;
            }

            if (_currentPreviewTileIndex == tileIndex)
                return;

            _currentPreviewTileIndex = tileIndex;
            ShowHighlight(CalculateAffectedTiles(slot.ToolItemId, tileIndex));
        }

        private void HandleEndDragTool(FindMoongchiToolSlotView slot, PointerEventData eventData)
        {
            ClearHighlight();
            _currentToolSlot = null;

            if (slot == null || eventData == null)
                return;

            if (!TryGetTileIndex(eventData, out int tileIndex))
            {
                DebugTool.Log($"[FindMoongchiGamePanel] 보드 밖 드롭 취소: ToolId={slot.ToolItemId}", DebugType.FindMoongchi, this);
                return;
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 도구 드롭 완료: ToolId={slot.ToolItemId}, Tile={tileIndex}", DebugType.FindMoongchi, this);
            OnToolDropped?.Invoke(slot.ToolItemId, tileIndex);
        }

        private bool TryGetTileIndex(PointerEventData eventData, out int tileIndex)
        {
            tileIndex = -1;

            if (_boardArea == null || eventData == null || _currentBoardWidth <= 0 || _currentBoardHeight <= 0)
                return false;

            Camera eventCamera = eventData.pressEventCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _boardArea,
                    eventData.position,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = _boardArea.rect;

            if (!rect.Contains(localPoint))
                return false;

            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float normalizedY = Mathf.InverseLerp(rect.yMax, rect.yMin, localPoint.y);

            int column = Mathf.Clamp(Mathf.FloorToInt(normalizedX * _currentBoardWidth), 0, _currentBoardWidth - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt(normalizedY * _currentBoardHeight), 0, _currentBoardHeight - 1);

            tileIndex = row * _currentBoardWidth + column;
            return tileIndex >= 0 && tileIndex < CurrentTileCount;
        }

        private List<int> CalculateAffectedTiles(int toolItemId, int tileIndex)
        {
            return FindMoongchiGameLogic.CalculateAffectedTiles(
                toolItemId,
                tileIndex,
                _currentBoardWidth,
                _currentBoardHeight,
                _toolItemIds);
        }

        private void ShowHighlight(IReadOnlyList<int> tileIndices)
        {
            ClearHighlight();

            if (_highlightRoot == null || _boardArea == null || tileIndices == null)
                return;

            if (_currentBoardWidth <= 0 || _currentBoardHeight <= 0)
                return;

            EnsureHighlightImageCount(tileIndices.Count);

            Rect rect = _boardArea.rect;
            float cellWidth = rect.width / _currentBoardWidth;
            float cellHeight = rect.height / _currentBoardHeight;

            for (int i = 0; i < tileIndices.Count; i++)
            {
                int tileIndex = tileIndices[i];

                if (tileIndex < 0 || tileIndex >= CurrentTileCount)
                    continue;

                int row = tileIndex / _currentBoardWidth;
                int column = tileIndex % _currentBoardWidth;

                Image image = _highlightImages[i];
                RectTransform rectTransform = image.rectTransform;

                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);
                rectTransform.anchoredPosition = new Vector2(
                    rect.xMin + cellWidth * (column + 0.5f),
                    rect.yMax - cellHeight * (row + 0.5f));

                image.color = _highlightColor;
                image.gameObject.SetActive(true);
            }
        }

        private void EnsureHighlightImageCount(int count)
        {
            int beforeCount = _highlightImages.Count;
            while (_highlightImages.Count < count)
            {
                GameObject go = new GameObject($"Highlight_{_highlightImages.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_highlightRoot, false);

                Image image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _highlightImages.Add(image);
            }

            if (_highlightImages.Count > beforeCount)
                DebugTool.Log($"[FindMoongchiGamePanel] 하이라이트 풀 확장: {beforeCount} → {_highlightImages.Count}", DebugType.FindMoongchi, this);
        }

        private void ClearHighlight()
        {
            foreach (Image image in _highlightImages)
            {
                if (image != null)
                    image.gameObject.SetActive(false);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);

            foreach (FindMoongchiToolSlotView slot in _toolSlots)
            {
                if (slot == null)
                    continue;

                slot.OnBeginDragTool -= HandleBeginDragTool;
                slot.OnDragTool -= HandleDragTool;
                slot.OnEndDragTool -= HandleEndDragTool;
            }

            foreach (TargetVisualEntry entry in _targetVisualEntries)
                entry?.Dispose();

            _targetVisualEntries.Clear();
        }

        private sealed class TargetVisualEntry
        {
            public readonly GameObject GameObject;
            public readonly Image Image;
            public readonly RectTransform RectTransform;
            public readonly GameObject FoundMarkGameObject;
            public readonly Image FoundMarkImage;
            public readonly RectTransform FoundMarkRectTransform;
            public UISpriteController Controller;
            public UISpriteController FoundMarkController;
            public string FoundMarkLoadedKey;

            public TargetVisualEntry(GameObject gameObject, Image image, GameObject foundMarkGameObject, Image foundMarkImage)
            {
                GameObject = gameObject;
                Image = image;
                RectTransform = gameObject != null ? gameObject.transform as RectTransform : null;
                FoundMarkGameObject = foundMarkGameObject;
                FoundMarkImage = foundMarkImage;
                FoundMarkRectTransform = foundMarkGameObject != null ? foundMarkGameObject.transform as RectTransform : null;
            }

            public void Dispose()
            {
                Controller?.Dispose();
                Controller = null;

                FoundMarkController?.Dispose();
                FoundMarkController = null;
                FoundMarkLoadedKey = null;
            }
        }
    }
}
