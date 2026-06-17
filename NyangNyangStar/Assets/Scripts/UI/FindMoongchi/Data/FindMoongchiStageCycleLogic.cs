using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 스테이지 진행 결과
    public enum FindMoongchiStageAdvanceResult
    {
        Invalid,
        MovedToNextStage,
        StartedNewCycle
    }

    // 기획서 기준 주차별 5개 스테이지 랜덤 순서 및 사이클 진행 로직 (현재 하드 코딩 값 사용 중 으로 보임)
    // RuntimeData만 갱신하며, Firestore 저장은 호출자가 SaveProgressAsync로 처리
    public static class FindMoongchiStageCycleLogic
    {
        private const int StagesPerCycle = 5;
        private const int MinWeek = 1;
        private const int MaxWeek = 2;

        private static readonly int[] Week1StagePool = { 1, 2, 3, 4, 5 };
        private static readonly int[] Week2StagePool = { 6, 7, 8, 9, 10 };


        // 스테이지 사이클 준비 여부 확인
        // 준비되지 않았다면 새로운 사이클 시작
        // 준비되었다면 현재 스테이지 ID 동기화
        public static void EnsureStageCycleReady(FindMoongchiProgressRuntimeData progress)
        {
            if (progress == null)
                return;

            progress.CurrentWeek = ClampWeek(progress.CurrentWeek);

            if (!IsCycleValid(progress))
                StartNewCycle(progress);
            else
                SyncCurrentStageId(progress);
        }

        // 현재 스테이지 ID 반환
        public static int GetCurrentStageId(FindMoongchiProgressRuntimeData progress)
        {
            if (progress == null)
                return 0;

            EnsureStageCycleReady(progress);
            return progress.CurrentStageID;
        }

        // 스테이지 클리어 시 스테이지 진행 결과 반환
        public static FindMoongchiStageAdvanceResult AdvanceStageOnClear(FindMoongchiProgressRuntimeData progress)
        {
            if (progress == null)
                return FindMoongchiStageAdvanceResult.Invalid;

            EnsureStageCycleReady(progress);
            ClearBoardProgress(progress);

            progress.CurrentCycleIndex++;

            // 사이클 종료 시 새로운 사이클 시작
            if (progress.CurrentCycleIndex >= StagesPerCycle)
            {
                StartNewCycle(progress);
                return FindMoongchiStageAdvanceResult.StartedNewCycle;
            }
            // 다음 스테이지로 이동
            progress.CurrentStageID = progress.CurrentCycleStageIDs[progress.CurrentCycleIndex];
            return FindMoongchiStageAdvanceResult.MovedToNextStage;
        }


        // 새로운 사이클 시작
        // 해당 주차에 해당하는 스테이지 풀에서 랜덤 순서로 5개 스테이지 선택
        private static void StartNewCycle(FindMoongchiProgressRuntimeData progress)
        {
            int[] pool = GetStagePool(progress.CurrentWeek);
            List<int> shuffled = new List<int>(pool);
            Shuffle(shuffled);

            progress.CurrentCycleStageIDs.Clear();
            progress.CurrentCycleStageIDs.AddRange(shuffled);
            progress.CurrentCycleIndex = 0;
            progress.CurrentStageID = shuffled[0];

            ClearBoardProgress(progress);
        }

        // 사이클 유효성
        private static bool IsCycleValid(FindMoongchiProgressRuntimeData progress)
        {
            if (progress.CurrentCycleStageIDs == null ||
                progress.CurrentCycleStageIDs.Count != StagesPerCycle)
                return false;

            if (progress.CurrentCycleIndex < 0 || progress.CurrentCycleIndex >= StagesPerCycle)
                return false;

            int[] pool = GetStagePool(progress.CurrentWeek);
            HashSet<int> expected = new HashSet<int>(pool);
            HashSet<int> actual = new HashSet<int>();

            for (int i = 0; i < progress.CurrentCycleStageIDs.Count; i++)
            {
                int stageId = progress.CurrentCycleStageIDs[i];

                if (!expected.Contains(stageId))
                    return false;

                if (!actual.Add(stageId))
                    return false;
            }

            return true;
        }
        // 현재 스테이지 ID 동기화
        // 현재 스테이지 ID가 사이클 내 스테이지 ID와 일치하지 않으면 첫 스테이지로 이동
        // -> 현재 스테이지 ID 유지 -> 주차 내 스테이지 순서 변경 시 유지 
        private static void SyncCurrentStageId(FindMoongchiProgressRuntimeData progress)
        {
            if (progress.CurrentCycleIndex < 0 || progress.CurrentCycleIndex >= progress.CurrentCycleStageIDs.Count)
            {
                progress.CurrentCycleIndex = 0;
            }

            int expectedStageId = progress.CurrentCycleStageIDs[progress.CurrentCycleIndex];

            if (progress.CurrentStageID != expectedStageId)
                progress.CurrentStageID = expectedStageId;
        }
        // 게임판 진행 상태 초기화
        private static void ClearBoardProgress(FindMoongchiProgressRuntimeData progress)
        {
            progress.OpenedTileIDs?.Clear();
            progress.FoundTargetIDs?.Clear();
        }

        // 주차에 해당하는 스테이지 풀 반환 
        private static int[] GetStagePool(int week)
        {
            return week >= MaxWeek ? Week2StagePool : Week1StagePool;
        }
        // 주차 클램프
        private static int ClampWeek(int week)
        {
            if (week < MinWeek)
                return MinWeek;

            if (week > MaxWeek)
                return MaxWeek;

            return week;
        }
        // 랜덤 순서로 섞기
        private static void Shuffle(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }
    }
}
