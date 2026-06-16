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
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            ResolveSlotViews();
        }

        public void SetData(IReadOnlyList<FindMoongchiMissionViewData> missions)
        {
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
        }

        private void EnsureSlotCount(int count)
        {
            if (_missionSlotPrefab == null || _contentRoot == null)
                return;

            while (_slotViews.Count < count)
            {
                EventMissionSlotView slot = Instantiate(_missionSlotPrefab, _contentRoot, false);
                slot.name = $"EventMissionSlot_{_slotViews.Count}";
                _slotViews.Add(slot);
            }
        }

        private void HandleBackButtonClicked()
        {
            OnBackButtonClicked?.Invoke();
        }

        private void HandleClaimMissionClicked(int missionId)
        {
            OnClaimMissionClicked?.Invoke(missionId);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }
    }
}
