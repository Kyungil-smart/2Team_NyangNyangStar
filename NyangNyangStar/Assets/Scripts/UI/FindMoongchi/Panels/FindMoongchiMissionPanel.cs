using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiMissionPanel : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _backButton;

        [Header("Legacy / 통합 Content")]
        [Tooltip("기존 단일 Content입니다. Daily/Weekly Content가 비어 있으면 여기에 일일+주간 미션을 모두 생성합니다.")]
        [SerializeField] private Transform _contentRoot;

        [Header("Daily / Weekly Content")]
        [Tooltip("일일 미션 슬롯이 들어갈 Content입니다. 비워두면 Content Root를 사용합니다.")]
        [SerializeField] private Transform _dailyContentRoot;
        [Tooltip("주간 미션 슬롯이 들어갈 Content입니다. 비워두면 Content Root를 사용합니다.")]
        [SerializeField] private Transform _weeklyContentRoot;

        [Header("Prefab")]
        [SerializeField] private EventMissionSlotView _missionSlotPrefab;

        [Header("Section Text")]
        [SerializeField] private TMP_Text _dailyMissionText;
        [SerializeField] private TMP_Text _weeklyMissionText;
        [SerializeField] private string _dailyMissionTitle = "일일 미션";
        [SerializeField] private string _weeklyMissionTitleFormat = "주간 미션 - {0}주차";

        private readonly List<EventMissionSlotView> _legacySlotViews = new();
        private readonly List<EventMissionSlotView> _dailySlotViews = new();
        private readonly List<EventMissionSlotView> _weeklySlotViews = new();

        public event Action OnBackButtonClicked;
        public event Action<int> OnClaimMissionClicked;

        public void Init()
        {
            DebugTool.Log("[FindMoongchiMissionPanel] 초기화 시작", DebugType.FindMoongchi, this);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            ResolveSlotViews();
            DebugTool.Log(
                $"[FindMoongchiMissionPanel] 초기화 완료: Legacy={_legacySlotViews.Count}, Daily={_dailySlotViews.Count}, Weekly={_weeklySlotViews.Count}",
                DebugType.FindMoongchi,
                this);
        }

        public void SetData(IReadOnlyList<FindMoongchiMissionViewData> missions)
        {
            int missionCount = missions?.Count ?? 0;
            DebugTool.Log($"[FindMoongchiMissionPanel] 통합 미션 데이터 적용: {missionCount}개", DebugType.FindMoongchi, this);

            ResolveSlotViews();
            SetSectionVisible(_dailyMissionText, missionCount > 0);
            SetSectionVisible(_weeklyMissionText, false);
            SetSlotGroup(_legacySlotViews, ResolveLegacyContentRoot(), missions);

            HideSlots(_dailySlotViews);
            HideSlots(_weeklySlotViews);
        }

        public void SetData(
            IReadOnlyList<FindMoongchiMissionViewData> dailyMissions,
            IReadOnlyList<FindMoongchiMissionViewData> weeklyMissions)
        {
            SetData(dailyMissions, weeklyMissions, 0);
        }

        public void SetData(
            IReadOnlyList<FindMoongchiMissionViewData> dailyMissions,
            IReadOnlyList<FindMoongchiMissionViewData> weeklyMissions,
            int currentWeek)
        {
            int dailyCount = dailyMissions?.Count ?? 0;
            int weeklyCount = weeklyMissions?.Count ?? 0;

            ApplySectionTitles(currentWeek);

            DebugTool.Log(
                $"[FindMoongchiMissionPanel] 미션 데이터 적용: Week={currentWeek}, Daily={dailyCount}, Weekly={weeklyCount}",
                DebugType.FindMoongchi,
                this);

            ResolveSlotViews();

            bool hasSeparatedRoots = _dailyContentRoot != null || _weeklyContentRoot != null;

            if (!hasSeparatedRoots)
            {
                SetLegacyGroupedMissionData(dailyMissions, weeklyMissions);

                HideSlots(_dailySlotViews);
                HideSlots(_weeklySlotViews);
                return;
            }

            SetSectionVisible(_dailyMissionText, dailyCount > 0);
            SetSectionVisible(_weeklyMissionText, weeklyCount > 0);
            SetSlotGroup(_dailySlotViews, ResolveDailyContentRoot(), dailyMissions);
            SetSlotGroup(_weeklySlotViews, ResolveWeeklyContentRoot(), weeklyMissions);
            HideSlots(_legacySlotViews);
        }

        private void ApplySectionTitles(int currentWeek)
        {
            if (_dailyMissionText != null)
                _dailyMissionText.text = string.IsNullOrWhiteSpace(_dailyMissionTitle) ? "일일 미션" : _dailyMissionTitle;

            if (_weeklyMissionText == null)
                return;

            int displayWeek = currentWeek <= 0 ? 1 : currentWeek;
            string format = string.IsNullOrWhiteSpace(_weeklyMissionTitleFormat)
                ? "주간 미션 - {0}주차"
                : _weeklyMissionTitleFormat;

            _weeklyMissionText.text = string.Format(format, displayWeek);
        }

        private void ResolveSlotViews()
        {
            if (_legacySlotViews.Count > 0 || _dailySlotViews.Count > 0 || _weeklySlotViews.Count > 0)
                return;

            if (_contentRoot != null)
                _legacySlotViews.AddRange(_contentRoot.GetComponentsInChildren<EventMissionSlotView>(true));

            if (_dailyContentRoot != null)
                _dailySlotViews.AddRange(_dailyContentRoot.GetComponentsInChildren<EventMissionSlotView>(true));

            if (_weeklyContentRoot != null)
                _weeklySlotViews.AddRange(_weeklyContentRoot.GetComponentsInChildren<EventMissionSlotView>(true));

            // 기존 프리팹에서 ContentRoot 하위 슬롯만 쓰는 경우를 보호합니다.
            if (_contentRoot == null && _dailyContentRoot == null && _weeklyContentRoot == null)
                _legacySlotViews.AddRange(GetComponentsInChildren<EventMissionSlotView>(true));

            DebugTool.Log(
                $"[FindMoongchiMissionPanel] 기존 미션 슬롯 조회: Legacy={_legacySlotViews.Count}, Daily={_dailySlotViews.Count}, Weekly={_weeklySlotViews.Count}",
                DebugType.FindMoongchi,
                this);
        }

        private Transform ResolveLegacyContentRoot()
        {
            if (_contentRoot != null)
                return _contentRoot;

            if (_dailyContentRoot != null)
                return _dailyContentRoot;

            return _weeklyContentRoot;
        }

        private Transform ResolveDailyContentRoot()
        {
            return _dailyContentRoot != null ? _dailyContentRoot : ResolveLegacyContentRoot();
        }

        private Transform ResolveWeeklyContentRoot()
        {
            return _weeklyContentRoot != null ? _weeklyContentRoot : ResolveLegacyContentRoot();
        }

        private void SetLegacyGroupedMissionData(
            IReadOnlyList<FindMoongchiMissionViewData> dailyMissions,
            IReadOnlyList<FindMoongchiMissionViewData> weeklyMissions)
        {
            Transform parent = ResolveLegacyContentRoot();
            int dailyCount = dailyMissions?.Count ?? 0;
            int weeklyCount = weeklyMissions?.Count ?? 0;
            int totalCount = dailyCount + weeklyCount;

            SetSectionVisible(_dailyMissionText, dailyCount > 0);
            SetSectionVisible(_weeklyMissionText, weeklyCount > 0);
            EnsureSlotCount(_legacySlotViews, parent, totalCount);

            int slotIndex = 0;
            int siblingIndex = 0;

            MoveSectionText(_dailyMissionText, parent, ref siblingIndex);

            for (int i = 0; i < dailyCount; i++)
            {
                EventMissionSlotView slot = _legacySlotViews[slotIndex++];
                slot.SetData(dailyMissions[i], HandleClaimMissionClicked);
                MoveSlot(slot, parent, ref siblingIndex);
            }

            MoveSectionText(_weeklyMissionText, parent, ref siblingIndex);

            for (int i = 0; i < weeklyCount; i++)
            {
                EventMissionSlotView slot = _legacySlotViews[slotIndex++];
                slot.SetData(weeklyMissions[i], HandleClaimMissionClicked);
                MoveSlot(slot, parent, ref siblingIndex);
            }

            for (int i = slotIndex; i < _legacySlotViews.Count; i++)
                _legacySlotViews[i].SetData(null, null);

            DebugTool.Log(
                $"[FindMoongchiMissionPanel] 통합 Content 정렬 완료: Daily={dailyCount}, Weekly={weeklyCount}",
                DebugType.FindMoongchi,
                this);
        }

        private void SetSlotGroup(
            List<EventMissionSlotView> slotViews,
            Transform parent,
            IReadOnlyList<FindMoongchiMissionViewData> missions)
        {
            int count = missions?.Count ?? 0;
            EnsureSlotCount(slotViews, parent, count);

            for (int i = 0; i < slotViews.Count; i++)
            {
                FindMoongchiMissionViewData data = missions != null && i < missions.Count ? missions[i] : null;
                slotViews[i].SetData(data, HandleClaimMissionClicked);
            }
        }

        private void EnsureSlotCount(List<EventMissionSlotView> slotViews, Transform parent, int count)
        {
            if (slotViews.Count >= count)
                return;

            if (_missionSlotPrefab == null || parent == null)
            {
                DebugTool.Warning(
                    $"[FindMoongchiMissionPanel] MissionSlotPrefab 또는 ContentRoot가 없어 슬롯을 생성할 수 없습니다. 요청={count}",
                    DebugType.FindMoongchi,
                    this);
                return;
            }

            while (slotViews.Count < count)
            {
                EventMissionSlotView slot = Instantiate(_missionSlotPrefab, parent, false);
                slot.name = $"EventMissionSlot_{slotViews.Count}";
                slotViews.Add(slot);
            }

            DebugTool.Log($"[FindMoongchiMissionPanel] 미션 슬롯 수 보정 완료: {slotViews.Count}/{count}", DebugType.FindMoongchi, this);
        }

        private static void MoveSectionText(TMP_Text text, Transform parent, ref int siblingIndex)
        {
            if (text == null || parent == null || !text.gameObject.activeSelf)
                return;

            if (text.transform.parent == parent)
                text.transform.SetSiblingIndex(siblingIndex++);
        }

        private static void MoveSlot(EventMissionSlotView slot, Transform parent, ref int siblingIndex)
        {
            if (slot == null || parent == null || !slot.gameObject.activeSelf)
                return;

            if (slot.transform.parent == parent)
                slot.transform.SetSiblingIndex(siblingIndex++);
        }

        private static void HideSlots(List<EventMissionSlotView> slotViews)
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                if (slotViews[i] != null)
                    slotViews[i].SetData(null, null);
            }
        }

        private static void SetSectionVisible(TMP_Text text, bool visible)
        {
            if (text != null)
                text.gameObject.SetActive(visible);
        }

        private void HandleBackButtonClicked()
        {
            DebugTool.Log("[FindMoongchiMissionPanel] 뒤로가기 버튼 클릭", DebugType.FindMoongchi, this);
            OnBackButtonClicked?.Invoke();
        }

        private void HandleClaimMissionClicked(int missionId)
        {
            DebugTool.Log($"[FindMoongchiMissionPanel] 미션 슬롯 클릭: MissionId={missionId}", DebugType.FindMoongchi, this);
            OnClaimMissionClicked?.Invoke(missionId);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }
    }
}
