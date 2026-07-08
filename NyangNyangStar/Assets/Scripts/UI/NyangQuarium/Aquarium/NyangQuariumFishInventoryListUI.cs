using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.NyangQuarium.MergeBoard;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 머지보드에서 관상어와 자연 요소를 가져와
/// 냥쿠아리움 통합 인벤토리에 표시합니다.
/// </summary>
public sealed class NyangQuariumFishInventoryListUI : MonoBehaviour
{
    [SerializeField]
    private string EmptyFishMessage =
        "배치할 수 있는 관상어가 없습니다.";

    [SerializeField]
    private string EmptyNatureMessage =
        "배치할 수 있는 자연요소가 없습니다.";

    [Header("현재 수조")]
    [Tooltip("담수 수조는 Freshwater, 해수 수조는 Saltwater")]
    [SerializeField]
    private FishType _aquariumType =
        FishType.Freshwater;

    [Header("카테고리 버튼")]
    [Tooltip("관상어 목록을 표시하는 버튼")]
    [SerializeField]
    private Button _fishTabButton;

    [Tooltip("자연 요소 목록을 표시하는 버튼")]
    [SerializeField]
    private Button _natureTabButton;

    [Header("카테고리 탭 색상")]
    [Tooltip("현재 선택된 탭 버튼 색상")]
    [SerializeField]
    private Color _selectedTabColor =
        new Color32(120, 120, 120, 255);

    [Tooltip("선택되지 않은 탭 버튼 색상")]
    [SerializeField]
    private Color _normalTabColor = Color.white;

    [Header("카테고리별 필터 UI")]
    [Tooltip("관상어 탭에서 표시할 필터 관련 오브젝트")]
    [SerializeField]
    private GameObject[] _fishFilterObjects;

    [Tooltip("자연 요소 탭에서 표시할 필터 관련 오브젝트")]
    [SerializeField]
    private GameObject[] _natureFilterObjects;

    [Header("목록")]
    [SerializeField]
    private Transform _content;

    [SerializeField]
    private NyangQuariumFishInventoryItem _itemTemplate;

    [SerializeField]
    private NyangQuariumFishPlacementController _placementController;

    [Header("빈 목록 안내")]
    [Tooltip("현재 카테고리에 보유 아이템이 없을 때 표시할 안내 Text")]
    [SerializeField]
    private TMP_Text _emptyMessageText;

    [Header("관상어 필터")]
    [SerializeField]
    private NyangQuariumFishFilterToggle[] _filterToggles;

    [Header("자연 요소 필터")]
    [SerializeField]
    private NyangQuariumFishFilterToggle[] _natureFilterToggles;

    private readonly List<NyangQuariumMergeBoardFishEntry>
        _ownedFishEntries = new();

    private readonly List<NyangQuariumMergeBoardNatureEntry>
        _ownedNatureEntries = new();

    private readonly List<GroupedFishEntry>
        _groupedFishEntries = new();

    private readonly List<GroupedNatureEntry>
        _groupedNatureEntries = new();

    private readonly List<NyangQuariumFishInventoryItem>
        _createdItems = new();

    private NyangQuariumPlacementCategory _currentCategory =
        NyangQuariumPlacementCategory.Fish;

    private NyangQuariumFishInventoryItem _selectedItem;
    private bool _isChangingToggleState;

    private bool _isRefreshingFromFirestore;
    private int _refreshRequestId;

    public NyangQuariumPlacementCategory CurrentCategory =>
        _currentCategory;

    private void Awake()
    {
        if (_itemTemplate != null)
            _itemTemplate.gameObject.SetActive(false);

        SetEmptyMessageActive(false);

        BindCategoryButtons();
        BindFilterToggles();
        InitializeFilterState();
        ApplyCategoryUI();
    }

    private void OnEnable()
    {
        NyangQuariumMergeBoardInventoryService.InventoryChanged +=
            HandleMergeBoardInventoryChanged;

        // 패널 재오픈 시 현재 목록 카테고리와 탭 표시를 다시 동기화합니다.
        ApplyCategoryUI();

        _ = RefreshFromFirestoreAndRebuildAsync();
    }

    private void OnDisable()
    {
        _refreshRequestId++;

        NyangQuariumMergeBoardInventoryService.InventoryChanged -=
            HandleMergeBoardInventoryChanged;

        ClearInventorySelection();
    }

    private void OnDestroy()
    {
        UnbindCategoryButtons();
        UnbindFilterToggles();
    }

    /// <summary>
    /// 관상어 탭을 활성화합니다.
    /// </summary>
    public void ShowFishItems()
    {
        if (_currentCategory ==
            NyangQuariumPlacementCategory.Fish)
        {
            RefreshCategoryTabVisual();
            return;
        }

        _currentCategory =
            NyangQuariumPlacementCategory.Fish;

        ApplyCategoryUI();
        Refresh();

        DebugTool.Log(
            "[NyangQuariumFishInventoryListUI] 관상어 탭 활성화",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 자연 요소 탭을 활성화합니다.
    /// </summary>
    public void ShowNatureItems()
    {
        if (_currentCategory ==
            NyangQuariumPlacementCategory.Nature)
        {
            RefreshCategoryTabVisual();
            return;
        }

        _currentCategory =
            NyangQuariumPlacementCategory.Nature;

        ApplyCategoryUI();
        Refresh();

        DebugTool.Log(
            "[NyangQuariumFishInventoryListUI] 자연 요소 탭 활성화",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 현재 선택된 카테고리와 수조에 맞춰 목록을 다시 생성합니다.
    /// 실제 보유 아이템만 표시하며 빈 슬롯은 생성하지 않습니다.
    /// </summary>
    public void Refresh()
    {
        ClearCreatedItems();
        SetEmptyMessageActive(false);

        if (!NyangQuariumMergeBoardInventoryService.IsReady)
        {
            ShowCurrentCategoryEmptyMessage();

            DebugTool.Warning(
                "[NyangQuariumFishInventoryListUI] " +
                "머지보드가 준비되지 않아 아이템 목록을 표시할 수 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        switch (_currentCategory)
        {
            case NyangQuariumPlacementCategory.Fish:
                RefreshFishItems();
                break;

            case NyangQuariumPlacementCategory.Nature:
                RefreshNatureItems();
                break;

            default:
                ShowCurrentCategoryEmptyMessage();
                break;
        }
    }
    /// <summary>
    /// 배치 패널이 열릴 때 Firestore의 냥쿠아리움 머지보드 데이터를 다시 읽고,
    /// 로드 완료 후 인벤토리 버튼과 Sprite를 다시 생성합니다.
    /// </summary>
    public async Task RefreshFromFirestoreAndRebuildAsync()
    {
        int requestId = ++_refreshRequestId;

        _isRefreshingFromFirestore = true;

        ClearCreatedItems();
        SetEmptyMessageActive(false);

        try
        {
            bool isRefreshed =
                await NyangQuariumMergeBoardInventoryService
                    .RefreshFromFirestoreAsync();

            if (requestId != _refreshRequestId ||
                !isActiveAndEnabled)
            {
                return;
            }

            if (!isRefreshed)
            {
                DebugTool.Warning(
                    "[NyangQuariumFishInventoryListUI] " +
                    "Firestore 최신화에 실패했습니다. 현재 캐시 또는 머지보드 상태로 목록을 갱신합니다.",
                    DebugType.UI,
                    this);
            }

            Refresh();

            DebugTool.Log(
                "[NyangQuariumFishInventoryListUI] " +
                "배치 패널 진입 시 머지보드 최신 데이터로 인벤토리를 갱신했습니다.",
                DebugType.UI,
                this);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[NyangQuariumFishInventoryListUI] 인벤토리 Firestore 갱신 중 오류: {exception.Message}");

            if (requestId == _refreshRequestId &&
                isActiveAndEnabled)
            {
                Refresh();
            }
        }
        finally
        {
            if (requestId == _refreshRequestId)
                _isRefreshingFromFirestore = false;
        }
    }

    private void HandleMergeBoardInventoryChanged()
    {
        if (_isRefreshingFromFirestore)
            return;

        // 머지보드에서 아이템을 생성/삭제하면 런타임 보드 데이터가 아니라
        // Firestore의 NyangQuariumMergeBoard 슬롯을 다시 읽어서 버튼/Sprite를 재생성합니다.
        _ = RefreshFromFirestoreAndRebuildAsync();
    }

    private void RefreshFishItems()
    {
        NyangQuariumMergeBoardInventoryService.CopyOwnedFishEntries(
            _ownedFishEntries,
            _aquariumType,
            int.MaxValue);

        GroupOwnedFishEntries();
        CreateFishItems();

        DebugTool.Log(
            "[NyangQuariumFishInventoryListUI] " +
            $"관상어 목록 갱신 완료 - " +
            $"Raw:{_ownedFishEntries.Count}, " +
            $"Grouped:{_groupedFishEntries.Count}, " +
            $"Visible:{_createdItems.Count}",
            DebugType.UI,
            this);
    }

    private void RefreshNatureItems()
    {
        NyangQuariumMergeBoardInventoryService.CopyOwnedNatureEntries(
            _ownedNatureEntries,
            int.MaxValue);

        GroupOwnedNatureEntries();
        CreateNatureItems();

        DebugTool.Log(
            "[NyangQuariumFishInventoryListUI] " +
            $"자연 요소 목록 갱신 완료 - " +
            $"Raw:{_ownedNatureEntries.Count}, " +
            $"Grouped:{_groupedNatureEntries.Count}, " +
            $"Visible:{_createdItems.Count}, " +
            $"Aquarium:{_aquariumType}",
            DebugType.UI,
            this);
    }

    private void GroupOwnedFishEntries()
    {
        _groupedFishEntries.Clear();

        Dictionary<int, GroupedFishEntry> groupedById =
            new();

        foreach (NyangQuariumMergeBoardFishEntry entry
                 in _ownedFishEntries)
        {
            if (groupedById.TryGetValue(
                    entry.FishId,
                    out GroupedFishEntry grouped))
            {
                grouped.Count++;
                groupedById[entry.FishId] = grouped;
                continue;
            }

            groupedById.Add(
                entry.FishId,
                new GroupedFishEntry(entry, 1));
        }

        foreach (GroupedFishEntry grouped
                 in groupedById.Values)
        {
            _groupedFishEntries.Add(grouped);
        }

        _groupedFishEntries.Sort(
            (left, right) =>
                left.Entry.FishId.CompareTo(
                    right.Entry.FishId));
    }

    private void GroupOwnedNatureEntries()
    {
        _groupedNatureEntries.Clear();

        Dictionary<int, GroupedNatureEntry> groupedById =
            new();

        foreach (NyangQuariumMergeBoardNatureEntry entry
                 in _ownedNatureEntries)
        {
            if (!CanShowNatureInCurrentAquarium(
                    entry.ItemKey))
            {
                continue;
            }

            if (groupedById.TryGetValue(
                    entry.ItemId,
                    out GroupedNatureEntry grouped))
            {
                grouped.Count++;
                groupedById[entry.ItemId] = grouped;
                continue;
            }

            groupedById.Add(
                entry.ItemId,
                new GroupedNatureEntry(entry, 1));
        }

        foreach (GroupedNatureEntry grouped
                 in groupedById.Values)
        {
            _groupedNatureEntries.Add(grouped);
        }

        _groupedNatureEntries.Sort(
            (left, right) =>
                left.Entry.ItemId.CompareTo(
                    right.Entry.ItemId));
    }

    private void CreateFishItems()
    {
        if (!ValidateListReferences())
        {
            ShowCurrentCategoryEmptyMessage();
            return;
        }

        foreach (GroupedFishEntry grouped
                 in _groupedFishEntries)
        {
            if (!MatchesCheckedFishFilters(
                    grouped.Entry.FishType))
            {
                continue;
            }

            NyangQuariumFishInventoryItem item =
                Instantiate(
                    _itemTemplate,
                    _content);

            item.gameObject.SetActive(true);

            item.Initialize(
                grouped.Entry.FishId,
                grouped.Count,
                grouped.Entry.Sprite,
                grouped.Entry.FishName,
                GetFishTypeColor(
                    grouped.Entry.FishType),
                NyangQuariumPlacementCategory.Fish,
                _placementController,
                HandleItemSelected);

            item.gameObject.name =
                $"FishInventoryItem_" +
                $"{grouped.Entry.FishId}_" +
                $"{grouped.Entry.FishName}";

            _createdItems.Add(item);
        }

        UpdateEmptyMessage();
    }

    private void CreateNatureItems()
    {
        if (!ValidateListReferences())
        {
            ShowCurrentCategoryEmptyMessage();
            return;
        }

        foreach (GroupedNatureEntry grouped
                 in _groupedNatureEntries)
        {
            if (!MatchesCheckedNatureFilters(
                    grouped.Entry.ItemKey))
            {
                continue;
            }

            NyangQuariumFishInventoryItem item =
                Instantiate(
                    _itemTemplate,
                    _content);

            item.gameObject.SetActive(true);

            item.Initialize(
                grouped.Entry.ItemId,
                grouped.Count,
                grouped.Entry.Sprite,
                grouped.Entry.ItemName,
                GetNatureTypeColor(
                    grouped.Entry.ItemKey),
                NyangQuariumPlacementCategory.Nature,
                _placementController,
                HandleItemSelected);

            item.gameObject.name =
                $"NatureInventoryItem_" +
                $"{grouped.Entry.ItemId}_" +
                $"{grouped.Entry.ItemName}";

            _createdItems.Add(item);
        }

        UpdateEmptyMessage();
    }

    /// <summary>
    /// 선택된 슬롯 하나만 강조합니다.
    /// </summary>
    private void HandleItemSelected(
        NyangQuariumFishInventoryItem selectedItem)
    {
        if (selectedItem == null)
            return;

        if (_selectedItem != null &&
            _selectedItem != selectedItem)
        {
            _selectedItem.SetSelected(false);
        }

        _selectedItem = selectedItem;
        _selectedItem.SetSelected(true);
    }

    private void UpdateEmptyMessage()
    {
        if (_createdItems.Count > 0)
        {
            SetEmptyMessageActive(false);
            return;
        }

        ShowCurrentCategoryEmptyMessage();
    }

    private void ShowCurrentCategoryEmptyMessage()
    {
        if (_emptyMessageText == null)
            return;

        _emptyMessageText.text =
            _currentCategory ==
            NyangQuariumPlacementCategory.Fish
                ? EmptyFishMessage
                : EmptyNatureMessage;

        SetEmptyMessageActive(true);
    }

    private void SetEmptyMessageActive(
        bool isActive)
    {
        if (_emptyMessageText != null)
            _emptyMessageText.gameObject.SetActive(isActive);
    }

    /// <summary>
    /// 자연 요소 Key를 기준으로 현재 수조에 표시 가능한지 검사합니다.
    /// </summary>
    private bool CanShowNatureInCurrentAquarium(
        string itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
            return false;

        bool isShelter =
            itemKey.StartsWith(
                "Env_Shelter_",
                StringComparison.Ordinal);

        if (isShelter)
            return true;

        if (_aquariumType == FishType.Freshwater)
        {
            return itemKey.StartsWith(
                       "Env_Stone_",
                       StringComparison.Ordinal) ||
                   itemKey.StartsWith(
                       "Env_Plant_",
                       StringComparison.Ordinal);
        }

        if (_aquariumType == FishType.Saltwater)
        {
            return itemKey.StartsWith(
                       "Env_Marine_",
                       StringComparison.Ordinal) ||
                   itemKey.StartsWith(
                       "Env_Coral_",
                       StringComparison.Ordinal);
        }

        return false;
    }

    private bool ValidateListReferences()
    {
        if (_content != null &&
            _itemTemplate != null)
        {
            return true;
        }

        DebugTool.Warning(
            "[NyangQuariumFishInventoryListUI] " +
            "Content 또는 ItemTemplate이 연결되지 않았습니다.",
            DebugType.UI,
            this);

        return false;
    }

    private bool MatchesCheckedFishFilters(
        FishType fishType)
    {
        if (_filterToggles == null ||
            _filterToggles.Length == 0)
        {
            return true;
        }

        NyangQuariumFishFilterType targetFilter =
            ToFilterType(fishType);

        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
        {
            if (toggle == null ||
                !toggle.IsOn)
            {
                continue;
            }

            if (toggle.FilterType ==
                    NyangQuariumFishFilterType.All ||
                toggle.FilterType == targetFilter)
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesCheckedNatureFilters(
        string itemKey)
    {
        if (_natureFilterToggles == null ||
            _natureFilterToggles.Length == 0)
        {
            return true;
        }

        NyangQuariumFishFilterType targetFilter =
            ToNatureFilterType(itemKey);

        foreach (NyangQuariumFishFilterToggle toggle
                 in _natureFilterToggles)
        {
            if (toggle == null || !toggle.IsOn)
                continue;

            if (toggle.FilterType ==
                    NyangQuariumFishFilterType.All ||
                toggle.FilterType == targetFilter)
            {
                return true;
            }
        }

        return false;
    }

    private static NyangQuariumFishFilterType ToNatureFilterType(
        string itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
            return NyangQuariumFishFilterType.All;

        if (itemKey.StartsWith(
                "Env_Stone_",
                StringComparison.Ordinal))
        {
            return NyangQuariumFishFilterType.Stone;
        }

        if (itemKey.StartsWith(
                "Env_Plant_",
                StringComparison.Ordinal))
        {
            return NyangQuariumFishFilterType.Plant;
        }

        if (itemKey.StartsWith(
                "Env_Marine_",
                StringComparison.Ordinal))
        {
            return NyangQuariumFishFilterType.Marine;
        }

        if (itemKey.StartsWith(
                "Env_Coral_",
                StringComparison.Ordinal))
        {
            return NyangQuariumFishFilterType.Coral;
        }

        if (itemKey.StartsWith(
                "Env_Shelter_",
                StringComparison.Ordinal))
        {
            return NyangQuariumFishFilterType.Shelter;
        }

        return NyangQuariumFishFilterType.All;
    }

    private static NyangQuariumFishFilterType ToFilterType(
        FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Freshwater:
                return NyangQuariumFishFilterType.Freshwater;

            case FishType.Saltwater:
                return NyangQuariumFishFilterType.Saltwater;

            case FishType.BrackishWater:
                return NyangQuariumFishFilterType.BrackishWater;

            default:
                return NyangQuariumFishFilterType.All;
        }
    }

    private static Color GetFishTypeColor(
        FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Freshwater:
                return ParseHtmlColor("#B5F6E4");

            case FishType.BrackishWater:
                return ParseHtmlColor("#9AFFF5");

            case FishType.Saltwater:
                return ParseHtmlColor("#98D2F3");

            default:
                return Color.white;
        }
    }

    private static Color GetNatureTypeColor(
        string itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
            return Color.white;

        if (itemKey.StartsWith(
                "Env_Stone_",
                StringComparison.Ordinal))
        {
            return ParseHtmlColor("#CFD8DC");
        }

        if (itemKey.StartsWith(
                "Env_Plant_",
                StringComparison.Ordinal))
        {
            return ParseHtmlColor("#C5E1A5");
        }

        if (itemKey.StartsWith(
                "Env_Marine_",
                StringComparison.Ordinal))
        {
            return ParseHtmlColor("#64B5F6");
        }

        if (itemKey.StartsWith(
                "Env_Coral_",
                StringComparison.Ordinal))
        {
            return ParseHtmlColor("#FFB7B2");
        }

        if (itemKey.StartsWith(
                "Env_Shelter_",
                StringComparison.Ordinal))
        {
            return ParseHtmlColor("#DCAE96");
        }

        return Color.white;
    }

    private static Color ParseHtmlColor(
        string htmlColor)
    {
        return ColorUtility.TryParseHtmlString(
            htmlColor,
            out Color color)
                ? color
                : Color.white;
    }

    private void BindCategoryButtons()
    {
        if (_fishTabButton != null)
        {
            _fishTabButton.onClick.RemoveListener(
                ShowFishItems);

            _fishTabButton.onClick.AddListener(
                ShowFishItems);
        }

        if (_natureTabButton != null)
        {
            _natureTabButton.onClick.RemoveListener(
                ShowNatureItems);

            _natureTabButton.onClick.AddListener(
                ShowNatureItems);
        }
    }

    private void UnbindCategoryButtons()
    {
        if (_fishTabButton != null)
        {
            _fishTabButton.onClick.RemoveListener(
                ShowFishItems);
        }

        if (_natureTabButton != null)
        {
            _natureTabButton.onClick.RemoveListener(
                ShowNatureItems);
        }
    }

    private void ApplyCategoryUI()
    {
        bool isFishCategory =
            _currentCategory ==
            NyangQuariumPlacementCategory.Fish;

        SetFilterObjectsActive(
            _fishFilterObjects,
            isFishCategory);

        SetFilterObjectsActive(
            _natureFilterObjects,
            !isFishCategory);

        RefreshCategoryTabVisual();
    }

    /// <summary>
    /// EventSystem의 현재 포커스와 무관하게 현재 카테고리 탭 색상을 유지합니다.
    /// </summary>
    private void RefreshCategoryTabVisual()
    {
        bool isFishCategory =
            _currentCategory ==
            NyangQuariumPlacementCategory.Fish;

        ApplyTabVisual(
            _fishTabButton,
            isFishCategory);

        ApplyTabVisual(
            _natureTabButton,
            !isFishCategory);
    }

    private void ApplyTabVisual(
        Button button,
        bool isSelected)
    {
        if (button == null)
            return;

        // Button의 Selected 상태는 외부 클릭 시 해제되므로
        // 전환 효과와 분리하여 현재 카테고리 색상을 직접 유지합니다.
        button.transition = Selectable.Transition.None;

        Graphic targetGraphic = button.targetGraphic;

        if (targetGraphic == null)
            return;

        targetGraphic.color = isSelected
            ? _selectedTabColor
            : _normalTabColor;
    }

    private static void SetFilterObjectsActive(
        GameObject[] targets,
        bool isActive)
    {
        if (targets == null)
            return;

        foreach (GameObject target in targets)
        {
            if (target != null)
                target.SetActive(isActive);
        }
    }

    private void BindFilterToggles()
    {
        BindFilterToggleArray(_filterToggles);
        BindFilterToggleArray(_natureFilterToggles);
    }

    private void BindFilterToggleArray(
        NyangQuariumFishFilterToggle[] toggles)
    {
        if (toggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle == null)
                continue;

            toggle.ValueChanged -= HandleFilterChanged;
            toggle.ValueChanged += HandleFilterChanged;
        }
    }

    private void UnbindFilterToggles()
    {
        UnbindFilterToggleArray(_filterToggles);
        UnbindFilterToggleArray(_natureFilterToggles);
    }

    private void UnbindFilterToggleArray(
        NyangQuariumFishFilterToggle[] toggles)
    {
        if (toggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle != null)
                toggle.ValueChanged -= HandleFilterChanged;
        }
    }

    private void InitializeFilterState()
    {
        _isChangingToggleState = true;

        EnableOnlyAll(_filterToggles);
        EnableOnlyAll(_natureFilterToggles);

        _isChangingToggleState = false;
    }

    private void HandleFilterChanged(
        NyangQuariumFishFilterToggle changedToggle,
        bool isOn)
    {
        if (_isChangingToggleState || changedToggle == null)
            return;

        NyangQuariumFishFilterToggle[] activeToggles =
            GetCurrentCategoryFilterToggles();

        if (!ContainsToggle(activeToggles, changedToggle))
            return;

        _isChangingToggleState = true;

        if (changedToggle.FilterType ==
            NyangQuariumFishFilterType.All)
        {
            if (isOn)
            {
                EnableOnlyAll(activeToggles);
            }
            else if (!HasEnabledFilter(activeToggles))
            {
                changedToggle.SetIsOnWithoutNotify(true);
            }
        }
        else
        {
            if (isOn)
            {
                SetAllFilter(activeToggles, false);

                if (AreAllIndividualFiltersEnabled(activeToggles))
                {
                    EnableOnlyAll(activeToggles);

                    DebugTool.Log(
                        "[NyangQuariumFishInventoryListUI] " +
                        "모든 개별 필터가 선택되어 전체 필터로 전환했습니다.",
                        DebugType.UI,
                        this);
                }
            }
            else if (!HasEnabledFilter(activeToggles))
            {
                SetAllFilter(activeToggles, true);
            }
        }

        _isChangingToggleState = false;
        Refresh();
    }

    private NyangQuariumFishFilterToggle[]
        GetCurrentCategoryFilterToggles()
    {
        return _currentCategory ==
               NyangQuariumPlacementCategory.Fish
            ? _filterToggles
            : _natureFilterToggles;
    }

    private static bool ContainsToggle(
        NyangQuariumFishFilterToggle[] toggles,
        NyangQuariumFishFilterToggle target)
    {
        if (toggles == null || target == null)
            return false;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle == target)
                return true;
        }

        return false;
    }

    private bool AreAllIndividualFiltersEnabled(
        NyangQuariumFishFilterToggle[] toggles)
    {
        if (toggles == null)
            return false;

        bool hasApplicableFilter = false;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle == null ||
                !IsFilterApplicableToCurrentCategory(
                    toggle.FilterType))
            {
                continue;
            }

            hasApplicableFilter = true;

            if (!toggle.IsOn)
                return false;
        }

        return hasApplicableFilter;
    }

    private bool IsFilterApplicableToCurrentCategory(
        NyangQuariumFishFilterType filterType)
    {
        if (filterType == NyangQuariumFishFilterType.All)
            return false;

        if (_currentCategory == NyangQuariumPlacementCategory.Fish)
            return IsFishFilterForCurrentAquarium(filterType);

        return IsNatureFilterForCurrentAquarium(filterType);
    }

    private bool IsFishFilterForCurrentAquarium(
        NyangQuariumFishFilterType filterType)
    {
        if (filterType ==
            NyangQuariumFishFilterType.BrackishWater)
        {
            return true;
        }

        return _aquariumType == FishType.Freshwater
            ? filterType == NyangQuariumFishFilterType.Freshwater
            : filterType == NyangQuariumFishFilterType.Saltwater;
    }

    private bool IsNatureFilterForCurrentAquarium(
        NyangQuariumFishFilterType filterType)
    {
        if (filterType == NyangQuariumFishFilterType.Shelter)
            return true;

        if (_aquariumType == FishType.Freshwater)
        {
            return filterType == NyangQuariumFishFilterType.Stone ||
                   filterType == NyangQuariumFishFilterType.Plant;
        }

        if (_aquariumType == FishType.Saltwater)
        {
            return filterType == NyangQuariumFishFilterType.Marine ||
                   filterType == NyangQuariumFishFilterType.Coral;
        }

        return false;
    }

    private static void EnableOnlyAll(
        NyangQuariumFishFilterToggle[] toggles)
    {
        if (toggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle == null)
                continue;

            toggle.SetIsOnWithoutNotify(
                toggle.FilterType ==
                NyangQuariumFishFilterType.All);
        }
    }

    private static void SetAllFilter(
        NyangQuariumFishFilterToggle[] toggles,
        bool isOn)
    {
        if (toggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle == null ||
                toggle.FilterType !=
                NyangQuariumFishFilterType.All)
            {
                continue;
            }

            toggle.SetIsOnWithoutNotify(isOn);
            return;
        }
    }

    private static bool HasEnabledFilter(
        NyangQuariumFishFilterToggle[] toggles)
    {
        if (toggles == null)
            return false;

        foreach (NyangQuariumFishFilterToggle toggle in toggles)
        {
            if (toggle != null && toggle.IsOn)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 슬롯 강조 상태와 배치 컨트롤러의 선택 데이터를 함께 초기화합니다.
    /// </summary>
    private void ClearInventorySelection()
    {
        if (_selectedItem != null)
            _selectedItem.SetSelected(false);

        _selectedItem = null;
        _placementController?.ClearSelectedItem();
    }

    private void ClearCreatedItems()
    {
        ClearInventorySelection();

        foreach (NyangQuariumFishInventoryItem item
                 in _createdItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        _createdItems.Clear();
    }

    [Serializable]
    private struct GroupedFishEntry
    {
        public NyangQuariumMergeBoardFishEntry Entry;
        public int Count;

        public GroupedFishEntry(
            NyangQuariumMergeBoardFishEntry entry,
            int count)
        {
            Entry = entry;
            Count = count;
        }
    }

    [Serializable]
    private struct GroupedNatureEntry
    {
        public NyangQuariumMergeBoardNatureEntry Entry;
        public int Count;

        public GroupedNatureEntry(
            NyangQuariumMergeBoardNatureEntry entry,
            int count)
        {
            Entry = entry;
            Count = count;
        }
    }
}