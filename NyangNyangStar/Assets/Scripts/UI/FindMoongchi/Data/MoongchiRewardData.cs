using System;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 일일 미션은 2개임 으아아아악
    // 주간 보상 1개

    [Serializable]
    public class MoongchiRewardData
    {
        [SerializeField] private MoongchiCurrencyType _rewardType;
        [SerializeField] private int _rewardAmount;

        public MoongchiCurrencyType RewardType => _rewardType;
        public int RewardAmount => _rewardAmount;

        public bool IsValid =>
            _rewardType != MoongchiCurrencyType.None && _rewardAmount > 0; // 보상터입이 존재 하고 수량도 1이상이면

        public MoongchiRewardData(MoongchiCurrencyType rewardType, int rewardAmount)
        {
            _rewardType = rewardType;
            _rewardAmount = rewardAmount;
        }
    }
}