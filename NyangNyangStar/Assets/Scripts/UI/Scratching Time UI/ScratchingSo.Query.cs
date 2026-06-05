using Services.Enums;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        /// <summary>
        /// 지정한 스테이지 타입의 최대 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 최대 내구도</returns>
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

        /// <summary>
        /// 지정한 스테이지 타입의 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 현재 내구도</returns>
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

        /// <summary>
        /// 지정한 스테이지 타입의 클리어 경험치를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 클리어 경험치</returns>
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

        /// <summary>
        /// 지정한 스테이지 타입의 최대 도전 횟수를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 최대 도전 횟수</returns>
        public int GetMaxChallengeCount(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].GetMaxChallengeCount(stageType);
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 남은 도전 횟수</returns>
        public int GetRemainingChallengeCount(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].GetRemainingChallengeCount(stageType);
        }

        /// <summary>
        /// 이벤트 진행 주차 기준으로 개방된 최대 단계를 반환합니다.
        /// </summary>
        /// <returns>이벤트 진행 주차 기준으로 개방된 최대 단계</returns>
        public int GetOpenedStageBySchedule()
            => _openedStageBySchedule;

        /// <summary>
        /// 지정한 스테이지 타입에서 클리어한 가장 높은 단계를 반환합니다.
        /// </summary>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>클리어한 가장 높은 단계</returns>
        public int GetHighestClearedStage(StageType stageType)
        {
            return stageType switch
            {
                StageType.Daily => _highestClearedDailyStage,
                StageType.Weekly => _highestClearedWeeklyStage,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지가 이벤트 주차 기준으로 개방되어 있는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <returns>이벤트 주차 기준으로 개방되어 있으면 true, 아니면 false</returns>
        public bool IsOpenedBySchedule(int stage)
            => stage > 0 && stage <= _openedStageBySchedule;

        /// <summary>
        /// 지정한 스테이지가 이전 단계 클리어 조건을 만족하는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <param name="stageType">확인할 스테이지 타입</param>
        /// <returns>이전 단계 클리어 조건을 만족하면 true, 아니면 false</returns>
        public bool IsStageUnlocked(int stage, StageType stageType)
        {
            if (stage <= 0)
                return false;

            if (!IsOpenedBySchedule(stage))
                return false;

            int highestClearedStage = GetHighestClearedStage(stageType);
            return stage <= highestClearedStage + 1;
        }

        /// <summary>
        /// 지정한 스테이지를 현재 도전할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <param name="stageType">확인할 스테이지 타입</param>
        /// <returns>단계 개방과 남은 도전 횟수 조건을 모두 만족하면 true, 아니면 false</returns>
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
