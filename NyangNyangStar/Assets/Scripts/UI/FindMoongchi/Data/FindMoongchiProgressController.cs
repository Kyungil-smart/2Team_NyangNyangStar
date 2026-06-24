using System;
using System.Threading.Tasks;
using Data.ScriptableObjects.MoongchiSO;
using UnityEngine;

namespace UI.FindMoongchi
{
    // FindMoongchiPopup 등 UI는 진행값 -> 코인, 탐색기회, 미션, 상점, 보드 사용
    // 미션/상점 목록(시트 정적 데이터)은 DataManager.ShopSO, MissionSO 등 기존 API를 그대로 사용

    // 1. 팝업 열 때 await EnsureLoadedAsync()
    // 2. SyncGameLogicFromProgress(_gameLogic)
    // 3. OnProgressChanged 구독 후 패널 갱신
    public sealed class FindMoongchiProgressController : MonoBehaviour
    {
        [Tooltip("비워두면 같은 GameObject, 자식 순으로 FindMoongchiDataManager를 찾습니다.")]
        [SerializeField] private FindMoongchiDataManager _dataManager;

        // 시트 SO 접근용. 진행값은 아래 프로퍼티/API 사용
        public FindMoongchiDataManager DataManager => _dataManager;

        // EnsureLoadedAsync 성공 후 true
        public bool IsProgressReady => _dataManager != null && _dataManager.IsProgressReady;

        // Firestore 로드 완료 (패널 첫 갱신)
        public event Action OnProgressReady;

        // 진행 데이터 Firestore 저장 완료 (코인, 탐색기회, 미션 등 UI 재갱신)
        public event Action OnProgressChanged;

        // 읽기 전용 진행값 

        public int EventCurrency =>
            IsProgressReady ? _dataManager.Progress.EventCurrency : 0;

        public int SearchChance =>
            IsProgressReady ? _dataManager.Progress.SearchChance : 0;

        public int CurrentWeek =>
            IsProgressReady ? _dataManager.Progress.CurrentWeek : 1;

        public int DailyEnergySpendProgress =>
            _dataManager != null ? _dataManager.DailyEnergySpendProgress : 0;

        // 보너스 탐색 기회 지급 기준 에너지량
        public int EnergySpendTarget => FindMoongchiConstants.EnergySpendTarget;

        private void Awake()
        {
            ResolveDataManager();
        }

        private void OnEnable()
        {
            SubscribeDataManagerEvents();
        }

        private void OnDisable()
        {
            UnsubscribeDataManagerEvents();
        }

        public void ResolveDataManager()
        {
            if (_dataManager != null)
                return;

            _dataManager = GetComponent<FindMoongchiDataManager>();

            if (_dataManager == null)
                _dataManager = GetComponentInChildren<FindMoongchiDataManager>(true);
        }

        // --- 로드 / 게임 동기화 ---

        // [필수] 팝업 열기 또는 게임 패널 진입 전 Firestore에서 진행 데이터 로드
        // FireStoreManager.IsInitialized && 로그인 완료 후 호출
        public async Task<bool> EnsureLoadedAsync()
        {
            ResolveDataManager();

            if (_dataManager == null)
            {
                DebugTool.Warning("[FindMoongchiProgressController] FindMoongchiDataManager를 찾지 못했습니다.", DebugType.Data, this);
                return false;
            }

            return await _dataManager.EnsureProgressLoadedAsync();
        }

        // 서버 진행 데이터를 FindMoongchiGameLogic에 반영 (스테이지, 열린 타일 등)
        // EnsureLoadedAsync 직후, 스테이지 클리어 후에 호출
        public void SyncGameLogicFromProgress(FindMoongchiGameLogic gameLogic)
        {
            _dataManager?.SyncGameLogicFromProgress(gameLogic);
        }

        // 현재 플레이 중인 스테이지 ID (1~10)
        public int GetCurrentStageId()
        {
            return _dataManager != null ? _dataManager.GetCurrentStageIdFromProgress() : 0;
        }


        // 도구 사용 전 탐색 기회 차감 (메모리만, 저장은 PersistAfterToolUseAsync)
        public bool TryConsumeSearchChance(int amount = 1)
        {
            return _dataManager != null && _dataManager.TryConsumeSearchChance(amount);
        }

        // 보드 아이템 소비 실패 등으로 탐색 기회 되돌릴 때
        public void RestoreSearchChance(int amount = 1)
        {
            _dataManager?.RestoreSearchChance(amount);
        }

        // 도구 사용 성공 후 : 미션 추적 + 보드 상태 Firestore 저장
        public async Task<bool> PersistAfterToolUseAsync(
            FindMoongchiGameLogic gameLogic,
            FindMoongchiUseToolResult result)
        {
            if (_dataManager == null)
                return false;

            return await _dataManager.PersistAfterToolUseAsync(gameLogic, result);
        }

        // 미션 추적 없이 보드 상태만 저장
        public async Task<bool> PersistAfterToolUseAsync(FindMoongchiGameLogic gameLogic)
        {
            if (_dataManager == null)
                return false;

            return await _dataManager.PersistBoardStateAsync(gameLogic);
        }

        // 스테이지 클리어 후 다음 스테이지 진행 + Firestore 저장
        public async Task<bool> AdvanceStageAndPersistAsync(FindMoongchiGameLogic gameLogic)
        {
            if (_dataManager == null)
                return false;

            bool saved = await _dataManager.AdvanceStageAndPersistAsync(gameLogic);

            if (saved)
                SyncGameLogicFromProgress(gameLogic);

            return saved;
        }

        // 에너지 소비 시 일일 미션 진행 + 보너스 탐색기회 연동
        // MergeBoardItemService.Instance.AddItemByIdAsync 에서 호출
        public async Task<bool> NotifyEnergySpentAsync(int amount)
        {
            if (_dataManager == null || !_dataManager.IsProgressReady)
                return false;

            if (!_dataManager.TrackEnergySpent(amount))
                return true;

            return await _dataManager.PersistProgressAsync();
        }


        // 미션 현재 진행도
        public int GetMissionCurrentAmount(int missionId)
        {
            return _dataManager != null ? _dataManager.GetMissionCurrentAmount(missionId) : 0;
        }

        // 미션 슬롯 UI 상태 (InProgress / Completed / Claimed)
        public FindMoongchiMissionSlotState GetMissionSlotState(MoongchiMissionData mission)
        {
            return _dataManager != null
                ? _dataManager.GetMissionSlotState(mission)
                : FindMoongchiMissionSlotState.InProgress;
        }

        // 미션 보상 수령 + Firestore 저장
        public async Task<bool> TryClaimMissionAndPersistAsync(int missionId)
        {
            return _dataManager != null && await _dataManager.TryClaimMissionAndPersistAsync(missionId);
        }

        // --- 상점 패널 ---

        // 상품 구매 횟수. mock _shopPurchaseCounts 대체
        public int GetShopPurchaseCount(int shopItemId)
        {
            return _dataManager != null ? _dataManager.GetShopPurchaseCount(shopItemId) : 0;
        }

        // 상점 구매 처리 + Firestore 저장 (코인 차감, 구매 횟수, 보상 지급 포함)
        public async Task<bool> TryPurchaseAndPersistAsync(int shopItemId, int count, int totalCost, int limitCount)
        {
            return _dataManager != null &&
                   await _dataManager.TryPurchaseShopItemAsync(shopItemId, count, totalCost, limitCount);
        }

        // 프로필 상품 보유 여부
        public bool HasOwnedProfile(int profileId)
        {
            return _dataManager != null && _dataManager.HasOwnedProfile(profileId);
        }

        private void SubscribeDataManagerEvents()
        {
            ResolveDataManager();

            if (_dataManager == null)
                return;

            _dataManager.OnProgressReady -= HandleProgressReady;
            _dataManager.OnProgressChanged -= HandleProgressChanged;
            _dataManager.OnProgressReady += HandleProgressReady;
            _dataManager.OnProgressChanged += HandleProgressChanged;
        }

        private void UnsubscribeDataManagerEvents()
        {
            if (_dataManager == null)
                return;

            _dataManager.OnProgressReady -= HandleProgressReady;
            _dataManager.OnProgressChanged -= HandleProgressChanged;
        }

        private void HandleProgressReady()
        {
            OnProgressReady?.Invoke();
        }

        private void HandleProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }
    }
}
