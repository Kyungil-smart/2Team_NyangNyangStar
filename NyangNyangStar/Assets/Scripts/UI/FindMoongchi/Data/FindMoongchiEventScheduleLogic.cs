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
