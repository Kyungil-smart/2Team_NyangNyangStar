using System;
using System.Collections.Generic;
using UI.FindMoongchi;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    public static class FindMoongchiProgressResetLogic
    {
        private const int MaxEventWeek = 2;

        private static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);

        public static bool ApplyResetsIfNeeded(
            FindMoongchiProgressRuntimeData progress,
            MoongchiMissionSO missionSO)
        {
            if (progress == null)
                return false;

            long now = GetCurrentUnixTimeSeconds();
            bool changed = false;

            // 신규 문서(LastDailyResetUnixTime == 0)는 첫 진입 시 일일 기본값만 세팅
            if (progress.LastDailyResetUnixTime <= 0)
            {
                ApplyDailyReset(progress, missionSO, now);
                changed = true;
            }
            else if (ShouldResetDaily(progress.LastDailyResetUnixTime, now))
            {
                ApplyDailyReset(progress, missionSO, now);
                changed = true;
            }

            // 신규 문서(LastWeeklyResetUnixTime == 0)는 주차를 올리지 않고 이번 주 기준 시각만 기록
            // (기존: 0이면 ApplyWeeklyReset → CurrentWeek가 1→2로 즉시 증가하는 버그)
            if (progress.LastWeeklyResetUnixTime <= 0)
            {
                progress.LastWeeklyResetUnixTime = GetWeeklyAnchorUnixTime(now);
                changed = true;

                DebugTool.Log(
                    $"[FindMoongchiProgressResetLogic] 주간 초기화 기준 시각 설정 (신규 유저, CurrentWeek={progress.CurrentWeek} 유지)",
                    DebugType.Data);
            }
            else if (ShouldResetWeekly(progress.LastWeeklyResetUnixTime, now))
            {
                ApplyWeeklyReset(progress, missionSO, now);
                changed = true;
            }

            return changed;
        }

        public static long GetCurrentUnixTimeSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static int GetDailyEnergySpendProgress(FindMoongchiProgressRuntimeData progress)
        {
            return progress?.DailyEnergySpendProgress ?? 0;
        }

        // 0(미설정)은 ApplyResetsIfNeeded에서 신규/실제 리셋으로 분기하므로 여기서는 날짜 비교만 수행
        private static bool ShouldResetDaily(long lastResetUnixTime, long nowUnixTime)
        {
            return GetDailyAnchorUnixTime(lastResetUnixTime) < GetDailyAnchorUnixTime(nowUnixTime);
        }

        private static bool ShouldResetWeekly(long lastResetUnixTime, long nowUnixTime)
        {
            return GetWeeklyAnchorUnixTime(lastResetUnixTime) < GetWeeklyAnchorUnixTime(nowUnixTime);
        }

        private static void ApplyDailyReset(
            FindMoongchiProgressRuntimeData progress,
            MoongchiMissionSO missionSO,
            long nowUnixTime)
        {
            ClearMissionProgressByType(progress, missionSO, MoongchiMissionType.DAILY);

            progress.SearchChance = FindMoongchiConstants.DailySearchChance;
            progress.TodayBonusSearchChanceCount = 0;
            progress.DailyEnergySpendProgress = 0;
            progress.LastDailyResetUnixTime = GetDailyAnchorUnixTime(nowUnixTime);

            DebugTool.Log("[FindMoongchiProgressResetLogic] 일일 진행 데이터 초기화", DebugType.Data);
        }

        private static void ApplyWeeklyReset(
            FindMoongchiProgressRuntimeData progress,
            MoongchiMissionSO missionSO,
            long nowUnixTime)
        {
            ClearMissionProgressByType(progress, missionSO, MoongchiMissionType.WEEKLY);
            ClearMissionProgressByType(progress, missionSO, MoongchiMissionType.WEEKLY_1ST);
            ClearMissionProgressByType(progress, missionSO, MoongchiMissionType.WEEKLY_2ND);

            if (progress.CurrentWeek < MaxEventWeek)
                progress.CurrentWeek++;

            progress.LastWeeklyResetUnixTime = GetWeeklyAnchorUnixTime(nowUnixTime);

            DebugTool.Log(
                $"[FindMoongchiProgressResetLogic] 주간 진행 데이터 초기화: CurrentWeek={progress.CurrentWeek}",
                DebugType.Data);
        }

        private static void ClearMissionProgressByType(
            FindMoongchiProgressRuntimeData progress,
            MoongchiMissionSO missionSO,
            MoongchiMissionType missionType)
        {
            if (progress?.MissionProgresses == null || missionSO == null)
                return;

            List<MoongchiMissionData> missions = missionSO.GetMissionsByType(missionType);

            for (int i = 0; i < missions.Count; i++)
            {
                MoongchiMissionData mission = missions[i];

                if (mission == null)
                    continue;

                FindMoongchiProgressHelper.RemoveMissionProgress(progress, mission.ID);
            }
        }

        private static long GetDailyAnchorUnixTime(long unixSeconds)
        {
            DateTimeOffset kst = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToOffset(KstOffset);
            DateTimeOffset dayStart = new DateTimeOffset(kst.Year, kst.Month, kst.Day, 0, 0, 0, KstOffset);
            return dayStart.ToUnixTimeSeconds();
        }

        private static long GetWeeklyAnchorUnixTime(long unixSeconds)
        {
            DateTimeOffset kst = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToOffset(KstOffset);
            int daysFromMonday = ((int)kst.DayOfWeek + 6) % 7;
            DateTimeOffset weekStart = new DateTimeOffset(
                kst.Year,
                kst.Month,
                kst.Day,
                0,
                0,
                0,
                KstOffset).AddDays(-daysFromMonday);

            return weekStart.ToUnixTimeSeconds();
        }
    }
}
