using System;
using UnityEngine;

namespace Data.ScriptableObjects.HideAndSeekSO
{
    // 일일 미션은 2개임 으아아아악
    // 주간 보상 1개

    [Serializable]
    public class HideAndSeekRewardData
    {
        [SerializeField] private HideAndSeekCurrencyType _rewardType;
        [SerializeField] private int _rewardAmount;

        public HideAndSeekCurrencyType RewardType => _rewardType;
        public int RewardAmount => _rewardAmount;

        public bool IsValid =>
            _rewardType != HideAndSeekCurrencyType.None && _rewardAmount > 0; // 보상터입이 존재 하고 수량도 1이상이면

        public HideAndSeekRewardData(HideAndSeekCurrencyType rewardType, int rewardAmount)
        {
            _rewardType = rewardType;
            _rewardAmount = rewardAmount;
        }
    }
}