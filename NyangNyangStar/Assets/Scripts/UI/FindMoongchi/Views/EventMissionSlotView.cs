using System;
using Data.ScriptableObjects.MoongchiSO;
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

        [Header("Addressable 이미지")]
        [SerializeField] private Image _slotBackgroundImage;

        [Header("이벤트 재화 보상")]
        [SerializeField] private Image _eventCoinRewardIcon;
        [SerializeField] private TMP_Text _eventCoinRewardAmountText;
        [SerializeField] private GameObject _eventCoinClaimedCoverImage;
        [SerializeField] private Image _eventCoinClaimedCheckImage;

        [Header("에너지 보상")]
        [SerializeField] private GameObject _energyRewardRoot;
        [SerializeField] private Image _energyRewardIcon;
        [SerializeField] private TMP_Text _energyRewardAmountText;
        [SerializeField] private GameObject _energyClaimedCoverImage;
        [SerializeField] private Image _energyClaimedCheckImage;

        private FindMoongchiMissionViewData _data;
        private Action<int> _onClaimClicked;

        private UISpriteController _slotBackgroundController;
        private UISpriteController _eventCoinIconController;
        private UISpriteController _eventCoinCheckController;
        private UISpriteController _energyIconController;
        private UISpriteController _energyCheckController;

        private void Awake()
        {
            if (_claimButton == null)
                _claimButton = GetComponent<Button>();

            if (_slotBackgroundImage == null)
                _slotBackgroundImage = GetComponent<Image>();

            LoadStaticSprites();
        }

        private void LoadStaticSprites()
        {
            LoadSprite(ref _slotBackgroundController, _slotBackgroundImage, FindMoongchiSpriteKeys.SlotMission);
            LoadSprite(ref _eventCoinIconController, _eventCoinRewardIcon, FindMoongchiSpriteKeys.IconCoin);
            LoadSprite(ref _eventCoinCheckController, _eventCoinClaimedCheckImage, FindMoongchiSpriteKeys.IconCheck);
            LoadSprite(ref _energyIconController, _energyRewardIcon, FindMoongchiSpriteKeys.IconEnergy);
            LoadSprite(ref _energyCheckController, _energyClaimedCheckImage, FindMoongchiSpriteKeys.IconCheck);
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

            SetRewardAmount(_eventCoinRewardIcon, _eventCoinRewardAmountText, data.Reward1);
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

        private bool SetEnergyReward(MoongchiRewardData reward)
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

            SetRewardAmount(_energyRewardIcon, _energyRewardAmountText, reward);
            return true;
        }

        private static void SetRewardAmount(Image iconImage, TMP_Text amountText, MoongchiRewardData reward)
        {
            bool isValid = reward != null && reward.IsValid;

            if (iconImage != null)
                iconImage.enabled = isValid;

            if (amountText != null)
                amountText.text = isValid ? reward.RewardAmount.ToString() : string.Empty;
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

        private static void LoadSprite(ref UISpriteController controller, Image image, string key)
        {
            if (image == null || string.IsNullOrEmpty(key))
                return;

            controller ??= new UISpriteController(image);
            controller.ChangeSprite(key);
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(HandleClaimButtonClicked);

            _slotBackgroundController?.Dispose();
            _eventCoinIconController?.Dispose();
            _eventCoinCheckController?.Dispose();
            _energyIconController?.Dispose();
            _energyCheckController?.Dispose();
        }
    }
}
