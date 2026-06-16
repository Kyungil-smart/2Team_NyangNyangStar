using System;
using UnityEngine;


namespace Data.ScriptableObjects.MoongchiSO
{
    // 숨바꼭질_미션 테이블의 한 행을 나타냄
    // Id, 미션 타입, 미션 내용, 목표량, 보상1, 보상 수령, 보상 2

    [Serializable]
    public class MoongchiMissionData
    {
        [SerializeField] private int _id;
        [SerializeField] private MoongchiMissionType _missionType;
        [SerializeField] private string _missionContent;
        [SerializeField] private int _targetAmount;
        [SerializeField] private MoongchiRewardData _reward1;
        [SerializeField] private MoongchiRewardData _reward2;

        public int ID => _id;
        public MoongchiMissionType MissionType => _missionType;
        public string MissionContent => _missionContent;
        public int TargetAmount => _targetAmount;
        public MoongchiRewardData Reward1 => _reward1;
        public MoongchiRewardData Reward2 => _reward2;

        public bool HasReward2 => _reward2 != null && _reward2.IsValid;

        public MoongchiMissionData(
            int id,
            MoongchiMissionType missionType,
            string missionContent,
            int targetAmount,
            MoongchiRewardData reward1,
            MoongchiRewardData reward2)
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