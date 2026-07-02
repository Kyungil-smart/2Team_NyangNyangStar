using Core.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// 배치 완료된 물고기와 자연 요소의 삭제 UI를 담당합니다.
/// </summary>
public sealed class NyangQuariumFishDeleteUI : MonoBehaviour
{
    [Header("배치 물고기 시스템")]
    [SerializeField]
    private NyangquariumPlacedFishRenderer _placedFishRenderer;

    private NyangQuariumFishPlacementController _placementController;

    [Header("오브젝트 데이터")]
    [Tooltip("SpriteKey로 물고기와 자연 요소 이름을 조회할 SO")]
    [SerializeField]
    private NyangQuariumFishSO _fishSO;

    [Tooltip("이 삭제 UI가 저장할 수조 타입입니다. 배치 컨트롤러가 있으면 해당 값을 자동으로 따릅니다.")]
    [SerializeField]
    private FishType _aquariumType = FishType.Freshwater;

    [Header("선택 오브젝트 UI")]
    [Tooltip("제거 버튼과 이름 텍스트를 포함하는 UI 루트")]
    [SerializeField]
    private RectTransform _selectionUIRoot;

    [SerializeField]
    private Button _deleteButton;

    [SerializeField]
    private TMP_Text _fishNameText;

    [Header("삭제 모드 취소 영역")]
    [Tooltip("제거 버튼 외 화면 클릭을 감지하는 전체 화면 투명 버튼")]
    [SerializeField]
    private Button _cancelAreaButton;

    [Header("삭제 확인 팝업")]
    [Tooltip("삭제/취소 버튼을 포함하는 확인 팝업 루트")]
    [SerializeField]
    private GameObject _deleteConfirmPopup;

    [SerializeField]
    private Button _confirmDeleteButton;

    [SerializeField]
    private Button _cancelDeleteButton;

    [Header("표시 위치")]
    //[SerializeField]private float _deleteButtonOffsetX = 30f;
    //[SerializeField]private float _deleteButtonOffsetY = 70f;
    private Vector2 _deleteButtonOffset = new Vector2(30f, 80f);

    [SerializeField]
    private float _fishNameOffsetY = 65f;

    [Header("삭제 모드에서 숨길 UI")]
    [Tooltip("인벤토리, Change 버튼, 뒤로가기 등 삭제 모드에서 숨길 UI")]
    [SerializeField]
    private GameObject[] _normalUIObjects;

    private NyangquariumFishController _selectedFish;
    private NyangQuariumPlacedNature _selectedNature;
    private bool[] _normalUIActiveStates;
    private bool _isDeleteMode;
    private int _selectedNatureSiblingIndex = -1;
    private DeleteCancelAreaPointerRelay _cancelAreaPointerRelay;

    /// <summary>
    /// 물고기 삭제 후 최신 배치 물고기 목록을 전달합니다.
    /// </summary>
    public event Action<IReadOnlyList<NyangquariumPlacedFishData>>
        PlacedFishDataChanged;

    /// <summary>
    /// 자연 요소 삭제 후 삭제된 ItemId와 SpriteKey를 전달합니다.
    /// </summary>
    public event Action<int, string> PlacedNatureDeleted;

    public bool IsDeleteMode => _isDeleteMode;

    public bool IsDeleteConfirmPopupOpened =>
        _deleteConfirmPopup != null &&
        _deleteConfirmPopup.activeSelf;

    private bool HasSelectedObject =>
        _selectedFish != null ||
        _selectedNature != null;

    private void Awake()
    {
        ResolveAquariumType();
        BindButtons();
        HideDeleteUI();
    }

    private void OnEnable()
    {
        SubscribeRenderer();

        NyangQuariumPlacedNature.Clicked -= HandlePlacedNatureClicked;
        NyangQuariumPlacedNature.Clicked += HandlePlacedNatureClicked;
    }

    private void OnDisable()
    {
        NyangQuariumPlacedNature.Clicked -= HandlePlacedNatureClicked;

        UnsubscribeRenderer();
        ExitDeleteMode(false);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void LateUpdate()
    {
        if (!_isDeleteMode || !HasSelectedObject)
            return;

        UpdateSelectionUIPosition();
    }

    private void BindButtons()
    {
        if (_deleteButton != null)
        {
            _deleteButton.onClick.RemoveListener(OpenDeleteConfirmPopup);
            _deleteButton.onClick.AddListener(OpenDeleteConfirmPopup);
        }

        if (_cancelAreaButton != null)
        {
            _cancelAreaButton.onClick.RemoveListener(CancelDeleteMode);

            _cancelAreaPointerRelay =
                _cancelAreaButton.GetComponent<DeleteCancelAreaPointerRelay>();

            if (_cancelAreaPointerRelay == null)
            {
                _cancelAreaPointerRelay =
                    _cancelAreaButton.gameObject.AddComponent<
                        DeleteCancelAreaPointerRelay>();
            }

            _cancelAreaPointerRelay.Initialize(HandleCancelAreaPointerClick);
        }

        if (_confirmDeleteButton != null)
        {
            _confirmDeleteButton.onClick.RemoveListener(ConfirmDeleteSelectedObject);
            _confirmDeleteButton.onClick.AddListener(ConfirmDeleteSelectedObject);
        }

        if (_cancelDeleteButton != null)
        {
            _cancelDeleteButton.onClick.RemoveListener(CancelDeleteConfirmPopup);
            _cancelDeleteButton.onClick.AddListener(CancelDeleteConfirmPopup);
        }
    }

    private void UnbindButtons()
    {
        if (_deleteButton != null)
            _deleteButton.onClick.RemoveListener(OpenDeleteConfirmPopup);

        if (_cancelAreaButton != null)
            _cancelAreaButton.onClick.RemoveListener(CancelDeleteMode);

        if (_cancelAreaPointerRelay != null)
            _cancelAreaPointerRelay.Initialize(null);

        if (_confirmDeleteButton != null)
            _confirmDeleteButton.onClick.RemoveListener(ConfirmDeleteSelectedObject);

        if (_cancelDeleteButton != null)
            _cancelDeleteButton.onClick.RemoveListener(CancelDeleteConfirmPopup);
    }

    private void SubscribeRenderer()
    {
        if (_placedFishRenderer == null)
        {
            DebugTool.Warning("[NyangQuariumFishDeleteUI] PlacedFishRenderer가 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return;
        }

        _placedFishRenderer.SelectedFishChanged -= HandleSelectedFishChanged;
        _placedFishRenderer.SelectedFishChanged += HandleSelectedFishChanged;
        _placedFishRenderer.SetSelectionEnabled(true);
    }

    private void UnsubscribeRenderer()
    {
        if (_placedFishRenderer == null)
            return;

        _placedFishRenderer.SelectedFishChanged -= HandleSelectedFishChanged;
    }

    private void HandleSelectedFishChanged(
        NyangquariumFishController selectedFish)
    {
        if (selectedFish == null)
        {
            if (_isDeleteMode && _selectedNature == null)
                ExitDeleteMode(false);

            return;
        }

        EnterFishDeleteMode(selectedFish);
    }

    private void HandlePlacedNatureClicked(
        NyangQuariumPlacedNature selectedNature)
    {
        if (selectedNature == null)
            return;

        EnterNatureDeleteMode(selectedNature);
    }

    private void EnterFishDeleteMode(
        NyangquariumFishController selectedFish)
    {
        if (selectedFish == null)
            return;

        bool wasDeleteMode = _isDeleteMode;

        RestoreSelectedObjectState();

        _selectedNature = null;
        _selectedFish = selectedFish;
        _isDeleteMode = true;

        _selectedFish.SetMovementEnabled(false);

        if (!wasDeleteMode)
            HideNormalUI();

        ShowDeleteUI();
        RefreshSelectedObjectName();
        UpdateSelectionUIPosition();

        DebugTool.Log($"[NyangQuariumFishDeleteUI] 물고기 삭제 모드 진입 - " +
            $"Key:{_selectedFish.SpriteKey}",
            DebugType.UI,
            this);
    }

    private void EnterNatureDeleteMode(
        NyangQuariumPlacedNature selectedNature)
    {
        if (selectedNature == null)
            return;

        bool wasDeleteMode = _isDeleteMode;

        RestoreSelectedObjectState();

        _selectedFish = null;
        _selectedNature = selectedNature;
        _isDeleteMode = true;

        if (_placedFishRenderer != null &&
            _placedFishRenderer.HasSelectedFish)
        {
            _placedFishRenderer.ClearSelection();
        }

        _selectedNature.SetSelected(true);

        RectTransform natureRect = _selectedNature.RectTransform;

        if (natureRect != null)
        {
            _selectedNatureSiblingIndex = natureRect.GetSiblingIndex();
            natureRect.SetAsLastSibling();
        }

        if (!wasDeleteMode)
            HideNormalUI();

        ShowDeleteUI();
        RefreshSelectedObjectName();
        UpdateSelectionUIPosition();

        DebugTool.Log($"[NyangQuariumFishDeleteUI] 자연 요소 삭제 모드 진입 - " +
            $"ItemId:{_selectedNature.ItemId}, " +
            $"SpriteKey:{_selectedNature.SpriteKey}",
            DebugType.UI,
            this);
    }

    private void OpenDeleteConfirmPopup()
    {
        if (!_isDeleteMode || !HasSelectedObject)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (_deleteConfirmPopup == null)
        {
            DebugTool.Warning("[NyangQuariumFishDeleteUI] 삭제 확인 팝업이 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return;
        }

        if (_cancelAreaButton != null)
            _cancelAreaButton.interactable = false;

        _deleteConfirmPopup.SetActive(true);
        _deleteConfirmPopup.transform.SetAsLastSibling();

        DebugTool.Log("[NyangQuariumFishDeleteUI] 삭제 확인 팝업 열기",
            DebugType.UI,
            this);
    }

    private async void ConfirmDeleteSelectedObject()
    {
        if (!_isDeleteMode || !HasSelectedObject)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (_selectedFish != null)
        {
            await DeleteSelectedFishAsync();
            return;
        }

        await DeleteSelectedNatureAsync();
    }

    private async Task DeleteSelectedFishAsync()
    {
        if (_placementController == null)
        {
            DebugTool.Warning("[NyangQuariumFishDeleteUI] " +
                "FishPlacementController를 찾지 못했습니다.",
                DebugType.UI,
                this);

            CloseDeleteConfirmPopup();
            ExitDeleteMode(true);
            return;
        }

        bool deleted =
            await _placementController.DeleteSelectedFishAsync();

        if (!deleted)
        {
            DebugTool.Warning("[NyangQuariumFishDeleteUI] " +
                "선택된 물고기를 삭제하거나 저장하지 못했습니다.",
                DebugType.UI,
                this);

            CloseDeleteConfirmPopup();
            ShowDeleteUI();
            return;
        }

        IReadOnlyList<NyangquariumPlacedFishData> currentData =
            _placedFishRenderer != null
                ? _placedFishRenderer.GetCurrentPlacedFishData()
                : Array.Empty<NyangquariumPlacedFishData>();

        PlacedFishDataChanged?.Invoke(currentData);

        _selectedFish = null;

        CloseDeleteConfirmPopup();
        ExitDeleteMode(false);

        DebugTool.Log("[NyangQuariumFishDeleteUI] 물고기 삭제 완료 - " +
            $"남은 수:{currentData.Count}",
            DebugType.UI,
            this);
    }

    private void ResolveAquariumType()
    {
        _placementController =
            GetComponent<NyangQuariumFishPlacementController>();

        if (_placementController == null)
        {
            _placementController =
                GetComponentInParent<NyangQuariumFishPlacementController>();
        }

        if (_placementController != null)
            _aquariumType = _placementController.AquariumType;
    }

    private async Task DeleteSelectedNatureAsync()
    {
        if (_selectedNature == null)
            return;

        if (_placementController == null)
        {
            DebugTool.Warning("[NyangQuariumFishDeleteUI] " +
                "FishPlacementController를 찾지 못했습니다.",
                DebugType.UI,
                this);

            CloseDeleteConfirmPopup();
            ShowDeleteUI();
            return;
        }

        NyangQuariumPlacedNature deletedNature = _selectedNature;
        int deletedItemId = deletedNature.ItemId;
        string deletedSpriteKey = deletedNature.SpriteKey;

        bool deleted =
            await _placementController.DeletePlacedNatureAsync(
                deletedNature);

        if (!deleted)
        {
            DebugTool.Warning("[NyangQuariumFishDeleteUI] " +
                "자연 요소를 삭제하거나 저장하지 못했습니다.",
                DebugType.UI,
                this);

            CloseDeleteConfirmPopup();
            ShowDeleteUI();
            return;
        }

        _selectedNature = null;
        _selectedNatureSiblingIndex = -1;

        CloseDeleteConfirmPopup();
        ExitDeleteMode(false);

        PlacedNatureDeleted?.Invoke(
            deletedItemId,
            deletedSpriteKey);

        DebugTool.Log("[NyangQuariumFishDeleteUI] 자연 요소 삭제 완료 - " +
            $"ItemId:{deletedItemId}, SpriteKey:{deletedSpriteKey}",
            DebugType.UI,
            this);
    }

    private void CancelDeleteConfirmPopup()
    {
        if (!_isDeleteMode || !HasSelectedObject)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        CloseDeleteConfirmPopup();
        ShowDeleteUI();
        RefreshSelectedObjectName();
        UpdateSelectionUIPosition();

        DebugTool.Log("[NyangQuariumFishDeleteUI] 삭제 확인 취소 - 삭제 모드 복귀",
            DebugType.UI,
            this);
    }

    private void CloseDeleteConfirmPopup()
    {
        if (_deleteConfirmPopup != null)
            _deleteConfirmPopup.SetActive(false);

        if (_cancelAreaButton != null)
            _cancelAreaButton.interactable = true;
    }

    private void HandleCancelAreaPointerClick(PointerEventData eventData)
    {
        if (!_isDeleteMode || eventData == null)
            return;

        List<RaycastResult> raycastResults = new();

        if (EventSystem.current != null)
            EventSystem.current.RaycastAll(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;

            if (hitObject == null ||
                (_cancelAreaButton != null &&
                 hitObject == _cancelAreaButton.gameObject))
            {
                continue;
            }

            NyangquariumFishController clickedFish =
                hitObject.GetComponentInParent<
                    NyangquariumFishController>();

            if (IsPlacedFish(clickedFish))
            {
                _placedFishRenderer.SelectFish(clickedFish);
                return;
            }

            NyangQuariumPlacedNature clickedNature =
                hitObject.GetComponentInParent<
                    NyangQuariumPlacedNature>();

            if (clickedNature != null)
            {
                EnterNatureDeleteMode(clickedNature);
                return;
            }
        }

        CancelDeleteMode();
    }

    private bool IsPlacedFish(
        NyangquariumFishController fish)
    {
        if (fish == null || _placedFishRenderer == null)
            return false;

        IReadOnlyList<NyangquariumFishController> placedFishes =
            _placedFishRenderer.SpawnedFishes;

        for (int i = 0; i < placedFishes.Count; i++)
        {
            if (placedFishes[i] == fish)
                return true;
        }

        return false;
    }

    private void CancelDeleteMode()
    {
        if (!_isDeleteMode)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        ExitDeleteMode(true);
    }

    private void ExitDeleteMode(bool clearSelection)
    {
        if (!_isDeleteMode && !HasSelectedObject)
            return;

        RestoreSelectedObjectState();

        _selectedFish = null;
        _selectedNature = null;
        _selectedNatureSiblingIndex = -1;
        _isDeleteMode = false;

        CloseDeleteConfirmPopup();
        HideDeleteUI();
        RestoreNormalUI();

        if (clearSelection &&
            _placedFishRenderer != null &&
            _placedFishRenderer.HasSelectedFish)
        {
            _placedFishRenderer.ClearSelection();
        }

        DebugTool.Log("[NyangQuariumFishDeleteUI] 삭제 모드 종료",
            DebugType.UI,
            this);
    }

    private void RestoreSelectedObjectState()
    {
        if (_selectedFish != null)
            _selectedFish.SetMovementEnabled(true);

        if (_selectedNature == null)
            return;

        _selectedNature.SetSelected(false);

        RectTransform natureRect = _selectedNature.RectTransform;

        if (natureRect == null ||
            _selectedNatureSiblingIndex < 0)
        {
            return;
        }

        int maxIndex = Mathf.Max(0, natureRect.parent.childCount - 1);
        natureRect.SetSiblingIndex(
            Mathf.Min(_selectedNatureSiblingIndex, maxIndex));
    }

    private void ShowDeleteUI()
    {
        if (_cancelAreaButton != null)
            _cancelAreaButton.gameObject.SetActive(true);

        if (_selectionUIRoot != null)
        {
            _selectionUIRoot.gameObject.SetActive(true);
            _selectionUIRoot.SetAsLastSibling();
        }
    }

    private void HideDeleteUI()
    {
        CloseDeleteConfirmPopup();

        if (_selectionUIRoot != null)
            _selectionUIRoot.gameObject.SetActive(false);

        if (_cancelAreaButton != null)
            _cancelAreaButton.gameObject.SetActive(false);

        if (_fishNameText != null)
            _fishNameText.text = string.Empty;
    }

    private void RefreshSelectedObjectName()
    {
        if (_fishNameText == null)
            return;

        string spriteKey = GetSelectedSpriteKey();

        if (string.IsNullOrWhiteSpace(spriteKey))
        {
            _fishNameText.text = string.Empty;
            return;
        }

        if (TryGetObjectName(spriteKey, out string objectName))
        {
            _fishNameText.text = objectName;
            return;
        }

        _fishNameText.text = spriteKey;

        DebugTool.Warning($"[NyangQuariumFishDeleteUI] " +
            $"SpriteKey와 일치하는 데이터를 찾지 못했습니다. " +
            $"Key:{spriteKey}",
            DebugType.UI,
            this);
    }

    private string GetSelectedSpriteKey()
    {
        if (_selectedFish != null)
            return _selectedFish.SpriteKey;

        if (_selectedNature != null)
            return _selectedNature.SpriteKey;

        return string.Empty;
    }

    private bool TryGetObjectName(
        string spriteKey,
        out string objectName)
    {
        objectName = string.Empty;

        if (_fishSO == null ||
            _fishSO.FishData == null ||
            string.IsNullOrWhiteSpace(spriteKey))
        {
            return false;
        }

        IReadOnlyList<NyangQuariumFishData> dataList =
            _fishSO.FishData;

        for (int i = 0; i < dataList.Count; i++)
        {
            NyangQuariumFishData data = dataList[i];

            if (data == null ||
                !string.Equals(
                    data.FishKey,
                    spriteKey,
                    StringComparison.Ordinal))
            {
                continue;
            }

            objectName = data.FishName;
            return !string.IsNullOrWhiteSpace(objectName);
        }

        return false;
    }

    private void UpdateSelectionUIPosition()
    {
        RectTransform selectedRect =
            GetSelectedRectTransform();

        if (selectedRect == null ||
            _selectionUIRoot == null)
        {
            return;
        }

        RectTransform uiParent =
            _selectionUIRoot.parent as RectTransform;

        if (uiParent == null)
            return;

        Camera uiCamera = GetUICamera(uiParent);

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                selectedRect.position);

        bool converted =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiParent,
                screenPosition,
                uiCamera,
                out Vector2 localPosition);

        if (!converted)
            return;

        _selectionUIRoot.anchoredPosition = localPosition;

        if (_deleteButton != null &&
            _deleteButton.transform is RectTransform deleteRect)
        {
            deleteRect.anchoredPosition =
                _deleteButtonOffset;
        }

        if (_fishNameText != null)
        {
            _fishNameText.rectTransform.anchoredPosition =
                new Vector2(0f, -_fishNameOffsetY);
        }
    }

    private RectTransform GetSelectedRectTransform()
    {
        if (_selectedFish != null)
            return _selectedFish.RectTransform;

        if (_selectedNature != null)
            return _selectedNature.RectTransform;

        return null;
    }

    private static Camera GetUICamera(RectTransform target)
    {
        Canvas canvas = target.GetComponentInParent<Canvas>();

        if (canvas == null ||
            canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void HideNormalUI()
    {
        if (_normalUIObjects == null)
            return;

        _normalUIActiveStates =
            new bool[_normalUIObjects.Length];

        for (int i = 0; i < _normalUIObjects.Length; i++)
        {
            GameObject target = _normalUIObjects[i];

            if (target == null)
                continue;

            _normalUIActiveStates[i] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void RestoreNormalUI()
    {
        if (_normalUIObjects == null ||
            _normalUIActiveStates == null)
        {
            return;
        }

        int count = Mathf.Min(
            _normalUIObjects.Length,
            _normalUIActiveStates.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject target = _normalUIObjects[i];

            if (target != null)
                target.SetActive(_normalUIActiveStates[i]);
        }

        _normalUIActiveStates = null;
    }
}

/// <summary>
/// 전체 화면 삭제 취소 영역의 포인터 위치를 삭제 UI에 전달합니다.
/// </summary>
public sealed class DeleteCancelAreaPointerRelay :
    MonoBehaviour,
    IPointerClickHandler
{
    private Action<PointerEventData> _onPointerClick;

    public void Initialize(Action<PointerEventData> onPointerClick)
    {
        _onPointerClick = onPointerClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onPointerClick?.Invoke(eventData);
    }
}