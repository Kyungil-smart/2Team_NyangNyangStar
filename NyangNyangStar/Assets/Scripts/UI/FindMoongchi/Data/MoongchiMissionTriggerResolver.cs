using System;

namespace Data.ScriptableObjects.MoongchiSO
{
    // FindMoongchiMissionTracker는 그 행동에 맞는 미션만 진행도를 올려야 하는데,
    // 시트에는 문자열만 있으니까 중간에서 enum으로 바꿔 주는 게 이 Resolver
    public static class MoongchiMissionTriggerResolver
    {
        public static MoongchiMissionTrigger Resolve(MoongchiMissionData mission)
        {
            if (mission == null || string.IsNullOrWhiteSpace(mission.MissionContent))
                return MoongchiMissionTrigger.None;

            string content = mission.MissionContent.Trim();

            if (Enum.TryParse(content, true, out MoongchiMissionTrigger explicitTrigger))
                return explicitTrigger;

            if (content.StartsWith("TRIGGER :", StringComparison.OrdinalIgnoreCase))
            {
                string triggerName = content.Substring("TRIGGER :".Length).Trim();

                if (Enum.TryParse(triggerName, true, out MoongchiMissionTrigger parsedTrigger))
                    return parsedTrigger;
            }

            string lower = content.ToLowerInvariant();
            
            // 시트 미션 설명 문장에 "에너지", "탐색", "뭉치" 같은 단어가 포함 
            // ->  해당 종류(에너지 / 도구 / 뭉chi 찾기 등) 로 분류하는 문자열 매칭

            if (ContainsAny(lower, "energy_spend", "에너지"))
                return MoongchiMissionTrigger.EnergySpend;

            if (ContainsAny(lower, "use_search_tool", "탐색", "도구 사용", "도구를 사용"))
                return MoongchiMissionTrigger.UseSearchTool;

            if (ContainsAny(lower, "find_moongchi", "뭉치", "메인 목표"))
                return MoongchiMissionTrigger.FindMoongchi;

            if (ContainsAny(lower, "find_target", "목표물", "목표 발견", "숨은"))
                return MoongchiMissionTrigger.FindTarget;

            if (ContainsAny(lower, "open_tile", "타일", "칸"))
                return MoongchiMissionTrigger.OpenTile;

            if (ContainsAny(lower, "stage_clear", "스테이지", "클리어", "완료"))
                return MoongchiMissionTrigger.StageClear;

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
            for (int i = 0; i < keywords.Length; i++)
            {
                if (source.Contains(keywords[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
