using Services.Enums;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        /// <summary>
        /// Firestore에서 불러올 예정인 스크래칭 타임 진행 데이터를 적용합니다.
        /// </summary>
        /// <param name="openedStageBySchedule">이벤트 진행 주차 기준으로 개방된 최대 단계</param>
        /// <param name="remainingDailyChallengeCount">오늘 남은 일일 스테이지 도전 횟수</param>
        /// <param name="remainingWeeklyChallengeCount">이번 주 남은 주간 스테이지 도전 횟수</param>
        /// <param name="highestClearedDailyStage">일일 스테이지에서 클리어한 가장 높은 단계</param>
        /// <param name="highestClearedWeeklyStage">주간 스테이지에서 클리어한 가장 높은 단계</param>
        public void SetProgressData(
            int openedStageBySchedule,
            int remainingDailyChallengeCount,
            int remainingWeeklyChallengeCount,
            int highestClearedDailyStage,
            int highestClearedWeeklyStage)
        {
            _openedStageBySchedule = ClampStage(openedStageBySchedule);
            _remainingDailyChallengeCount = Mathf.Max(remainingDailyChallengeCount, 0);
            _remainingWeeklyChallengeCount = Mathf.Max(remainingWeeklyChallengeCount, 0);
            _highestClearedDailyStage = ClampClearedStage(highestClearedDailyStage);
            _highestClearedWeeklyStage = ClampClearedStage(highestClearedWeeklyStage);

            SetRemainingChallengeCountForAll(StageType.Daily, _remainingDailyChallengeCount);
            SetRemainingChallengeCountForAll(StageType.Weekly, _remainingWeeklyChallengeCount);
        }

        /// <summary>
        /// 이벤트 진행 주차 기준으로 개방된 최대 단계를 설정합니다.
        /// Firestore 연동 전에는 테스트 값으로 사용할 수 있습니다.
        /// </summary>
        /// <param name="openedStage">개방할 최대 단계</param>
        public void SetOpenedStageBySchedule(int openedStage)
        {
            _openedStageBySchedule = ClampStage(openedStage);
        }

        public int GetRemainingChallengeCount(StageType stageType)
        {
            return stageType switch
            {
                StageType.Daily => _remainingDailyChallengeCount,
                StageType.Weekly => _remainingWeeklyChallengeCount,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 모든 스테이지 데이터에 동일하게 적용합니다.
        /// </summary>
        /// <param name="stageType">적용할 스테이지 타입</param>
        /// <param name="remainingCount">남은 도전 횟수</param>
        private void SetRemainingChallengeCountForAll(StageType stageType, int remainingCount)
        {
            foreach (ScratchingData data in _scratchingData)
                data.SetRemainingChallengeCount(stageType, remainingCount);

            switch (stageType)
            {
                case StageType.Daily:
                    _remainingDailyChallengeCount = Mathf.Max(remainingCount, 0);
                    break;

                case StageType.Weekly:
                    _remainingWeeklyChallengeCount = Mathf.Max(remainingCount, 0);
                    break;
            }
        }
    }
}
