using System;

namespace Data.ScriptableObjects.MoongchiSO
{
    public static class MoongchiMissionTriggerResolver
    {
        private const string Energy = "\uC5D0\uB108\uC9C0";
        private const string Search = "\uD0D0\uC0C9";
        private const string Tool = "\uB3C4\uAD6C";
        private const string Moongchi = "\uBB49\uCE58";
        private const string MainTarget = "\uBA54\uC778\uBAA9\uD45C";
        private const string Target = "\uBAA9\uD45C";
        private const string TargetObject = "\uBAA9\uD45C\uBB3C";
        private const string Find = "\uBC1C\uACAC";
        private const string Tile = "\uD0C0\uC77C";
        private const string Cell = "\uCE78";
        private const string Open = "\uC5F4";
        private const string Reveal = "\uACF5\uAC1C";
        private const string Stage = "\uC2A4\uD14C\uC774\uC9C0";
        private const string Clear = "\uD074\uB9AC\uC5B4";
        private const string Complete = "\uC644\uB8CC";
        private const string Spend = "\uC18C\uBAA8";
        private const string Use = "\uC0AC\uC6A9";

        public static MoongchiMissionTrigger Resolve(MoongchiMissionData mission)
        {
            if (mission == null || string.IsNullOrWhiteSpace(mission.MissionContent))
                return MoongchiMissionTrigger.None;

            string content = mission.MissionContent.Trim();

            if (Enum.TryParse(content, true, out MoongchiMissionTrigger explicitTrigger))
                return explicitTrigger;

            if (content.StartsWith("TRIGGER:", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("TRIGGER :", StringComparison.OrdinalIgnoreCase))
            {
                int separatorIndex = content.IndexOf(':');
                string triggerName = separatorIndex >= 0
                    ? content.Substring(separatorIndex + 1).Trim()
                    : string.Empty;

                if (Enum.TryParse(triggerName, true, out MoongchiMissionTrigger parsedTrigger))
                    return parsedTrigger;
            }

            string normalized = Normalize(content);

            if (ContainsAny(normalized, "energyspend", Energy + Spend, Energy + Use, Energy))
                return MoongchiMissionTrigger.EnergySpend;

            if (ContainsAny(normalized, "usesearchtool", Search + Tool, Tool + Use, Search))
                return MoongchiMissionTrigger.UseSearchTool;

            if (ContainsAny(normalized, "findmoongchi", Moongchi, MainTarget))
                return MoongchiMissionTrigger.FindMoongchi;

            if (ContainsAny(normalized, "opentile", Tile, Cell + Open, Reveal))
                return MoongchiMissionTrigger.OpenTile;

            if (ContainsAny(normalized, "stageclear", Stage + Clear, Stage + Complete, Clear))
                return MoongchiMissionTrigger.StageClear;

            if (ContainsAny(normalized, "findtarget", TargetObject, Target + Find, Find))
                return MoongchiMissionTrigger.FindTarget;

            return MoongchiMissionTrigger.None;
        }

        public static bool IsMissionActiveForWeek(MoongchiMissionData mission, int currentWeek)
        {
            if (mission == null)
                return false;

            return mission.MissionType switch
            {
                MoongchiMissionType.DAILY => true,
                MoongchiMissionType.WEEKLY => currentWeek >= 1,
                MoongchiMissionType.WEEKLY_1ST => currentWeek == 1,
                MoongchiMissionType.WEEKLY_2ND => currentWeek >= 2,
                _ => false
            };
        }

        private static bool ContainsAny(string source, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(source) || keywords == null)
                return false;

            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = Normalize(keywords[i]);

                if (!string.IsNullOrEmpty(keyword) &&
                    source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
        }
    }
}
