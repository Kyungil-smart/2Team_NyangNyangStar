using System;
using Data.ScriptableObjects.HideAndSeekSO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class EventMissionSlotView : MonoBehaviour
    {
        [Header("미션")]
        [SerializeField] private TMP_Text _missionDescriptionText;
        [SerializeField] private TMP_Text _missionProgressText;

        [Header("루트 버튼")]
        [SerializeField] private Button _claimButton;

        [Header("이벤트 재화 보상")]
        [SerializeField] private Image _eventCoinRewardIcon;
        [SerializeField] private TMP_Text _eventCoinRewardAmountText;
        [SerializeField] private GameObject _eventCoinClaimedCoverImage;

        [Header("에너지 보상")]
        [SerializeField] private GameObject _energyRewardRoot;
        [SerializeField] private Image _energyRewardIcon;
        [SerializeField] private TMP_Text _energyRewardAmountText;
        [SerializeField] private GameObject _energyClaimedCoverImage;

        private FindMoongchiMissionViewData _data;
        private Action<int> _onClaimClicked;

        private void Awake()
        {
            if (_claimButton == null)
                _claimButton = GetComponent<Button>();
        }

        public void SetData(FindMoongchiMissionViewData data, Action<int> onClaimClicked)
        {
            _data = data;
            _onClaimClicked = onClaimClicked;

            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            SetText(_missionDescriptionText, data.MissionDescription);
            SetText(_missionProgressText, $"{data.CurrentAmount}/{data.TargetAmount}");

            SetReward(_eventCoinRewardIcon, _eventCoinRewardAmountText, data.Reward1);
            bool hasEnergyReward = SetEnergyReward(data.Reward2);

            RefreshState(data.State, hasEnergyReward);
            BindButton();
        }

        private void BindButton()
        {
            if (_claimButton == null)
                return;

            _claimButton.onClick.RemoveListener(HandleClaimButtonClicked);
            _claimButton.onClick.AddListener(HandleClaimButtonClicked);
        }

        private void HandleClaimButtonClicked()
        {
            if (_data == null)
                return;

            if (_data.State != FindMoongchiMissionSlotState.Completed)
                return;

            _onClaimClicked?.Invoke(_data.MissionId);
        }

        private void RefreshState(FindMoongchiMissionSlotState state, bool hasEnergyReward)
        {
            bool canClaim = state == FindMoongchiMissionSlotState.Completed;
            bool isClaimed = state == FindMoongchiMissionSlotState.Claimed;

            if (_claimButton != null)
                _claimButton.interactable = canClaim;

            SetActive(_eventCoinClaimedCoverImage, isClaimed);
            SetActive(_energyClaimedCoverImage, isClaimed && hasEnergyReward);
        }

        private bool SetEnergyReward(HideAndSeekRewardData reward)
        {
            bool hasReward = reward != null && reward.IsValid;

            SetActive(_energyRewardRoot, hasReward);

            if (!hasReward)
            {
                if (_energyRewardIcon != null)
                    _energyRewardIcon.enabled = false;

                if (_energyRewardAmountText != null)
                    _energyRewardAmountText.text = string.Empty;

                SetActive(_energyClaimedCoverImage, false);
                return false;
            }

            SetReward(_energyRewardIcon, _energyRewardAmountText, reward);
            return true;
        }

        private static void SetReward(Image iconImage, TMP_Text amountText, HideAndSeekRewardData reward)
        {
            if (reward == null || !reward.IsValid)
            {
                if (iconImage != null)
                    iconImage.enabled = false;

                if (amountText != null)
                    amountText.text = string.Empty;

                return;
            }

            if (iconImage != null)
                iconImage.enabled = true;

            if (amountText != null)
                amountText.text = reward.RewardAmount.ToString();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null)
                target.SetActive(isActive);
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(HandleClaimButtonClicked);
        }
    }
}