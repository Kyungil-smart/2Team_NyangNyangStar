using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiGamePanel : MonoBehaviour
    {
        private const float SearchChanceHelpPanelWidth = 560f;
        private const float SearchChanceHelpPanelHeight = 86f;
        private const float SearchChanceHelpPanelMargin = 16f;
        private const float SearchChanceHelpPanelYOffset = 12f;

        [Header("Header")]
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _weekText;
        [SerializeField] private TMP_Text _remainTimeText;
        [SerializeField] private TMP_Text _searchChanceText;
        [SerializeField] private TMP_Text _energyProgressText;
        [SerializeField] private Image _energyProgressFillImage;
        [SerializeField] private bool _forceEnergyProgressFillType = true;
        [SerializeField] private Button _searchChanceHelpButton;
        [SerializeField] private GameObject _searchChanceHelpPanel;
        [SerializeField] private RectTransform _searchChanceHelpBubbleRect;
        [SerializeField] private Button _searchChanceHelpCloseAreaButton;

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
        [SerializeField] private Vector2 _foundMarkSize = new(90f, 90f);
        [SerializeField] private Vector2 _foundMarkOffset = Vector2.zero;

        [Header("Tools")]
        [SerializeField] private List<FindMoongchiToolSlotView> _toolSlots = new();

        [Header("First Touch Guide")]
        [SerializeField] private GameObject _firstTouchGuideRoot;
        [SerializeField] private CanvasGroup _firstTouchGuideCanvasGroup;
        [SerializeField] private bool _useFirstTouchGuide = true;
        [SerializeField] private float _firstTouchGuideMinAlpha = 0f;
        [SerializeField] private float _firstTouchGuideMaxAlpha = 0.8f;
        [SerializeField] private float _firstTouchGuideFadeDuration = 0.45f;

        [Header("Responsive Layout")]
        [SerializeField] private bool _useResponsiveLayout = true;
        [SerializeField] private RectTransform _layoutRoot;
        [SerializeField] private RectTransform _headerRoot;
        [SerializeField] private RectTransform _toolBarRoot;
        [SerializeField] private Vector2 _referencePanelSize = new(1000f, 1650f);
        [SerializeField] private Vector2 _boardMinSize = new(560f, 720f);
        [SerializeField] private Vector2 _boardMaxSize = new(770f, 990f);
        [SerializeField] private float _horizontalPadding = 60f;
        [SerializeField] private float _topPadding = 0f;
        [SerializeField] private float _bottomPadding = 50f;
        [SerializeField] private float _headerHeight = 300f;
        [SerializeField] private float _toolBarHeight = 250f;
        [SerializeField] private float _contentSpacing = 30f;
        [SerializeField] private float _minTileSize = 64f;
        [SerializeField] private float _maxTileSize = 110f;

        private readonly List<FindMoongchiTileView> _tileViews = new();
        private readonly List<Image> _highlightImages = new();
        private readonly List<TargetVisualEntry> _targetVisualEntries = new();
        private readonly HashSet<int> _revealedTiles = new();

        private readonly int[] _toolItemIds = new int[FindMoongchiConstants.ToolSlotCount];

        private FindMoongchiToolSlotView _currentToolSlot;
        private int _currentPreviewTileIndex = -1;
        private int _currentBoardWidth = FindMoongchiConstants.BoardWidth;
        private int _currentBoardHeight = FindMoongchiConstants.BoardHeight;
        private Tween _firstTouchGuideTween;
        private int _currentFirstTouchGuideBoardKey = int.MinValue;
        private bool _isFirstTouchGuideCompletedForBoard;
        private bool _isFirstTouchGuideDragInProgress;
        private bool _hasFirstTouchGuideUsableTool;
        private bool _isApplyingResponsiveLayout;
        private readonly Vector3[] _searchChanceHelpButtonCorners = new Vector3[4];

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

            BindButton(_searchChanceHelpButton, HandleSearchChanceHelpButtonClicked);
            BindButton(_searchChanceHelpCloseAreaButton, HideSearchChanceHelpPanel);
            HideSearchChanceHelpPanel();

            ResolveEnergyProgressFillImage();
            ResolveToolSlots();
            BindToolSlots();

            if (_generateTilesOnInit)
                EnsureTiles(_defaultBoardWidth, _defaultBoardHeight);

            ApplyResponsiveLayout();
            TryStartFirstTouchGuide();

            DebugTool.Log(
                $"[FindMoongchiGamePanel] 초기화 완료: 타일={_tileViews.Count}, 보드={_currentBoardWidth}x{_currentBoardHeight}, 도구슬롯={_toolSlots.Count}, 힌트={_targetHints.Count}",
                DebugType.FindMoongchi,
                this);
        }

        private void OnEnable()
        {
            ApplyResponsiveLayout();
            TryStartFirstTouchGuide();
        }

        private void OnDisable()
        {
            _isFirstTouchGuideDragInProgress = false;
            HideSearchChanceHelpPanel();
            StopFirstTouchGuideTween();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            ApplyResponsiveLayout();
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
            RefreshFirstTouchGuideBoardState(data, boardWidth, boardHeight);

            EnsureTiles(boardWidth, boardHeight);
            ApplyResponsiveLayout();
            Canvas.ForceUpdateCanvases();

            DebugTool.Log(
                $"[FindMoongchiGamePanel] 데이터 적용: 보드={boardWidth}x{boardHeight}, 주차={data.CurrentWeek}, 탐색기회={data.SearchChance}, 공개타일={data.RevealedTileIndices.Count}, 도구={data.Tools.Count}, 힌트={data.TargetHints.Count}, 목표이미지={data.TargetVisuals.Count}",
                DebugType.FindMoongchi,
                this);

            SetText(_weekText, $"{data.CurrentWeek}주차");
            SetText(_remainTimeText, data.RemainTimeText);
            SetText(_searchChanceText, "탐색 기회 : " + data.SearchChance.ToString());
            RefreshEnergyProgress(data.EnergySpendProgress, data.EnergySpendTarget);

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
            TryStartFirstTouchGuide();
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
            ApplyResponsiveGridCells();
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
            ApplyResponsiveGridCells();
        }

        private void ApplyResponsiveLayout()
        {
            if (_isApplyingResponsiveLayout)
                return;

            ResolveResponsiveLayoutReferences();

            if (!_useResponsiveLayout)
            {
                ApplyResponsiveGridCells();
                RefreshSearchChanceHelpPanelLayout();
                return;
            }

            if (_layoutRoot == null || _boardArea == null)
            {
                RefreshSearchChanceHelpPanelLayout();
                return;
            }

            Rect rootRect = _layoutRoot.rect;

            if (rootRect.width <= 0f || rootRect.height <= 0f)
            {
                RefreshSearchChanceHelpPanelLayout();
                return;
            }

            _isApplyingResponsiveLayout = true;

            try
            {
                float scale = CalculateResponsiveScale(rootRect.size);
                float horizontalPadding = Mathf.Max(0f, _horizontalPadding * scale);
                float topPadding = Mathf.Max(0f, _topPadding * scale);
                float bottomPadding = Mathf.Max(0f, _bottomPadding * scale);
                float headerHeight = Mathf.Max(0f, _headerHeight * scale);
                float toolBarHeight = Mathf.Max(0f, _toolBarHeight * scale);
                float contentSpacing = Mathf.Max(0f, _contentSpacing * scale);

                float contentWidth = Mathf.Max(1f, rootRect.width - horizontalPadding * 2f);
                float headerWidth = Mathf.Min(rootRect.width, Mathf.Max(contentWidth, _referencePanelSize.x * scale));
                float toolBarWidth = Mathf.Min(contentWidth, 850f * scale);

                ApplyTopRect(_headerRoot, headerWidth, headerHeight, topPadding);
                ApplyBottomRect(_toolBarRoot, toolBarWidth, toolBarHeight, bottomPadding);

                float contentTop = rootRect.yMax - topPadding - headerHeight;
                float contentBottom = rootRect.yMin + bottomPadding + toolBarHeight;
                float availableBoardHeight = Mathf.Max(1f, contentTop - contentBottom - contentSpacing * 2f);
                Vector2 boardSize = CalculateResponsiveBoardSize(contentWidth, availableBoardHeight, scale);

                _boardArea.anchorMin = new Vector2(0.5f, 0.5f);
                _boardArea.anchorMax = new Vector2(0.5f, 0.5f);
                _boardArea.pivot = new Vector2(0.5f, 0.5f);
                _boardArea.sizeDelta = boardSize;
                _boardArea.anchoredPosition = new Vector2(0f, (contentTop + contentBottom) * 0.5f);

                ApplyResponsiveGridCells(scale);
                RefreshSearchChanceHelpPanelLayout();
            }
            finally
            {
                _isApplyingResponsiveLayout = false;
            }
        }

        private Vector2 CalculateResponsiveBoardSize(float contentWidth, float availableBoardHeight, float scale)
        {
            float aspect = _currentBoardWidth > 0 && _currentBoardHeight > 0
                ? (float)_currentBoardWidth / _currentBoardHeight
                : (float)_defaultBoardWidth / Mathf.Max(1, _defaultBoardHeight);

            float maxWidth = Mathf.Min(contentWidth, Mathf.Max(1f, _boardMaxSize.x * scale));
            float maxHeight = Mathf.Min(availableBoardHeight, Mathf.Max(1f, _boardMaxSize.y * scale));
            float width = Mathf.Min(maxWidth, maxHeight * aspect);
            float height = width / aspect;

            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * aspect;
            }

            float minWidth = Mathf.Min(maxWidth, Mathf.Max(1f, _boardMinSize.x * scale));
            float minHeight = Mathf.Min(maxHeight, Mathf.Max(1f, _boardMinSize.y * scale));

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

        private void ApplyResponsiveGridCells(float scale = -1f)
        {
            if (_tileRoot == null || _currentBoardWidth <= 0 || _currentBoardHeight <= 0)
                return;

            RectTransform tileRootRect = _tileRoot as RectTransform;
            GridLayoutGroup gridLayoutGroup = _tileRoot.GetComponent<GridLayoutGroup>();

            if (tileRootRect == null || gridLayoutGroup == null)
                return;

            if (tileRootRect.rect.width <= 0f || tileRootRect.rect.height <= 0f)
                return;

            if (scale < 0f)
                scale = _layoutRoot != null ? CalculateResponsiveScale(_layoutRoot.rect.size) : 1f;

            float availableWidth = tileRootRect.rect.width - gridLayoutGroup.padding.horizontal - gridLayoutGroup.spacing.x * Mathf.Max(0, _currentBoardWidth - 1);
            float availableHeight = tileRootRect.rect.height - gridLayoutGroup.padding.vertical - gridLayoutGroup.spacing.y * Mathf.Max(0, _currentBoardHeight - 1);
            float cellSize = Mathf.Min(availableWidth / _currentBoardWidth, availableHeight / _currentBoardHeight);

            if (float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
                return;

            float minTileSize = Mathf.Max(1f, _minTileSize * scale);
            float maxTileSize = Mathf.Max(minTileSize, _maxTileSize * scale);
            cellSize = Mathf.Clamp(cellSize, minTileSize, maxTileSize);

            gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
            gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            LayoutRebuilder.MarkLayoutForRebuild(tileRootRect);
        }

        private void ApplyTopRect(RectTransform rectTransform, float width, float height, float topPadding)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -topPadding);
            rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        }

        private void ApplyBottomRect(RectTransform rectTransform, float width, float height, float bottomPadding)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottomPadding);
            rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        }

        private float CalculateResponsiveScale(Vector2 currentSize)
        {
            float referenceWidth = Mathf.Max(1f, _referencePanelSize.x);
            float referenceHeight = Mathf.Max(1f, _referencePanelSize.y);
            float widthScale = currentSize.x > 0f ? currentSize.x / referenceWidth : 1f;
            float heightScale = currentSize.y > 0f ? currentSize.y / referenceHeight : 1f;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.01f, 1f);
        }

        private void ResolveResponsiveLayoutReferences()
        {
            _layoutRoot ??= transform as RectTransform;

            if (_headerRoot == null)
            {
                Transform header = transform.Find("Header");
                _headerRoot = header as RectTransform;
            }

            if (_toolBarRoot == null)
            {
                Transform toolBar = transform.Find("ToolBar");
                _toolBarRoot = toolBar as RectTransform;
            }
        }

        private void ResolveEnergyProgressFillImage()
        {
            if (_energyProgressFillImage != null)
                return;

            _energyProgressFillImage = FindChildImage("RemainEnergyFill");

            if (_energyProgressFillImage == null)
                _energyProgressFillImage = FindChildImage("EnergyProgressFill");

            if (_energyProgressFillImage == null)
                DebugTool.Warning("[FindMoongchiGamePanel] 에너지 진행도 Fill Image를 찾지 못했습니다. RemainEnergyFill 오브젝트를 연결하세요.", DebugType.FindMoongchi, this);
        }

        private Image FindChildImage(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
                return null;

            Transform[] children = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];

                if (child == null || child.name != childName)
                    continue;

                return child.GetComponent<Image>();
            }

            return null;
        }

        private void RefreshEnergyProgress(int progress, int target)
        {
            int safeTarget = Mathf.Max(1, target);
            int safeProgress = Mathf.Clamp(progress, 0, safeTarget);
            float fillAmount = Mathf.Clamp01((float)safeProgress / safeTarget);

            SetText(_energyProgressText, $"{safeProgress}/{safeTarget}");

            ResolveEnergyProgressFillImage();

            if (_energyProgressFillImage == null)
                return;

            if (_forceEnergyProgressFillType && _energyProgressFillImage.type != Image.Type.Filled)
            {
                _energyProgressFillImage.type = Image.Type.Filled;
                _energyProgressFillImage.fillMethod = Image.FillMethod.Horizontal;
                _energyProgressFillImage.fillOrigin = 0;
            }

            _energyProgressFillImage.fillAmount = fillAmount;
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

            bool shouldShow = _showFoundMark && isFound;
            entry.FoundMarkGameObject.SetActive(shouldShow);

            if (!shouldShow || entry.FoundMarkImage == null)
                return;

            RectTransform markRect = entry.FoundMarkRectTransform;
            if (markRect != null)
            {
                markRect.anchorMin = new Vector2(0.5f, 0.5f);
                markRect.anchorMax = new Vector2(0.5f, 0.5f);
                markRect.pivot = new Vector2(0.5f, 0.5f);
                markRect.anchoredPosition = _foundMarkOffset;

                Vector2 targetSize = entry.RectTransform != null
                    ? entry.RectTransform.sizeDelta
                    : Vector2.zero;

                markRect.sizeDelta = targetSize.x > 0f && targetSize.y > 0f
                    ? targetSize
                    : _foundMarkSize;
                markRect.SetAsLastSibling();
            }

            entry.FoundMarkImage.raycastTarget = false;
            entry.FoundMarkImage.preserveAspect = false;

            if (string.IsNullOrWhiteSpace(_foundMarkSpriteKey))
                return;

            entry.FoundMarkController ??= new UISpriteController(entry.FoundMarkImage);

            if (entry.FoundMarkLoadedKey == _foundMarkSpriteKey && entry.FoundMarkImage.sprite != null)
                return;

            entry.FoundMarkLoadedKey = _foundMarkSpriteKey;
            entry.FoundMarkController.ChangeSprite(_foundMarkSpriteKey);
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
            _hasFirstTouchGuideUsableTool = HasUsableTool(tools);

            for (int i = 0; i < _toolSlots.Count; i++)
            {
                FindMoongchiToolViewData data = tools != null && i < tools.Count ? tools[i] : null;
                _toolSlots[i]?.SetData(data);
            }
        }

        private static bool HasUsableTool(IReadOnlyList<FindMoongchiToolViewData> tools)
        {
            if (tools == null)
                return false;

            for (int i = 0; i < tools.Count; i++)
            {
                FindMoongchiToolViewData tool = tools[i];

                if (tool != null && tool.Count > 0 && tool.IsUsable)
                    return true;
            }

            return false;
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
            HideSearchChanceHelpPanel();
            OnBackButtonClicked?.Invoke();
        }

        private void HandleSearchChanceHelpButtonClicked()
        {
            bool nextVisible = _searchChanceHelpPanel != null && !_searchChanceHelpPanel.activeSelf;
            SetSearchChanceHelpPanelVisible(nextVisible);
        }

        private void HideSearchChanceHelpPanel()
        {
            SetSearchChanceHelpPanelVisible(false);
        }

        private void SetSearchChanceHelpPanelVisible(bool visible)
        {
            if (_searchChanceHelpPanel != null && _searchChanceHelpPanel.activeSelf != visible)
                _searchChanceHelpPanel.SetActive(visible);

            if (visible)
                RefreshSearchChanceHelpPanelLayout();
        }

        private void RefreshSearchChanceHelpPanelLayout()
        {
            RectTransform layerRect = _searchChanceHelpPanel != null
                ? _searchChanceHelpPanel.transform as RectTransform
                : null;
            RectTransform panelRect = _searchChanceHelpBubbleRect;
            RectTransform buttonRect = _searchChanceHelpButton != null
                ? _searchChanceHelpButton.transform as RectTransform
                : null;

            if (panelRect == null && layerRect != null && layerRect.childCount > 0)
                panelRect = layerRect.GetChild(0) as RectTransform;

            if (panelRect == null || layerRect == null || buttonRect == null)
                return;

            layerRect.SetAsLastSibling();
            panelRect.SetAsLastSibling();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.pivot = new Vector2(0.5f, 0.5f);
            layerRect.localScale = Vector3.one;

            Rect layerBounds = ResolveSearchChanceHelpLayerBounds(layerRect);

            if (layerBounds.width <= 0f || layerBounds.height <= 0f)
                return;

            float maxScaleByWidth = (layerBounds.width - SearchChanceHelpPanelMargin * 2f) / SearchChanceHelpPanelWidth;
            float maxScaleByHeight = (layerBounds.height - SearchChanceHelpPanelMargin * 2f) / SearchChanceHelpPanelHeight;
            float fitScale = Mathf.Max(0.1f, Mathf.Min(maxScaleByWidth, maxScaleByHeight));
            float visualScale = Mathf.Min(1f, fitScale);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(SearchChanceHelpPanelWidth, SearchChanceHelpPanelHeight);
            panelRect.localScale = new Vector3(visualScale, visualScale, 1f);

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            buttonRect.GetWorldCorners(_searchChanceHelpButtonCorners);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, _searchChanceHelpButtonCorners[3]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRect, screenPoint, eventCamera, out Vector2 buttonBottomRight))
                return;

            float scaledWidth = SearchChanceHelpPanelWidth * visualScale;
            float scaledHeight = SearchChanceHelpPanelHeight * visualScale;
            float margin = SearchChanceHelpPanelMargin * visualScale;
            float x = buttonBottomRight.x;
            float y = buttonBottomRight.y - SearchChanceHelpPanelYOffset * visualScale;

            x = Mathf.Clamp(x, layerBounds.xMin + scaledWidth + margin, layerBounds.xMax - margin);
            y = Mathf.Clamp(y, layerBounds.yMin + scaledHeight + margin, layerBounds.yMax - margin);
            panelRect.anchoredPosition = new Vector2(x, y);
        }

        private static Rect ResolveSearchChanceHelpLayerBounds(RectTransform layerRect)
        {
            Rect layerBounds = layerRect.rect;

            if (layerBounds.width > 0f && layerBounds.height > 0f)
                return layerBounds;

            RectTransform parentRect = layerRect.parent as RectTransform;

            if (parentRect == null)
                return layerBounds;

            Rect parentBounds = parentRect.rect;
            return new Rect(
                -parentBounds.width * layerRect.pivot.x,
                -parentBounds.height * layerRect.pivot.y,
                parentBounds.width,
                parentBounds.height);
        }

        public void NotifyFirstTouchGuideToolUseSucceeded()
        {
            _isFirstTouchGuideCompletedForBoard = true;
            _isFirstTouchGuideDragInProgress = false;
            HideFirstTouchGuideImmediate();
        }

        public void NotifyFirstTouchGuideToolUseCanceled()
        {
            _isFirstTouchGuideDragInProgress = false;
            TryStartFirstTouchGuide();
        }

        private void HandleBeginDragTool(FindMoongchiToolSlotView slot, PointerEventData eventData)
        {
            DebugTool.Log($"[FindMoongchiGamePanel] 도구 드래그 시작: ToolId={slot?.ToolItemId}, Count={slot?.Count}", DebugType.FindMoongchi, this);
            _isFirstTouchGuideDragInProgress = true;
            HideFirstTouchGuideImmediate();
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
            {
                NotifyFirstTouchGuideToolUseCanceled();
                return;
            }

            if (!TryGetTileIndex(eventData, out int tileIndex))
            {
                DebugTool.Log($"[FindMoongchiGamePanel] 보드 밖 드롭 취소: ToolId={slot.ToolItemId}", DebugType.FindMoongchi, this);
                NotifyFirstTouchGuideToolUseCanceled();
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

        private void TryStartFirstTouchGuide()
        {
            if (!isActiveAndEnabled)
                return;

            ResolveFirstTouchGuideReferences();

            if (!_useFirstTouchGuide ||
                _isFirstTouchGuideCompletedForBoard ||
                _isFirstTouchGuideDragInProgress ||
                !_hasFirstTouchGuideUsableTool)
            {
                HideFirstTouchGuideImmediate();
                return;
            }

            if (_firstTouchGuideRoot == null)
                return;

            if (_firstTouchGuideTween != null && _firstTouchGuideTween.IsActive())
                return;

            _firstTouchGuideRoot.SetActive(true);

            if (_firstTouchGuideCanvasGroup == null)
                return;

            float fadeDuration = Mathf.Max(0.01f, _firstTouchGuideFadeDuration);
            float minAlpha = Mathf.Min(_firstTouchGuideMinAlpha, _firstTouchGuideMaxAlpha);
            float maxAlpha = Mathf.Max(_firstTouchGuideMinAlpha, _firstTouchGuideMaxAlpha);

            StopFirstTouchGuideTween();

            _firstTouchGuideCanvasGroup.interactable = false;
            _firstTouchGuideCanvasGroup.blocksRaycasts = false;
            _firstTouchGuideCanvasGroup.alpha = minAlpha;

            _firstTouchGuideTween = _firstTouchGuideCanvasGroup
                .DOFade(maxAlpha, fadeDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void HideFirstTouchGuideImmediate()
        {
            ResolveFirstTouchGuideReferences();
            StopFirstTouchGuideTween();

            if (_firstTouchGuideCanvasGroup != null)
                _firstTouchGuideCanvasGroup.alpha = 0f;

            if (_firstTouchGuideRoot != null)
                _firstTouchGuideRoot.SetActive(false);
        }

        private void StopFirstTouchGuideTween()
        {
            _firstTouchGuideTween?.Kill();
            _firstTouchGuideTween = null;
            _firstTouchGuideCanvasGroup?.DOKill();
        }

        private void ResolveFirstTouchGuideReferences()
        {
            if (_firstTouchGuideRoot == null)
            {
                Transform found = FindChildTransform("FirstTouchAnounce");
                if (found != null)
                    _firstTouchGuideRoot = found.gameObject;
            }

            if (_firstTouchGuideCanvasGroup == null && _firstTouchGuideRoot != null)
                _firstTouchGuideCanvasGroup = _firstTouchGuideRoot.GetComponent<CanvasGroup>();
        }

        private Transform FindChildTransform(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
                return null;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            Transform fallback = null;

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];

                if (child == null || child.name != childName)
                    continue;

                if (child.GetComponent<CanvasGroup>() != null)
                    return child;

                fallback ??= child;
            }

            return fallback;
        }

        private void RefreshFirstTouchGuideBoardState(FindMoongchiGameViewData data, int boardWidth, int boardHeight)
        {
            int boardKey = GetFirstTouchGuideBoardKey(data, boardWidth, boardHeight);

            if (_currentFirstTouchGuideBoardKey == boardKey)
                return;

            _currentFirstTouchGuideBoardKey = boardKey;
            _isFirstTouchGuideCompletedForBoard = false;
            _isFirstTouchGuideDragInProgress = false;
        }

        private static int GetFirstTouchGuideBoardKey(FindMoongchiGameViewData data, int boardWidth, int boardHeight)
        {
            unchecked
            {
                int key = data != null && data.StageId > 0 ? data.StageId : 17;
                key = key * 31 + boardWidth;
                key = key * 31 + boardHeight;
                key = key * 31 + (data?.CurrentWeek ?? 0);
                return key;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
        }

        private void OnDestroy()
        {
            StopFirstTouchGuideTween();

            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);

            UnbindButton(_searchChanceHelpButton, HandleSearchChanceHelpButtonClicked);
            UnbindButton(_searchChanceHelpCloseAreaButton, HideSearchChanceHelpPanel);

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
