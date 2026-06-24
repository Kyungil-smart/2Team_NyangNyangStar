using System;
using System.Globalization;
using System.Threading.Tasks;

public static class UserSessionTimeService
{
    private static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);
    private const string KstDateTimeFormat = "yyyy.MM.dd HH:mm";

    public static string FormatNowKst()
    {
        return DateTimeOffset.UtcNow.ToOffset(KstOffset).ToString(KstDateTimeFormat, CultureInfo.InvariantCulture);
    }

    public static string FormatUnixTimeKst(int unixTimeSeconds)
    {
        if (unixTimeSeconds <= 0)
            return string.Empty;

        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds)
            .ToOffset(KstOffset)
            .ToString(KstDateTimeFormat, CultureInfo.InvariantCulture);
    }

    public static bool TryParseKstDateTime(string value, out DateTimeOffset kstTime)
    {
        kstTime = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (DateTime.TryParseExact(
                value,
                KstDateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime localTime))
        {
            kstTime = new DateTimeOffset(localTime, KstOffset);
            return true;
        }

        return false;
    }
    public static async Task RecordLoginAsync()
    {
        if (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
        {
            DebugTool.Warning(
                "[UserSessionTimeService] Firestore가 초기화되지 않아 lastLogin 저장을 건너뜁니다.",
                DebugType.Data);
            return;
        }

        if (!FireStoreManager.Instance.TryGetStore(out UsersSO usersSO) || usersSO == null)
        {
            DebugTool.Warning(
                "[UserSessionTimeService] UsersSO를 찾지 못해 lastLogin 저장을 건너뜁니다.",
                DebugType.Data);
            return;
        }

        try
        {
            await usersSO.RecordLoginAsync();
        }
        catch (Exception e)
        {
            DebugTool.Warning(
                $"[UserSessionTimeService] lastLogin 저장 실패: {e.Message}",
                DebugType.Data);
        }
    }

    public static async Task RecordLogoutAsync()
    {
        if (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
        {
            DebugTool.Warning(
                "[UserSessionTimeService] Firestore가 초기화되지 않아 lastLogout 저장을 건너뜁니다.",
                DebugType.Data);
            return;
        }

        if (!FireStoreManager.Instance.TryGetStore(out UsersSO usersSO) || usersSO == null)
        {
            DebugTool.Warning(
                "[UserSessionTimeService] UsersSO를 찾지 못해 lastLogout 저장을 건너뜁니다.",
                DebugType.Data);
            return;
        }

        try
        {
            await usersSO.RecordLogoutAsync();
        }
        catch (Exception e)
        {
            DebugTool.Warning(
                $"[UserSessionTimeService] lastLogout 저장 실패: {e.Message}",
                DebugType.Data);
        }
    }
}
