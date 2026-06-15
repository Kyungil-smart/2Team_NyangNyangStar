using System;
using UnityEngine;


namespace Data.ScriptableObjects.HideAndSeekSO
{
    // 숨바꼭질_미션 테이블의 한 행을 나타냄
    // Id, 미션 타입, 미션 내용, 목표량, 보상1, 보상 수령, 보상 2

    [Serializable]
    public class HideAndSeekMissionData
    {
        [SerializeField] private int _id;
        [SerializeField] private HideAndSeekMissionType _missionType;
        [SerializeField] private string _missionContent;
        [SerializeField] private int _targetAmount;
        [SerializeField] private HideAndSeekRewardData _reward1;
        [SerializeField] private HideAndSeekRewardData _reward2;

        public int ID => _id;
        public HideAndSeekMissionType MissionType => _missionType;
        public string MissionContent => _missionContent;
        public int TargetAmount => _targetAmount;
        public HideAndSeekRewardData Reward1 => _reward1;
        public HideAndSeekRewardData Reward2 => _reward2;

        public bool HasReward2 => _reward2 != null && _reward2.IsValid;

        public HideAndSeekMissionData(
            int id,
            HideAndSeekMissionType missionType,
            string missionContent,
            int targetAmount,
            HideAndSeekRewardData reward1,
            HideAndSeekRewardData reward2)
        {
            _id = id;
            _missionType = missionType;
            _missionContent = missionContent;
            _targetAmount = targetAmount;
            _reward1 = reward1;
            _reward2 = reward2;
        }
    }
}