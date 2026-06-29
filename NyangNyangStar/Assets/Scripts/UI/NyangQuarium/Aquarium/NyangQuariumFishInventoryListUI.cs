using System;
using System.Collections.Generic;
using UI.NyangQuarium.MergeBoard;
using UnityEngine;

/// <summary>
/// 기존 머지보드 공개 API만 사용하여 보유 관상어를 배치 패널에 표시합니다.
/// MergeBoardInventoryService와 MergeBoardRuntime은 수정하지 않습니다.
/// </summary>
public sealed class NyangQuariumFishInventoryListUI : MonoBehaviour
{
    [Header("현재 수조")]
    [Tooltip("담수 수조는 Freshwater, 해수 수조는 Saltwater")]
    [SerializeField] private FishType _aquariumType = FishType.Freshwater;

    [Header("목록")]
    [SerializeField] private Transform _content;
    [SerializeField] private NyangQuariumFishInventoryItem _itemTemplate;
    [SerializeField] private NyangQuariumFishPlacementController _placementController;

    [Tooltip("보유 물고기가 적어도 기본적으로 유지할 슬롯 개수")]
    [SerializeField, Min(1)] private int _minimumSlotCount = 8;

    [Header("필터")]
    [SerializeField] private NyangQuariumFishFilterToggle[] _filterToggles;

    private readonly List<NyangQuariumMergeBoardFishEntry> _ownedEntries = new();
    private readonly List<GroupedFishEntry> _groupedEntries = new();
    private readonly List<NyangQuariumFishInventoryItem> _createdItems = new();

    private bool _isChangingToggleState;

    private void Awake()
    {
        if (_itemTemplate != null)
            _itemTemplate.gameObject.SetActive(false);

        BindFilterToggles();
        InitializeFilterState();
    }

    private void OnEnable()
    {
        NyangQuariumMergeBoardInventoryService.InventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        NyangQuariumMergeBoardInventoryService.InventoryChanged -= Refresh;
    }

    private void OnDestroy()
    {
        UnbindFilterToggles();
    }

    public void Refresh()
    {
        ClearCreatedItems();

        if (!NyangQuariumMergeBoardInventoryService.IsReady)
        {
            FillEmptySlots();

            Debug.LogWarning(
                "[NyangQuariumFishInventoryListUI] 머지보드가 등록되지 않아 빈 슬롯만 표시합니다.",
                this);
            return;
        }

        NyangQuariumMergeBoardInventoryService.CopyOwnedFishEntries(
            _ownedEntries,
            _aquariumType,
            int.MaxValue);

        GroupOwnedEntries();
        CreateFilteredItems();

        Debug.Log(
            $"[NyangQuariumFishInventoryListUI] 목록 갱신 완료 - Raw:{_ownedEntries.Count}, Grouped:{_groupedEntries.Count}",
            this);
    }

    private void GroupOwnedEntries()
    {
        _groupedEntries.Clear();

        Dictionary<int, GroupedFishEntry> groupedById = new();

        foreach (NyangQuariumMergeBoardFishEntry entry in _ownedEntries)
        {
            if (groupedById.TryGetValue(entry.FishId, out GroupedFishEntry grouped))
            {
                grouped.Count++;
                groupedById[entry.FishId] = grouped;
                continue;
            }

            groupedById.Add(entry.FishId, new GroupedFishEntry(entry, 1));
        }

        foreach (GroupedFishEntry grouped in groupedById.Values)
            _groupedEntries.Add(grouped);

        _groupedEntries.Sort(
            (left, right) => left.Entry.FishId.CompareTo(right.Entry.FishId));
    }

    private void CreateFilteredItems()
    {
        if (_content == null || _itemTemplate == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishInventoryListUI] Content 또는 ItemTemplate이 연결되지 않았습니다.",
                this);
            return;
        }

        foreach (GroupedFishEntry grouped in _groupedEntries)
        {
            if (!MatchesCheckedFilters(grouped.Entry.FishType))
                continue;

            NyangQuariumFishInventoryItem item =
                Instantiate(_itemTemplate, _content);

            item.gameObject.SetActive(true);
            item.Initialize(
                grouped.Entry.FishId,
                grouped.Count,
                grouped.Entry.Sprite,
                _placementController);

            item.gameObject.name =
                $"FishInventoryItem_{grouped.Entry.FishId}_{grouped.Entry.FishName}";

            _createdItems.Add(item);
        }

        FillEmptySlots();
    }

    /// <summary>
    /// 현재 표시 중인 아이템이 최소 슬롯 수보다 적으면 빈 슬롯을 채웁니다.
    /// </summary>
    private void FillEmptySlots()
    {
        if (_content == null || _itemTemplate == null)
            return;

        int emptySlotCount =
            Mathf.Max(0, _minimumSlotCount - _createdItems.Count);

        for (int i = 0; i < emptySlotCount; i++)
        {
            NyangQuariumFishInventoryItem emptyItem =
                Instantiate(_itemTemplate, _content);

            emptyItem.gameObject.SetActive(true);
            emptyItem.InitializeEmpty();
            emptyItem.gameObject.name = $"EmptyFishSlot_{i + 1}";

            _createdItems.Add(emptyItem);
        }
    }

    private bool MatchesCheckedFilters(FishType fishType)
    {
        if (_filterToggles == null || _filterToggles.Length == 0)
            return true;

        NyangQuariumFishFilterType targetFilter = ToFilterType(fishType);

        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle == null || !toggle.IsOn)
                continue;

            if (toggle.FilterType == NyangQuariumFishFilterType.All ||
                toggle.FilterType == targetFilter)
            {
                return true;
            }
        }

        return false;
    }

    private static NyangQuariumFishFilterType ToFilterType(FishType fishType)
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

    private void BindFilterToggles()
    {
        if (_filterToggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle == null)
                continue;

            toggle.ValueChanged -= HandleFilterChanged;
            toggle.ValueChanged += HandleFilterChanged;
        }
    }

    private void UnbindFilterToggles()
    {
        if (_filterToggles == null)
            return;

        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle != null)
                toggle.ValueChanged -= HandleFilterChanged;
        }
    }

    private void InitializeFilterState()
    {
        if (_filterToggles == null)
            return;

        _isChangingToggleState = true;

        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle == null)
                continue;

            toggle.SetIsOnWithoutNotify(
                toggle.FilterType == NyangQuariumFishFilterType.All);
        }

        _isChangingToggleState = false;
    }

    private void HandleFilterChanged(
        NyangQuariumFishFilterToggle changedToggle,
        bool isOn)
    {
        if (_isChangingToggleState)
            return;

        _isChangingToggleState = true;

        if (changedToggle.FilterType == NyangQuariumFishFilterType.All)
        {
            if (isOn)
                EnableOnlyAll();
            else if (!HasEnabledFilter())
                changedToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            if (isOn)
                SetAllFilter(false);
            else if (!HasEnabledFilter())
                SetAllFilter(true);
        }

        _isChangingToggleState = false;
        Refresh();
    }

    private void EnableOnlyAll()
    {
        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle == null)
                continue;

            toggle.SetIsOnWithoutNotify(
                toggle.FilterType == NyangQuariumFishFilterType.All);
        }
    }

    private void SetAllFilter(bool isOn)
    {
        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle == null ||
                toggle.FilterType != NyangQuariumFishFilterType.All)
            {
                continue;
            }

            toggle.SetIsOnWithoutNotify(isOn);
            return;
        }
    }

    private bool HasEnabledFilter()
    {
        foreach (NyangQuariumFishFilterToggle toggle in _filterToggles)
        {
            if (toggle != null && toggle.IsOn)
                return true;
        }

        return false;
    }

    private void ClearCreatedItems()
    {
        foreach (NyangQuariumFishInventoryItem item in _createdItems)
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
}
