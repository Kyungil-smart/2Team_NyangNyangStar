using System;
using System.Collections.Generic;
using UI.NyangQuarium.MergeBoard;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 머지보드에서 관상어와 자연 요소를 가져와
/// 냥쿠아리움 통합 인벤토리에 표시합니다.
/// </summary>
public sealed class NyangQuariumFishInventoryListUI : MonoBehaviour
{
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

    [Header("관상어 필터 UI")]
    [Tooltip("자연 요소 탭에서 숨길 관상어 필터 관련 오브젝트")]
    [SerializeField]
    private GameObject[] _fishFilterObjects;

    [Header("목록")]
    [SerializeField]
    private Transform _content;

    [SerializeField]
    private NyangQuariumFishInventoryItem _itemTemplate;

    [SerializeField]
    private NyangQuariumFishPlacementController _placementController;

    [Tooltip("아이템이 적어도 기본적으로 유지할 슬롯 개수")]
    [SerializeField, Min(1)]
    private int _minimumSlotCount = 8;

    [Header("관상어 필터")]
    [SerializeField]
    private NyangQuariumFishFilterToggle[] _filterToggles;

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

    private bool _isChangingToggleState;

    public NyangQuariumPlacementCategory CurrentCategory =>
        _currentCategory;

    private void Awake()
    {
        if (_itemTemplate != null)
            _itemTemplate.gameObject.SetActive(false);

        BindCategoryButtons();
        BindFilterToggles();
        InitializeFilterState();
        ApplyCategoryUI();
    }

    private void OnEnable()
    {
        NyangQuariumMergeBoardInventoryService.InventoryChanged +=
            Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        NyangQuariumMergeBoardInventoryService.InventoryChanged -=
            Refresh;
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
            return;
        }

        _currentCategory =
            NyangQuariumPlacementCategory.Fish;

        ApplyCategoryUI();
        Refresh();

        Debug.Log(
            "[NyangQuariumFishInventoryListUI] 관상어 탭 활성화",
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
            return;
        }

        _currentCategory =
            NyangQuariumPlacementCategory.Nature;

        ApplyCategoryUI();
        Refresh();

        Debug.Log(
            "[NyangQuariumFishInventoryListUI] 자연 요소 탭 활성화",
            this);
    }

    /// <summary>
    /// 현재 선택된 카테고리와 수조에 맞춰 목록을 다시 생성합니다.
    /// </summary>
    public void Refresh()
    {
        ClearCreatedItems();

        if (!NyangQuariumMergeBoardInventoryService.IsReady)
        {
            FillEmptySlots();

            Debug.LogWarning(
                "[NyangQuariumFishInventoryListUI] " +
                "머지보드가 등록되지 않아 빈 슬롯만 표시합니다.",
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
                FillEmptySlots();
                break;
        }
    }

    private void RefreshFishItems()
    {
        NyangQuariumMergeBoardInventoryService.CopyOwnedFishEntries(
            _ownedFishEntries,
            _aquariumType,
            int.MaxValue);

        GroupOwnedFishEntries();
        CreateFishItems();

        Debug.Log(
            $"[NyangQuariumFishInventoryListUI] " +
            $"관상어 목록 갱신 완료 - " +
            $"Raw:{_ownedFishEntries.Count}, " +
            $"Grouped:{_groupedFishEntries.Count}",
            this);
    }

    private void RefreshNatureItems()
    {
        NyangQuariumMergeBoardInventoryService.CopyOwnedNatureEntries(
            _ownedNatureEntries,
            int.MaxValue);

        GroupOwnedNatureEntries();
        CreateNatureItems();

        Debug.Log(
            $"[NyangQuariumFishInventoryListUI] " +
            $"자연 요소 목록 갱신 완료 - " +
            $"Raw:{_ownedNatureEntries.Count}, " +
            $"Grouped:{_groupedNatureEntries.Count}, " +
            $"Aquarium:{_aquariumType}",
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
            return;

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
                NyangQuariumPlacementCategory.Fish,
                _placementController);

            item.gameObject.name =
                $"FishInventoryItem_" +
                $"{grouped.Entry.FishId}_" +
                $"{grouped.Entry.FishName}";

            _createdItems.Add(item);
        }

        FillEmptySlots();
    }

    private void CreateNatureItems()
    {
        if (!ValidateListReferences())
            return;

        foreach (GroupedNatureEntry grouped
                 in _groupedNatureEntries)
        {
            NyangQuariumFishInventoryItem item =
                Instantiate(
                    _itemTemplate,
                    _content);

            item.gameObject.SetActive(true);

            item.Initialize(
                grouped.Entry.ItemId,
                grouped.Count,
                grouped.Entry.Sprite,
                NyangQuariumPlacementCategory.Nature,
                _placementController);

            item.gameObject.name =
                $"NatureInventoryItem_" +
                $"{grouped.Entry.ItemId}_" +
                $"{grouped.Entry.ItemName}";

            _createdItems.Add(item);
        }

        FillEmptySlots();
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

        Debug.LogWarning(
            "[NyangQuariumFishInventoryListUI] " +
            "Content 또는 ItemTemplate이 연결되지 않았습니다.",
            this);

        return false;
    }

    private void FillEmptySlots()
    {
        if (_content == null ||
            _itemTemplate == null)
        {
            return;
        }

        int emptySlotCount =
            Mathf.Max(
                0,
                _minimumSlotCount - _createdItems.Count);

        for (int i = 0;
             i < emptySlotCount;
             i++)
        {
            NyangQuariumFishInventoryItem emptyItem =
                Instantiate(
                    _itemTemplate,
                    _content);

            emptyItem.gameObject.SetActive(true);
            emptyItem.InitializeEmpty();

            emptyItem.gameObject.name =
                $"EmptyInventorySlot_{i + 1}";

            _createdItems.Add(emptyItem);
        }
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

        if (_fishFilterObjects == null)
            return;

        foreach (GameObject target
                 in _fishFilterObjects)
        {
            if (target != null)
                target.SetActive(isFishCategory);
        }
    }

    private void BindFilterToggles()
    {
        if (_filterToggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
        {
            if (toggle == null)
                continue;

            toggle.ValueChanged -=
                HandleFilterChanged;

            toggle.ValueChanged +=
                HandleFilterChanged;
        }
    }

    private void UnbindFilterToggles()
    {
        if (_filterToggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
        {
            if (toggle != null)
            {
                toggle.ValueChanged -=
                    HandleFilterChanged;
            }
        }
    }

    private void InitializeFilterState()
    {
        if (_filterToggles == null)
            return;

        _isChangingToggleState = true;

        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
        {
            if (toggle == null)
                continue;

            toggle.SetIsOnWithoutNotify(
                toggle.FilterType ==
                NyangQuariumFishFilterType.All);
        }

        _isChangingToggleState = false;
    }

    private void HandleFilterChanged(
        NyangQuariumFishFilterToggle changedToggle,
        bool isOn)
    {
        if (_isChangingToggleState ||
            _currentCategory !=
            NyangQuariumPlacementCategory.Fish)
        {
            return;
        }

        _isChangingToggleState = true;

        if (changedToggle.FilterType ==
            NyangQuariumFishFilterType.All)
        {
            if (isOn)
            {
                EnableOnlyAll();
            }
            else if (!HasEnabledFilter())
            {
                changedToggle.SetIsOnWithoutNotify(true);
            }
        }
        else
        {
            if (isOn)
            {
                SetAllFilter(false);
            }
            else if (!HasEnabledFilter())
            {
                SetAllFilter(true);
            }
        }

        _isChangingToggleState = false;
        Refresh();
    }

    private void EnableOnlyAll()
    {
        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
        {
            if (toggle == null)
                continue;

            toggle.SetIsOnWithoutNotify(
                toggle.FilterType ==
                NyangQuariumFishFilterType.All);
        }
    }

    private void SetAllFilter(bool isOn)
    {
        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
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

    private bool HasEnabledFilter()
    {
        foreach (NyangQuariumFishFilterToggle toggle
                 in _filterToggles)
        {
            if (toggle != null &&
                toggle.IsOn)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearCreatedItems()
    {
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