using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapPlacementController : MonoBehaviour
{
    private enum ToyPlacementState
    {
        None,
        ToySelected,
        MarkerPlaced,
        Dragging
    }

    [Header("입력 및 배치 영역")]
    [Tooltip("장난감 목표 위치를 선택할 화면상의 UI 영역입니다.")]
    [SerializeField] private RectTransform _clickArea;

    [Tooltip("사진에 실제 아이템 이미지가 생성될 영역입니다.")]
    [SerializeField] private RectTransform _placementArea;

    [Header("미리보기 이미지")]
    [Tooltip("드래그 중 표시할 장난감 이미지입니다. 비워두면 자동 생성됩니다.")]
    [SerializeField] private Image _previewImage;

    [Tooltip("마커 범위 밖에서 적용할 미리보기 알파값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float _invalidPreviewAlpha = 0.5f;

    [Header("장난감 마커")]
    [Tooltip("선택한 목표 위치를 표시할 마커입니다.")]
    [SerializeField] private Image _markerImage;

    [Tooltip("ToolSO 범위를 찾지 못했을 때 사용할 기본 드롭 반경입니다.")]
    [Min(1f)]
    [SerializeField] private float _markerDropRadius = 100f;

    [Header("도구 범위 표시")]
    [Tooltip("마커 주변의 드롭 허용 범위를 표시할 원형 이미지입니다.")]
    [SerializeField] private Image _rangeImage;

    [Tooltip("ItemID에 맞는 ItemRange를 가져올 Tool SO입니다.")]
    [SerializeField] private NyangNyangSnapToolSO _toolSO;

    [Tooltip("ItemRange를 UI 거리로 변환할 배율입니다.")]
    [Min(0.1f)]
    [SerializeField] private float _itemRangeScale = 10f;

    [Header("배치 설정")]
    [Tooltip("사진 영역에 생성될 아이템 이미지 크기입니다.")]
    [SerializeField] private Vector2 _placedItemSize = new Vector2(160f, 160f);

    [Header("실패 알림")]
    [Tooltip("잘못 드롭했을 때 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text _retryMessageText;

    [Tooltip("실패 알림 표시 시간입니다.")]
    [Min(0.1f)]
    [SerializeField] private float _retryMessageDuration = 1.5f;

    private Canvas _canvas;
    private RectTransform _canvasRectTransform;
    private Image _placedImage;
    private Coroutine _retryMessageCoroutine;

    private ToyPlacementState _toyState = ToyPlacementState.None;

    private int _selectedItemID = -1;
    private string _selectedItemName = string.Empty;
    private Sprite _selectedSprite;

    private int _placedItemID = -1;

    private Vector2 _markerCanvasPosition;
    private Vector2 _markerPlacementPosition;

    public int SelectedItemID => _placedItemID;
    public bool HasPlacedItem => _placedItemID > 0;
    public RectTransform PlacedItemRectTransform => _placedImage != null ? _placedImage.rectTransform : null;

    public event Action<int> OnItemPlaced;
    public event Action OnToyDropFailed;

    private void Awake()
    {
        AutoAssign();

        SetPreviewActive(false);
        SetMarkerActive(false);
        SetRangeActive(false);
        SetRetryMessageActive(false);
    }

    private void OnDisable()
    {
        SetPreviewAlpha(1f);
        SetPreviewActive(false);
        SetMarkerActive(false);
        SetRangeActive(false);
        SetRetryMessageActive(false);
    }

    private void Update()
    {
        HandleMarkerInput();
    }

    public bool SelectToy(int itemID, string itemName, Sprite itemSprite)
    {
        if (!ValidateSelection(itemID, itemSprite))
            return false;

        bool isSameToy = _selectedItemID == itemID && _selectedSprite == itemSprite;

        if (isSameToy &&
            (_toyState == ToyPlacementState.MarkerPlaced ||
             _toyState == ToyPlacementState.Dragging))
        {
            DebugTool.Log(
                $"[NyangNyangSnapPlacementController] 동일 장난감 재선택 / 마커 유지 / ItemID:{itemID}",
                DebugType.UI,
                this
            );

            return true;
        }

        _selectedItemID = itemID;
        _selectedItemName = itemName;
        _selectedSprite = itemSprite;
        _toyState = ToyPlacementState.ToySelected;

        SetPreviewAlpha(1f);
        SetPreviewActive(false);
        SetMarkerActive(false);
        SetRangeActive(false);

        DebugTool.Log(
            $"[NyangNyangSnapPlacementController] 장난감 선택 완료 / ItemID:{itemID}, ItemName:{itemName}",
            DebugType.UI,
            this
        );

        return true;
    }

    public void SelectItem(int itemID, string itemName, Sprite itemSprite)
    {
        if (!ValidateSelection(itemID, itemSprite))
            return;

        _selectedItemID = itemID;
        _selectedItemName = itemName;
        _selectedSprite = itemSprite;

        if (_previewImage != null)
        {
            _previewImage.sprite = itemSprite;
            _previewImage.preserveAspect = true;
        }

        SetPreviewAlpha(1f);
        SetPreviewActive(false);

        DebugTool.Log(
            $"[NyangNyangSnapPlacementController] 아이템 선택 완료 / ItemID:{itemID}, ItemName:{itemName}",
            DebugType.UI,
            this
        );
    }

    public bool BeginSelection(int itemID, string itemName, Sprite itemSprite, Vector2 pointerPosition)
    {
        if (!ValidateSelection(itemID, itemSprite))
            return false;

        _selectedItemID = itemID;
        _selectedItemName = itemName;
        _selectedSprite = itemSprite;

        if (_previewImage == null)
            return false;

        _previewImage.sprite = itemSprite;
        _previewImage.preserveAspect = true;
        _previewImage.raycastTarget = false;

        SetPreviewAlpha(1f);
        SetPreviewActive(true);
        MovePreview(pointerPosition);

        return true;
    }

    public bool EndSelection(Vector2 pointerPosition)
    {
        if (!TryGetPlacementLocalPoint(pointerPosition, out Vector2 localPoint))
        {
            SetPreviewAlpha(1f);
            SetPreviewActive(false);

            return false;
        }

        PlaceSelectedItem(localPoint);

        return true;
    }

    public bool BeginToyDrag(Vector2 pointerPosition)
    {
        if (_toyState != ToyPlacementState.MarkerPlaced)
        {
            DebugTool.Warning(
                "[NyangNyangSnapPlacementController] 먼저 사진 영역을 터치해 마커를 표시해주세요.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_previewImage == null || _selectedSprite == null)
            return false;

        _toyState = ToyPlacementState.Dragging;

        _previewImage.sprite = _selectedSprite;
        _previewImage.preserveAspect = true;
        _previewImage.raycastTarget = false;

        SetPreviewAlpha(1f);
        SetPreviewActive(true);

        SetMarkerActive(true);
        SetRangeActive(true);

        _previewImage.transform.SetAsLastSibling();

        UpdateToyDrag(pointerPosition);

        DebugTool.Log(
            $"[NyangNyangSnapPlacementController] 장난감 드래그 시작 / ItemID:{_selectedItemID}",
            DebugType.UI,
            this
        );

        return true;
    }

    public void UpdateToyDrag(Vector2 pointerPosition)
    {
        if (_toyState != ToyPlacementState.Dragging)
            return;

        if (!TryGetCanvasLocalPoint(pointerPosition, out Vector2 canvasPosition))
            return;

        _previewImage.rectTransform.anchoredPosition = canvasPosition;

        bool canDrop = Vector2.Distance(canvasPosition, _markerCanvasPosition) <= GetCurrentDropRadius();

        SetPreviewAlpha(canDrop ? 1f : _invalidPreviewAlpha);
    }

    public bool EndToyDrag(Vector2 pointerPosition)
    {
        if (_toyState != ToyPlacementState.Dragging)
            return false;

        if (!TryGetCanvasLocalPoint(pointerPosition, out Vector2 canvasPosition))
        {
            FailToyDrop();
            return false;
        }

        float distance = Vector2.Distance(canvasPosition, _markerCanvasPosition);

        if (distance > GetCurrentDropRadius())
        {
            FailToyDrop();
            return false;
        }

        PlaceSelectedItem(_markerPlacementPosition);

        return true;
    }

    public void CancelSelection()
    {
        _toyState = ToyPlacementState.None;

        _selectedItemID = -1;
        _selectedItemName = string.Empty;
        _selectedSprite = null;

        SetPreviewAlpha(1f);
        SetPreviewActive(false);
        SetMarkerActive(false);
        SetRangeActive(false);
    }

    public void ClearPlacedItem()
    {
        if (_placedImage != null)
        {
            Destroy(_placedImage.gameObject);
            _placedImage = null;
        }

        _placedItemID = -1;
    }

    private void HandleMarkerInput()
    {
        if (_toyState != ToyPlacementState.ToySelected)
            return;

        if (!TryGetPointerDownPosition(out Vector2 pointerPosition))
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(_clickArea, pointerPosition, GetEventCamera()))
            return;

        if (!TryGetPlacementLocalPoint(pointerPosition, out Vector2 placementPosition))
            return;

        if (!TryGetCanvasLocalPoint(pointerPosition, out Vector2 canvasPosition))
            return;

        _markerPlacementPosition = placementPosition;
        _markerCanvasPosition = canvasPosition;
        _toyState = ToyPlacementState.MarkerPlaced;

        ShowMarker(canvasPosition);
        ShowRange(canvasPosition);

        DebugTool.Log(
            $"[NyangNyangSnapPlacementController] 마커 표시 완료 / ItemID:{_selectedItemID}, Position:{placementPosition}",
            DebugType.UI,
            this
        );
    }

    private bool TryGetPointerDownPosition(out Vector2 pointerPosition)
    {
        pointerPosition = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase != TouchPhase.Began)
                return false;

            pointerPosition = touch.position;

            return true;
        }

        if (!Input.GetMouseButtonDown(0))
            return false;

        pointerPosition = Input.mousePosition;

        return true;
    }

    private void PlaceSelectedItem(Vector2 localPoint)
    {
        if (_selectedItemID <= 0 || _selectedSprite == null || _placementArea == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapPlacementController] 배치할 아이템 정보가 없습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        ClearPlacedItem();

        GameObject itemObject = new GameObject(
            $"PlacedItem_{_selectedItemID}",
            typeof(RectTransform),
            typeof(Image)
        );

        itemObject.transform.SetParent(_placementArea, false);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();

        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = _placedItemSize;
        itemRect.anchoredPosition = localPoint;

        _placedImage = itemObject.GetComponent<Image>();

        _placedImage.sprite = _selectedSprite;
        _placedImage.preserveAspect = true;
        _placedImage.raycastTarget = false;

        _placedItemID = _selectedItemID;

        int placedItemID = _placedItemID;

        CancelSelection();

        OnItemPlaced?.Invoke(placedItemID);

        DebugTool.Log(
            $"[NyangNyangSnapPlacementController] 아이템 배치 완료 / ItemID:{placedItemID}, Position:{localPoint}",
            DebugType.UI,
            this
        );
    }

    private void FailToyDrop()
    {
        _toyState = ToyPlacementState.MarkerPlaced;

        SetPreviewAlpha(1f);
        SetPreviewActive(false);

        SetMarkerActive(true);
        SetRangeActive(true);

        ShowRetryMessage();

        OnToyDropFailed?.Invoke();

        DebugTool.Warning(
            $"[NyangNyangSnapPlacementController] 마커 범위 밖에 놓았습니다. ItemID:{_selectedItemID}",
            DebugType.UI,
            this
        );
    }

    private bool ValidateSelection(int itemID, Sprite itemSprite)
    {
        AutoAssign();

        if (itemID <= 0)
        {
            DebugTool.Warning(
                "[NyangNyangSnapPlacementController] 잘못된 ItemID입니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (itemSprite == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapPlacementController] Sprite가 없습니다. ItemID:{itemID}",
                DebugType.UI,
                this
            );

            return false;
        }

        if (_clickArea == null || _placementArea == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapPlacementController] ClickArea 또는 PlacementArea가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        return true;
    }

    private float GetCurrentDropRadius()
    {
        if (_toolSO != null &&
            _toolSO.TryGetToolDataByItemID(_selectedItemID, out NyangNyangSnapToolData toolData))
        {
            return toolData.ItemRange * _itemRangeScale;
        }

        return _markerDropRadius;
    }

    private bool TryGetPlacementLocalPoint(Vector2 screenPosition, out Vector2 placementLocalPoint)
    {
        placementLocalPoint = Vector2.zero;

        if (_clickArea == null || _placementArea == null)
            return false;

        Camera eventCamera = GetEventCamera();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _clickArea,
                screenPosition,
                eventCamera,
                out Vector2 clickLocalPoint))
        {
            return false;
        }

        if (!_clickArea.rect.Contains(clickLocalPoint))
            return false;

        float normalizedX = Mathf.InverseLerp(_clickArea.rect.xMin, _clickArea.rect.xMax, clickLocalPoint.x);
        float normalizedY = Mathf.InverseLerp(_clickArea.rect.yMin, _clickArea.rect.yMax, clickLocalPoint.y);

        Rect placementRect = _placementArea.rect;

        placementLocalPoint = new Vector2(
            Mathf.Lerp(placementRect.xMin, placementRect.xMax, normalizedX),
            Mathf.Lerp(placementRect.yMin, placementRect.yMax, normalizedY)
        );

        return true;
    }

    private bool TryGetCanvasLocalPoint(Vector2 screenPosition, out Vector2 canvasLocalPoint)
    {
        canvasLocalPoint = Vector2.zero;

        if (_canvasRectTransform == null)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            screenPosition,
            GetEventCamera(),
            out canvasLocalPoint
        );
    }

    private void MovePreview(Vector2 screenPosition)
    {
        if (_previewImage == null)
            return;

        if (!TryGetCanvasLocalPoint(screenPosition, out Vector2 canvasPosition))
            return;

        _previewImage.rectTransform.anchoredPosition = canvasPosition;
    }

    private void AutoAssign()
    {
        if (_canvas == null && _clickArea != null)
            _canvas = _clickArea.GetComponentInParent<Canvas>();

        if (_canvas != null && _canvasRectTransform == null)
            _canvasRectTransform = _canvas.GetComponent<RectTransform>();

        if (_previewImage == null)
            _previewImage = CreateRuntimeImage("SelectedItemPreview", _placedItemSize);
    }

    private Image CreateRuntimeImage(string objectName, Vector2 size)
    {
        if (_canvas == null)
            return null;

        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        imageObject.transform.SetParent(_canvas.transform, false);
        imageObject.transform.SetAsLastSibling();

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();

        image.raycastTarget = false;
        image.preserveAspect = true;
        image.gameObject.SetActive(false);

        return image;
    }

    private void ShowMarker(Vector2 canvasPosition)
    {
        if (_markerImage == null)
            return;

        _markerImage.rectTransform.anchoredPosition = canvasPosition;
        _markerImage.raycastTarget = false;

        SetMarkerActive(true);
    }

    private void ShowRange(Vector2 canvasPosition)
    {
        if (_rangeImage == null)
            return;

        float radius = GetCurrentDropRadius();
        float diameter = radius * 2f;

        _rangeImage.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        _rangeImage.rectTransform.anchoredPosition = canvasPosition;
        _rangeImage.raycastTarget = false;

        SetRangeActive(true);
    }

    private void ShowRetryMessage()
    {
        if (_retryMessageText == null)
            return;

        if (_retryMessageCoroutine != null)
            StopCoroutine(_retryMessageCoroutine);

        _retryMessageCoroutine = StartCoroutine(RetryMessageRoutine());
    }

    private IEnumerator RetryMessageRoutine()
    {
        _retryMessageText.text = "다시 하세요.";

        SetRetryMessageActive(true);

        yield return new WaitForSeconds(_retryMessageDuration);

        SetRetryMessageActive(false);

        _retryMessageCoroutine = null;
    }

    private void SetPreviewAlpha(float alpha)
    {
        if (_previewImage == null)
            return;

        Color color = _previewImage.color;

        color.a = alpha;

        _previewImage.color = color;
    }

    private void SetPreviewActive(bool isActive)
    {
        if (_previewImage != null)
            _previewImage.gameObject.SetActive(isActive);
    }

    private void SetMarkerActive(bool isActive)
    {
        if (_markerImage != null)
            _markerImage.gameObject.SetActive(isActive);
    }

    private void SetRangeActive(bool isActive)
    {
        if (_rangeImage != null)
            _rangeImage.gameObject.SetActive(isActive);
    }

    private void SetRetryMessageActive(bool isActive)
    {
        if (_retryMessageText != null)
            _retryMessageText.gameObject.SetActive(isActive);
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return _canvas.worldCamera;
    }
}