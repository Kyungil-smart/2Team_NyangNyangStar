using Services.Enums;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        // 지정한 스테이지 타입의 현재 내구도를 최대 내구도로 초기화
        public void StageStart(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return;

            switch (stageType)
            {
                case StageType.Daily:
                    _dataDict[stage].CurrentDailyDurability = _dataDict[stage].MaxDailyDurability;
                    break;

                case StageType.Weekly:
                    _dataDict[stage].CurrentWeeklyDurability = _dataDict[stage].MaxWeeklyDurability;
                    break;

                default:
                    DebugTool.Warning($"{stageType} 처리할 수 없는 스테이지 타입입니다.", DebugType.Data);
                    break;
            }
        }

        // 지정한 스테이지 타입의 스크래쳐에 데미지를 적용하고 남은 현재 내구도를 반환
        public int TakeDamageOnScratcher(int stage, StageType stageType, int damage)
        {
            if (!IsContainsKey(stage))
                return 0;

            switch (stageType)
            {
                case StageType.Daily:
                    _dataDict[stage].CurrentDailyDurability -= damage;
                    return _dataDict[stage].CurrentDailyDurability;

                case StageType.Weekly:
                    _dataDict[stage].CurrentWeeklyDurability -= damage;
                    return _dataDict[stage].CurrentWeeklyDurability;

                default:
                    DebugTool.Warning($"{stageType} 처리할 수 없는 스테이지 타입입니다.", DebugType.Data);
                    return 0;
            }
        }

        // 스테이지 시작 시 남은 도전 횟수를 1 차감
        public bool TryConsumeChallengeCount(int stage, StageType stageType)
        {
            if (!CanChallenge(stage, stageType))
                return false;

            int remainingCount = GetRemainingChallengeCount(stage, stageType);
            SetRemainingChallengeCountForAll(stageType, remainingCount - 1);
            return true;
        }

        // 스테이지 클리어를 기록하고 최고 클리어 단계를 갱신
        // 도전 횟수는 START 시점에 차감됨
        public void RecordStageClear(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return;

            switch (stageType)
            {
                case StageType.Daily:
                    _highestClearedDailyStage = Mathf.Max(_highestClearedDailyStage, stage);
                    break;

                case StageType.Weekly:
                    _highestClearedWeeklyStage = Mathf.Max(_highestClearedWeeklyStage, stage);
                    break;

                default:
                    DebugTool.Warning($"{stageType} 처리할 수 없는 스테이지 타입입니다.", DebugType.Data);
                    break;
            }
        }
    }
}
