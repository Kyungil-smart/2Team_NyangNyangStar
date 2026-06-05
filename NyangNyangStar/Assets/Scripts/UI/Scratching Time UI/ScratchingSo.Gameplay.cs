using Services.Enums;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        /// <summary>
        /// 지정한 스테이지 타입의 현재 내구도를 최대 내구도로 초기화합니다.
        /// </summary>
        /// <param name="stage">초기화를 적용할 스테이지 값</param>
        /// <param name="stageType">초기화할 스테이지 타입</param>
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

        /// <summary>
        /// 지정한 스테이지 타입의 스크래쳐에 데미지를 적용하고 남은 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">데미지를 적용할 스테이지 값</param>
        /// <param name="stageType">데미지를 적용할 스테이지 타입</param>
        /// <param name="damage">적용할 데미지 값</param>
        /// <returns>데미지 적용 후 남은 현재 내구도</returns>
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

        /// <summary>
        /// 스테이지 시작 시 남은 도전 횟수를 1 차감합니다.
        /// </summary>
        /// <param name="stage">시작할 스테이지 값</param>
        /// <param name="stageType">시작할 스테이지 타입</param>
        /// <returns>차감에 성공하면 true</returns>
        public bool TryConsumeChallengeCount(int stage, StageType stageType)
        {
            if (!CanChallenge(stage, stageType))
                return false;

            int remainingCount = GetRemainingChallengeCount(stage, stageType);
            SetRemainingChallengeCountForAll(stageType, remainingCount - 1);
            return true;
        }

        /// <summary>
        /// 스테이지 클리어를 기록하고 최고 클리어 단계를 갱신합니다.
        /// 도전 횟수는 START 시점에 차감됩니다.
        /// </summary>
        /// <param name="stage">클리어한 스테이지 값</param>
        /// <param name="stageType">클리어한 스테이지 타입</param>
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
