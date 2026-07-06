using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.LibrarySystem;
using Data.Loader;
using Data.ScriptableObjects.MoongchiSO;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.FindMoongchi
{
    // 시트 SO + Firestore 진행 데이터 허브. UI는 FindMoongchiProgressController 사용
    public class FindMoongchiDataManager : MonoBehaviour
    {
        [Header("상점/보상 데이터")] [SerializeField] private MoongchiShopSO _shopSO;

        [Header("숨바꼭질 미션 데이터")] [SerializeField] private MoongchiMissionSO _missionSO;

        [Header("프로필 데이터")]
        [SerializeField] private MoongchiProfileSO _profileSO;

        [Header("유저 진행 데이터")] [SerializeField] private FindMoongchiProgressFirestoreSO _progressSO;

        [Header("이벤트 기간")]
        [SerializeField] private FindMoongchiEventScheduleSO _eventScheduleSO;

        [Header("보상 지급")]
        [Tooltip("에너지/골드 지급에 사용합니다. UsersSO 서브컬렉션 ResourcesSO를 연결하세요.")]
        [SerializeField] private ResourcesSO _resourcesSO;

        [Header("로드 상태 옵션")]
        [FormerlySerializedAs("_loadOnStart")]
        [SerializeField] private bool _notifyLoadedOnStart = true;
        [SerializeField] private bool _warnWhenStaticDataMissing = true;

        public MoongchiShopSO ShopSO => _shopSO;
        public MoongchiMissionSO MissionSO => _missionSO;
        public MoongchiProfileSO ProfileSO => _profileSO;
        public FindMoongchiProgressFirestoreSO ProgressSO => _progressSO;
        public FindMoongchiEventScheduleSO EventScheduleSO => _eventScheduleSO;

        public bool IsLoaded { get; private set; }

        public event Action OnLoadCompleted;
        public event Action OnProgressReady;
        public event Action OnProgressChanged;

        private FindMoongchiProgressRuntimeData _progress;
        private bool _isProgressReady;
        private int _progressLoadVersion;
        private string _progressUserId = string.Empty;
        private string _loadingProgressUserId = string.Empty;

        private bool _isWaitingForGlobalDataReady;

        private static readonly IReadOnlyList<MoongchiMissionData> EmptyMissions =
            Array.Empty<MoongchiMissionData>();
        private static readonly IReadOnlyList<MoongchiShopItemData> EmptyShopItems =
            Array.Empty<MoongchiShopItemData>();
        private static readonly IReadOnlyList<MoongchiProfileData> EmptyProfiles =
            Array.Empty<MoongchiProfileData>();

        private sealed class ToolUseProgressSnapshot
        {
            public List<int> OpenedTileIDs;
            public List<int> FoundTargetIDs;
            public List<FindMoongchiMissionProgressData> MissionProgresses;
        }

        public FindMoongchiProgressRuntimeData Progress => _progress;
        public bool IsProgressReady => _isProgressReady && _progress != null;

        private void Start()
        {
            if (_notifyLoadedOnStart)
                NotifyWhenSheetLoaderReady();

            SubscribeAuthChanges();
        }

        [ContextMenu("FindMoongchi 시트 로드 완료 알림")]
        public void LoadAll()
        {
            // 실제 시트 다운로드는 공용 SheetLoader 담당
            // 기존 UI 흐름이 기다리는 로드 완료 이벤트 유지
            IsLoaded = false;

            ResolveStaticDataReferences();
            ValidateStaticDataReferences();

            _shopSO?.PrintData();
            _missionSO?.PrintData();
            _profileSO?.PrintData();

            IsLoaded = true;
            OnLoadCompleted?.Invoke();

            DebugTool.Log("[FindMoongchiDataManager] SheetLoader 정적 데이터 확인 완료", DebugType.Data, this);
        }

        private void NotifyWhenSheetLoaderReady()
        {
            if (LocalDataAccess.Instance?.Game == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] LocalDataAccess가 없어 현재 SO 상태로 로드 완료를 알립니다.", DebugType.Data, this);
                LoadAll();
                return;
            }

            _isWaitingForGlobalDataReady = true;
            LocalDataAccess.Instance.Game.OnReady -= HandleGlobalDataReady;
            LocalDataAccess.Instance.Game.OnReady += HandleGlobalDataReady;
        }

        private void HandleGlobalDataReady()
        {
            UnsubscribeGlobalDataReady();
            LoadAll();
        }

        private void OnDestroy()
        {
            UnsubscribeGlobalDataReady();
            UnsubscribeAuthChanges();
            InvalidateProgress();
        }

        private void SubscribeAuthChanges()
        {
            if (AuthManager.Instance == null)
                return;

            AuthManager.Instance.OnUserIdChanged -= HandleUserIdChanged;
            AuthManager.Instance.OnUserIdChanged += HandleUserIdChanged;
        }

        private void UnsubscribeAuthChanges()
        {
            if (AuthManager.Instance == null)
                return;

            AuthManager.Instance.OnUserIdChanged -= HandleUserIdChanged;
        }

        private void HandleUserIdChanged(string userId)
        {
            string normalizedUserId = NormalizeUserId(userId);

            if (string.Equals(normalizedUserId, _progressUserId, StringComparison.Ordinal) ||
                string.Equals(normalizedUserId, _loadingProgressUserId, StringComparison.Ordinal))
            {
                DebugTool.Log($"[FindMoongchiDataManager] 동일 유저 진행 데이터 무효화 생략: UserId={normalizedUserId}", DebugType.Data, this);
                return;
            }

            _progressUserId = normalizedUserId;
            InvalidateProgress();
        }

        private void UnsubscribeGlobalDataReady()
        {
            if (!_isWaitingForGlobalDataReady || LocalDataAccess.Instance?.Game == null)
                return;

            LocalDataAccess.Instance.Game.OnReady -= HandleGlobalDataReady;
            _isWaitingForGlobalDataReady = false;
        }

        // 이벤트 팝업 진입 시 호출. 서버에서 진행 데이터를 읽고 RuntimeData를 준비한다.
        public async Task<bool> EnsureProgressLoadedAsync()
        {
            if (_isProgressReady && _progress != null)
                return true;

            string loadUserId = GetCurrentProgressUserId();
            _loadingProgressUserId = loadUserId;

            int loadVersion = ++_progressLoadVersion;
            FindMoongchiProgressRuntimeData loaded = await LoadProgressAsync();
            string currentUserId = GetCurrentProgressUserId();

            if (loadVersion == _progressLoadVersion)
                _loadingProgressUserId = string.Empty;

            if (loadVersion != _progressLoadVersion ||
                !string.Equals(loadUserId, currentUserId, StringComparison.Ordinal))
            {
                DebugTool.Warning(
                    $"[FindMoongchiDataManager] 진행 데이터 로드 중 유저가 변경되어 결과를 폐기합니다. LoadUser={loadUserId}, CurrentUser={currentUserId}",
                    DebugType.Data,
                    this);
                return false;
            }

            if (loaded == null)
                return false;

            bool hadValidCycle = loaded.CurrentCycleStageIDs != null &&
                                 loaded.CurrentCycleStageIDs.Count == 5;

            bool resetApplied = FindMoongchiProgressResetLogic.ApplyResetsIfNeeded(
                loaded,
                _missionSO,
                _eventScheduleSO);

            EnsureStageCycleReady(loaded);
            bool searchChanceNormalized = NormalizeSearchChanceLimits(loaded);

            _progress = loaded;
            _isProgressReady = true;
            _progressUserId = currentUserId;

            bool energyBonusApplied = TryGrantEnergySpendBonus();

            OnProgressReady?.Invoke();

            if (!hadValidCycle || resetApplied || searchChanceNormalized || energyBonusApplied)
                await SaveProgressAsync(_progress);

            return true;
        }

        public void InvalidateProgress()
        {
            _progressLoadVersion++;
            _isProgressReady = false;
            _progress = null;
            _loadingProgressUserId = string.Empty;
        }

        private static string GetCurrentProgressUserId()
        {
            if (FireStoreManager.Instance != null &&
                !string.IsNullOrEmpty(FireStoreManager.Instance.CurrentUserId))
            {
                return FireStoreManager.Instance.CurrentUserId;
            }

            return AuthManager.Instance != null
                ? NormalizeUserId(AuthManager.Instance.CurrentUserId)
                : string.Empty;
        }

        private static string NormalizeUserId(string userId)
        {
            return userId ?? string.Empty;
        }

        // 현재 메모리상 Progress를 Firestore에 저장
        public async Task<bool> PersistProgressAsync()
        {
            if (!_isProgressReady || _progress == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] 저장할 진행 데이터가 준비되지 않았습니다.", DebugType.Data, this);
                return false;
            }

            bool saved = await SaveProgressAsync(_progress);

            if (saved)
                OnProgressChanged?.Invoke();

            return saved;
        }

        public int GetMissionCurrentAmount(int missionId)
        {
            return IsProgressReady
                ? FindMoongchiProgressHelper.GetMissionCurrentAmount(_progress, missionId)
                : 0;
        }

        public bool IsMissionRewardClaimed(int missionId)
        {
            return IsProgressReady &&
                   FindMoongchiProgressHelper.IsMissionRewardClaimed(_progress, missionId);
        }

        public FindMoongchiMissionSlotState GetMissionSlotState(MoongchiMissionData mission)
        {
            if (mission == null)
                return FindMoongchiMissionSlotState.InProgress;

            if (IsMissionRewardClaimed(mission.ID))
                return FindMoongchiMissionSlotState.Claimed;

            int currentAmount = GetMissionCurrentAmount(mission.ID);
            return currentAmount >= mission.TargetAmount
                ? FindMoongchiMissionSlotState.Completed
                : FindMoongchiMissionSlotState.InProgress;
        }

        public bool TryClaimMissionReward(int missionId, out MoongchiMissionData missionData)
        {
            missionData = null;

            if (!IsProgressReady || !TryGetMission(missionId, out missionData) || missionData == null)
                return false;

            if (GetMissionSlotState(missionData) != FindMoongchiMissionSlotState.Completed)
                return false;

            FindMoongchiMissionProgressData entry =
                FindMoongchiProgressHelper.GetOrCreateMissionProgress(_progress, missionId);

            return entry != null && !entry.IsRewardClaimed;
        }

        public int DailyEnergySpendProgress =>
            IsProgressReady ? _progress.DailyEnergySpendProgress : 0;

        public bool HasOwnedProfile(int profileId)
        {
            return IsProgressReady && FindMoongchiProgressHelper.HasOwnedProfile(_progress, profileId);
        }

        public bool TrackEnergySpent(int amount)
        {
            if (!IsProgressReady || amount <= 0)
                return false;

            _progress.DailyEnergySpendProgress = Mathf.Max(0, _progress.DailyEnergySpendProgress) + amount;

            bool changed = true;
            changed |= FindMoongchiMissionTracker.Track(
                _missionSO,
                _progress,
                MoongchiMissionTrigger.EnergySpend,
                amount);

            changed |= TryGrantEnergySpendBonus();
            return changed;
        }

        public bool TrackToolUseResult(FindMoongchiUseToolResult result)
        {
            if (!IsProgressReady || result == null)
                return false;

            bool changed = FindMoongchiMissionTracker.Track(
                _missionSO,
                _progress,
                MoongchiMissionTrigger.UseSearchTool,
                1);

            if (result.NewlyRevealedTileIndices.Count > 0)
            {
                changed |= FindMoongchiMissionTracker.Track(
                    _missionSO,
                    _progress,
                    MoongchiMissionTrigger.OpenTile,
                    result.NewlyRevealedTileIndices.Count);
            }

            if (result.NewlyFoundTargets.Count > 0)
            {
                List<FindMoongchiTargetTrackInfo> trackInfos = new List<FindMoongchiTargetTrackInfo>();

                for (int i = 0; i < result.NewlyFoundTargets.Count; i++)
                {
                    FindMoongchiTargetRuntimeData target = result.NewlyFoundTargets[i];

                    if (target == null)
                        continue;

                    trackInfos.Add(new FindMoongchiTargetTrackInfo(target.TargetId, target.IsMainTarget, target.TargetName));
                }

                changed |= FindMoongchiMissionTracker.TrackNewlyFoundTargets(_missionSO, _progress, trackInfos);
            }

            if (result.IsStageCleared)
            {
                changed |= FindMoongchiMissionTracker.Track(
                    _missionSO,
                    _progress,
                    MoongchiMissionTrigger.StageClear,
                    1);
            }

            return changed;
        }

        public async Task<bool> PersistAfterToolUseAsync(
            FindMoongchiGameLogic gameLogic,
            FindMoongchiUseToolResult result)
        {
            ToolUseProgressSnapshot snapshot = CreateToolUseProgressSnapshot();
            TrackToolUseResult(result);
            CaptureBoardFromGame(gameLogic);

            bool saved = await PersistProgressAsync();

            if (!saved)
                RestoreToolUseProgressSnapshot(snapshot);

            return saved;
        }

        private ToolUseProgressSnapshot CreateToolUseProgressSnapshot()
        {
            if (!IsProgressReady)
                return null;

            return new ToolUseProgressSnapshot
            {
                OpenedTileIDs = CloneIntList(_progress.OpenedTileIDs),
                FoundTargetIDs = CloneIntList(_progress.FoundTargetIDs),
                MissionProgresses = CloneMissionProgresses(_progress.MissionProgresses)
            };
        }

        private void RestoreToolUseProgressSnapshot(ToolUseProgressSnapshot snapshot)
        {
            if (!IsProgressReady || snapshot == null)
                return;

            _progress.OpenedTileIDs ??= new List<int>();
            _progress.OpenedTileIDs.Clear();
            _progress.OpenedTileIDs.AddRange(snapshot.OpenedTileIDs);

            _progress.FoundTargetIDs ??= new List<int>();
            _progress.FoundTargetIDs.Clear();
            _progress.FoundTargetIDs.AddRange(snapshot.FoundTargetIDs);

            _progress.MissionProgresses ??= new List<FindMoongchiMissionProgressData>();
            _progress.MissionProgresses.Clear();
            _progress.MissionProgresses.AddRange(CloneMissionProgresses(snapshot.MissionProgresses));
        }

        private static List<FindMoongchiMissionProgressData> CloneMissionProgresses(
            IReadOnlyList<FindMoongchiMissionProgressData> source)
        {
            List<FindMoongchiMissionProgressData> result = new List<FindMoongchiMissionProgressData>();

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                FindMoongchiMissionProgressData entry = source[i];

                if (entry == null)
                    continue;

                result.Add(new FindMoongchiMissionProgressData(
                    entry.MissionID,
                    entry.CurrentAmount,
                    entry.IsRewardClaimed));
            }

            return result;
        }

        private static List<int> CloneIntList(IReadOnlyList<int> source)
        {
            List<int> result = new List<int>();

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(source[i]);

            return result;
        }

        public async Task<bool> TryClaimMissionAndPersistAsync(int missionId)
        {
            if (!TryClaimMissionReward(missionId, out MoongchiMissionData missionData))
                return false;

            FindMoongchiMissionProgressData entry =
                FindMoongchiProgressHelper.GetOrCreateMissionProgress(_progress, missionId);

            entry.SetRewardClaimed(true);

            bool reward1Granted = await FindMoongchiRewardGrantService.GrantMissionRewardAsync(
                missionData.Reward1,
                _progress,
                _resourcesSO);

            bool reward2Granted = !missionData.HasReward2 ||
                                    await FindMoongchiRewardGrantService.GrantMissionRewardAsync(
                                        missionData.Reward2,
                                        _progress,
                                        _resourcesSO);

            if (!reward1Granted || !reward2Granted)
            {
                entry.SetRewardClaimed(false);
                DebugTool.Warning($"[FindMoongchiDataManager] 미션 보상 지급 실패: MissionId={missionId}", DebugType.Data, this);
                return false;
            }

            return await PersistProgressAsync();
        }

        public async Task<bool> TryPurchaseShopItemAsync(int shopItemId, int count, int totalCost, int limitCount)
        {
            if (!IsProgressReady || shopItemId <= 0 || count <= 0 || totalCost <= 0)
                return false;

            if (!TryGetShopItem(shopItemId, out MoongchiShopItemData shopItem) || shopItem == null)
                return false;

            if (!TrySpendEventCurrency(totalCost))
                return false;

            if (limitCount > 0 && GetShopPurchaseCount(shopItemId) + count > limitCount)
            {
                _progress.EventCurrency += totalCost;
                return false;
            }

            bool granted = await FindMoongchiRewardGrantService.GrantShopProductAsync(
                shopItem,
                count,
                _progress,
                _resourcesSO);

            if (!granted)
            {
                _progress.EventCurrency += totalCost;
                DebugTool.Warning($"[FindMoongchiDataManager] 상점 상품 지급 실패: ShopItemId={shopItemId}", DebugType.Data, this);
                return false;
            }

            RecordShopPurchase(shopItemId, count);
            return await PersistProgressAsync();
        }

        public async Task<bool> PersistBoardStateAsync(FindMoongchiGameLogic gameLogic)
        {
            CaptureBoardFromGame(gameLogic);
            return await PersistProgressAsync();
        }

        public async Task<bool> AdvanceStageAndPersistAsync(FindMoongchiGameLogic gameLogic)
        {
            if (AdvanceStageAfterClear() == FindMoongchiStageAdvanceResult.Invalid)
                return false;

            return await PersistProgressAsync();
        }

        public int GetShopPurchaseCount(int shopItemId)
        {
            return IsProgressReady
                ? FindMoongchiProgressHelper.GetShopPurchaseCount(_progress, shopItemId)
                : 0;
        }

        public void RecordShopPurchase(int shopItemId, int count)
        {
            if (!IsProgressReady || shopItemId <= 0 || count <= 0)
                return;

            FindMoongchiProgressHelper.AddShopPurchaseCount(_progress, shopItemId, count);
        }

        public bool TrySpendEventCurrency(int amount)
        {
            if (!IsProgressReady || amount <= 0 || _progress.EventCurrency < amount)
                return false;

            _progress.EventCurrency -= amount;
            return true;
        }

        public void CaptureBoardFromGame(FindMoongchiGameLogic gameLogic)
        {
            if (!IsProgressReady || gameLogic == null)
                return;

            _progress.OpenedTileIDs.Clear();
            _progress.OpenedTileIDs.AddRange(gameLogic.GetOpenedTileIds());

            _progress.FoundTargetIDs.Clear();
            _progress.FoundTargetIDs.AddRange(gameLogic.GetFoundTargetIds());

            BackfillSpecificTargetMissions(gameLogic);
        }

        private void BackfillSpecificTargetMissions(FindMoongchiGameLogic gameLogic)
        {
            if (!IsProgressReady || gameLogic?.Targets == null)
                return;

            List<FindMoongchiTargetTrackInfo> trackInfos = new List<FindMoongchiTargetTrackInfo>();

            for (int i = 0; i < gameLogic.Targets.Count; i++)
            {
                FindMoongchiTargetRuntimeData target = gameLogic.Targets[i];

                if (target == null || !target.IsFound || target.IsMainTarget)
                    continue;

                trackInfos.Add(new FindMoongchiTargetTrackInfo(target.TargetId, false, target.TargetName));
            }

            if (trackInfos.Count <= 0)
                return;

            FindMoongchiMissionTracker.TrackNewlyFoundTargets(
                _missionSO,
                _progress,
                trackInfos,
                includeGenericTargetMissions: false);
        }

        public void ApplyBoardToGame(FindMoongchiGameLogic gameLogic)
        {
            if (!IsProgressReady || gameLogic == null)
                return;

            gameLogic.RestoreBoardProgress(_progress.OpenedTileIDs, _progress.FoundTargetIDs);
        }

        public FindMoongchiStageAdvanceResult AdvanceStageAfterClear()
        {
            if (!IsProgressReady)
                return FindMoongchiStageAdvanceResult.Invalid;

            return AdvanceStageOnClear(_progress);
        }

        public bool TryConsumeSearchChance(int amount = 1)
        {
            if (!IsProgressReady || amount <= 0 || _progress.SearchChance < amount)
                return false;

            _progress.SearchChance -= amount;
            return true;
        }

        public void RestoreSearchChance(int amount = 1)
        {
            if (!IsProgressReady || amount <= 0)
                return;

            _progress.SearchChance = Mathf.Min(
                FindMoongchiConstants.MaxDailySearchChance,
                _progress.SearchChance + amount);
        }

        public int GetCurrentStageIdFromProgress()
        {
            return IsProgressReady ? GetCurrentStageId(_progress) : 0;
        }

        public void SyncGameLogicFromProgress(FindMoongchiGameLogic gameLogic)
        {
            if (!IsProgressReady || gameLogic == null)
                return;

            EnsureStageCycleReady(_progress);

            int stageId = GetCurrentStageId(_progress);
            gameLogic.LoadStage(stageId);
            ApplyBoardToGame(gameLogic);
        }

        private bool TryGrantEnergySpendBonus()
        {
            if (!IsProgressReady)
                return false;

            bool changed = NormalizeSearchChanceLimits(_progress);
            int target = FindMoongchiConstants.EnergySpendTarget;

            if (target <= 0)
                return changed;

            _progress.DailyEnergySpendProgress = Mathf.Max(0, _progress.DailyEnergySpendProgress);

            if (_progress.DailyEnergySpendProgress < target)
                return changed;

            int remainBonusCount =
                FindMoongchiConstants.MaxDailyBonusSearchChance - _progress.TodayBonusSearchChanceCount;

            if (remainBonusCount <= 0)
            {
                _progress.DailyEnergySpendProgress = 0;
                return true;
            }

            int grantCount = Mathf.Min(_progress.DailyEnergySpendProgress / target, remainBonusCount);
            int remainProgress = _progress.DailyEnergySpendProgress % target;

            if (grantCount <= 0)
                return changed;

            if (grantCount >= remainBonusCount)
                remainProgress = 0;

            _progress.DailyEnergySpendProgress = remainProgress;
            _progress.SearchChance = Mathf.Min(
                FindMoongchiConstants.MaxDailySearchChance,
                _progress.SearchChance + grantCount);
            _progress.TodayBonusSearchChanceCount += grantCount;

            DebugTool.Log(
                $"[FindMoongchiDataManager] 에너지 소비 보너스 탐색 기회 지급: +{grantCount}, SearchChance={_progress.SearchChance}, Progress={_progress.DailyEnergySpendProgress}/{target}",
                DebugType.Data,
                this);

            return true;
        }

        private static bool NormalizeSearchChanceLimits(FindMoongchiProgressRuntimeData progress)
        {
            if (progress == null)
                return false;

            int originalSearchChance = progress.SearchChance;
            int originalBonusCount = progress.TodayBonusSearchChanceCount;
            int originalEnergyProgress = progress.DailyEnergySpendProgress;

            int searchChance = Mathf.Clamp(
                progress.SearchChance,
                0,
                FindMoongchiConstants.MaxDailySearchChance);

            int bonusCount = Mathf.Clamp(
                progress.TodayBonusSearchChanceCount,
                0,
                FindMoongchiConstants.MaxDailyBonusSearchChance);

            int inferredBonusCount = Mathf.Clamp(
                searchChance - FindMoongchiConstants.DailySearchChance,
                0,
                FindMoongchiConstants.MaxDailyBonusSearchChance);

            bonusCount = Mathf.Max(bonusCount, inferredBonusCount);

            int energyProgress = Mathf.Max(0, progress.DailyEnergySpendProgress);

            if (bonusCount >= FindMoongchiConstants.MaxDailyBonusSearchChance)
                energyProgress = 0;

            progress.SearchChance = searchChance;
            progress.TodayBonusSearchChanceCount = bonusCount;
            progress.DailyEnergySpendProgress = energyProgress;

            return originalSearchChance != progress.SearchChance ||
                   originalBonusCount != progress.TodayBonusSearchChanceCount ||
                   originalEnergyProgress != progress.DailyEnergySpendProgress;
        }

        // 이벤트 화면 진입 시 호출하는 진행 데이터 로드 API
        // Firestore 접근 세부 구현은 여기서만 처리, 호출자는 RuntimeData만 사용
        public async Task<FindMoongchiProgressRuntimeData> LoadProgressAsync()
        {
            if (!await WaitForFirestoreReadyAsync())
                return null;

            if (!TryResolveProgressSO(out FindMoongchiProgressFirestoreSO progressSO))
                return null;

            try
            {
                if (!await progressSO.LoadOrCreateFromServerAsync())
                    return null;

                FindMoongchiProgressRuntimeData progressData = new FindMoongchiProgressRuntimeData();
                progressSO.ApplyTo(progressData);

                DebugTool.Log("[FindMoongchiDataManager] 진행 데이터 서버 로드 완료", DebugType.Data, this);
                return progressData;
            }
            catch (Exception e)
            {
                DebugTool.Warning($"[FindMoongchiDataManager] 진행 데이터 서버 로드 실패: {e.Message}", DebugType.Data, this);
                return null;
            }
        }

        // 주차별 5개 스테이지 랜덤 사이클 준비 (진입 시)
        public void EnsureStageCycleReady(FindMoongchiProgressRuntimeData progressData)
        {
            FindMoongchiStageCycleLogic.EnsureStageCycleReady(progressData);
        }

        // 현재 플레이 중인 스테이지 ID (1~10)
        public int GetCurrentStageId(FindMoongchiProgressRuntimeData progressData)
        {
            return FindMoongchiStageCycleLogic.GetCurrentStageId(progressData);
        }

        // 스테이지 클리어 후 다음 스테이지 또는 새 사이클로 진행
        public FindMoongchiStageAdvanceResult AdvanceStageOnClear(FindMoongchiProgressRuntimeData progressData)
        {
            return FindMoongchiStageCycleLogic.AdvanceStageOnClear(progressData);
        }

        // 진행 상태 변경 후 호출하는 저장 API
        // 타일 오픈, 탐색 기회 차감, 미션 보상 수령, 상점 구매 후 RuntimeData 갱신 뒤 저장
        public async Task<bool> SaveProgressAsync(FindMoongchiProgressRuntimeData progressData)
        {
            if (progressData == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] 저장할 진행 데이터가 null입니다.", DebugType.Data, this);
                return false;
            }

            NormalizeSearchChanceLimits(progressData);

            if (!await WaitForFirestoreReadyAsync())
                return false;

            if (!TryResolveProgressSO(out FindMoongchiProgressFirestoreSO progressSO))
                return false;

            try
            {
                progressSO.CaptureFrom(progressData);
                await progressSO.SetDataAsync(progressSO.ToFirestoreDictionary());

                DebugTool.Log("[FindMoongchiDataManager] 진행 데이터 서버 저장 완료", DebugType.Data, this);
                return true;
            }
            catch (Exception e)
            {
                DebugTool.Warning($"[FindMoongchiDataManager] 진행 데이터 서버 저장 실패: {e.Message}", DebugType.Data, this);
                return false;
            }
        }

        // 인스펙터에 직접 연결된 FindMoongchiProgressFirestoreSO 사용 (서브컬렉션 SO는 직접 참조)
        // UI 쪽에서는 FireStoreManager 직접 접근 대신 LoadProgressAsync/SaveProgressAsync만 사용
        private static async Task<bool> WaitForFirestoreReadyAsync(int timeoutMs = 5000)
        {
            const int intervalMs = 100;
            int elapsedMs = 0;

            while (elapsedMs < timeoutMs)
            {
                if (FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized)
                    return true;

                await Task.Delay(intervalMs);
                elapsedMs += intervalMs;
            }

            return FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized;
        }

        private bool TryResolveProgressSO(out FindMoongchiProgressFirestoreSO progressSO)
        {
            progressSO = _progressSO;

            if (FireStoreManager.Instance == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] FireStoreManager.Instance가 없습니다.", DebugType.Data, this);
                return false;
            }

            if (!FireStoreManager.Instance.IsInitialized)
            {
                DebugTool.Warning("[FindMoongchiDataManager] Firestore 초기화 완료 전에는 진행 데이터를 사용할 수 없습니다.", DebugType.Data, this);
                return false;
            }

            if (progressSO == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] FindMoongchiProgressFirestoreSO가 인스펙터에 연결되지 않았습니다.", DebugType.Data, this);
                return false;
            }

            return true;
        }

        // 미니게임 UI용 미션 조회 API

        public IReadOnlyList<MoongchiMissionData> GetDailyMissions()
        {
            // 일일 미션 UI 데이터
            return GetMissionsByType(MoongchiMissionType.DAILY);
        }

        public IReadOnlyList<MoongchiMissionData> GetWeeklyMissions()
        {
            ResolveStaticDataReferences();

            // 주간 미션 UI에서 사용하는 전체 주간 미션 목록
            // WEEKLY, WEEKLY_1ST, WEEKLY_2ND를 한 번에 모아서 반환

            if (_missionSO == null)
                return EmptyMissions;

            List<MoongchiMissionData> result = new List<MoongchiMissionData>();

            AddMissions(result, MoongchiMissionType.WEEKLY);
            AddMissions(result, MoongchiMissionType.WEEKLY_1ST);
            AddMissions(result, MoongchiMissionType.WEEKLY_2ND);

            return result;
        }

        public IReadOnlyList<MoongchiMissionData> GetWeek1Missions()
        {
            // 1주차 전용 미션 UI에서 사용하는 데이터
            return GetMissionsByType(MoongchiMissionType.WEEKLY_1ST);
        }

        public IReadOnlyList<MoongchiMissionData> GetWeek2Missions()
        {
            // 2주차 전용 미션 UI에서 사용하는 데이터
            return GetMissionsByType(MoongchiMissionType.WEEKLY_2ND);
        }

        public IReadOnlyList<MoongchiMissionData> GetMissionsByType(MoongchiMissionType missionType)
        {
            ResolveStaticDataReferences();

            // 원하는 미션 타입만 골라서 가져갈 수 있는 공통 조회 메서드
            // SO가 연결되지 않았거나 로드 전이어도 null 대신 빈 목록을 반환

            if (_missionSO == null)
                return EmptyMissions;

            return _missionSO.GetMissionsByType(missionType);
        }

        public bool TryGetMission(int missionID, out MoongchiMissionData missionData)
        {
            ResolveStaticDataReferences();

            // 특정 미션 ID 하나만 찾아야 할 때 사용하는 메서드

            missionData = null;

            if (_missionSO == null)
                return false;

            return _missionSO.TryGetMission(missionID, out missionData);
        }

        public bool HasMissionData()
        {
            ResolveStaticDataReferences();

            // 미션 UI를 그리기 전에 데이터가 실제로 있는지 확인할 때 사용

            return _missionSO != null &&
                   _missionSO.Missions != null &&
                   _missionSO.Missions.Count > 0;
        }

        private void AddMissions(List<MoongchiMissionData> target, MoongchiMissionType missionType)
        {
            // 여러 미션 타입을 하나의 리스트로 합치기 위한 내부 메서드

            if (target == null || _missionSO == null)
                return;

            List<MoongchiMissionData> missions = _missionSO.GetMissionsByType(missionType);

            for (int i = 0; i < missions.Count; i++)
            {
                target.Add(missions[i]);
            }
        }


        // 상점 UI 상품 조회
        public IReadOnlyList<MoongchiShopItemData> GetShopItems()
        {
            ResolveStaticDataReferences();
            return _shopSO != null ? _shopSO.ShopItems : EmptyShopItems;
        }


        public bool TryGetShopItem(int itemID, out MoongchiShopItemData itemData)
        {
            ResolveStaticDataReferences();

            // 특정 상점 상품 ID 단건 조회
            // 구매 처리용
            itemData = null;
            if (_shopSO == null)
                return false;
            return _shopSO.TryGetShopItem(itemID, out itemData);
        }

        public bool HasShopData()
        {
            ResolveStaticDataReferences();

            // 상점 UI를 그리기 전에 상품 데이터가 실제로 있는지 확인할 때 사용
            return _shopSO != null &&
                   _shopSO.ShopItems != null &&
                   _shopSO.ShopItems.Count > 0;
        }

        // 상점 UI용 프로필 조회 API
        public IReadOnlyList<MoongchiProfileData> GetProfiles()
        {
            ResolveStaticDataReferences();

            // 상점 상품 중 PROFILE 타입 상품을 표시할 때 사용할 프로필 목록
            // 프로필 전체 목록이 필요한 UI에서 사용

            return _profileSO != null ? _profileSO.Profiles : EmptyProfiles;
        }

        public bool TryGetProfile(int profileID, out MoongchiProfileData profileData)
        {
            ResolveStaticDataReferences();

            // 상점 상품의 ProductType이 PROFILE일 때,
            // ProductID를 ProfileID로 사용해서 프로필 정보를 찾는 용도
            profileData = null;
            if (_profileSO == null)
                return false;
            return _profileSO.TryGetProfile(profileID, out profileData);
        }

        public bool HasProfileData()
        {
            ResolveStaticDataReferences();

            // 프로필 보상 데이터가 실제로 있는지 확인할 때 사용
            return _profileSO != null &&
                   _profileSO.Profiles != null &&
                   _profileSO.Profiles.Count > 0;
        }

        private void ResolveStaticDataReferences()
        {
            bool changed = false;

            changed |= TryResolveStaticDataFromGameModule();
            changed |= TryResolveStaticDataFromSheetLoader();

            if (!changed)
                return;

            DebugTool.Log(
                $"[FindMoongchiDataManager] 정적 데이터 SO 재연결 완료 / Shop={GetShopDataCount()}, Mission={GetMissionDataCount()}, Profile={GetProfileDataCount()}",
                DebugType.Data,
                this);
        }

        private bool TryResolveStaticDataFromGameModule()
        {
            if (LocalDataAccess.Instance?.Game == null)
                return false;

            bool changed = false;

            if (LocalDataAccess.Instance.Game.TryGetFindMoongchiShopSO(out MoongchiShopSO shopSO))
                changed |= TryApplyShopSO(shopSO, "GameDataModule");

            if (LocalDataAccess.Instance.Game.TryGetFindMoongchiMissionSO(out MoongchiMissionSO missionSO))
                changed |= TryApplyMissionSO(missionSO, "GameDataModule");

            if (LocalDataAccess.Instance.Game.TryGetFindMoongchiProfileSO(out MoongchiProfileSO profileSO))
                changed |= TryApplyProfileSO(profileSO, "GameDataModule");

            return changed;
        }

        private bool TryResolveStaticDataFromSheetLoader()
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader == null)
                return false;

            bool changed = false;

            if (sheetLoader.TryGetFindMoongchiShopSO(out MoongchiShopSO shopSO))
                changed |= TryApplyShopSO(shopSO, "SheetLoader");

            if (sheetLoader.TryGetFindMoongchiMissionSO(out MoongchiMissionSO missionSO))
                changed |= TryApplyMissionSO(missionSO, "SheetLoader");

            if (sheetLoader.TryGetFindMoongchiProfileSO(out MoongchiProfileSO profileSO))
                changed |= TryApplyProfileSO(profileSO, "SheetLoader");

            return changed;
        }

        private bool TryApplyShopSO(MoongchiShopSO candidate, string source)
        {
            if (candidate == null || ReferenceEquals(_shopSO, candidate))
                return false;

            int currentScore = GetShopDataCount(_shopSO);
            int candidateScore = GetShopDataCount(candidate);

            if (_shopSO != null && currentScore > 0 && currentScore >= candidateScore)
                return false;

            _shopSO = candidate;

            DebugTool.Log(
                $"[FindMoongchiDataManager] 상점 SO 교체 / Source={source}, Count={currentScore}->{candidateScore}",
                DebugType.Data,
                this);

            return true;
        }

        private bool TryApplyMissionSO(MoongchiMissionSO candidate, string source)
        {
            if (candidate == null || ReferenceEquals(_missionSO, candidate))
                return false;

            int currentDataCount = GetMissionDataCount(_missionSO);
            int candidateDataCount = GetMissionDataCount(candidate);
            int currentDisplayCount = GetDisplayMissionCount(_missionSO);
            int candidateDisplayCount = GetDisplayMissionCount(candidate);

            bool shouldReplace =
                _missionSO == null ||
                currentDisplayCount <= 0 && candidateDisplayCount > 0 ||
                candidateDisplayCount > currentDisplayCount ||
                currentDataCount <= 0 && candidateDataCount > 0;

            if (!shouldReplace)
                return false;

            _missionSO = candidate;

            DebugTool.Log(
                $"[FindMoongchiDataManager] 미션 SO 교체 / Source={source}, Data={currentDataCount}->{candidateDataCount}, Display={currentDisplayCount}->{candidateDisplayCount}",
                DebugType.Data,
                this);

            return true;
        }

        private bool TryApplyProfileSO(MoongchiProfileSO candidate, string source)
        {
            if (candidate == null || ReferenceEquals(_profileSO, candidate))
                return false;

            int currentScore = GetProfileDataCount(_profileSO);
            int candidateScore = GetProfileDataCount(candidate);

            if (_profileSO != null && currentScore > 0 && currentScore >= candidateScore)
                return false;

            _profileSO = candidate;

            DebugTool.Log(
                $"[FindMoongchiDataManager] 프로필 SO 교체 / Source={source}, Count={currentScore}->{candidateScore}",
                DebugType.Data,
                this);

            return true;
        }

        private bool ShouldReplaceShopSO()
        {
            return _shopSO == null || GetShopDataCount() <= 0;
        }

        private bool ShouldReplaceMissionSO()
        {
            return _missionSO == null || GetDisplayMissionCount(_missionSO) <= 0;
        }

        private bool ShouldReplaceProfileSO()
        {
            return _profileSO == null || GetProfileDataCount() <= 0;
        }

        private int GetShopDataCount()
        {
            return GetShopDataCount(_shopSO);
        }

        private static int GetShopDataCount(MoongchiShopSO shopSO)
        {
            return shopSO?.ShopItems?.Count ?? 0;
        }

        private int GetMissionDataCount()
        {
            return GetMissionDataCount(_missionSO);
        }

        private static int GetMissionDataCount(MoongchiMissionSO missionSO)
        {
            return missionSO?.Missions?.Count ?? 0;
        }

        private static int GetDisplayMissionCount(MoongchiMissionSO missionSO)
        {
            if (missionSO == null)
                return 0;

            int count = 0;
            count += missionSO.GetMissionsByType(MoongchiMissionType.DAILY)?.Count ?? 0;
            count += missionSO.GetMissionsByType(MoongchiMissionType.WEEKLY)?.Count ?? 0;
            count += missionSO.GetMissionsByType(MoongchiMissionType.WEEKLY_1ST)?.Count ?? 0;
            count += missionSO.GetMissionsByType(MoongchiMissionType.WEEKLY_2ND)?.Count ?? 0;
            return count;
        }

        private int GetProfileDataCount()
        {
            return GetProfileDataCount(_profileSO);
        }

        private static int GetProfileDataCount(MoongchiProfileSO profileSO)
        {
            return profileSO?.Profiles?.Count ?? 0;
        }

        private void ValidateStaticDataReferences()
        {
            if (!_warnWhenStaticDataMissing)
                return;

            if (_shopSO == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] 상점/보상 SO가 할당되지 않았습니다.", DebugType.Data, this);
            }
            else if (!HasShopData())
            {
                DebugTool.Warning("[FindMoongchiDataManager] 상점/보상 데이터가 비어있습니다. SheetLoader 설정을 확인해주세요.", DebugType.Data, this);
            }

            if (_missionSO == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] 미션 SO가 할당되지 않았습니다.", DebugType.Data, this);
            }
            else if (!HasMissionData())
            {
                DebugTool.Warning("[FindMoongchiDataManager] 미션 데이터가 비어있습니다. SheetLoader 설정을 확인해주세요.", DebugType.Data, this);
            }

            if (_profileSO == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] 프로필 SO가 할당되지 않았습니다.", DebugType.Data, this);
            }
            else if (!HasProfileData())
            {
                DebugTool.Warning("[FindMoongchiDataManager] 프로필 데이터가 비어있습니다. SheetLoader 설정을 확인해주세요.", DebugType.Data, this);
            }

            if (_eventScheduleSO == null)
            {
                DebugTool.Warning("[FindMoongchiDataManager] 이벤트 기간 SO가 할당되지 않았습니다.", DebugType.Data, this);
            }
        }
    }
}
