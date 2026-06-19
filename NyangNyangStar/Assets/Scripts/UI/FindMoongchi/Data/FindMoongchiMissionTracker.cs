using System.Collections.Generic;

namespace Data.ScriptableObjects.MoongchiSO
{
    // MoongchiMissionTriggerResolver로 MissionContent와 트리거를 매칭하고
    // 현재 주차에 해당하는 미션만 FindMoongchiProgressHelper로 진행도를 갱신합
    
    // EnergySpend    - 에너지 소비 (NotifyEnergySpentAsync)
    // UseSearchTool  - 탐색 도구 사용
    // OpenTile       - 타일 신규 공개
    // FindTarget     - 일반 목표물 발견
    // FindMoongchi   - 메인 목표(뭉치) 발견
    // StageClear     - 스테이지 클리어
    public static class FindMoongchiMissionTracker
    {
        // trigger에 해당하는 모든 활성 미션 진행도를 amount만큼 증가
        // 하나라도 변경되면 true (이후 DataManager가 Firestore 저장)
        public static bool Track(
            MoongchiMissionSO missionSO,
            FindMoongchiProgressRuntimeData progress,
            MoongchiMissionTrigger trigger,
            int amount = 1)
        {
            if (missionSO == null || progress == null || trigger == MoongchiMissionTrigger.None || amount <= 0)
                return false;

            IReadOnlyList<MoongchiMissionData> missions = missionSO.Missions;
            bool changed = false;

            for (int i = 0; i < missions.Count; i++)
            {
                MoongchiMissionData mission = missions[i];

                if (mission == null)
                    continue;

                if (MoongchiMissionTriggerResolver.Resolve(mission) != trigger)
                    continue;

                if (!MoongchiMissionTriggerResolver.IsMissionActiveForWeek(mission, progress.CurrentWeek))
                    continue;

                if (FindMoongchiProgressHelper.TryAddMissionProgress(
                        progress,
                        mission.ID,
                        amount,
                        mission.TargetAmount))
                {
                    changed = true;
                }
            }

            return changed;
        }

        // 도구 사용으로 새로 발견한 목표물마다 미션 트래킹
        // 메인 목표 -> FindMoongchi, 일반 목표 -> FindTarget
        public static bool TrackNewlyFoundTargets(
            MoongchiMissionSO missionSO,
            FindMoongchiProgressRuntimeData progress,
            IEnumerable<FindMoongchiTargetTrackInfo> newlyFoundTargets,
            bool includeGenericTargetMissions = true)
        {
            if (newlyFoundTargets == null)
                return false;

            bool changed = false;

            foreach (FindMoongchiTargetTrackInfo target in newlyFoundTargets)
            {
                if (target == null)
                    continue;

                if (target.IsMainTarget)
                {
                    if (Track(missionSO, progress, MoongchiMissionTrigger.FindMoongchi, 1))
                        changed = true;

                    continue;
                }

                if (TrackSpecificTargetMission(missionSO, progress, target, includeGenericTargetMissions))
                    changed = true;
            }

            return changed;
        }

        private static bool TrackSpecificTargetMission(
            MoongchiMissionSO missionSO,
            FindMoongchiProgressRuntimeData progress,
            FindMoongchiTargetTrackInfo target,
            bool includeGenericTargetMissions)
        {
            if (missionSO == null || progress == null || target == null)
                return false;

            IReadOnlyList<MoongchiMissionData> missions = missionSO.Missions;
            bool changed = false;

            for (int i = 0; i < missions.Count; i++)
            {
                MoongchiMissionData mission = missions[i];

                if (mission == null)
                    continue;

                if (!MoongchiMissionTriggerResolver.IsMissionActiveForWeek(mission, progress.CurrentWeek))
                    continue;

                if (!IsTargetMissionFor(mission, target, includeGenericTargetMissions))
                    continue;

                if (FindMoongchiProgressHelper.TryAddMissionProgress(
                        progress,
                        mission.ID,
                        1,
                        mission.TargetAmount))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private static bool IsTargetMissionFor(
            MoongchiMissionData mission,
            FindMoongchiTargetTrackInfo target,
            bool includeGenericTargetMissions)
        {
            if (mission == null || target == null)
                return false;

            MoongchiMissionTrigger trigger = MoongchiMissionTriggerResolver.Resolve(mission);

            if (trigger == MoongchiMissionTrigger.FindMoongchi)
                return false;

            if (MissionMentionsTarget(mission.MissionContent, target))
                return true;

            return includeGenericTargetMissions &&
                   trigger == MoongchiMissionTrigger.FindTarget &&
                   IsGenericTargetMission(mission.MissionContent);
        }

        private static bool MissionMentionsTarget(string missionContent, FindMoongchiTargetTrackInfo target)
        {
            string normalizedContent = NormalizeText(missionContent);

            if (string.IsNullOrEmpty(normalizedContent) || target == null)
                return false;

            string normalizedTargetName = NormalizeText(target.TargetName);

            if (!string.IsNullOrEmpty(normalizedTargetName) &&
                normalizedContent.Contains(normalizedTargetName))
            {
                return true;
            }

            if (!TryGetTargetKeywords(target.TargetId, out string[] keywords))
                return false;

            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = NormalizeText(keywords[i]);

                if (!string.IsNullOrEmpty(keyword) &&
                    normalizedContent.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGenericTargetMission(string missionContent)
        {
            string normalizedContent = NormalizeText(missionContent);

            return normalizedContent.Contains("\uBAA9\uD45C\uBB3C") ||
                   normalizedContent.Contains("\uC11C\uBE0C\uBAA9\uD45C") ||
                   normalizedContent.Contains("findtarget");
        }

        private static bool TryGetTargetKeywords(int targetId, out string[] keywords)
        {
            switch (targetId)
            {
                case 1002:
                    keywords = new[] { "\uC2A4\uB9C8\uD2B8\uD3F0" };
                    return true;
                case 1003:
                    keywords = new[] { "\uBB34\uC120\uC774\uC5B4\uD3F0\uCF00\uC774\uC2A4", "\uC774\uC5B4\uD3F0" };
                    return true;
                case 2002:
                    keywords = new[] { "\uB2E4\uC774\uC5B4\uB9AC" };
                    return true;
                case 2003:
                    keywords = new[] { "\uBE57", "\uD5E4\uC5B4\uBE0C\uB7EC\uC2DC" };
                    return true;
                case 3002:
                    keywords = new[] { "\uC591\uB9D0", "\uC591\uB9D0\uAFB8\uB7EC\uBBF8" };
                    return true;
                case 3003:
                    keywords = new[] { "\uBCFC\uD39C" };
                    return true;
                case 4002:
                    keywords = new[] { "tv\uB9AC\uBAA8\uCEE8", "\uB9AC\uBAA8\uCEE8" };
                    return true;
                case 4003:
                    keywords = new[] { "\uC548\uACBD" };
                    return true;
                case 5002:
                    keywords = new[] { "\uC5F4\uC1E0", "\uC5F4\uC1E0\uAFB8\uB7EC\uBBF8" };
                    return true;
                case 5003:
                    keywords = new[] { "\uC9C0\uAC11" };
                    return true;
                case 6002:
                    keywords = new[] { "\uBAA8\uC790" };
                    return true;
                case 6003:
                    keywords = new[] { "\uB9BD\uBC24" };
                    return true;
                case 7002:
                    keywords = new[] { "\uC5D0\uCF54\uBC31" };
                    return true;
                case 7003:
                    keywords = new[] { "\uBCF4\uC870\uBC30\uD130\uB9AC", "\uBC30\uD130\uB9AC" };
                    return true;
                case 8002:
                    keywords = new[] { "\uCDA9\uC804\uCF00\uC774\uBE14", "\uCF00\uC774\uBE14" };
                    return true;
                case 8003:
                    keywords = new[] { "\uD578\uB4DC\uD06C\uB9BC" };
                    return true;
                case 9002:
                    keywords = new[] { "\uBB34\uC120\uB9C8\uC6B0\uC2A4", "\uB9C8\uC6B0\uC2A4" };
                    return true;
                case 9003:
                    keywords = new[] { "\uC0AC\uC6D0\uC99D" };
                    return true;
                case 10002:
                    keywords = new[] { "\uBB3C\uD2F0\uC288" };
                    return true;
                case 10003:
                    keywords = new[] { "\uC190\uD1B1\uAE4E\uC774" };
                    return true;
                default:
                    keywords = null;
                    return false;
            }
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
        }
    }

    // TrackNewlyFoundTargets 입력용. FindMoongchiUseToolResult.NewlyFoundTargets에서 변환
    public sealed class FindMoongchiTargetTrackInfo
    {
        public int TargetId { get; }
        public bool IsMainTarget { get; }
        public string TargetName { get; }

        public FindMoongchiTargetTrackInfo(int targetId, bool isMainTarget)
            : this(targetId, isMainTarget, null)
        {
        }

        public FindMoongchiTargetTrackInfo(int targetId, bool isMainTarget, string targetName)
        {
            TargetId = targetId;
            IsMainTarget = isMainTarget;
            TargetName = targetName;
        }
    }
}
