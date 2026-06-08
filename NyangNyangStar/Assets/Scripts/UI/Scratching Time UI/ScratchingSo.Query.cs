using Services.Enums;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        // 지정한 스테이지 타입의 최대 내구도를 반환
        public int GetMaxDurability(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return stageType switch
            {
                StageType.Daily => _dataDict[stage].MaxDailyDurability,
                StageType.Weekly => _dataDict[stage].MaxWeeklyDurability,
                _ => 0
            };
        }

        // 지정한 스테이지 타입의 현재 내구도를 반환
        public int GetCurrentDurability(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return stageType switch
            {
                StageType.Daily => _dataDict[stage].CurrentDailyDurability,
                StageType.Weekly => _dataDict[stage].CurrentWeeklyDurability,
                _ => 0
            };
        }

        // 지정한 스테이지 타입의 클리어 경험치를 반환
        public int GetClearExp(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return stageType switch
            {
                StageType.Daily => _dataDict[stage].DailyClearExp,
                StageType.Weekly => _dataDict[stage].WeeklyClearExp,
                _ => 0
            };
        }

        // 지정한 스테이지 타입의 최대 도전 횟수를 반환
        public int GetMaxChallengeCount(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].GetMaxChallengeCount(stageType);
        }

        // 지정한 스테이지 타입의 남은 도전 횟수를 반환
        public int GetRemainingChallengeCount(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].GetRemainingChallengeCount(stageType);
        }

        // 이벤트 진행 주차 기준으로 개방된 최대 단계를 반환
        public int GetOpenedStageBySchedule()
            => _openedStageBySchedule;

        // 지정한 스테이지 타입에서 클리어한 가장 높은 단계를 반환
        public int GetHighestClearedStage(StageType stageType)
        {
            return stageType switch
            {
                StageType.Daily => _highestClearedDailyStage,
                StageType.Weekly => _highestClearedWeeklyStage,
                _ => 0
            };
        }

        // 지정한 스테이지가 이벤트 주차 기준으로 개방되어 있는지 확인
        public bool IsOpenedBySchedule(int stage)
            => stage > 0 && stage <= _openedStageBySchedule;

        // 지정한 스테이지가 이전 단계 클리어 조건을 만족하는지 확인
        public bool IsStageUnlocked(int stage, StageType stageType)
        {
            if (stage <= 0)
                return false;

            if (!IsOpenedBySchedule(stage))
                return false;

            int highestClearedStage = GetHighestClearedStage(stageType);
            return stage <= highestClearedStage + 1;
        }

        // 지정한 스테이지를 현재 도전할 수 있는지 확인
        public bool CanChallenge(int stage, StageType stageType)
        {
            if (stageType == StageType.None)
                return false;

            if (!IsStageUnlocked(stage, stageType))
                return false;

            return GetRemainingChallengeCount(stage, stageType) > 0;
        }
    }
}
