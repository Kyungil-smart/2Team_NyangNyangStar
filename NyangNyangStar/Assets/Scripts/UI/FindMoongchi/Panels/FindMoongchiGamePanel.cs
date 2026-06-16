using System;
using System.Collections.Generic;
using TMPro;
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
        [SerializeField] private Color _highlightColor = new(1f, 0f, 0f, 0.45f);

        [Header("Tile Auto Generate")]
        [SerializeField] private FindMoongchiTileView _tilePrefab;
        [SerializeField] private int _defaultBoardWidth = FindMoongchiConstants.BoardWidth;
        [SerializeField] private int _defaultBoardHeight = FindMoongchiConstants.BoardHeight;
        [SerializeField] private bool _generateTilesOnInit = true;

        [Header("Targets")]
        [SerializeField] private List<FindMoongchiTargetHintView> _targetHints = new();

        [Header("Tools")]
        [SerializeField] private List<FindMoongchiToolSlotView> _toolSlots = new();

        private readonly List<FindMoongchiTileView> _tileViews = new();
        private readonly List<Image> _highlightImages = new();
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

            SetToolItemIds(FindMoongchiConstants.DefaultToolItemIds);

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

        public void SetData(FindMoongchiGameViewData data)
        {
            if (data == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] SetData 데이터가 null입니다.", DebugType.FindMoongchi, this);
                return;
            }

            int boardWidth = data.BoardWidth > 0 ? data.BoardWidth : _defaultBoardWidth;
            int boardHeight = data.BoardHeight > 0 ? data.BoardHeight : _defaultBoardHeight;

            EnsureTiles(boardWidth, boardHeight);

            DebugTool.Log(
                $"[FindMoongchiGamePanel] 데이터 적용: 보드={boardWidth}x{boardHeight}, 주차={data.CurrentWeek}, 탐색기회={data.SearchChance}, 공개타일={data.RevealedTileIndices.Count}, 도구={data.Tools.Count}, 힌트={data.TargetHints.Count}",
                DebugType.FindMoongchi,
                this);

            SetText(_weekText, $"{data.CurrentWeek}주차");
            SetText(_remainTimeText, data.RemainTimeText);
            SetText(_searchChanceText, data.SearchChance.ToString());
            SetText(_energyProgressText, $"{data.EnergySpendProgress}/{data.EnergySpendTarget}");

            _revealedTiles.Clear();
            foreach (int tileIndex in data.RevealedTileIndices)
            {
                if (tileIndex < 0 || tileIndex >= CurrentTileCount)
                    continue;

                _revealedTiles.Add(tileIndex);
            }

            RefreshTiles();
            RefreshTools(data.Tools);
            RefreshTargetHints(data.TargetHints);
            ClearHighlight();
        }

        public void RevealTiles(IEnumerable<int> tileIndices)
        {
            if (tileIndices == null)
            {
                DebugTool.Warning("[FindMoongchiGamePanel] 공개할 타일 목록이 null입니다.", DebugType.FindMoongchi, this);
                return;
            }

            int beforeCount = _revealedTiles.Count;

            foreach (int tileIndex in tileIndices)
            {
                if (tileIndex < 0 || tileIndex >= CurrentTileCount)
                    continue;

                _revealedTiles.Add(tileIndex);
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 타일 공개 요청 반영: 이전={beforeCount}, 이후={_revealedTiles.Count}", DebugType.FindMoongchi, this);
            RefreshTiles();
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

        private void RefreshTiles()
        {
            int activeCount = 0;

            for (int i = 0; i < _tileViews.Count; i++)
            {
                FindMoongchiTileView tileView = _tileViews[i];

                if (tileView == null || !tileView.gameObject.activeSelf)
                    continue;

                tileView.SetRevealed(_revealedTiles.Contains(i));
                activeCount++;
            }

            DebugTool.Log($"[FindMoongchiGamePanel] 타일 상태 갱신: 전체={activeCount}, 공개={_revealedTiles.Count}", DebugType.FindMoongchi, this);
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
        }
    }
}
