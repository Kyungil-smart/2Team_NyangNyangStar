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
        [SerializeField] private GameObject _eventCoinRewardRoot;
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

            ResolveRewardRoots();
            LoadStaticSprites();
        }

        private void ResolveRewardRoots()
        {
            if (_eventCoinRewardRoot == null)
                _eventCoinRewardRoot = FindChildGameObject("EventCoinReward");

            if (_energyRewardRoot == null)
            {
                _energyRewardRoot = FindChildGameObject("EnergyReward");

                // 기존 프리팹 오타 호환
                if (_energyRewardRoot == null)
                    _energyRewardRoot = FindChildGameObject("EnegryReward");
            }
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];

                if (child != null && child.name == childName)
                    return child.gameObject;
            }

            return null;
        }

        private void LoadStaticSprites()
        {
            LoadSprite(ref _slotBackgroundController, _slotBackgroundImage, FindMoongchiSpriteKeys.SlotMission);

            // 현재 프리팹 배치 기준:
            // _eventCoinRewardRoot 계열 필드 = 왼쪽 보상 슬롯
            // _energyRewardRoot 계열 필드 = 오른쪽 보상 슬롯
            // 기획 UI 기준으로 왼쪽은 에너지, 오른쪽은 이벤트 코인을 표시한다.
            LoadSprite(ref _eventCoinIconController, _eventCoinRewardIcon, FindMoongchiSpriteKeys.IconEnergy);
            LoadSprite(ref _eventCoinCheckController, _eventCoinClaimedCheckImage, FindMoongchiSpriteKeys.IconCheck);
            LoadSprite(ref _energyIconController, _energyRewardIcon, FindMoongchiSpriteKeys.IconCoin);
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

            MoongchiRewardData energyReward = FindReward(data, MoongchiCurrencyType.ENERGY);
            MoongchiRewardData eventCoinReward = FindReward(data, MoongchiCurrencyType.EVENT_COIN);

            // 현재 프리팹 배치 기준:
            // _eventCoinRewardRoot 계열 필드 = 왼쪽 보상 슬롯
            // _energyRewardRoot 계열 필드 = 오른쪽 보상 슬롯
            // 기획 UI 기준: 왼쪽 에너지, 오른쪽 이벤트 코인.
            bool hasEnergyReward = SetRewardSlot(
                _eventCoinRewardRoot,
                _eventCoinRewardIcon,
                _eventCoinRewardAmountText,
                energyReward);

            bool hasEventCoinReward = SetRewardSlot(
                _energyRewardRoot,
                _energyRewardIcon,
                _energyRewardAmountText,
                eventCoinReward);

            RefreshState(data.State, hasEnergyReward, hasEventCoinReward);
            BindButton();
        }

        private static MoongchiRewardData FindReward(FindMoongchiMissionViewData data, MoongchiCurrencyType rewardType)
        {
            if (data == null)
                return null;

            if (IsRewardType(data.Reward1, rewardType))
                return data.Reward1;

            if (IsRewardType(data.Reward2, rewardType))
                return data.Reward2;

            return null;
        }

        private static bool IsRewardType(MoongchiRewardData reward, MoongchiCurrencyType rewardType)
        {
            return reward != null && reward.IsValid && reward.RewardType == rewardType;
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

        private void RefreshState(FindMoongchiMissionSlotState state, bool hasEnergyReward, bool hasEventCoinReward)
        {
            bool canClaim = state == FindMoongchiMissionSlotState.Completed;
            bool isClaimed = state == FindMoongchiMissionSlotState.Claimed;

            if (_claimButton != null)
                _claimButton.interactable = canClaim;

            // 왼쪽 슬롯은 에너지, 오른쪽 슬롯은 이벤트 코인으로 사용한다.
            SetActive(_eventCoinClaimedCoverImage, isClaimed && hasEnergyReward);
            SetActive(_energyClaimedCoverImage, isClaimed && hasEventCoinReward);
        }

        private static bool SetRewardSlot(GameObject rewardRoot, Image iconImage, TMP_Text amountText, MoongchiRewardData reward)
        {
            bool hasReward = reward != null && reward.IsValid;

            // 보상이 없으면 아이콘/텍스트만 끄는 것이 아니라 보상 슬롯 배경까지 통째로 숨긴다.
            SetActive(rewardRoot, hasReward);

            if (iconImage != null)
                iconImage.enabled = hasReward;

            if (amountText != null)
                amountText.text = hasReward ? reward.RewardAmount.ToString() : string.Empty;

            return hasReward;
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
