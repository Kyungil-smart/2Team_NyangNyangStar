using Core.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.NyangQuarium;
using UI.NyangQuarium.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("자연 요소 배치 가능 여부 표시")]
    [Tooltip("자연 요소가 배치 가능 영역을 벗어났을 때 적용할 색상")]
    [SerializeField]
    private Color _invalidNatureColor = Color.red;

    [Tooltip("Top, Bottom, Left, Right Image를 포함하는 배치 가능구역 테두리 부모")]
    [SerializeField]
    private GameObject _naturePlacementAreaBorder;

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

    private Image _naturePreviewImage;
    private Color _naturePreviewDefaultColor = Color.white;
    private bool _isNaturePlacementValid;

    private bool _isPlacementMode;
    private bool _hasLoadedPlacedFish;
    private bool _hasLoadedPlacedNature;
    private int _placedFishMutationVersion;
    private int _placedNatureMutationVersion;

    private NyangQuariumFirestoreSO _firestoreSO;
    private NyangQuariumAquariumLevelSO _aquariumLevelSO;
    private bool _isPlacementLimitReady;

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
        SetNaturePlacementBorderActive(false);

        _ = RefreshPlacementLimitDataAsync();
    }

    private void OnEnable()
    {
        LoadPlacedObjectsFromFirestore();

        // 화면에 다시 들어올 때 현재 수조 레벨과 배치 한도를 갱신합니다.
        _ = RefreshPlacementLimitDataAsync();
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
            DebugTool.Warning($"[NyangQuariumFishPlacementController] " +
                $"아이템 ID와 일치하는 SpriteKey를 찾지 못했습니다. " +
                $"ItemId:{itemId}, Category:{category}",
                DebugType.UI,
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

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"아이템 선택 - " +
            $"ID:{itemId}, " +
            $"SpriteKey:{spriteKey}, " +
            $"Category:{category}",
            DebugType.UI,
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

        DebugTool.Warning($"[NyangQuariumFishPlacementController] " +
            $"지원하지 않는 배치 카테고리입니다. " +
            $"Category:{_selectedCategory}",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 현재 선택된 자연 요소의 배치를 확정합니다.
    /// 배치 확정 전에 머지보드 보유 수량을 검사하고 1개 차감합니다.
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!_isPlacementMode ||
            _selectedCategory !=
            NyangQuariumPlacementCategory.Nature ||
            string.IsNullOrWhiteSpace(_selectedSpriteKey) ||
            _naturePreview == null)
        {
            return;
        }

        GameManager.Audio.PlaySfx(
            "Main_SFX_Touch");

        if (!_isNaturePlacementValid)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "자연 요소가 배치 가능 영역을 벗어나 확정할 수 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        if (!CanPlaceMoreNature())
            return;

        if (!NyangQuariumMergeBoardInventoryService
                .TryConsumeNature(
                    _selectedItemId,
                    1))
        {
            DebugTool.Warning($"[NyangQuariumFishPlacementController] " +
                $"머지보드 자연 요소 차감에 실패했습니다. " +
                $"배치 미리보기는 유지됩니다. " +
                $"ItemId:{_selectedItemId}",
                DebugType.UI,
                this);

            return;
        }

        RectTransform confirmedObject =
            ConfirmNaturePlacement();

        if (confirmedObject == null)
        {
            DebugTool.Error($"[NyangQuariumFishPlacementController] " +
                $"자연 요소 차감 후 배치 확정에 실패했습니다. " +
                $"ItemId:{_selectedItemId}",
                DebugType.UI,
                this);

            return;
        }

        int confirmedItemId =
            _selectedItemId;

        ExitPlacementMode();
        ClearSelection();

        PlacementConfirmed?.Invoke(
            confirmedObject,
            confirmedItemId,
            NyangQuariumPlacementCategory.Nature);

        // 현재 배치된 자연 요소 전체 목록을 Firestore에 저장합니다.
        SavePlacedNatureSnapshot();

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"자연 요소 배치 및 머지보드 차감 완료 - " +
            $"ItemId:{confirmedItemId}, " +
            $"Object:{confirmedObject.name}",
            DebugType.UI,
            this);
    }

    private void ConfirmSelectedFish()
    {
        if (!CanPlaceMoreFish())
            return;

        NyangquariumFishController spawnedFish = ConfirmFishPlacement();

        if (spawnedFish == null)
            return;

        if (!NyangQuariumMergeBoardInventoryService
                .TryConsumeFish(_selectedItemId, 1))
        {
            _placedFishRenderer.DeleteFish(spawnedFish);

            DebugTool.Warning($"[NyangQuariumFishPlacementController] " +
                $"머지보드 물고기 차감에 실패하여 배치를 취소했습니다. " +
                $"ItemId:{_selectedItemId}",
                DebugType.UI,
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

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"물고기 배치 및 머지보드 차감 완료 - " +
            $"ItemId:{confirmedItemId}, " +
            $"Object:{confirmedObject.name}",
            DebugType.UI,
            this);
    }

    private void StartNaturePlacement()
    {
        if (!CanPlaceMoreNature())
            return;

        EnterPlacementMode();
        SetNaturePlacementBorderActive(true);
        CreateNaturePreview();

        if (_naturePreview == null)
        {
            ExitPlacementMode();
            return;
        }

        SetDecisionButtonsActive(true);
        UpdateNatureDecisionButtonPositions();

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"자연 요소 배치 모드 시작 - " +
            $"ItemId:{_selectedItemId}, " +
            $"SpriteKey:{_selectedSpriteKey}",
            DebugType.UI,
            this);
    }
    /// <summary>
    /// Firestore의 현재 수조 레벨과 수조 레벨 SO를 준비합니다.
    /// </summary>
    private async Task RefreshPlacementLimitDataAsync()
    {
        _isPlacementLimitReady = false;

        _aquariumLevelSO =
            NyangQuariumQuestSOLocator.ResolveAquariumLevelSO();

        if (_aquariumLevelSO == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumAquariumLevelSO를 찾지 못했습니다.",
                DebugType.UI,
                this);

            return;
        }

        _firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (_firestoreSO == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                "Firestore에서 현재 수조 레벨을 가져오지 못했습니다.",
                DebugType.UI,
                this);

            return;
        }

        if (!_firestoreSO.TryGetCurrentMaxPlaceableCount(
                _aquariumLevelSO,
                out int maxPlaceableFish,
                out int maxPlaceableNature))
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                $"현재 수조 레벨의 배치 한도 데이터를 찾지 못했습니다. " +
                $"AquariumLevel:{_firestoreSO.AquariumLevel}",
                DebugType.UI,
                this);

            return;
        }

        _isPlacementLimitReady = true;

        DebugTool.Log(
            "[NyangQuariumFishPlacementController] " +
            $"배치 한도 데이터 준비 완료 - " +
            $"AquariumLevel:{_firestoreSO.AquariumLevel}, " +
            $"Fish:{maxPlaceableFish}, " +
            $"Nature:{maxPlaceableNature}",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 현재 수조 레벨에 해당하는 물고기와 자연 요소 최대 배치 수를 가져옵니다.
    /// </summary>
    private bool TryGetCurrentPlacementLimits(out int maxPlaceableFish, out int maxPlaceableNature)
    {
        maxPlaceableFish = 0;
        maxPlaceableNature = 0;

        if (!_isPlacementLimitReady ||
            _firestoreSO == null ||
            _aquariumLevelSO == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                "배치 한도 데이터가 아직 준비되지 않았습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (!_firestoreSO.TryGetCurrentMaxPlaceableCount(
                _aquariumLevelSO,
                out maxPlaceableFish,
                out maxPlaceableNature))
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                $"현재 수조 레벨의 배치 한도 조회에 실패했습니다. " +
                $"AquariumLevel:{_firestoreSO.AquariumLevel}",
                DebugType.UI,
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 현재 수조에 물고기를 추가로 배치할 수 있는지 확인합니다.
    /// 레벨 시스템 적용 전까지 최대 15마리로 제한합니다.
    /// </summary>
    /// <summary>
    /// 현재 수조 레벨의 물고기 최대 배치 수를 기준으로
    /// 물고기를 추가 배치할 수 있는지 확인합니다.
    /// </summary>
    private bool CanPlaceMoreFish()
    {
        if (_placedFishRenderer == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                "PlacedFishRenderer가 연결되지 않아 " +
                "물고기 배치 개수를 확인할 수 없습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (!TryGetCurrentPlacementLimits(
                out int maxPlaceableFish,
                out _))
        {
            return false;
        }

        int currentCount =
            _placedFishRenderer
                .GetCurrentPlacedFishData()
                .Count;

        return ValidatePlacementCount(
            currentCount,
            maxPlaceableFish,
            "물고기");
    }

    /// <summary>
    /// 현재 수조에 자연 요소를 추가로 배치할 수 있는지 확인합니다.
    /// 레벨 시스템 적용 전까지 최대 5개로 제한합니다.
    /// </summary>
    /// <summary>
    /// 현재 수조 레벨의 자연 요소 최대 배치 수를 기준으로
    /// 자연 요소를 추가 배치할 수 있는지 확인합니다.
    /// </summary>
    private bool CanPlaceMoreNature()
    {
        if (_nyangQuariumLayoutPanel == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFishPlacementController] " +
                "NyangQuariumLayoutPanel이 연결되지 않아 " +
                "자연 요소 배치 개수를 확인할 수 없습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (!TryGetCurrentPlacementLimits(
                out _,
                out int maxPlaceableNature))
        {
            return false;
        }

        int currentCount =
            GetCurrentPlacedNatureData().Count;

        return ValidatePlacementCount(
            currentCount,
            maxPlaceableNature,
            "자연 요소");
    }

    /// <summary>
    /// 현재 배치 개수와 최대 배치 개수를 비교합니다.
    /// </summary>
    private bool ValidatePlacementCount(
        int currentCount,
        int maxCount,
        string categoryName)
    {
        if (currentCount < maxCount)
            return true;

        DebugTool.Warning("[NyangQuariumFishPlacementController] " +
            $"{categoryName} 최대 배치 수에 도달했습니다. " +
            $"현재:{currentCount}, 최대:{maxCount}",
            DebugType.UI,
            this);

        return false;
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
            DebugTool.Error("[NyangQuariumFishPlacementController] " +
                "PlacedFishRenderer가 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return null;
        }

        if (string.IsNullOrWhiteSpace(
                _selectedSpriteKey))
        {
            DebugTool.Error("[NyangQuariumFishPlacementController] " +
                "선택된 물고기의 SpriteKey가 비어 있습니다.",
                DebugType.UI,
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
            DebugTool.Error($"[NyangQuariumFishPlacementController] " +
                $"물고기 생성에 실패했습니다. " +
                $"SpriteKey:{_selectedSpriteKey}",
                DebugType.UI,
                this);

            return null;
        }

        if (spawnedFish.RectTransform == null)
        {
            _placedFishRenderer.DeleteFish(spawnedFish);

            DebugTool.Error($"[NyangQuariumFishPlacementController] " +
                $"생성된 물고기의 RectTransform을 가져오지 못했습니다. " +
                $"SpriteKey:{_selectedSpriteKey}",
                DebugType.UI,
                this);

            return null;
        }

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"물고기 생성 완료 - " +
            $"ItemId:{_selectedItemId}, " +
            $"SpriteKey:{_selectedSpriteKey}",
            DebugType.UI,
            this);

        return spawnedFish;
    }

    public async void SavePlacedFishSnapshot()
    {
        if (_placedFishRenderer == null)
            return;

        IReadOnlyList<NyangquariumPlacedFishData> placedFishData =
            _placedFishRenderer.GetCurrentPlacedFishData();

        bool isSaved =
            await SavePlacedFishSnapshotAsync(placedFishData);

        if (isSaved)
        {
            NyangQuariumPlacedCountUI.NotifyCountChanged(
                _aquariumType);
        }
    }

    public async void SavePlacedFishSnapshot(
        IReadOnlyList<NyangquariumPlacedFishData> placedFishData)
    {
        bool isSaved =
            await SavePlacedFishSnapshotAsync(placedFishData);

        if (isSaved)
        {
            NyangQuariumPlacedCountUI.NotifyCountChanged(
                _aquariumType);
        }
    }

    /// <summary>
    /// 선택된 물고기를 삭제하고 최신 배치 목록을 저장합니다.
    /// 저장 성공 후 현재 수조의 카운트 UI에 변경을 알립니다.
    /// </summary>
    public async Task<bool> DeleteSelectedFishAsync()
    {
        if (_placedFishRenderer == null)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "PlacedFishRenderer가 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (!_placedFishRenderer.DeleteSelectedFish())
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "선택된 물고기를 삭제하지 못했습니다.",
                DebugType.UI,
                this);

            return false;
        }

        IReadOnlyList<NyangquariumPlacedFishData> currentData =
            _placedFishRenderer.GetCurrentPlacedFishData();

        bool isSaved =
            await SavePlacedFishSnapshotAsync(currentData);

        if (!isSaved)
            return false;

        NyangQuariumPlacedCountUI.NotifyCountChanged(
            _aquariumType);

        return true;
    }

    private async Task<bool> SavePlacedFishSnapshotAsync(
        IEnumerable<NyangquariumPlacedFishData> placedFishData)
    {
        _placedFishMutationVersion++;

        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "Firestore가 준비되지 않아 수조 배치 저장을 생략합니다.",
                DebugType.UI,
                this);

            return false;
        }

        bool isSaved = await firestoreSO.SavePlacedFishAsync(
            _aquariumType,
            placedFishData,
            _fishSO);

        if (!isSaved)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                $"물고기 Firestore 저장 실패 - AquariumType:{_aquariumType}",
                DebugType.UI,
                this);

            return false;
        }

        return true;
    }
    /// <summary>
    /// 현재 수조에 배치된 자연 요소의 식별 정보, 위치, 크기를 수집해 저장합니다.
    /// </summary>
    public async void SavePlacedNatureSnapshot()
    {
        if (_nyangQuariumLayoutPanel == null)
            return;

        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
            GetCurrentPlacedNatureData();

        bool isSaved =
            await SavePlacedNatureSnapshotAsync(placedNatureData);

        if (isSaved)
        {
            NyangQuariumPlacedCountUI.NotifyCountChanged(
                _aquariumType);
        }
    }

    /// <summary>
    /// 자연 요소 저장을 외부에서 요청할 때 사용합니다.
    /// 삭제는 DeletePlacedNatureAsync를 사용합니다.
    /// </summary>
    public async void SavePlacedNatureSnapshot(
        NyangQuariumPlacedNature excludedNature)
    {
        if (_nyangQuariumLayoutPanel == null)
            return;

        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
            GetCurrentPlacedNatureData(excludedNature);

        bool isSaved =
            await SavePlacedNatureSnapshotAsync(placedNatureData);

        if (isSaved)
        {
            NyangQuariumPlacedCountUI.NotifyCountChanged(
                _aquariumType);
        }
    }

    /// <summary>
    /// 지정한 자연 요소를 제외한 목록을 저장한 뒤 오브젝트를 삭제합니다.
    /// 저장 성공 후 현재 수조의 카운트 UI에 변경을 알립니다.
    /// </summary>
    public async Task<bool> DeletePlacedNatureAsync(
        NyangQuariumPlacedNature targetNature)
    {
        if (targetNature == null ||
            _nyangQuariumLayoutPanel == null)
        {
            return false;
        }

        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
            GetCurrentPlacedNatureData(targetNature);

        bool isSaved =
            await SavePlacedNatureSnapshotAsync(placedNatureData);

        if (!isSaved)
            return false;

        Destroy(targetNature.gameObject);

        NyangQuariumPlacedCountUI.NotifyCountChanged(
            _aquariumType);

        return true;
    }

    private List<NyangQuariumPlacedNatureData> GetCurrentPlacedNatureData(
        NyangQuariumPlacedNature excludedNature = null)
    {
        List<NyangQuariumPlacedNatureData> result = new();

        if (_nyangQuariumLayoutPanel == null)
            return result;

        NyangQuariumPlacedNature[] placedNatures =
            _nyangQuariumLayoutPanel.GetComponentsInChildren<NyangQuariumPlacedNature>(true);

        foreach (NyangQuariumPlacedNature placedNature in placedNatures)
        {
            if (placedNature == null ||
                placedNature == excludedNature ||
                placedNature.ItemId <= 0 ||
                string.IsNullOrWhiteSpace(placedNature.SpriteKey) ||
                placedNature.RectTransform == null)
            {
                continue;
            }

            RectTransform rect = placedNature.RectTransform;
            Vector3 localScale = rect.localScale;

            result.Add(new NyangQuariumPlacedNatureData(
                placedNature.ItemId,
                placedNature.SpriteKey,
                rect.anchoredPosition,
                new Vector2(localScale.x, localScale.y)));
        }

        return result;
    }

    private async Task<bool> SavePlacedNatureSnapshotAsync(
        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData)
    {
        _placedNatureMutationVersion++;

        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "Firestore가 준비되지 않아 자연 요소 저장을 생략합니다.",
                DebugType.UI,
                this);

            return false;
        }

        bool isSaved = await firestoreSO.SavePlacedNatureAsync(
            _aquariumType,
            placedNatureData);

        if (!isSaved)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                $"자연 요소 Firestore 저장 실패 - AquariumType:{_aquariumType}",
                DebugType.UI,
                this);

            return false;
        }

        DebugTool.Log("[NyangQuariumFishPlacementController] " +
            $"자연 요소 Firestore 저장 완료 - AquariumType:{_aquariumType}, " +
            $"Count:{placedNatureData?.Count ?? 0}",
            DebugType.UI,
            this);

        return true;
    }

    private async void LoadPlacedObjectsFromFirestore()
    {
        if ((_hasLoadedPlacedFish || _placedFishRenderer == null) &&
            _hasLoadedPlacedNature)
        {
            return;
        }

        int fishLoadVersion = _placedFishMutationVersion;
        int natureLoadVersion = _placedNatureMutationVersion;

        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
            return;

        bool loadedOrCreated = await firestoreSO.LoadOrCreateFromServerAsync();
        if (!loadedOrCreated)
            return;

        if (!_hasLoadedPlacedFish &&
            _placedFishRenderer != null &&
            fishLoadVersion == _placedFishMutationVersion)
        {
            IReadOnlyList<NyangquariumPlacedFishData> placedFishData =
                firestoreSO.GetPlacedFishData(_aquariumType, _fishSO);

            _placedFishRenderer.ShowPlacedFishes(placedFishData);
            _hasLoadedPlacedFish = true;
        }

        if (!_hasLoadedPlacedNature &&
            natureLoadVersion == _placedNatureMutationVersion)
        {
            IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
                firestoreSO.GetPlacedNatureData(_aquariumType);

            RestorePlacedNature(placedNatureData);
            _hasLoadedPlacedNature = true;
        }
    }

    private void RestorePlacedNature(
        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData)
    {
        if (_nyangQuariumLayoutPanel == null || placedNatureData == null)
            return;

        NyangQuariumPlacedNature[] existing =
            _nyangQuariumLayoutPanel.GetComponentsInChildren<NyangQuariumPlacedNature>(true);

        foreach (NyangQuariumPlacedNature nature in existing)
        {
            if (nature != null)
                Destroy(nature.gameObject);
        }

        foreach (NyangQuariumPlacedNatureData data in placedNatureData)
        {
            if (data == null ||
                data.ItemId <= 0 ||
                string.IsNullOrWhiteSpace(data.SpriteKey))
            {
                continue;
            }

            NyangquariumFishController visualController =
                NyangquariumFishController.SpawnMovingFish(
                    _nyangQuariumLayoutPanel,
                    data.SpriteKey,
                    data.AnchoredPosition,
                    1f);

            if (visualController == null || visualController.RectTransform == null)
                continue;

            visualController.SetMovementEnabled(false);
            visualController.SetSelectable(false);

            RectTransform rect = visualController.RectTransform;
            rect.name = $"PlacedNature_{data.ItemId}_{_nyangQuariumLayoutPanel.childCount}";
            rect.anchoredPosition = data.AnchoredPosition;
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(data.Scale.x, data.Scale.y, 1f);

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.preserveAspect = true;
            }

            NyangQuariumPlacedNature placedNature =
                rect.GetComponent<NyangQuariumPlacedNature>();

            if (placedNature == null)
                placedNature = rect.gameObject.AddComponent<NyangQuariumPlacedNature>();

            placedNature.Initialize(data.ItemId, data.SpriteKey, visualController);
        }

        DebugTool.Log("[NyangQuariumFishPlacementController] " +
            $"자연 요소 복원 완료 - AquariumType:{_aquariumType}, " +
            $"Count:{placedNatureData.Count}",
            DebugType.UI,
            this);
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

        DebugTool.Log("[NyangQuariumFishPlacementController] " +
            "배치 취소",
            DebugType.UI,
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

        DebugTool.Log("[NyangQuariumFishPlacementController] " +
            "현재 배치 상태 초기화",
            DebugType.UI,
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
            DebugTool.Error($"[NyangQuariumFishPlacementController] " +
                $"자연 요소 생성에 실패했습니다. " +
                $"SpriteKey:{_selectedSpriteKey}",
                DebugType.UI,
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

        _naturePreviewImage =
            _naturePreview.GetComponent<Image>();

        if (_naturePreviewImage != null)
        {
            _naturePreviewImage.raycastTarget = true;
            _naturePreviewImage.preserveAspect = true;
            _naturePreviewDefaultColor = _naturePreviewImage.color;
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
            _placementArea,
            HandleNaturePlacementValidityChanged);

        HandleNaturePlacementValidityChanged(
            _placementArea.IsFullyInsideNaturePlacementArea(
                _naturePreview));

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"자연 요소 미리보기 생성 - " +
            $"ID:{_selectedItemId}, " +
            $"SpriteKey:{_selectedSpriteKey}",
            DebugType.UI,
            this);
    }


    /// <summary>
    /// 자연 요소가 배치 가능 영역 안에 완전히 포함되는지에 따라
    /// 미리보기 색상과 확정 버튼 상태를 갱신합니다.
    /// </summary>
    private void HandleNaturePlacementValidityChanged(bool isValid)
    {
        _isNaturePlacementValid = isValid;

        if (_naturePreviewImage != null)
        {
            _naturePreviewImage.color = isValid
                ? _naturePreviewDefaultColor
                : _invalidNatureColor;
        }

        if (_confirmButton != null)
            _confirmButton.interactable = isValid;
    }

    /// <summary>
    /// 자연 요소의 현재 위치를 최종 배치 위치로 확정합니다.
    /// </summary>
    private RectTransform ConfirmNaturePlacement()
    {
        if (_naturePreview == null)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "확정할 자연 요소 미리보기가 없습니다.",
                DebugType.UI,
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
        _naturePreviewImage = null;
        _isNaturePlacementValid = false;

        DebugTool.Log($"[NyangQuariumFishPlacementController] " +
            $"자연 요소 배치 확정 - " +
            $"ItemId:{placedNature.ItemId}, " +
            $"SpriteKey:{placedNature.SpriteKey}",
            DebugType.UI,
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
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "NyangQuariumFishSO가 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return false;
        }

        IReadOnlyList<NyangQuariumFishData> dataList =
            _fishSO.FishData;

        if (dataList == null ||
            dataList.Count == 0)
        {
            DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                "NyangQuariumFishSO의 데이터가 비어 있습니다.",
                DebugType.UI,
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
                DebugTool.Warning($"[NyangQuariumFishPlacementController] " +
                    $"아이템 카테고리와 SO 데이터 타입이 일치하지 않습니다. " +
                    $"ItemId:{itemId}, " +
                    $"Category:{category}, " +
                    $"FishType:{itemData.FishType}",
                    DebugType.UI,
                    this);

                return false;
            }

            spriteKey = itemData.FishKey;

            if (string.IsNullOrWhiteSpace(
                    spriteKey))
            {
                DebugTool.Warning($"[NyangQuariumFishPlacementController] " +
                    $"아이템 데이터는 찾았지만 SpriteKey가 비어 있습니다. " +
                    $"ItemId:{itemId}",
                    DebugType.UI,
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
        _naturePreviewImage = null;
        _isNaturePlacementValid = false;
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
        SetNaturePlacementBorderActive(false);
    }

    /// <summary>
    /// 자연 요소 배치 가능구역의 네 방향 테두리를 함께 표시하거나 숨깁니다.
    /// </summary>
    private void SetNaturePlacementBorderActive(bool isActive)
    {
        if (_naturePlacementAreaBorder == null)
        {
            if (isActive)
            {
                DebugTool.Warning("[NyangQuariumFishPlacementController] " +
                    "자연 요소 배치 가능구역 테두리가 연결되지 않았습니다.",
                    DebugType.UI,
                    this);
            }

            return;
        }

        _naturePlacementAreaBorder.SetActive(isActive);
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

            _confirmButton.interactable =
                isActive && _isNaturePlacementValid;
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
            DebugTool.Error("[NyangQuariumFishPlacementController] " +
                "NyangQuariumLayoutPanel이 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (_placementArea == null)
        {
            DebugTool.Error("[NyangQuariumFishPlacementController] " +
                "PlacementArea가 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                _selectedSpriteKey))
        {
            DebugTool.Error("[NyangQuariumFishPlacementController] " +
                "자연 요소 SpriteKey가 비어 있습니다.",
                DebugType.UI,
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