using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.ScriptableObjects.MoongchiSO;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiProgressService
    {
        private FindMoongchiProgressRuntimeData _progress;
        private bool _usesLocalFallback;

        public bool IsLoaded { get; private set; }
        public bool UsesLocalFallback => _usesLocalFallback;
        public FindMoongchiProgressRuntimeData Progress => _progress;

        public int CurrentWeek => _progress != null ? Mathf.Max(1, _progress.CurrentWeek) : 1;
        public int CurrentStageId => _progress != null ? _progress.CurrentStageID : 0;
        public int SearchChance => _progress != null ? Mathf.Max(0, _progress.SearchChance) : 0;
        public int EventCoin => _progress != null ? Mathf.Max(0, _progress.EventCurrency) : 0;
        public int TodayBonusSearchChanceCount => _progress != null ? Mathf.Max(0, _progress.TodayBonusSearchChanceCount) : 0;

        public async Task LoadOrCreateAsync(
            FindMoongchiDataManager dataManager,
            int fallbackWeek,
            int fallbackStageId,
            int fallbackSearchChance,
            int fallbackEventCoin)
        {
            IsLoaded = false;
            _usesLocalFallback = false;

            FindMoongchiProgressRuntimeData loadedProgress = null;

            if (dataManager != null)
                loadedProgress = await dataManager.LoadProgressAsync();

            if (loadedProgress == null)
            {
                loadedProgress = CreateFallbackProgress(fallbackWeek, fallbackStageId, fallbackSearchChance, fallbackEventCoin);
                _usesLocalFallback = true;
                DebugTool.Warning("[FindMoongchiProgressService] Firestore 진행 데이터를 불러오지 못해 임시 로컬 진행 데이터로 동작합니다.", DebugType.FindMoongchi);
            }

            _progress = loadedProgress;
            NormalizeProgress(dataManager, fallbackWeek, fallbackStageId, fallbackSearchChance, fallbackEventCoin);
            IsLoaded = true;
        }

        public void ApplyToGameLogic(FindMoongchiGameLogic gameLogic)
        {
            if (gameLogic == null || _progress == null)
                return;

            int stageId = _progress.CurrentStageID > 0
                ? _progress.CurrentStageID
                : (_progress.CurrentWeek <= 1 ? 1 : 6);

            gameLogic.LoadStage(stageId);
            gameLogic.RestoreBoardProgress(_progress.OpenedTileIDs, _progress.FoundTargetIDs);

            _progress.CurrentWeek = gameLogic.CurrentWeek;
            _progress.CurrentStageID = gameLogic.CurrentStageId;
        }

        public async Task<FindMoongchiOperationResult> RegisterToolUseAsync(
            FindMoongchiDataManager dataManager,
            FindMoongchiGameLogic gameLogic,
            FindMoongchiUseToolResult useToolResult,
            bool debugInfiniteToolUse)
        {
            if (_progress == null)
                return FindMoongchiOperationResult.Fail("진행 데이터가 준비되지 않았습니다.", 3001);

            if (!debugInfiniteToolUse)
                _progress.SearchChance = Mathf.Max(0, _progress.SearchChance - 1);

            SyncBoardProgress(gameLogic);
            IncrementDailySearchMissions(dataManager, 1);
            IncrementWeeklyTargetMissions(dataManager, useToolResult?.NewlyFoundTargets);

            await SaveAsync(dataManager);
            return FindMoongchiOperationResult.Success();
        }

        public async Task<FindMoongchiOperationResult> AdvanceStageAfterClearAsync(FindMoongchiDataManager dataManager, FindMoongchiGameLogic gameLogic)
        {
            if (_progress == null)
                return FindMoongchiOperationResult.Fail("진행 데이터가 준비되지 않았습니다.", 3002);

            if (dataManager != null)
                dataManager.AdvanceStageOnClear(_progress);
            else
                AdvanceStageFallback();

            ApplyToGameLogic(gameLogic);
            await SaveAsync(dataManager);

            return FindMoongchiOperationResult.Success();
        }

        public int GetMissionCurrentAmount(int missionId)
        {
            FindMoongchiMissionProgressData missionProgress = FindMissionProgress(missionId);
            return missionProgress != null ? Mathf.Max(0, missionProgress.CurrentAmount) : 0;
        }

        public FindMoongchiMissionSlotState GetMissionState(MoongchiMissionData mission)
        {
            if (mission == null)
                return FindMoongchiMissionSlotState.InProgress;

            FindMoongchiMissionProgressData missionProgress = FindMissionProgress(mission.ID);

            if (missionProgress != null && missionProgress.IsRewardClaimed)
                return FindMoongchiMissionSlotState.Claimed;

            int currentAmount = missionProgress != null ? missionProgress.CurrentAmount : 0;
            return currentAmount >= mission.TargetAmount
                ? FindMoongchiMissionSlotState.Completed
                : FindMoongchiMissionSlotState.InProgress;
        }

        public async Task<FindMoongchiOperationResult> ClaimMissionAsync(FindMoongchiDataManager dataManager, MoongchiMissionData mission)
        {
            if (_progress == null)
                return FindMoongchiOperationResult.Fail("진행 데이터가 준비되지 않았습니다.", 3100);

            if (mission == null)
                return FindMoongchiOperationResult.Fail("미션 정보를 찾을 수 없습니다.", 3101);

            FindMoongchiMissionSlotState state = GetMissionState(mission);

            if (state == FindMoongchiMissionSlotState.Claimed)
                return FindMoongchiOperationResult.Fail("이미 수령한 미션입니다.");

            if (state != FindMoongchiMissionSlotState.Completed)
                return FindMoongchiOperationResult.Fail("아직 완료하지 않은 미션입니다.");

            ApplyReward(mission.Reward1);
            ApplyReward(mission.Reward2);

            int currentAmount = Mathf.Max(GetMissionCurrentAmount(mission.ID), mission.TargetAmount);
            SetMissionProgress(mission.ID, currentAmount, true);

            await SaveAsync(dataManager);
            return FindMoongchiOperationResult.Success("미션 보상을 수령했습니다.");
        }

        public int GetShopPurchaseCount(int shopItemId)
        {
            FindMoongchiShopPurchaseData purchaseData = FindShopPurchase(shopItemId);
            return purchaseData != null ? Mathf.Max(0, purchaseData.PurchaseCount) : 0;
        }

        public int GetMaxBuyCount(FindMoongchiShopViewData data)
        {
            if (data == null || data.CostAmount <= 0)
                return 0;

            int affordableCount = EventCoin / data.CostAmount;
            int remainingLimit = data.HasLimit ? data.RemainingLimit : int.MaxValue;
            return Mathf.Max(0, Mathf.Min(affordableCount, remainingLimit));
        }

        public async Task<FindMoongchiOperationResult> PurchaseAsync(FindMoongchiDataManager dataManager, FindMoongchiShopViewData data, int count)
        {
            if (_progress == null)
                return FindMoongchiOperationResult.Fail("진행 데이터가 준비되지 않았습니다.", 3200);

            if (data == null)
                return FindMoongchiOperationResult.Fail("상품 정보를 찾을 수 없습니다.", 3201);

            if (count <= 0)
                return FindMoongchiOperationResult.Fail("구매 수량이 올바르지 않습니다.", 3202);

            int totalCost = data.CostAmount * count;

            if (EventCoin < totalCost)
                return FindMoongchiOperationResult.Fail("이벤트 재화가 부족합니다.", 1001);

            int purchasedCount = GetShopPurchaseCount(data.ShopItemId);
            if (data.HasLimit && purchasedCount + count > data.LimitCount)
                return FindMoongchiOperationResult.Fail("구매 가능 횟수를 초과했습니다.", 1002);

            _progress.EventCurrency = Mathf.Max(0, _progress.EventCurrency - totalCost);
            SetShopPurchaseCount(data.ShopItemId, purchasedCount + count);
            ApplyShopProductReward(data, count);

            await SaveAsync(dataManager);
            return FindMoongchiOperationResult.Success("구매가 완료되었습니다.");
        }

        public async Task<bool> SaveAsync(FindMoongchiDataManager dataManager)
        {
            if (_progress == null)
                return false;

            if (_usesLocalFallback || dataManager == null)
                return false;

            return await dataManager.SaveProgressAsync(_progress);
        }

        private void NormalizeProgress(
            FindMoongchiDataManager dataManager,
            int fallbackWeek,
            int fallbackStageId,
            int fallbackSearchChance,
            int fallbackEventCoin)
        {
            if (_progress == null)
                _progress = CreateFallbackProgress(fallbackWeek, fallbackStageId, fallbackSearchChance, fallbackEventCoin);

            if (_progress.CurrentWeek <= 0)
                _progress.CurrentWeek = Mathf.Max(1, fallbackWeek);

            if (_progress.SearchChance < 0)
                _progress.SearchChance = Mathf.Max(0, fallbackSearchChance);

            if (_progress.EventCurrency < 0)
                _progress.EventCurrency = Mathf.Max(0, fallbackEventCoin);

            if (_progress.CurrentCycleStageIDs == null)
                _progress.CurrentCycleStageIDs = new List<int>();

            if (_progress.OpenedTileIDs == null)
                _progress.OpenedTileIDs = new List<int>();

            if (_progress.FoundTargetIDs == null)
                _progress.FoundTargetIDs = new List<int>();

            if (_progress.MissionProgresses == null)
                _progress.MissionProgresses = new List<FindMoongchiMissionProgressData>();

            if (_progress.ShopPurchaseCounts == null)
                _progress.ShopPurchaseCounts = new List<FindMoongchiShopPurchaseData>();

            if (dataManager != null)
                dataManager.EnsureStageCycleReady(_progress);

            if (_progress.CurrentStageID <= 0)
                _progress.CurrentStageID = fallbackStageId > 0 ? fallbackStageId : (_progress.CurrentWeek <= 1 ? 1 : 6);
        }

        private static FindMoongchiProgressRuntimeData CreateFallbackProgress(int week, int stageId, int searchChance, int eventCoin)
        {
            return new FindMoongchiProgressRuntimeData
            {
                CurrentWeek = Mathf.Max(1, week),
                CurrentStageID = stageId > 0 ? stageId : (week <= 1 ? 1 : 6),
                SearchChance = Mathf.Max(0, searchChance),
                EventCurrency = Mathf.Max(0, eventCoin)
            };
        }

        private void SyncBoardProgress(FindMoongchiGameLogic gameLogic)
        {
            if (_progress == null || gameLogic == null)
                return;

            _progress.CurrentWeek = gameLogic.CurrentWeek;
            _progress.CurrentStageID = gameLogic.CurrentStageId;

            _progress.OpenedTileIDs.Clear();
            _progress.OpenedTileIDs.AddRange(gameLogic.GetOpenedTileIds());

            _progress.FoundTargetIDs.Clear();
            _progress.FoundTargetIDs.AddRange(gameLogic.GetFoundTargetIds());
        }

        private void IncrementDailySearchMissions(FindMoongchiDataManager dataManager, int amount)
        {
            if (dataManager == null || amount <= 0)
                return;

            IReadOnlyList<MoongchiMissionData> missions = dataManager.GetDailyMissions();

            for (int i = 0; i < missions.Count; i++)
            {
                MoongchiMissionData mission = missions[i];
                if (mission == null)
                    continue;

                int currentAmount = GetMissionCurrentAmount(mission.ID);
                SetMissionProgress(mission.ID, Mathf.Min(mission.TargetAmount, currentAmount + amount), false);
            }
        }

        private void IncrementWeeklyTargetMissions(FindMoongchiDataManager dataManager, IReadOnlyList<FindMoongchiTargetRuntimeData> newlyFoundTargets)
        {
            if (dataManager == null || newlyFoundTargets == null || newlyFoundTargets.Count == 0)
                return;

            List<MoongchiMissionData> weeklyMissions = new List<MoongchiMissionData>();
            AddMissionsByType(dataManager, weeklyMissions, MoongchiMissionType.WEEKLY);

            if (CurrentWeek <= 1)
                AddMissionsByType(dataManager, weeklyMissions, MoongchiMissionType.WEEKLY_1ST);
            else
                AddMissionsByType(dataManager, weeklyMissions, MoongchiMissionType.WEEKLY_2ND);

            for (int i = 0; i < newlyFoundTargets.Count; i++)
            {
                FindMoongchiTargetRuntimeData target = newlyFoundTargets[i];

                if (target == null)
                    continue;

                bool matched = false;
                string normalizedTargetName = NormalizeText(target.TargetName);

                for (int j = 0; j < weeklyMissions.Count; j++)
                {
                    MoongchiMissionData mission = weeklyMissions[j];

                    if (mission == null)
                        continue;

                    string missionText = NormalizeText(mission.MissionContent);
                    if (!missionText.Contains(normalizedTargetName))
                        continue;

                    int currentAmount = GetMissionCurrentAmount(mission.ID);
                    SetMissionProgress(mission.ID, Mathf.Min(mission.TargetAmount, currentAmount + 1), false);
                    matched = true;
                }

                if (!matched)
                    DebugTool.Log($"[FindMoongchiProgressService] 발견 목표와 연결되는 주간 미션이 없습니다. Target={target.TargetName}", DebugType.FindMoongchi);
            }
        }

        private static void AddMissionsByType(
            FindMoongchiDataManager dataManager,
            List<MoongchiMissionData> target,
            MoongchiMissionType missionType)
        {
            if (dataManager == null || target == null)
                return;

            IReadOnlyList<MoongchiMissionData> missions = dataManager.GetMissionsByType(missionType);

            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] != null)
                    target.Add(missions[i]);
            }
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }

        private void ApplyReward(MoongchiRewardData reward)
        {
            if (reward == null || !reward.IsValid)
                return;

            switch (reward.RewardType)
            {
                case MoongchiCurrencyType.EVENT_COIN:
                    _progress.EventCurrency += reward.RewardAmount;
                    break;

                case MoongchiCurrencyType.ENERGY:
                    DebugTool.Log($"[FindMoongchiProgressService] 에너지 보상 지급 연결 필요: Amount={reward.RewardAmount}", DebugType.FindMoongchi);
                    break;
            }
        }

        private void ApplyShopProductReward(FindMoongchiShopViewData data, int count)
        {
            int totalQuantity = Mathf.Max(0, data.Quantity * count);

            switch (data.ProductType)
            {
                case MoongchiProductType.CURRENCY:
                    DebugTool.Log($"[FindMoongchiProgressService] CURRENCY 상품 지급 연결 필요: ProductId={data.ProductId}, Quantity={totalQuantity}", DebugType.FindMoongchi);
                    break;

                case MoongchiProductType.ITEM:
                    DebugTool.Log($"[FindMoongchiProgressService] ITEM 상품 지급 연결 필요: ItemId={data.ProductId}, Quantity={totalQuantity}", DebugType.FindMoongchi);
                    break;

                case MoongchiProductType.PROFILE:
                    DebugTool.Log($"[FindMoongchiProgressService] PROFILE 상품 지급 연결 필요: ProfileId={data.ProductId}", DebugType.FindMoongchi);
                    break;
            }
        }

        private FindMoongchiMissionProgressData FindMissionProgress(int missionId)
        {
            if (_progress?.MissionProgresses == null)
                return null;

            for (int i = 0; i < _progress.MissionProgresses.Count; i++)
            {
                FindMoongchiMissionProgressData progressData = _progress.MissionProgresses[i];

                if (progressData != null && progressData.MissionID == missionId)
                    return progressData;
            }

            return null;
        }

        private void SetMissionProgress(int missionId, int currentAmount, bool isRewardClaimed)
        {
            if (_progress?.MissionProgresses == null)
                return;

            FindMoongchiMissionProgressData nextData = new FindMoongchiMissionProgressData(missionId, currentAmount, isRewardClaimed);

            for (int i = 0; i < _progress.MissionProgresses.Count; i++)
            {
                FindMoongchiMissionProgressData progressData = _progress.MissionProgresses[i];

                if (progressData == null || progressData.MissionID != missionId)
                    continue;

                bool claimed = progressData.IsRewardClaimed || isRewardClaimed;
                int amount = Mathf.Max(progressData.CurrentAmount, currentAmount);
                _progress.MissionProgresses[i] = new FindMoongchiMissionProgressData(missionId, amount, claimed);
                return;
            }

            _progress.MissionProgresses.Add(nextData);
        }

        private FindMoongchiShopPurchaseData FindShopPurchase(int shopItemId)
        {
            if (_progress?.ShopPurchaseCounts == null)
                return null;

            for (int i = 0; i < _progress.ShopPurchaseCounts.Count; i++)
            {
                FindMoongchiShopPurchaseData purchaseData = _progress.ShopPurchaseCounts[i];

                if (purchaseData != null && purchaseData.ShopItemID == shopItemId)
                    return purchaseData;
            }

            return null;
        }

        private void SetShopPurchaseCount(int shopItemId, int purchaseCount)
        {
            if (_progress?.ShopPurchaseCounts == null)
                return;

            FindMoongchiShopPurchaseData nextData = new FindMoongchiShopPurchaseData(shopItemId, purchaseCount);

            for (int i = 0; i < _progress.ShopPurchaseCounts.Count; i++)
            {
                FindMoongchiShopPurchaseData purchaseData = _progress.ShopPurchaseCounts[i];

                if (purchaseData == null || purchaseData.ShopItemID != shopItemId)
                    continue;

                _progress.ShopPurchaseCounts[i] = nextData;
                return;
            }

            _progress.ShopPurchaseCounts.Add(nextData);
        }

        private void AdvanceStageFallback()
        {
            if (_progress == null)
                return;

            List<int> stageIds = new List<int>(_progress.CurrentWeek <= 1
                ? new[] { 1, 2, 3, 4, 5 }
                : new[] { 6, 7, 8, 9, 10 });

            if (_progress.CurrentCycleStageIDs == null || _progress.CurrentCycleStageIDs.Count == 0)
                _progress.CurrentCycleStageIDs.AddRange(stageIds);

            _progress.OpenedTileIDs.Clear();
            _progress.FoundTargetIDs.Clear();
            _progress.CurrentCycleIndex++;

            if (_progress.CurrentCycleIndex >= _progress.CurrentCycleStageIDs.Count)
                _progress.CurrentCycleIndex = 0;

            _progress.CurrentStageID = _progress.CurrentCycleStageIDs[_progress.CurrentCycleIndex];
        }
    }
}
