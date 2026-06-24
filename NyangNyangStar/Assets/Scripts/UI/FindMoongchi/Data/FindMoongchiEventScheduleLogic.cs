using System;

namespace Data.ScriptableObjects.MoongchiSO
{
    public static class FindMoongchiEventScheduleLogic
    {
        private static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);

        public static DateTimeOffset GetEventStartKst(FindMoongchiEventScheduleSO schedule)
        {
            if (schedule == null)
                return default;

            return new DateTimeOffset(
                schedule.StartYear,
                schedule.StartMonth,
                schedule.StartDay,
                0,
                0,
                0,
                KstOffset);
        }

        public static DateTimeOffset GetEventEndKst(FindMoongchiEventScheduleSO schedule)
        {
            if (schedule == null)
                return default;

            return new DateTimeOffset(
                schedule.EndYear,
                schedule.EndMonth,
                schedule.EndDay,
                23,
                59,
                59,
                KstOffset);
        }

        public static bool IsEventActive(FindMoongchiEventScheduleSO schedule, long? nowUnixTimeSeconds = null)
        {
            if (schedule == null)
                return false;

            DateTimeOffset now = GetNowKst(nowUnixTimeSeconds);
            DateTimeOffset start = GetEventStartKst(schedule);
            DateTimeOffset end = GetEventEndKst(schedule);

            return now >= start && now <= end;
        }

        public static string FormatPeriodText(FindMoongchiEventScheduleSO schedule)
        {
            if (schedule == null)
                return string.Empty;

            return $"{FormatDate(schedule.StartYear, schedule.StartMonth, schedule.StartDay)} ~ {FormatDate(schedule.EndYear, schedule.EndMonth, schedule.EndDay)}";
        }

        public static string FormatPeriodRichText(
            FindMoongchiEventScheduleSO schedule,
            string colorHex = "7A3A2E")
        {
            string periodText = FormatPeriodText(schedule);

            if (string.IsNullOrEmpty(periodText))
                return string.Empty;

            if (string.IsNullOrEmpty(colorHex))
                return periodText;

            return $"<color=#{colorHex}>{periodText}</color>";
        }

        public static string FormatRemainTimeText(
            FindMoongchiEventScheduleSO schedule,
            long? nowUnixTimeSeconds = null)
        {
            if (schedule == null)
                return string.Empty;

            DateTimeOffset now = GetNowKst(nowUnixTimeSeconds);
            DateTimeOffset end = GetEventEndKst(schedule);
            TimeSpan remaining = end - now;

            if (remaining <= TimeSpan.Zero)
                return "0h";

            int days = remaining.Days;
            int hours = remaining.Hours;

            if (days > 0)
                return $"{days}d {hours}h";

            return $"{hours}h";
        }

        // 이벤트 시작일 00:00(KST)부터 7일 단위 주차. 1~7일차=1주차, 8~14일차=2주차
        public static int GetCurrentEventWeek(
            FindMoongchiEventScheduleSO schedule,
            long? nowUnixTimeSeconds = null,
            int maxWeek = 2)
        {
            if (schedule == null)
                return 1;

            DateTimeOffset start = GetEventStartKst(schedule);
            DateTimeOffset now = GetNowKst(nowUnixTimeSeconds);

            if (now < start)
                return 1;

            int daysSinceStart = GetDaysSinceEventStart(start, now);
            int week = (daysSinceStart / 7) + 1;
            return ClampWeek(week, maxWeek);
        }

        // 현재 시각이 속한 이벤트 주차의 시작 시각 (KST 00:00)
        public static long GetEventWeekAnchorUnixTime(
            FindMoongchiEventScheduleSO schedule,
            long unixSeconds)
        {
            if (schedule == null)
                return unixSeconds;

            DateTimeOffset start = GetEventStartKst(schedule);
            DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToOffset(KstOffset);

            if (time < start)
                return start.ToUnixTimeSeconds();

            int daysSinceStart = GetDaysSinceEventStart(start, time);
            int weekIndex = daysSinceStart / 7;
            return start.AddDays(weekIndex * 7).ToUnixTimeSeconds();
        }

        private static int GetDaysSinceEventStart(DateTimeOffset eventStartKst, DateTimeOffset timeKst)
        {
            DateTimeOffset startDay = GetDayStartKst(eventStartKst);
            DateTimeOffset timeDay = GetDayStartKst(timeKst);
            return (int)(timeDay - startDay).TotalDays;
        }

        private static DateTimeOffset GetDayStartKst(DateTimeOffset timeKst)
        {
            return new DateTimeOffset(timeKst.Year, timeKst.Month, timeKst.Day, 0, 0, 0, KstOffset);
        }

        private static int ClampWeek(int week, int maxWeek)
        {
            if (week < 1)
                return 1;

            if (week > maxWeek)
                return maxWeek;

            return week;
        }

        private static DateTimeOffset GetNowKst(long? nowUnixTimeSeconds)
        {
            if (nowUnixTimeSeconds.HasValue)
                return DateTimeOffset.FromUnixTimeSeconds(nowUnixTimeSeconds.Value).ToOffset(KstOffset);

            return DateTimeOffset.UtcNow.ToOffset(KstOffset);
        }

        private static string FormatDate(int year, int month, int day)
        {
            return $"{year:0000}.{month:00}.{day:00}";
        }
    }
}
