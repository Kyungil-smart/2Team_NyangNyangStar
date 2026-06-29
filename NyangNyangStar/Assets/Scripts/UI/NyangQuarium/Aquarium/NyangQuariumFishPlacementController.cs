using Core.Managers;
using System;
using System.Collections.Generic;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.UI;
using UI.NyangQuarium.MergeBoard;

/// <summary>
/// 물고기와 자연 요소의 배치 흐름을 관리합니다.
///
/// 물고기:
/// 아이템 선택
/// → ItemId로 NyangQuariumFishSO에서 SpriteKey 조회
/// → 확정
/// → NyangquariumPlacedFishRenderer를 통해 생성
///
/// 자연 요소:
/// 아이템 선택
/// → ItemId로 NyangQuariumFishSO에서 SpriteKey 조회
/// → 화면 중앙에 런타임 이미지 생성
/// → 드래그로 위치 조정
/// → 확정
/// </summary>
public sealed class NyangQuariumFishPlacementController : MonoBehaviour
{
    [Header("인벤토리")]
    [SerializeField]
    private GameObject _inventoryPanel;

    [Tooltip("FreshwaterLayoutPanel 안의 배치 버튼")]
    [SerializeField]
    private Button _inventoryConfirmButton;

    [Header("배치 대상")]
    [SerializeField]
    private RectTransform _nyangQuariumLayoutPanel;

    [SerializeField]
    private NyangQuariumFishPlacementArea _placementArea;

    [Header("배치 데이터")]
    [Tooltip("물고기 및 자연 요소 ItemId를 SpriteKey로 변환하는 SO")]
    [SerializeField]
    private NyangQuariumFishSO _fishSO;

    [Tooltip("이 배치 UI가 저장할 수조 타입입니다. 담수 수조는 Freshwater, 해수 수조는 Saltwater로 둡니다.")]
    [SerializeField]
    private FishType _aquariumType = FishType.Freshwater;

    [Header("물고기 배치")]
    [Tooltip("물고기 생성, 이동, 선택, 삭제를 담당하는 Renderer")]
    [SerializeField]
    private NyangquariumPlacedFishRenderer _placedFishRenderer;

    [Header("자연 요소 최종 확인/취소 버튼")]
    [Tooltip("NyangQuariumLayoutPanel 안의 자연 요소 최종 확정 버튼")]
    [SerializeField]
    private Button _confirmButton;

    [SerializeField]
    private Button _cancelButton;

    [Tooltip("자연 요소 미리보기 기준 최종 확정 버튼 위치")]
    [SerializeField]
    private Vector2 _natureConfirmOffset = new Vector2(-70f, 80f);

    [Tooltip("자연 요소 미리보기 기준 취소 버튼 위치")]
    [SerializeField]
    private Vector2 _natureCancelOffset = new Vector2(70f, 80f);

    [Header("배치 모드에서 숨길 UI")]
    [SerializeField]
    private GameObject[] _mainUIObjects;

    private bool[] _mainUIActiveStates;

    private int _selectedItemId;
    private string _selectedSpriteKey;
    private Sprite _selectedSprite;

    private NyangQuariumPlacementCategory _selectedCategory;

    private RectTransform _naturePreview;

    private NyangquariumFishController
        _naturePreviewController;

    private NyangQuariumFishPlacementDragHandler
        _natureDragHandler;

    private bool _isPlacementMode;
    private bool _hasLoadedPlacedFish;
    private int _placedFishMutationVersion;

    public bool IsPlacementMode => _isPlacementMode;
    public FishType AquariumType => _aquariumType;

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
        SetInventoryConfirmInteractable(false);
    }

    private void OnEnable()
    {
        LoadPlacedFishFromFirestore();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void LateUpdate()
    {
        if (!_isPlacementMode ||
            _naturePreview == null)
        {
            return;
        }

        UpdateNatureDecisionButtonPositions();
    }

    /// <summary>
    /// 인벤토리에서 선택한 아이템 정보를 배치 시스템에 전달합니다.
    /// 물고기와 자연 요소 모두 ItemId를 이용해 SO에서 SpriteKey를 조회합니다.
    /// </summary>
    public void SelectItem(
        int itemId,
        Sprite itemSprite,
        NyangQuariumPlacementCategory category)
    {
        if (_isPlacementMode || itemSprite == null)
            return;

        if (!TryGetSpriteKey(
                itemId,
                category,
                out string spriteKey))
        {
            Debug.LogWarning(
                $"[NyangQuariumFishPlacementController] " +
                $"아이템 ID와 일치하는 SpriteKey를 찾지 못했습니다. " +
                $"ItemId:{itemId}, Category:{category}",
                this);

            return;
        }

        GameManager.Audio.PlaySfx(
            "Main_SFX_Touch");

        _selectedItemId = itemId;
        _selectedSpriteKey = spriteKey;
        _selectedSprite = itemSprite;
        _selectedCategory = category;

        SetInventoryConfirmInteractable(true);

        ItemSelected?.Invoke(
            itemId,
            itemSprite,
            category);

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"아이템 선택 - " +
            $"ID:{itemId}, " +
            $"SpriteKey:{spriteKey}, " +
            $"Category:{category}",
            this);
    }

    /// <summary>
    /// FreshwaterLayoutPanel의 배치 버튼에서 호출합니다.
    /// 물고기는 즉시 배치하고, 자연 요소는 미리보기 배치 모드로 진입합니다.
    /// </summary>
    public void StartSelectedPlacement()
    {
        if (_isPlacementMode ||
            string.IsNullOrWhiteSpace(_selectedSpriteKey))
        {
            return;
        }

        GameManager.Audio.PlaySfx(
            "Main_SFX_Touch");

        if (_selectedCategory ==
            NyangQuariumPlacementCategory.Fish)
        {
            ConfirmSelectedFish();
            return;
        }

        if (_selectedCategory ==
            NyangQuariumPlacementCategory.Nature)
        {
            StartNaturePlacement();
            return;
        }

        Debug.LogWarning(
            $"[NyangQuariumFishPlacementController] " +
            $"지원하지 않는 배치 카테고리입니다. " +
            $"Category:{_selectedCategory}",
            this);
    }

    /// <summary>
    /// 현재 선택된 물고기 또는 자연 요소의 배치를 확정합니다.
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!_isPlacementMode ||
            _selectedCategory !=
            NyangQuariumPlacementCategory.Nature ||
            string.IsNullOrWhiteSpace(_selectedSpriteKey))
        {
            return;
        }

        GameManager.Audio.PlaySfx(
            "Main_SFX_Touch");

        RectTransform confirmedObject =
            ConfirmNaturePlacement();

        if (confirmedObject == null)
            return;

        int confirmedItemId =
            _selectedItemId;

        ExitPlacementMode();
        ClearSelection();

        PlacementConfirmed?.Invoke(
            confirmedObject,
            confirmedItemId,
            NyangQuariumPlacementCategory.Nature);

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"자연 요소 배치 확정 - {confirmedObject.name}",
            this);
    }

    private void ConfirmSelectedFish()
    {
        NyangquariumFishController spawnedFish = ConfirmFishPlacement();

        if (spawnedFish == null)
            return;

        if (!NyangQuariumMergeBoardInventoryService
                .TryConsumeFish(_selectedItemId, 1))
        {
            _placedFishRenderer.DeleteFish(spawnedFish);

            Debug.LogWarning(
                $"[NyangQuariumFishPlacementController] " +
                $"머지보드 물고기 차감에 실패하여 배치를 취소했습니다. " +
                $"ItemId:{_selectedItemId}",
                this);

            return;
        }

        RectTransform confirmedObject =
            spawnedFish.RectTransform;

        int confirmedItemId =
            _selectedItemId;

        PlacementConfirmed?.Invoke(
            confirmedObject,
            confirmedItemId,
            NyangQuariumPlacementCategory.Fish);

        SavePlacedFishSnapshot();
        ClearSelection();

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"물고기 배치 및 머지보드 차감 완료 - " +
            $"ItemId:{confirmedItemId}, " +
            $"Object:{confirmedObject.name}",
            this);
    }

    private void StartNaturePlacement()
    {
        EnterPlacementMode();
        CreateNaturePreview();

        if (_naturePreview == null)
        {
            ExitPlacementMode();
            return;
        }

        SetDecisionButtonsActive(true);
        UpdateNatureDecisionButtonPositions();

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"자연 요소 배치 모드 시작 - " +
            $"ItemId:{_selectedItemId}, " +
            $"SpriteKey:{_selectedSpriteKey}",
            this);
    }

    /// <summary>
    /// NyangquariumPlacedFishRenderer를 통해 움직이는 물고기를 생성합니다.
    /// </summary>
    /// <summary>
    /// NyangquariumPlacedFishRenderer를 통해 움직이는 물고기를 생성합니다.
    /// </summary>
    private NyangquariumFishController ConfirmFishPlacement()
    {
        if (_placedFishRenderer == null)
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "PlacedFishRenderer가 연결되지 않았습니다.",
                this);

            return null;
        }

        if (string.IsNullOrWhiteSpace(
                _selectedSpriteKey))
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "선택된 물고기의 SpriteKey가 비어 있습니다.",
                this);

            return null;
        }

        NyangquariumFishController spawnedFish =
            _placedFishRenderer.ConfirmPlacedFish(
                _selectedItemId,
                _selectedSpriteKey,
                1f);

        if (spawnedFish == null)
        {
            Debug.LogError(
                $"[NyangQuariumFishPlacementController] " +
                $"물고기 생성에 실패했습니다. " +
                $"SpriteKey:{_selectedSpriteKey}",
                this);

            return null;
        }

        if (spawnedFish.RectTransform == null)
        {
            _placedFishRenderer.DeleteFish(spawnedFish);

            Debug.LogError(
                $"[NyangQuariumFishPlacementController] " +
                $"생성된 물고기의 RectTransform을 가져오지 못했습니다. " +
                $"SpriteKey:{_selectedSpriteKey}",
                this);

            return null;
        }

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"물고기 생성 완료 - " +
            $"ItemId:{_selectedItemId}, " +
            $"SpriteKey:{_selectedSpriteKey}",
            this);

        return spawnedFish;
    }

    public void SavePlacedFishSnapshot()
    {
        if (_placedFishRenderer == null)
            return;

        SavePlacedFishSnapshotAsync(
            _placedFishRenderer.GetCurrentPlacedFishData());
    }

    public void SavePlacedFishSnapshot(
        IReadOnlyList<NyangquariumPlacedFishData> placedFishData)
    {
        SavePlacedFishSnapshotAsync(placedFishData);
    }

    private async void SavePlacedFishSnapshotAsync(
        IEnumerable<NyangquariumPlacedFishData> placedFishData)
    {
        _placedFishMutationVersion++;

        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementController] " +
                "Firestore가 준비되지 않아 수조 배치 저장을 생략합니다.",
                this);
            return;
        }

        await firestoreSO.SavePlacedFishAsync(
            _aquariumType,
            placedFishData,
            _fishSO);
    }

    private async void LoadPlacedFishFromFirestore()
    {
        if (_hasLoadedPlacedFish || _placedFishRenderer == null)
            return;

        int loadVersion = _placedFishMutationVersion;

        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
            return;

        bool loadedOrCreated =
            await firestoreSO.LoadOrCreateFromServerAsync();

        if (!loadedOrCreated ||
            _placedFishRenderer == null ||
            loadVersion != _placedFishMutationVersion)
        {
            return;
        }

        IReadOnlyList<NyangquariumPlacedFishData> placedFishData =
            firestoreSO.GetPlacedFishData(
                _aquariumType,
                _fishSO);

        _placedFishRenderer.ShowPlacedFishes(placedFishData);
        _hasLoadedPlacedFish = true;
    }

    /// <summary>
    /// 현재 진행 중인 배치를 취소합니다.
    /// </summary>
    public void CancelPlacement()
    {
        if (!_isPlacementMode)
            return;

        GameManager.Audio.PlaySfx(
            "Main_SFX_Touch");

        DestroyNaturePreview();

        ExitPlacementMode();
        ClearSelection();

        PlacementCanceled?.Invoke();

        Debug.Log(
            "[NyangQuariumFishPlacementController] " +
            "배치 취소",
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
    /// 자연 요소를 SO의 SpriteKey를 이용해 수조 중앙에 생성합니다.
    /// 별도의 자연 요소 Prefab은 사용하지 않습니다.
    /// </summary>
    private void CreateNaturePreview()
    {
        if (!ValidateNatureReferences())
            return;

        Vector2 centerPosition =
            _placementArea.GetCenterLocalPosition();

        _naturePreviewController =
            NyangquariumFishController.SpawnMovingFish(
                _nyangQuariumLayoutPanel,
                _selectedSpriteKey,
                centerPosition,
                1f);

        if (_naturePreviewController == null)
        {
            Debug.LogError(
                $"[NyangQuariumFishPlacementController] " +
                $"자연 요소 생성에 실패했습니다. " +
                $"SpriteKey:{_selectedSpriteKey}",
                this);

            return;
        }

        // 자연 요소는 물고기처럼 헤엄치지 않습니다.
        _naturePreviewController
            .SetMovementEnabled(false);

        _naturePreviewController
            .SetSelectable(false);

        _naturePreview =
            _naturePreviewController.RectTransform;

        if (_naturePreview == null)
        {
            Destroy(
                _naturePreviewController.gameObject);

            _naturePreviewController = null;
            return;
        }

        _naturePreview.name =
            $"NaturePreview_{_selectedItemId}";

        _naturePreview.localRotation =
            Quaternion.identity;

        _naturePreview.SetAsLastSibling();

        Image natureImage =
            _naturePreview.GetComponent<Image>();

        if (natureImage != null)
        {
            natureImage.raycastTarget = true;
            natureImage.preserveAspect = true;
        }

        _natureDragHandler =
            _naturePreview.GetComponent<
                NyangQuariumFishPlacementDragHandler>();

        if (_natureDragHandler == null)
        {
            _natureDragHandler =
                _naturePreview.gameObject.AddComponent<
                    NyangQuariumFishPlacementDragHandler>();
        }

        _natureDragHandler.Initialize(
            _placementArea);

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"자연 요소 미리보기 생성 - " +
            $"ID:{_selectedItemId}, " +
            $"SpriteKey:{_selectedSpriteKey}",
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

        NyangQuariumPlacedNature placedNature =
            confirmedNature.GetComponent<NyangQuariumPlacedNature>();

        if (placedNature == null)
        {
            placedNature =
                confirmedNature.gameObject.AddComponent<
                    NyangQuariumPlacedNature>();
        }

        placedNature.Initialize(
            _selectedItemId,
            _selectedSpriteKey,
            _naturePreviewController);

        _naturePreview = null;
        _naturePreviewController = null;
        _natureDragHandler = null;

        Debug.Log(
            $"[NyangQuariumFishPlacementController] " +
            $"자연 요소 배치 확정 - " +
            $"ItemId:{placedNature.ItemId}, " +
            $"SpriteKey:{placedNature.SpriteKey}",
            this);

        return confirmedNature;
    }

    /// <summary>
    /// ItemId와 카테고리를 기준으로 SO에서 SpriteKey를 조회합니다.
    /// </summary>
    private bool TryGetSpriteKey(
        int itemId,
        NyangQuariumPlacementCategory category,
        out string spriteKey)
    {
        spriteKey = string.Empty;

        if (_fishSO == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumFishSO가 연결되지 않았습니다.",
                this);

            return false;
        }

        IReadOnlyList<NyangQuariumFishData> dataList =
            _fishSO.FishData;

        if (dataList == null ||
            dataList.Count == 0)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumFishSO의 데이터가 비어 있습니다.",
                this);

            return false;
        }

        for (int i = 0; i < dataList.Count; i++)
        {
            NyangQuariumFishData itemData =
                dataList[i];

            if (itemData == null ||
                itemData.FishId != itemId)
            {
                continue;
            }

            if (!IsMatchingCategory(
                    itemData,
                    category))
            {
                Debug.LogWarning(
                    $"[NyangQuariumFishPlacementController] " +
                    $"아이템 카테고리와 SO 데이터 타입이 일치하지 않습니다. " +
                    $"ItemId:{itemId}, " +
                    $"Category:{category}, " +
                    $"FishType:{itemData.FishType}",
                    this);

                return false;
            }

            spriteKey = itemData.FishKey;

            if (string.IsNullOrWhiteSpace(
                    spriteKey))
            {
                Debug.LogWarning(
                    $"[NyangQuariumFishPlacementController] " +
                    $"아이템 데이터는 찾았지만 SpriteKey가 비어 있습니다. " +
                    $"ItemId:{itemId}",
                    this);

                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 선택된 배치 카테고리와 SO 데이터 타입이 일치하는지 검사합니다.
    /// </summary>
    private static bool IsMatchingCategory(
        NyangQuariumFishData itemData,
        NyangQuariumPlacementCategory category)
    {
        if (itemData == null)
            return false;

        switch (category)
        {
            case NyangQuariumPlacementCategory.Fish:
                return itemData.FishType !=
                       FishType.Environments &&
                       itemData.FishType !=
                       FishType.None;

            case NyangQuariumPlacementCategory.Nature:
                return itemData.FishType ==
                       FishType.Environments;

            default:
                return false;
        }
    }

    private void DestroyNaturePreview()
    {
        if (_naturePreviewController != null)
        {
            Destroy(
                _naturePreviewController.gameObject);
        }
        else if (_naturePreview != null)
        {
            Destroy(
                _naturePreview.gameObject);
        }

        _naturePreview = null;
        _naturePreviewController = null;
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

        for (int i = 0;
             i < _mainUIObjects.Length;
             i++)
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

        for (int i = 0;
             i < _mainUIObjects.Length;
             i++)
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

    /// <summary>
    /// 자연 요소 미리보기 위로 확정 버튼과 취소 버튼을 함께 이동시킵니다.
    /// </summary>
    private void UpdateNatureDecisionButtonPositions()
    {
        if (_naturePreview == null)
            return;

        UpdateDecisionButtonPosition(
            _confirmButton,
            _natureConfirmOffset);

        UpdateDecisionButtonPosition(
            _cancelButton,
            _natureCancelOffset);
    }

    /// <summary>
    /// 지정한 버튼을 자연 요소 미리보기 위치와 Offset에 맞춰 이동합니다.
    /// </summary>
    private void UpdateDecisionButtonPosition(
        Button targetButton,
        Vector2 offset)
    {
        if (targetButton == null)
            return;

        RectTransform buttonRect =
            targetButton.transform as RectTransform;

        RectTransform buttonParent =
            buttonRect != null
                ? buttonRect.parent as RectTransform
                : null;

        if (buttonRect == null ||
            buttonParent == null)
        {
            return;
        }

        Canvas canvas =
            buttonParent.GetComponentInParent<Canvas>();

        Camera uiCamera =
            canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                _naturePreview.position);

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    buttonParent,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPosition))
        {
            return;
        }

        buttonRect.anchoredPosition =
            localPosition + offset;

        buttonRect.SetAsLastSibling();
    }

    private void ClearSelection()
    {
        _selectedItemId = 0;
        _selectedSpriteKey = string.Empty;
        _selectedSprite = null;
        _selectedCategory = default;

        SetInventoryConfirmInteractable(false);
    }

    private void SetInventoryConfirmInteractable(
        bool isInteractable)
    {
        if (_inventoryConfirmButton != null)
            _inventoryConfirmButton.interactable = isInteractable;
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

        if (string.IsNullOrWhiteSpace(
                _selectedSpriteKey))
        {
            Debug.LogError(
                "[NyangQuariumFishPlacementController] " +
                "자연 요소 SpriteKey가 비어 있습니다.",
                this);

            return false;
        }

        return true;
    }

    private void BindButtons()
    {
        if (_inventoryConfirmButton != null)
        {
            _inventoryConfirmButton.onClick.RemoveListener(
                StartSelectedPlacement);

            _inventoryConfirmButton.onClick.AddListener(
                StartSelectedPlacement);
        }

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
        if (_inventoryConfirmButton != null)
        {
            _inventoryConfirmButton.onClick.RemoveListener(
                StartSelectedPlacement);
        }

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
