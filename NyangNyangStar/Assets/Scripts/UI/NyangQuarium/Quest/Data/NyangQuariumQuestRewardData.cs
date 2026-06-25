using System;
using UnityEngine;

namespace Data.ScriptableObjects.NyangQuariumSO
{
    // 냥쿠_퀘스트 보상 테이블의 한 줄(row)을 담는 데이터 클래스
    // 실제 시트 컬럼:
    // id, rewardItem1, amount, rewardItem2, amount, expAmount
    [Serializable]
    public class NyangQuariumQuestRewardData
    {
        // 보상 데이터 고유 ID : 예 : 44001
        [SerializeField] private int _id;

        // 첫 번째 보상 아이템 ID : 예 : 44001
        [SerializeField] private int _rewardItem1;

        // 첫 번째 보상 아이템 수량 : 예 : 10
        [SerializeField] private int _rewardAmount1;

        // 두 번째 보상 아이템 ID : 예 : 44002
        [SerializeField] private int _rewardItem2;

        // 두 번째 보상 아이템 수량 : 예 : 20
        [SerializeField] private int _rewardAmount2;

        // 수조 경험치 보상량 : 예 : 100
        [SerializeField] private int _expAmount;

        public int ID => _id;
        public int RewardItem1 => _rewardItem1;
        public int RewardAmount1 => _rewardAmount1;
        public int RewardItem2 => _rewardItem2;
        public int RewardAmount2 => _rewardAmount2;
        public int ExpAmount => _expAmount;

        // 첫 번째 아이템 보상이 유효한지 확인하기
        public bool HasRewardItem1 => _rewardItem1 > 0 && _rewardAmount1 > 0;

        // 두 번째 아이템 보상이 유효한지 확인하기
        public bool HasRewardItem2 => _rewardItem2 > 0 && _rewardAmount2 > 0;

        // 경험치 보상이 있는지 확인하기
        public bool HasExpReward => _expAmount > 0;

        // 시트에서 파싱한 값을 한 번에 넣기 위한 생성자
        public NyangQuariumQuestRewardData(
            int id,
            int rewardItem1,
            int rewardAmount1,
            int rewardItem2,
            int rewardAmount2,
            int expAmount)
        {
            _id = id;
            _rewardItem1 = rewardItem1;
            _rewardAmount1 = rewardAmount1;
            _rewardItem2 = rewardItem2;
            _rewardAmount2 = rewardAmount2;
            _expAmount = expAmount;
        }
    }
}