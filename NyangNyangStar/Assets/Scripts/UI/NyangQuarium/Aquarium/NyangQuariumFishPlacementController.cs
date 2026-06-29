using System;
using Core.Managers;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 물고기와 자연 요소의 배치 흐름을 관리합니다.
///
/// 물고기:
/// 인벤토리에서 아이템 선택
/// → FishId로 NyangQuariumFishSO에서 FishKey 조회
/// → Confirm 버튼 클릭
/// → NyangquariumPlacedFishRenderer API로 움직이는 물고기 생성
///
/// 자연 요소:
/// 인벤토리에서 아이템 선택
/// → 화면 중앙에 미리보기 생성
/// → 드래그로 위치 조정
/// → Confirm 버튼 클릭 시 현재 위치 확정
/// </summary>
public sealed class NyangQuariumFishPlacementController : MonoBehaviour
{
    [Header("인벤토리")]
    [SerializeField]
    private GameObject _inventoryPanel;

    [Header("배치 대상")]
    [SerializeField]
    private RectTransform _nyangQuariumLayoutPanel;

    [SerializeField]
    private NyangQuariumFishPlacementArea _placementArea;

    [Header("물고기 배치")]
    [Tooltip("FishId를 FishKey로 변환하는 물고기 데이터 SO")]
    [SerializeField]
    private NyangQuariumFishSO _fishSO;

    [Tooltip("물고기 생성, 이동, 선택, 삭제를 담당하는 팀원 Renderer")]
    [SerializeField]
    private NyangquariumPlacedFishRenderer _placedFishRenderer;

    [Header("자연 요소 배치")]
    [Tooltip("선택 즉시 중앙에 미리보기로 생성할 자연 요소 공통 프리팹")]
    [SerializeField]
    private RectTransform _placedNaturePrefab;

    [Header("화면 고정 확인/취소 버튼")]
    [SerializeField]
    private Button _confirmButton;

    [SerializeField]
    private Button _cancelButton;

    [Header("배치 모드에서 숨길 UI")]
    [SerializeField]
    private GameObject[] _mainUIObjects;

    private bool[] _mainUIActiveStates;

    private int _selectedItemId;
    private string _selectedFishKey;
    private Sprite _selectedSprite;
    private NyangQuariumPlacementCategory _selectedCategory;

    private RectTransform _naturePreview;
    private NyangQuariumFishPlacementDragHandler _natureDragHandler;
    private bool _isPlacementMode;

    public bool IsPlacementMode => _isPlacementMode;

    public event Action<
        int,
        Sprite,
        NyangQuariumPlacementCategory> ItemSelected;

    public event Action<
        RectTransform,
        int,
        NyangQuariumPlacementCategory> PlacementConfirmed;

    public event Action PlacementCanceled;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        SetDecisionButtonsActive(false);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    /// <summary>
    /// 인벤토리에서 선택한 아이템 정보를 배치 시스템에 전달합니다.
    /// 물고기라면 FishId로 FishKey를 미리 조회합니다.
    /// </summary>
    public void SelectItem(
        int itemId,
        Sprite itemSprite,
        NyangQuariumPlacementCategory category)
    {
        if (_isPlacementMode || itemSprite == null)
            return;

        string fishKey = string.Empty;

        if (category == NyangQuariumPlacementCategory.Fish)
        {
            if (!TryGetFishKey(itemId, out fishKey))
            {
                Debug.LogWarning(
                    $"[NyangQuariumFishPlacementController] " +
                    $"FishId와 일치하는 FishKey를 찾지 못했습니다. " +
                    $"FishId:{itemId}",
                    this);

                return;
            }
        }

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        _selectedItemId = itemId;
        _selectedFishKey = fishKey;
        _selectedSprite = itemSprite;
        _selectedCategory = category;

        EnterPlacementMode();

        if (category == NyangQuariumPlacementCategory.Nature)
            CreateNaturePreview();

        SetDecisionButtonsActive(true);

        ItemSelected?.Invoke(
            itemId,
            itemSprite,
            category);

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"아이템 선택 - " +
            $"ID:{itemId}, " +
            $"FishKey:{fishKey}, " +
            $"Category:{category}",
            this);
    }

    /// <summary>
    /// 현재 선택된 물고기 또는 자연 요소의 배치를 확정합니다.
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!_isPlacementMode || _selectedSprite == null)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        RectTransform confirmedObject = null;

        switch (_selectedCategory)
        {
            case NyangQuariumPlacementCategory.Fish:
                confirmedObject = ConfirmFishPlacement();
                break;

            case NyangQuariumPlacementCategory.Nature:
                confirmedObject = ConfirmNaturePlacement();
                break;

            default:
                Debug.LogWarning(
                    $"[NyangQuariumFishPlacementController] " +
                    $"지원하지 않는 배치 카테고리입니다. " +
                    $"Category:{_selectedCategory}",
                    this);
                break;
        }

        if (confirmedObject == null)
            return;

        int confirmedItemId = _selectedItemId;

        NyangQuariumPlacementCategory confirmedCategory =
            _selectedCategory;

        ExitPlacementMode();
        ClearSelection();

        PlacementConfirmed?.Invoke(
            confirmedObject,
            confirmedItemId,
            confirmedCategory);

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"배치 확정 - {confirmedObject.name}",
            this);
    }

    /// <summary>
    /// 팀원이 만든 NyangquariumPlacedFishRenderer를 통해
    /// 움직이는 물고기를 생성합니다.
    /// </summary>
    private RectTransform ConfirmFishPlacement()
    {
        if (_placedFishRenderer == null)
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "PlacedFishRenderer가 연결되지 않았습니다.",
                this);

            return null;
        }

        if (_selectedItemId <= 0)
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "선택된 물고기 ID가 올바르지 않습니다.",
                this);

            return null;
        }

        NyangquariumFishController spawnedFish =
            _placedFishRenderer.ConfirmPlacedFish(
                _selectedItemId,
                _fishSO,
                1f);

        if (spawnedFish == null)
        {
            Debug.LogError(
                $"[NyangQuariumFishPlacementController] " +
                $"물고기 생성에 실패했습니다. " +
                $"ItemId:{_selectedItemId}",
                this);

            return null;
        }

        RectTransform fishRect =
            spawnedFish.RectTransform;

        if (fishRect == null)
        {
            Debug.LogError(
                $"[NyangQuariumFishPlacementController] " +
                $"생성된 물고기의 RectTransform을 가져오지 못했습니다. " +
                $"FishKey:{_selectedFishKey}",
                this);

            return null;
        }

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"팀원 API를 통한 물고기 생성 완료 - " +
            $"FishKey:{_selectedFishKey}",
            this);

        return fishRect;
    }

    /// <summary>
    /// 현재 진행 중인 배치를 취소합니다.
    /// </summary>
    public void CancelPlacement()
    {
        if (!_isPlacementMode)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        DestroyNaturePreview();

        ExitPlacementMode();
        ClearSelection();

        PlacementCanceled?.Invoke();

        Debug.Log(
            "[NyangQuariumFishPlacementController] 배치 취소",
            this);
    }

    /// <summary>
    /// 외부에서 배치 상태를 강제로 초기화할 때 사용합니다.
    /// </summary>
    public void ResetCurrentPlacement()
    {
        DestroyNaturePreview();

        ExitPlacementMode();
        ClearSelection();

        Debug.Log(
            "[NyangQuariumFishPlacementController] " +
            "현재 배치 상태 초기화",
            this);
    }

    /// <summary>
    /// 자연 요소를 수조 중앙에 미리보기로 생성합니다.
    /// </summary>
    private void CreateNaturePreview()
    {
        if (!ValidateNatureReferences())
            return;

        _naturePreview = CreateNatureObject(
            $"NaturePreview_{_selectedItemId}");

        if (_naturePreview == null)
            return;

        _naturePreview.anchoredPosition =
            _placementArea.GetCenterLocalPosition();

        _natureDragHandler =
            _naturePreview.GetComponent<
                NyangQuariumFishPlacementDragHandler>();

        if (_natureDragHandler == null)
        {
            _natureDragHandler =
                _naturePreview.gameObject.AddComponent<
                    NyangQuariumFishPlacementDragHandler>();
        }

        _natureDragHandler.Initialize(_placementArea);

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"자연 요소 미리보기 생성 - ID:{_selectedItemId}",
            this);
    }

    /// <summary>
    /// 자연 요소의 현재 위치를 최종 배치 위치로 확정합니다.
    /// </summary>
    private RectTransform ConfirmNaturePlacement()
    {
        if (_naturePreview == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementController] " +
                "확정할 자연 요소 미리보기가 없습니다.",
                this);

            return null;
        }

        _natureDragHandler?.SetDraggable(false);

        RectTransform confirmedNature =
            _naturePreview;

        confirmedNature.name =
            $"PlacedNature_{_selectedItemId}_" +
            $"{_nyangQuariumLayoutPanel.childCount}";

        _naturePreview = null;
        _natureDragHandler = null;

        return confirmedNature;
    }

    /// <summary>
    /// 자연 요소 공통 프리팹을 생성하고 선택된 Sprite를 적용합니다.
    /// </summary>
    private RectTransform CreateNatureObject(
        string objectName)
    {
        if (_placedNaturePrefab == null ||
            _nyangQuariumLayoutPanel == null)
        {
            return null;
        }

        RectTransform placedObject =
            Instantiate(
                _placedNaturePrefab,
                _nyangQuariumLayoutPanel);

        placedObject.name =
            $"{objectName}_" +
            $"{_nyangQuariumLayoutPanel.childCount}";

        placedObject.localScale = Vector3.one;
        placedObject.SetAsLastSibling();

        Image image =
            placedObject.GetComponent<Image>();

        if (image == null)
        {
            image =
                placedObject.GetComponentInChildren<Image>(
                    true);
        }

        if (image == null)
        {
            Debug.LogError(
                $"[NyangQuariumFishPlacementController] " +
                $"{_placedNaturePrefab.name}에 Image가 없습니다.",
                this);

            Destroy(placedObject.gameObject);
            return null;
        }

        image.sprite = _selectedSprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        return placedObject;
    }

    /// <summary>
    /// 선택된 FishId와 일치하는 FishKey를
    /// NyangQuariumFishSO에서 조회합니다.
    /// </summary>
    private bool TryGetFishKey(
        int fishId,
        out string fishKey)
    {
        fishKey = string.Empty;

        if (_fishSO == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumFishSO가 연결되지 않았습니다.",
                this);

            return false;
        }

        if (_fishSO.FishData == null ||
            _fishSO.FishData.Count == 0)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumFishSO의 FishData가 비어 있습니다.",
                this);

            return false;
        }

        for (int i = 0; i < _fishSO.FishData.Count; i++)
        {
            NyangQuariumFishData fishData =
                _fishSO.FishData[i];

            if (fishData == null)
                continue;

            if (fishData.FishId != fishId)
                continue;

            fishKey = fishData.FishKey;

            if (string.IsNullOrWhiteSpace(fishKey))
            {
                Debug.LogWarning(
                    $"[NyangQuariumFishPlacementController] " +
                    $"FishId는 찾았지만 FishKey가 비어 있습니다. " +
                    $"FishId:{fishId}",
                    this);

                return false;
            }

            return true;
        }

        return false;
    }

    private void DestroyNaturePreview()
    {
        if (_naturePreview != null)
            Destroy(_naturePreview.gameObject);

        _naturePreview = null;
        _natureDragHandler = null;
    }

    private void EnterPlacementMode()
    {
        _isPlacementMode = true;

        if (_inventoryPanel != null)
            _inventoryPanel.SetActive(false);

        HideMainUIAndSaveStates();
    }

    private void ExitPlacementMode()
    {
        _isPlacementMode = false;

        if (_inventoryPanel != null)
            _inventoryPanel.SetActive(true);

        RestoreMainUIStates();
        SetDecisionButtonsActive(false);
    }

    /// <summary>
    /// 배치 모드 진입 전 일반 UI의 활성 상태를 저장하고 숨깁니다.
    /// </summary>
    private void HideMainUIAndSaveStates()
    {
        if (_mainUIObjects == null)
            return;

        _mainUIActiveStates =
            new bool[_mainUIObjects.Length];

        for (int i = 0; i < _mainUIObjects.Length; i++)
        {
            GameObject target =
                _mainUIObjects[i];

            if (target == null)
                continue;

            _mainUIActiveStates[i] =
                target.activeSelf;

            target.SetActive(false);
        }
    }

    /// <summary>
    /// 배치 모드 진입 전에 저장했던 UI 상태를 복구합니다.
    /// </summary>
    private void RestoreMainUIStates()
    {
        if (_mainUIObjects == null)
            return;

        for (int i = 0; i < _mainUIObjects.Length; i++)
        {
            GameObject target =
                _mainUIObjects[i];

            if (target == null)
                continue;

            bool wasActive =
                _mainUIActiveStates != null &&
                i < _mainUIActiveStates.Length &&
                _mainUIActiveStates[i];

            target.SetActive(wasActive);
        }

        _mainUIActiveStates = null;
    }

    private void SetDecisionButtonsActive(
        bool isActive)
    {
        if (_confirmButton != null)
        {
            _confirmButton.gameObject.SetActive(
                isActive);
        }

        if (_cancelButton != null)
        {
            _cancelButton.gameObject.SetActive(
                isActive);
        }
    }

    private void ClearSelection()
    {
        _selectedItemId = 0;
        _selectedFishKey = string.Empty;
        _selectedSprite = null;
        _selectedCategory = default;
    }

    /// <summary>
    /// 인스펙터 연결이 비어 있는 경우 같은 계층에서 참조를 탐색합니다.
    /// </summary>
    private void ResolveReferences()
    {
        if (_nyangQuariumLayoutPanel == null &&
            _placementArea != null)
        {
            _nyangQuariumLayoutPanel =
                _placementArea.AreaRect;
        }

        if (_placementArea == null &&
            _nyangQuariumLayoutPanel != null)
        {
            _placementArea =
                _nyangQuariumLayoutPanel.GetComponent<
                    NyangQuariumFishPlacementArea>();
        }

        if (_placedFishRenderer == null &&
            _nyangQuariumLayoutPanel != null)
        {
            _placedFishRenderer =
                _nyangQuariumLayoutPanel
                    .GetComponentInChildren<
                        NyangquariumPlacedFishRenderer>(
                        true);
        }
    }

    private bool ValidateNatureReferences()
    {
        if (_nyangQuariumLayoutPanel == null)
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumLayoutPanel이 연결되지 않았습니다.",
                this);

            return false;
        }

        if (_placementArea == null)
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "PlacementArea가 연결되지 않았습니다.",
                this);

            return false;
        }

        if (_placedNaturePrefab == null)
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "자연 요소 프리팹이 연결되지 않았습니다.",
                this);

            return false;
        }

        return true;
    }

    private void BindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(
                ConfirmPlacement);

            _confirmButton.onClick.AddListener(
                ConfirmPlacement);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(
                CancelPlacement);

            _cancelButton.onClick.AddListener(
                CancelPlacement);
        }
    }

    private void UnbindButtons()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(
                ConfirmPlacement);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(
                CancelPlacement);
        }
    }
}