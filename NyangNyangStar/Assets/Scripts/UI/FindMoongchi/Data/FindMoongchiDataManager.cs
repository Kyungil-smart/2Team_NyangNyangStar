using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.LibrarySystem;
using Data.ScriptableObjects.MoongchiSO;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.FindMoongchi
{
    public class FindMoongchiDataManager : MonoBehaviour
    {
        [Header("상점/보상 데이터")] [SerializeField] private MoongchiShopSO _shopSO;

        [Header("숨바꼭질 미션 데이터")] [SerializeField] private MoongchiMissionSO _missionSO;

        [Header("프로필 데이터")]
        [SerializeField] private MoongchiProfileSO _profileSO;

        [Header("유저 진행 데이터")] [SerializeField] private FindMoongchiProgressFirestoreSO _progressSO;

        [Header("로드 상태 옵션")]
        [FormerlySerializedAs("_loadOnStart")]
        [SerializeField] private bool _notifyLoadedOnStart = true;
        [SerializeField] private bool _warnWhenStaticDataMissing = true;

        public MoongchiShopSO ShopSO => _shopSO;
        public MoongchiMissionSO MissionSO => _missionSO;
        public MoongchiProfileSO ProfileSO => _profileSO;
        public FindMoongchiProgressFirestoreSO ProgressSO => _progressSO;

        public bool IsLoaded { get; private set; }

        public event Action OnLoadCompleted;

        private bool _isWaitingForGlobalDataReady;

        private static readonly IReadOnlyList<MoongchiMissionData> EmptyMissions =
            Array.Empty<MoongchiMissionData>();
        private static readonly IReadOnlyList<MoongchiShopItemData> EmptyShopItems =
            Array.Empty<MoongchiShopItemData>();
        private static readonly IReadOnlyList<MoongchiProfileData> EmptyProfiles =
            Array.Empty<MoongchiProfileData>();

        private void Start()
        {
            if (_notifyLoadedOnStart)
                NotifyWhenSheetLoaderReady();
        }

        [ContextMenu("FindMoongchi 시트 로드 완료 알림")]
        public void LoadAll()
        {
            // 실제 시트 다운로드는 공용 SheetLoader 담당
            // 기존 UI 흐름이 기다리는 로드 완료 이벤트 유지
            IsLoaded = false;

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
        }

        private void UnsubscribeGlobalDataReady()
        {
            if (!_isWaitingForGlobalDataReady || LocalDataAccess.Instance?.Game == null)
                return;

            LocalDataAccess.Instance.Game.OnReady -= HandleGlobalDataReady;
            _isWaitingForGlobalDataReady = false;
        }

        // 이벤트 화면 진입 시 호출하는 진행 데이터 로드 API
        // Firestore 접근 세부 구현은 여기서만 처리, 호출자는 RuntimeData만 사용
        public async Task<FindMoongchiProgressRuntimeData> LoadProgressAsync()
        {
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
            // 원하는 미션 타입만 골라서 가져갈 수 있는 공통 조회 메서드
            // SO가 연결되지 않았거나 로드 전이어도 null 대신 빈 목록을 반환

            if (_missionSO == null)
                return EmptyMissions;

            return _missionSO.GetMissionsByType(missionType);
        }

        public bool TryGetMission(int missionID, out MoongchiMissionData missionData)
        {
            // 특정 미션 ID 하나만 찾아야 할 때 사용하는 메서드

            missionData = null;

            if (_missionSO == null)
                return false;

            return _missionSO.TryGetMission(missionID, out missionData);
        }

        public bool HasMissionData()
        {
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
            return _shopSO != null ? _shopSO.ShopItems : EmptyShopItems;
        }


        public bool TryGetShopItem(int itemID, out MoongchiShopItemData itemData)
        {
            // 특정 상점 상품 ID 단건 조회
            // 구매 처리용
            itemData = null;
            if (_shopSO == null)
                return false;
            return _shopSO.TryGetShopItem(itemID, out itemData);
        }

        public bool HasShopData()
        {
            // 상점 UI를 그리기 전에 상품 데이터가 실제로 있는지 확인할 때 사용
            return _shopSO != null &&
                   _shopSO.ShopItems != null &&
                   _shopSO.ShopItems.Count > 0;
        }

        // 상점 UI용 프로필 조회 API
        public IReadOnlyList<MoongchiProfileData> GetProfiles()
        {
            // 상점 상품 중 PROFILE 타입 상품을 표시할 때 사용할 프로필 목록
            // 프로필 전체 목록이 필요한 UI에서 사용

            return _profileSO != null ? _profileSO.Profiles : EmptyProfiles;
        }

        public bool TryGetProfile(int profileID, out MoongchiProfileData profileData)
        {
            // 상점 상품의 ProductType이 PROFILE일 때,
            // ProductID를 ProfileID로 사용해서 프로필 정보를 찾는 용도
            profileData = null;
            if (_profileSO == null)
                return false;
            return _profileSO.TryGetProfile(profileID, out profileData);
        }

        public bool HasProfileData()
        {
            // 프로필 보상 데이터가 실제로 있는지 확인할 때 사용
            return _profileSO != null &&
                   _profileSO.Profiles != null &&
                   _profileSO.Profiles.Count > 0;
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
        }
    }
}