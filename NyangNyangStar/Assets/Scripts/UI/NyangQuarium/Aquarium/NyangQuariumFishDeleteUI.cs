using Core.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 배치 완료된 물고기의 삭제 모드 UI를 담당합니다.
///
/// 물고기 클릭:
/// - 기존 UI 숨김
/// - 선택 물고기 이동 정지
/// - 물고기 위에 X 버튼 표시
/// - 물고기 아래에 이름 표시
///
/// X 버튼 클릭:
/// - 팀원 API를 호출하여 선택 물고기 삭제
///
/// 다른 영역 클릭:
/// - 삭제하지 않고 삭제 모드 종료
/// </summary>
public sealed class NyangQuariumFishDeleteUI : MonoBehaviour
{
    [Header("배치 물고기 시스템")]
    [SerializeField]
    private NyangquariumPlacedFishRenderer _placedFishRenderer;

    [Header("물고기 데이터")]
    [Tooltip("FishKey로 물고기 이름을 조회할 냥쿠아리움 물고기 SO")]
    [SerializeField]
    private NyangQuariumFishSO _fishSO;

    [Header("선택 물고기 UI")]
    [Tooltip("X 버튼과 이름 텍스트를 포함하는 UI 루트")]
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
    [SerializeField]
    private float _deleteButtonOffsetY = 70f;

    [SerializeField]
    private float _fishNameOffsetY = 65f;

    [Header("삭제 모드에서 숨길 UI")]
    [Tooltip("인벤토리, Change 버튼, 뒤로가기 등 삭제 모드에서 숨길 UI")]
    [SerializeField]
    private GameObject[] _normalUIObjects;

    private NyangquariumFishController _selectedFish;
    private bool[] _normalUIActiveStates;
    private bool _isDeleteMode;
    private DeleteCancelAreaPointerRelay _cancelAreaPointerRelay;

    /// <summary>
    /// 물고기 삭제 후 최신 배치 물고기 목록을 전달합니다.
    /// 저장 기능에서 필요할 때 구독하면 됩니다.
    /// </summary>
    public event Action<IReadOnlyList<NyangquariumPlacedFishData>>
        PlacedFishDataChanged;

    /// <summary>
    /// 현재 물고기 삭제 모드 진행 여부입니다.
    /// </summary>
    public bool IsDeleteMode => _isDeleteMode;

    /// <summary>
    /// 영구 삭제 확인 팝업 표시 여부입니다.
    /// </summary>
    public bool IsDeleteConfirmPopupOpened =>
        _deleteConfirmPopup != null &&
        _deleteConfirmPopup.activeSelf;

    private void Awake()
    {
        BindButtons();
        HideDeleteUI();
    }

    private void OnEnable()
    {
        SubscribeRenderer();
    }

    private void OnDisable()
    {
        UnsubscribeRenderer();
        ExitDeleteMode(false);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void LateUpdate()
    {
        if (!_isDeleteMode || _selectedFish == null)
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
            // Button.onClick은 클릭 위치를 알 수 없어서 다른 물고기 선택을 구분할 수 없습니다.
            _cancelAreaButton.onClick.RemoveListener(CancelDeleteMode);

            _cancelAreaPointerRelay =
                _cancelAreaButton.GetComponent<DeleteCancelAreaPointerRelay>();

            if (_cancelAreaPointerRelay == null)
            {
                _cancelAreaPointerRelay =
                    _cancelAreaButton.gameObject.AddComponent<DeleteCancelAreaPointerRelay>();
            }

            _cancelAreaPointerRelay.Initialize(HandleCancelAreaPointerClick);
        }

        if (_confirmDeleteButton != null)
        {
            _confirmDeleteButton.onClick.RemoveListener(ConfirmDeleteSelectedFish);
            _confirmDeleteButton.onClick.AddListener(ConfirmDeleteSelectedFish);
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
            _confirmDeleteButton.onClick.RemoveListener(ConfirmDeleteSelectedFish);

        if (_cancelDeleteButton != null)
            _cancelDeleteButton.onClick.RemoveListener(CancelDeleteConfirmPopup);
    }

    private void SubscribeRenderer()
    {
        if (_placedFishRenderer == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishDeleteUI] PlacedFishRenderer가 연결되지 않았습니다.",
                this);

            return;
        }

        _placedFishRenderer.SelectedFishChanged -= HandleSelectedFishChanged;
        _placedFishRenderer.SelectedFishChanged += HandleSelectedFishChanged;

        // 팀원 코드에 구현된 물고기 클릭 선택 기능을 켭니다.
        _placedFishRenderer.SetSelectionEnabled(true);
    }

    private void UnsubscribeRenderer()
    {
        if (_placedFishRenderer == null)
            return;

        _placedFishRenderer.SelectedFishChanged -= HandleSelectedFishChanged;
    }

    /// <summary>
    /// 물고기가 클릭되어 선택 상태가 변경되면 호출됩니다.
    /// </summary>
    private void HandleSelectedFishChanged(
        NyangquariumFishController selectedFish)
    {
        if (selectedFish == null)
        {
            if (_isDeleteMode)
                ExitDeleteMode(false);

            return;
        }

        EnterDeleteMode(selectedFish);
    }

    private void EnterDeleteMode(
        NyangquariumFishController selectedFish)
    {
        if (selectedFish == null)
            return;

        // 이미 다른 물고기가 선택 중이었다면 이동을 다시 시작합니다.
        if (_selectedFish != null && _selectedFish != selectedFish)
            _selectedFish.SetMovementEnabled(true);

        bool wasDeleteMode = _isDeleteMode;

        _selectedFish = selectedFish;
        _isDeleteMode = true;

        // 삭제 UI를 누르기 편하도록 선택 물고기를 잠시 멈춥니다.
        _selectedFish.SetMovementEnabled(false);

        // 최초 삭제 모드 진입 때만 기존 UI 상태를 저장합니다.
        if (!wasDeleteMode)
            HideNormalUI();

        ShowDeleteUI();
        RefreshFishName();
        UpdateSelectionUIPosition();

        Debug.Log(
            $"[NyangQuariumFishDeleteUI] 삭제 모드 진입 - Key: {_selectedFish.SpriteKey}",
            this);
    }

    private void OpenDeleteConfirmPopup()
    {
        if (!_isDeleteMode || _selectedFish == null)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (_deleteConfirmPopup == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishDeleteUI] 삭제 확인 팝업이 연결되지 않았습니다.",
                this);

            return;
        }

        // 팝업이 열린 동안 뒤쪽 전체 화면 취소 버튼 입력을 차단합니다.
        if (_cancelAreaButton != null)
            _cancelAreaButton.interactable = false;

        _deleteConfirmPopup.SetActive(true);
        _deleteConfirmPopup.transform.SetAsLastSibling();

        Debug.Log(
            "[NyangQuariumFishDeleteUI] 삭제 확인 팝업 열기",
            this);
    }

    private void ConfirmDeleteSelectedFish()
    {
        if (!_isDeleteMode ||
            _selectedFish == null ||
            _placedFishRenderer == null)
        {
            return;
        }

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        bool deleted = _placedFishRenderer.DeleteSelectedFish();

        if (!deleted)
        {
            Debug.LogWarning(
                "[NyangQuariumFishDeleteUI] 선택된 물고기를 삭제하지 못했습니다.",
                this);

            CloseDeleteConfirmPopup();
            ExitDeleteMode(true);
            return;
        }

        List<NyangquariumPlacedFishData> currentData =
            _placedFishRenderer.GetCurrentPlacedFishData();

        PlacedFishDataChanged?.Invoke(currentData);

        CloseDeleteConfirmPopup();
        ExitDeleteMode(false);

        Debug.Log(
            $"[NyangQuariumFishDeleteUI] 물고기 삭제 완료 - 남은 수: {currentData.Count}",
            this);
    }

    private void CancelDeleteConfirmPopup()
    {
        if (!_isDeleteMode || _selectedFish == null)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        // 팝업만 닫고 현재 물고기의 삭제 모드로 돌아갑니다.
        CloseDeleteConfirmPopup();
        ShowDeleteUI();
        RefreshFishName();
        UpdateSelectionUIPosition();

        Debug.Log(
            "[NyangQuariumFishDeleteUI] 삭제 확인 취소 - 삭제 모드 복귀",
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

        // 전체 화면 취소 영역이 앞에 있어도 뒤에 있는 물고기까지 Raycast하여
        // 물고기를 눌렀다면 삭제 모드를 종료하지 않고 선택 대상을 변경합니다.
        List<RaycastResult> raycastResults = new();

        if (EventSystem.current != null)
            EventSystem.current.RaycastAll(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;

            if (hitObject == null ||
                hitObject == _cancelAreaButton.gameObject)
            {
                continue;
            }

            NyangquariumFishController clickedFish =
                hitObject.GetComponentInParent<NyangquariumFishController>();

            if (clickedFish == null)
                continue;

            if (_placedFishRenderer != null)
                _placedFishRenderer.SelectFish(clickedFish);

            return;
        }

        CancelDeleteMode();
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
        if (!_isDeleteMode && _selectedFish == null)
            return;

        NyangquariumFishController previousFish = _selectedFish;

        _selectedFish = null;
        _isDeleteMode = false;

        /*
         * 삭제된 물고기는 Unity의 null 상태가 되므로 실행되지 않고,
         * 취소된 물고기만 다시 이동합니다.
         */
        if (previousFish != null)
            previousFish.SetMovementEnabled(true);

        CloseDeleteConfirmPopup();
        HideDeleteUI();
        RestoreNormalUI();

        if (clearSelection &&
            _placedFishRenderer != null &&
            _placedFishRenderer.HasSelectedFish)
        {
            _placedFishRenderer.ClearSelection();
        }

        Debug.Log(
            "[NyangQuariumFishDeleteUI] 삭제 모드 종료",
            this);
    }

    private void ShowDeleteUI()
    {
        /*
         * 취소 영역을 먼저 활성화하고,
         * 선택 UI를 가장 앞으로 올려 X 버튼이 클릭되게 합니다.
         */
        if (_cancelAreaButton != null)
        {
            _cancelAreaButton.gameObject.SetActive(true);
        }

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

    /// <summary>
    /// 선택된 물고기의 SpriteKey와 FishSO의 FishKey를 비교합니다.
    /// </summary>
    private void RefreshFishName()
    {
        if (_fishNameText == null)
            return;

        if (_selectedFish == null)
        {
            _fishNameText.text = string.Empty;
            return;
        }

        string spriteKey = _selectedFish.SpriteKey;

        if (TryGetFishName(spriteKey, out string fishName))
        {
            _fishNameText.text = fishName;
            return;
        }

        // 이름 매핑 실패를 게임 화면과 로그에서 확인할 수 있도록 Key를 표시합니다.
        _fishNameText.text = spriteKey;

        Debug.LogWarning(
            $"[NyangQuariumFishDeleteUI] FishKey와 일치하는 물고기를 찾지 못했습니다. Key: {spriteKey}",
            this);
    }

    private bool TryGetFishName(
        string spriteKey,
        out string fishName)
    {
        fishName = string.Empty;

        if (_fishSO == null ||
            _fishSO.FishData == null ||
            string.IsNullOrWhiteSpace(spriteKey))
        {
            return false;
        }

        IReadOnlyList<NyangQuariumFishData> fishDataList =
            _fishSO.FishData;

        for (int i = 0; i < fishDataList.Count; i++)
        {
            NyangQuariumFishData fishData = fishDataList[i];

            if (fishData == null)
                continue;

            if (!string.Equals(
                    fishData.FishKey,
                    spriteKey,
                    StringComparison.Ordinal))
            {
                continue;
            }

            fishName = fishData.FishName;
            return !string.IsNullOrWhiteSpace(fishName);
        }

        return false;
    }

    /// <summary>
    /// 움직이는 물고기의 현재 위치를 따라 선택 UI를 이동합니다.
    /// </summary>
    private void UpdateSelectionUIPosition()
    {
        if (_selectedFish == null ||
            _selectionUIRoot == null)
        {
            return;
        }

        RectTransform fishRect = _selectedFish.RectTransform;
        RectTransform uiParent =
            _selectionUIRoot.parent as RectTransform;

        if (fishRect == null || uiParent == null)
            return;

        Camera uiCamera = GetUICamera(uiParent);

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                fishRect.position);

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
                new Vector2(0f, _deleteButtonOffsetY);
        }

        if (_fishNameText != null)
        {
            _fishNameText.rectTransform.anchoredPosition =
                new Vector2(0f, -_fishNameOffsetY);
        }
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
/// Button.onClick과 달리 PointerEventData를 전달할 수 있습니다.
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
