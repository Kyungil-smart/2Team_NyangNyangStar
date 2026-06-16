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

        [Header("Targets")]
        [SerializeField] private List<FindMoongchiTargetHintView> _targetHints = new();

        [Header("Tools")]
        [SerializeField] private List<FindMoongchiToolSlotView> _toolSlots = new();

        private readonly List<FindMoongchiTileView> _tileViews = new();
        private readonly List<Image> _highlightImages = new();
        private readonly HashSet<int> _revealedTiles = new();

        private FindMoongchiToolSlotView _currentToolSlot;
        private int _currentPreviewTileIndex = -1;

        public event Action OnBackButtonClicked;
        public event Action<int, int> OnToolDropped;

        public void Init()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            ResolveTiles();
            ResolveToolSlots();
            BindToolSlots();
        }

        public void SetData(FindMoongchiGameViewData data)
        {
            if (data == null)
                return;

            SetText(_weekText, $"{data.CurrentWeek}주차");
            SetText(_remainTimeText, data.RemainTimeText);
            SetText(_searchChanceText, data.SearchChance.ToString());
            SetText(_energyProgressText, $"{data.EnergySpendProgress}/{data.EnergySpendTarget}");

            _revealedTiles.Clear();
            foreach (int tileIndex in data.RevealedTileIndices)
                _revealedTiles.Add(tileIndex);

            RefreshTiles();
            RefreshTools(data.Tools);
            RefreshTargetHints(data.TargetHints);
            ClearHighlight();
        }

        public void RevealTiles(IEnumerable<int> tileIndices)
        {
            if (tileIndices == null)
                return;

            foreach (int tileIndex in tileIndices)
            {
                if (tileIndex < 0 || tileIndex >= FindMoongchiConstants.TileCount)
                    continue;

                _revealedTiles.Add(tileIndex);
            }

            RefreshTiles();
        }

        public IReadOnlyList<int> GetAffectedTiles(int toolItemId, int tileIndex)
        {
            return CalculateAffectedTiles(toolItemId, tileIndex);
        }

        private void ResolveTiles()
        {
            _tileViews.Clear();

            if (_tileRoot == null)
                return;

            for (int i = 0; i < _tileRoot.childCount; i++)
            {
                Transform child = _tileRoot.GetChild(i);
                FindMoongchiTileView tileView = child.GetComponent<FindMoongchiTileView>();

                if (tileView == null)
                    tileView = child.gameObject.AddComponent<FindMoongchiTileView>();

                tileView.Init(i);
                tileView.SetRevealed(_revealedTiles.Contains(i));
                _tileViews.Add(tileView);
            }
        }

        private void ResolveToolSlots()
        {
            if (_toolSlots.Count > 0)
                return;

            FindMoongchiToolSlotView[] foundSlots = GetComponentsInChildren<FindMoongchiToolSlotView>(true);
            _toolSlots.AddRange(foundSlots);
        }

        private void BindToolSlots()
        {
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
            }
        }

        private void RefreshTiles()
        {
            for (int i = 0; i < _tileViews.Count; i++)
                _tileViews[i].SetRevealed(_revealedTiles.Contains(i));
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
            OnBackButtonClicked?.Invoke();
        }

        private void HandleBeginDragTool(FindMoongchiToolSlotView slot, PointerEventData eventData)
        {
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
                return;

            OnToolDropped?.Invoke(slot.ToolItemId, tileIndex);
        }

        private bool TryGetTileIndex(PointerEventData eventData, out int tileIndex)
        {
            tileIndex = -1;

            if (_boardArea == null || eventData == null)
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

            int column = Mathf.Clamp(Mathf.FloorToInt(normalizedX * FindMoongchiConstants.BoardWidth), 0, FindMoongchiConstants.BoardWidth - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt(normalizedY * FindMoongchiConstants.BoardHeight), 0, FindMoongchiConstants.BoardHeight - 1);

            tileIndex = row * FindMoongchiConstants.BoardWidth + column;
            return tileIndex >= 0 && tileIndex < FindMoongchiConstants.TileCount;
        }

        private static List<int> CalculateAffectedTiles(int toolItemId, int tileIndex)
        {
            List<int> result = new List<int>();

            if (tileIndex < 0 || tileIndex >= FindMoongchiConstants.TileCount)
                return result;

            int row = tileIndex / FindMoongchiConstants.BoardWidth;
            int column = tileIndex % FindMoongchiConstants.BoardWidth;
            FindMoongchiToolPattern pattern = GetPattern(toolItemId);

            switch (pattern)
            {
                case FindMoongchiToolPattern.Row:
                    for (int x = 0; x < FindMoongchiConstants.BoardWidth; x++)
                        result.Add(row * FindMoongchiConstants.BoardWidth + x);
                    break;

                case FindMoongchiToolPattern.Column:
                    for (int y = 0; y < FindMoongchiConstants.BoardHeight; y++)
                        result.Add(y * FindMoongchiConstants.BoardWidth + column);
                    break;

                case FindMoongchiToolPattern.Square4x4:
                    int startX = Mathf.Clamp(column, 0, FindMoongchiConstants.BoardWidth - 4);
                    int startY = Mathf.Clamp(row, 0, FindMoongchiConstants.BoardHeight - 4);

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                            result.Add((startY + y) * FindMoongchiConstants.BoardWidth + (startX + x));
                    }
                    break;
            }

            return result;
        }

        private static FindMoongchiToolPattern GetPattern(int toolItemId)
        {
            return toolItemId switch
            {
                FindMoongchiConstants.ToolId01 => FindMoongchiToolPattern.Row,
                FindMoongchiConstants.ToolId02 => FindMoongchiToolPattern.Column,
                FindMoongchiConstants.ToolId03 => FindMoongchiToolPattern.Square4x4,
                _ => FindMoongchiToolPattern.Row
            };
        }

        private void ShowHighlight(IReadOnlyList<int> tileIndices)
        {
            ClearHighlight();

            if (_highlightRoot == null || _boardArea == null || tileIndices == null)
                return;

            EnsureHighlightImageCount(tileIndices.Count);

            Rect rect = _boardArea.rect;
            float cellWidth = rect.width / FindMoongchiConstants.BoardWidth;
            float cellHeight = rect.height / FindMoongchiConstants.BoardHeight;

            for (int i = 0; i < tileIndices.Count; i++)
            {
                int tileIndex = tileIndices[i];
                int row = tileIndex / FindMoongchiConstants.BoardWidth;
                int column = tileIndex % FindMoongchiConstants.BoardWidth;

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
            while (_highlightImages.Count < count)
            {
                GameObject go = new GameObject($"Highlight_{_highlightImages.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_highlightRoot, false);

                Image image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _highlightImages.Add(image);
            }
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
