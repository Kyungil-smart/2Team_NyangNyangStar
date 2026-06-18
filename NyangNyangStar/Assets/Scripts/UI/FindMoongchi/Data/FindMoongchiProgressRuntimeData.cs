using System.Collections.Generic;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 런타임 진행 모델. UI는 ProgressController 경유
    public sealed class FindMoongchiProgressRuntimeData
    {
        // 현재 진행 중인 주차와 스테이지 정보
        // CurrentCycleStageIDs / CurrentCycleIndex는 주차별 5개 스테이지
        public int CurrentWeek = 1;
        public int CurrentStageID;
        public List<int> CurrentCycleStageIDs = new();
        public int CurrentCycleIndex;

        // 현재 스테이지에서 이미 열린 타일과 발견 완료된 목표물 ID 목록
        public List<int> OpenedTileIDs = new();
        public List<int> FoundTargetIDs = new();

        // 탐색 기회, 일일 추가 획득 횟수, 이벤트 전용 재화
        public int SearchChance;
        public int TodayBonusSearchChanceCount;
        public int EventCurrency;

        // 일일 에너지 소비 누적 (탐색 기회 보너스 UI용)
        public int DailyEnergySpendProgress;

        // 상점에서 구매한 프로필 ID 목록
        public List<int> OwnedProfileIDs = new();

        // 미션 진행도/보상 수령 여부와 상점 상품별 구매 횟수
        public List<FindMoongchiMissionProgressData> MissionProgresses = new();
        public List<FindMoongchiShopPurchaseData> ShopPurchaseCounts = new();

        // 일일/주간 초기화 여부 판단에 사용하는 마지막 초기화 시각, UnixTimeSeconds 기준
        public long LastDailyResetUnixTime;
        public long LastWeeklyResetUnixTime;
    }
}
