using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiMissionPanel : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private EventMissionSlotView _missionSlotPrefab;
        [SerializeField] private TMP_Text _dailyMissionText;
        [SerializeField] private TMP_Text _weeklyMissionText;

        private readonly List<EventMissionSlotView> _slotViews = new();

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
            DebugTool.Log($"[FindMoongchiMissionPanel] 초기화 완료: 기존 슬롯={_slotViews.Count}", DebugType.FindMoongchi, this);
        }

        public void SetData(IReadOnlyList<FindMoongchiMissionViewData> missions)
        {
            int missionCount = missions?.Count ?? 0;
            DebugTool.Log($"[FindMoongchiMissionPanel] 미션 데이터 적용: {missionCount}개", DebugType.FindMoongchi, this);
            ResolveSlotViews();
            EnsureSlotCount(missions?.Count ?? 0);

            for (int i = 0; i < _slotViews.Count; i++)
            {
                FindMoongchiMissionViewData data = missions != null && i < missions.Count ? missions[i] : null;
                _slotViews[i].SetData(data, HandleClaimMissionClicked);
            }
        }

        private void ResolveSlotViews()
        {
            if (_slotViews.Count > 0)
                return;

            EventMissionSlotView[] foundSlots = GetComponentsInChildren<EventMissionSlotView>(true);
            _slotViews.AddRange(foundSlots);
            DebugTool.Log($"[FindMoongchiMissionPanel] 기존 미션 슬롯 조회: {_slotViews.Count}개", DebugType.FindMoongchi, this);
        }

        private void EnsureSlotCount(int count)
        {
            if (_slotViews.Count >= count)
                return;

            if (_missionSlotPrefab == null || _contentRoot == null)
            {
                DebugTool.Warning("[FindMoongchiMissionPanel] MissionSlotPrefab 또는 ContentRoot가 없어 슬롯을 생성할 수 없습니다.", DebugType.FindMoongchi, this);
                return;
            }

            while (_slotViews.Count < count)
            {
                EventMissionSlotView slot = Instantiate(_missionSlotPrefab, _contentRoot, false);
                slot.name = $"EventMissionSlot_{_slotViews.Count}";
                _slotViews.Add(slot);
            }

            DebugTool.Log($"[FindMoongchiMissionPanel] 미션 슬롯 수 보정 완료: {_slotViews.Count}/{count}", DebugType.FindMoongchi, this);
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
