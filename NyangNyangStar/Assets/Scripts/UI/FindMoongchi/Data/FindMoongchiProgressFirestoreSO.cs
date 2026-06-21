using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UI.FindMoongchi;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // Firestore 유저 진행 SO. UI 직접 접근 금지
    [FirestorePath("Users/{userId}/FindMoongchiProgress/{docId}")]
    [CreateAssetMenu(fileName = "FindMoongchiProgressFirestoreSO", menuName = "SO/FindMoongchi/FindMoongchiProgressFirestoreSO", order = 3)]
    public class FindMoongchiProgressFirestoreSO : BaseFireStore
    {
        private const int DefaultWeek = 1;

        [Header("스테이지 진행 상태")]
        [SerializeField] private int _currentWeek = DefaultWeek;
        [SerializeField] private int _currentStageID;
        [SerializeField] private List<int> _currentCycleStageIDs = new();
        [SerializeField] private int _currentCycleIndex;

        [Header("게임판 진행 상태")]
        [SerializeField] private List<int> _openedTileIDs = new();
        [SerializeField] private List<int> _foundTargetIDs = new();

        [Header("재화/탐색 기회")]
        [SerializeField] private int _searchChance = FindMoongchiConstants.DailySearchChance;
        [SerializeField] private int _todayBonusSearchChanceCount;
        [SerializeField] private int _eventCurrency;
        [SerializeField] private int _dailyEnergySpendProgress;

        [Header("미션 / 상점 진행 상태")]
        [FirestoreMap]
        [SerializeField] private List<FindMoongchiMissionProgressData> _missionProgresses = new();

        [FirestoreMap]
        [SerializeField] private List<FindMoongchiShopPurchaseData> _shopPurchaseCounts = new();

        [FirestoreMap]
        [SerializeField] private List<int> _ownedProfileIDs = new();

        [Header("초기화 기준 시간")]
        [SerializeField] private long _lastDailyResetUnixTime;
        [SerializeField] private long _lastWeeklyResetUnixTime;

        public int CurrentWeek => _currentWeek;
        public int CurrentStageID => _currentStageID;
        public IReadOnlyList<int> CurrentCycleStageIDs => _currentCycleStageIDs;
        public int CurrentCycleIndex => _currentCycleIndex;
        public IReadOnlyList<int> OpenedTileIDs => _openedTileIDs;
        public IReadOnlyList<int> FoundTargetIDs => _foundTargetIDs;
        public int SearchChance => _searchChance;
        public int TodayBonusSearchChanceCount => _todayBonusSearchChanceCount;
        public int EventCurrency => _eventCurrency;
        public int DailyEnergySpendProgress => _dailyEnergySpendProgress;
        public IReadOnlyList<FindMoongchiMissionProgressData> MissionProgresses => _missionProgresses;
        public IReadOnlyList<FindMoongchiShopPurchaseData> ShopPurchaseCounts => _shopPurchaseCounts;
        public IReadOnlyList<int> OwnedProfileIDs => _ownedProfileIDs;
        public long LastDailyResetUnixTime => _lastDailyResetUnixTime;
        public long LastWeeklyResetUnixTime => _lastWeeklyResetUnixTime;

        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            InitDataBase(database, userId);
            ResetToDefault();
            await SetDataAsync(ToFirestoreDictionary());
        }

        // 서버 문서 조회, 없으면 기본값으로 신규 생성
        public async Task<bool> LoadOrCreateFromServerAsync()
        {
            DocumentSnapshot snapshot = await UpdateFromServerAsync(false);
            if (snapshot.Exists)
                return true;

            await CreateNew(db);
            return true;
        }

        // Firestore에서 읽은 SO 값을 UI/게임 로직용 RuntimeData로 복사
        public void ApplyTo(FindMoongchiProgressRuntimeData progress)
        {
            if (progress == null)
                return;

            progress.CurrentWeek = _currentWeek;
            progress.CurrentStageID = _currentStageID;
            progress.CurrentCycleIndex = _currentCycleIndex;
            CopyList(_currentCycleStageIDs, progress.CurrentCycleStageIDs);

            CopyList(_openedTileIDs, progress.OpenedTileIDs);
            CopyList(_foundTargetIDs, progress.FoundTargetIDs);

            progress.SearchChance = _searchChance;
            progress.TodayBonusSearchChanceCount = _todayBonusSearchChanceCount;
            progress.EventCurrency = _eventCurrency;
            progress.DailyEnergySpendProgress = _dailyEnergySpendProgress;

            CopyList(_missionProgresses, progress.MissionProgresses);
            CopyList(_shopPurchaseCounts, progress.ShopPurchaseCounts);
            CopyList(_ownedProfileIDs, progress.OwnedProfileIDs);

            progress.LastDailyResetUnixTime = _lastDailyResetUnixTime;
            progress.LastWeeklyResetUnixTime = _lastWeeklyResetUnixTime;
        }

        // UI/게임 로직이 갱신한 RuntimeData 값을 Firestore 저장용 SO 필드로 복사
        public void CaptureFrom(FindMoongchiProgressRuntimeData progress)
        {
            if (progress == null)
                return;

            _currentWeek = progress.CurrentWeek;
            _currentStageID = progress.CurrentStageID;
            _currentCycleIndex = progress.CurrentCycleIndex;
            CopyList(progress.CurrentCycleStageIDs, _currentCycleStageIDs);

            CopyList(progress.OpenedTileIDs, _openedTileIDs);
            CopyList(progress.FoundTargetIDs, _foundTargetIDs);

            _searchChance = progress.SearchChance;
            _todayBonusSearchChanceCount = progress.TodayBonusSearchChanceCount;
            _eventCurrency = progress.EventCurrency;
            _dailyEnergySpendProgress = progress.DailyEnergySpendProgress;

            CopyList(progress.MissionProgresses, _missionProgresses);
            CopyList(progress.ShopPurchaseCounts, _shopPurchaseCounts);
            CopyList(progress.OwnedProfileIDs, _ownedProfileIDs);

            _lastDailyResetUnixTime = progress.LastDailyResetUnixTime;
            _lastWeeklyResetUnixTime = progress.LastWeeklyResetUnixTime;
        }

        private void ResetToDefault()
        {
            _currentWeek = DefaultWeek;
            _currentStageID = 0;
            _currentCycleStageIDs.Clear();
            _currentCycleIndex = 0;

            _openedTileIDs.Clear();
            _foundTargetIDs.Clear();

            _searchChance = FindMoongchiConstants.DailySearchChance;
            _todayBonusSearchChanceCount = 0;
            _eventCurrency = 0;
            _dailyEnergySpendProgress = 0;

            _missionProgresses.Clear();
            _shopPurchaseCounts.Clear();
            _ownedProfileIDs.Clear();

            _lastDailyResetUnixTime = 0;
            _lastWeeklyResetUnixTime = 0;
        }

        private static void CopyList<T>(IEnumerable<T> source, List<T> target)
        {
            if (target == null)
                return;

            target.Clear();

            if (source == null)
                return;

            target.AddRange(source);
        }
    }
}
