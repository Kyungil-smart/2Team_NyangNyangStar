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
            IEnumerable<FindMoongchiTargetTrackInfo> newlyFoundTargets)
        {
            if (newlyFoundTargets == null)
                return false;

            bool changed = false;

            foreach (FindMoongchiTargetTrackInfo target in newlyFoundTargets)
            {
                if (target == null)
                    continue;

                MoongchiMissionTrigger trigger = target.IsMainTarget
                    ? MoongchiMissionTrigger.FindMoongchi
                    : MoongchiMissionTrigger.FindTarget;

                if (Track(missionSO, progress, trigger, 1))
                    changed = true;
            }

            return changed;
        }
    }

    // TrackNewlyFoundTargets 입력용. FindMoongchiUseToolResult.NewlyFoundTargets에서 변환
    public sealed class FindMoongchiTargetTrackInfo
    {
        public int TargetId { get; }
        public bool IsMainTarget { get; }

        public FindMoongchiTargetTrackInfo(int targetId, bool isMainTarget)
        {
            TargetId = targetId;
            IsMainTarget = isMainTarget;
        }
    }
}
