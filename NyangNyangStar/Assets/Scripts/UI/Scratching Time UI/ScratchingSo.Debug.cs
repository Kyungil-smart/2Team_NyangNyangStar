using System.Text;
using Services.Enums;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        /// <summary>
        /// 로드된 스크래칭 타임 데이터를 로그로 출력합니다.
        /// </summary>
        public void PrintData()
        {
            StringBuilder log = new();
            log.AppendLine("[ScratchingTimeSO 로드 완료]");

            foreach (ScratchingData data in _scratchingData)
            {
                log.AppendLine($"[{data.Stage}] " +
                               $"일일 내구도 = {data.MaxDailyDurability}, " +
                               $"일일 경험치 = {data.DailyClearExp}, " +
                               $"일일 남은 횟수 = {data.RemainingDailyChallengeCount}/{data.MaxDailyChallengeCount}, " +
                               $"주간 내구도 = {data.MaxWeeklyDurability}, " +
                               $"주간 경험치 = {data.WeeklyClearExp}, " +
                               $"주간 남은 횟수 = {data.RemainingWeeklyChallengeCount}/{data.MaxWeeklyChallengeCount}");
            }

            DebugTool.Log(log.ToString(), DebugType.Data);
        }
    }
}
