using System;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 미션별 진행도 데이터
    // MissionID를 맵 키로 사용, 미션별 현재 진행도와 보상 수령 여부 저장
    [Serializable]
    public class FindMoongchiMissionProgressData
    {
        [FirestoreMapKey]
        [SerializeField] private int _missionID;

        [SerializeField] private int _currentAmount;
        [SerializeField] private bool _isRewardClaimed;

        public int MissionID => _missionID;
        public int CurrentAmount => _currentAmount;
        public bool IsRewardClaimed => _isRewardClaimed;

        public FindMoongchiMissionProgressData()
        {
        }

        public FindMoongchiMissionProgressData(int missionID, int currentAmount, bool isRewardClaimed)
        {
            _missionID = missionID;
            _currentAmount = Mathf.Max(0, currentAmount);
            _isRewardClaimed = isRewardClaimed;
        }

        public void SetCurrentAmount(int currentAmount)
        {
            _currentAmount = Mathf.Max(0, currentAmount);
        }

        public void SetRewardClaimed(bool isRewardClaimed)
        {
            _isRewardClaimed = isRewardClaimed;
        }
    }
}
