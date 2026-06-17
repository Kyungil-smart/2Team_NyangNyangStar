using System;
using System.Collections.Generic;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.MoongchiSO;
using Data.ScriptableObjects.MergeBoard;
using UI.Base;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiPopup : UIPopup
    {
        [Header("Panels")]
        [SerializeField] private FindMoongchiMainPanel _mainPanel;
        [SerializeField] private FindMoongchiGamePanel _gamePanel;
        [SerializeField] private FindMoongchiMissionPanel _missionPanel;
        [SerializeField] private FindMoongchiShopPanel _shopPanel;

        [Header("Modal")]
        [SerializeField] private GameObject _modalLayer;
        [SerializeField] private PurchasePopupView _purchasePopup;
        [SerializeField] private NoticePopupView _noticePopup;
        [SerializeField] private ErrorPopupView _errorPopup;

        [Header("Data")]
        [Tooltip("같은 오브젝트에 붙어있으면 비워도 자동으로 찾습니다.")]
        [SerializeField] private FindMoongchiDataManager _dataManager;

        [Header("임시 유저 값 - Firestore 연결 전")]
        [SerializeField] private int _eventCoin = 1000;
        [SerializeField] private int _searchChance = FindMoongchiConstants.DailySearchChance;
        [SerializeField] private int _energySpendProgress;
        [SerializeField] private int _currentWeek = 1;
        [SerializeField] private string _remainTimeText = "1d 12h";
        [SerializeField] private bool _completedMissionMock;

        [Header("임시 미니게임 스테이지 - SO/Firestore 연결 전")]
        [Tooltip("현재는 임시 하드코딩 스테이지를 사용합니다. 실제 데이터 담당 작업 후 SO/Firestore 값으로 교체하면 됩니다.")]
        [SerializeField] private int _currentStageId = 0;
        [Tooltip("Firestore 진행 데이터 연결 전 임시로 유저별 주차 스테이지 순서를 섞어서 사용합니다.")]
        [SerializeField] private bool _useRandomStageCycleMock = true;
        [SerializeField] private int _temporaryCycleIndex;
        [SerializeField] private int _temporaryCycleNumber;
        [SerializeField] private string _temporaryStageSeedOverride;

        [Header("탐색 도구 ID - 순서 고정")]
        [Tooltip("0: 가로 한 줄 도구, 1: 세로 한 줄 도구, 2: 4x4 사각형 도구. ID만 바꾸고 순서는 바꾸지 마세요.")]
        [SerializeField] private int[] _toolItemIds =
        {
            FindMoongchiConstants.ToolId01,
            FindMoongchiConstants.ToolId02,
            FindMoongchiConstants.ToolId03
        };

        private readonly FindMoongchiGameLogic _gameLogic = new();
        private readonly int[] _resolvedToolItemIds = new int[FindMoongchiConstants.ToolSlotCount];
        private readonly Dictionary<int, int> _shopPurchaseCounts = new();
        private readonly HashSet<int> _claimedMissionIds = new();

        private bool _initialized;
        private bool _isUsingTool;
        private bool _isStageClearWaitingForRestart;
        private FindMoongchiPanelType _currentPanelType = FindMoongchiPanelType.Main;

        private MoongchiShopSO ShopSO => _dataManager != null ? _dataManager.ShopSO : null;
        private MoongchiMissionSO MissionSO => _dataManager != null ? _dataManager.MissionSO : null;
        private MoongchiProfileSO ProfileSO => _dataManager != null ? _dataManager.ProfileSO : null;

        private void OnEnable()
        {
            if (!_initialized)
                Init();
        }

        public override void Init()
        {
            DebugTool.Log("[FindMoongchiPopup] 초기화 시작", DebugType.FindMoongchi, this);

            ResolveReferences();
            ResolveDataManager();
            InitChildren();
            ResolveToolItemIds();
            BindEvents();
            InitGameLogic();

            _initialized = true;

            DebugTool.Log("[FindMoongchiPopup] 초기화 완료", DebugType.FindMoongchi, this);
            ShowPanel(FindMoongchiPanelType.Main);
        }

        public override void PlayOpenAnimation()
        {
            DebugTool.Log("[FindMoongchiPopup] 팝업 열기", DebugType.FindMoongchi, this);

            if (!_initialized)
                Init();

            ShowPanel(FindMoongchiPanelType.Main);
        }

        public void ShowPanel(FindMoongchiPanelType panelType)
        {
            DebugTool.Log($"[FindMoongchiPopup] 패널 전환: {panelType}", DebugType.FindMoongchi, this);

            _currentPanelType = panelType;

            SetActive(_mainPanel, panelType == FindMoongchiPanelType.Main);
            SetActive(_gamePanel, panelType == FindMoongchiPanelType.Game);
            SetActive(_missionPanel, panelType == FindMoongchiPanelType.Mission);
            SetActive(_shopPanel, panelType == FindMoongchiPanelType.Shop);

            CloseAllModal();

            switch (panelType)
            {
                case FindMoongchiPanelType.Game:
                    RefreshGamePanel();
                    break;
                case FindMoongchiPanelType.Mission:
                    RefreshMissionPanel();
                    break;
                case FindMoongchiPanelType.Shop:
                    RefreshShopPanel();
                    break;
            }
        }

        public void OpenNotice(string message)
        {
            OpenNotice(message, null, null);
        }

        private void OpenNotice(string message, string confirmButtonText, Action onConfirm)
        {
            DebugTool.Log($"[FindMoongchiPopup] 안내 팝업: {message}", DebugType.FindMoongchi, this);
            OpenModalLayer();

            if (_noticePopup == null)
            {
                DebugTool.Warning("[FindMoongchiPopup] NoticePopup이 연결되지 않았습니다.", DebugType.FindMoongchi, this);
                return;
            }

            _noticePopup.Open(
                message,
                confirmButtonText,
                () =>
                {
                    CloseAllModal();
                    onConfirm?.Invoke();
                });
        }

        public void OpenError(string message, int errorCode = 0)
        {
            DebugTool.Warning($"[FindMoongchiPopup] 오류 팝업: {message}, Code: {errorCode}", DebugType.FindMoongchi, this);
            OpenModalLayer();
            _errorPopup?.Open(message, errorCode);
        }

        public override void ClosePopup()
        {
            DebugTool.Log("[FindMoongchiPopup] 팝업 닫기: Destroy하지 않고 비활성화합니다.", DebugType.FindMoongchi, this);
            CloseAllModal();
            gameObject.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (_mainPanel == null)
                _mainPanel = GetComponentInChildren<FindMoongchiMainPanel>(true);

            if (_gamePanel == null)
                _gamePanel = GetComponentInChildren<FindMoongchiGamePanel>(true);

            if (_missionPanel == null)
                _missionPanel = GetComponentInChildren<FindMoongchiMissionPanel>(true);

            if (_shopPanel == null)
                _shopPanel = GetComponentInChildren<FindMoongchiShopPanel>(true);

            if (_purchasePopup == null)
                _purchasePopup = GetComponentInChildren<PurchasePopupView>(true);

            if (_noticePopup == null)
                _noticePopup = GetComponentInChildren<NoticePopupView>(true);

            if (_errorPopup == null)
                _errorPopup = GetComponentInChildren<ErrorPopupView>(true);

            if (_modalLayer == null && _purchasePopup != null)
                _modalLayer = _purchasePopup.transform.parent != null ? _purchasePopup.transform.parent.gameObject : null;

            DebugTool.Log(
                "[FindMoongchiPopup] 참조 확인\n" +
                $"MainPanel: {_mainPanel != null}, GamePanel: {_gamePanel != null}, MissionPanel: {_missionPanel != null}, ShopPanel: {_shopPanel != null}\n" +
                $"ModalLayer: {_modalLayer != null}, PurchasePopup: {_purchasePopup != null}, NoticePopup: {_noticePopup != null}, ErrorPopup: {_errorPopup != null}",
                DebugType.FindMoongchi,
                this);
        }

        private void ResolveDataManager()
        {
            if (_dataManager == null)
                _dataManager = GetComponent<FindMoongchiDataManager>();

            if (_dataManager == null)
                _dataManager = GetComponentInChildren<FindMoongchiDataManager>(true);

            if (_dataManager == null)
                _dataManager = FindFirstObjectByType<FindMoongchiDataManager>();

            if (_dataManager == null)
            {
                DebugTool.Warning("[FindMoongchiPopup] FindMoongchiDataManager를 찾지 못했습니다.", DebugType.FindMoongchi, this);
                return;
            }

            DebugTool.Log($"[FindMoongchiPopup] DataManager 연결: {_dataManager.name}", DebugType.FindMoongchi, this);
            _dataManager.OnLoadCompleted -= HandleDataLoadCompleted;
            _dataManager.OnLoadCompleted += HandleDataLoadCompleted;
        }

        private void ResolveToolItemIds()
        {
            if (_toolItemIds == null || _toolItemIds.Length != FindMoongchiConstants.ToolSlotCount)
            {
                DebugTool.Warning(
                    $"[FindMoongchiPopup] 탐색 도구 ID 배열 길이가 올바르지 않습니다. 필요={FindMoongchiConstants.ToolSlotCount}, 현재={_toolItemIds?.Length ?? 0}. 부족한 값은 기본값을 사용합니다.",
                    DebugType.FindMoongchi,
                    this);
            }

            IReadOnlyList<int> defaults = FindMoongchiConstants.DefaultToolItemIds;

            for (int i = 0; i < FindMoongchiConstants.ToolSlotCount; i++)
            {
                int value = _toolItemIds != null && i < _toolItemIds.Length ? _toolItemIds[i] : 0;

                if (value <= 0)
                {
                    value = defaults[i];
                    DebugTool.Warning(
                        $"[FindMoongchiPopup] 탐색 도구 ID가 비어있어 기본값을 사용합니다. Index={i}, DefaultToolId={value}",
                        DebugType.FindMoongchi,
                        this);
                }

                _resolvedToolItemIds[i] = value;
            }

            _gameLogic.SetToolItemIds(_resolvedToolItemIds);
            _gamePanel?.SetToolItemIds(_resolvedToolItemIds);

            DebugTool.Log(
                $"[FindMoongchiPopup] 탐색 도구 ID 적용 완료: 0(Row)={_resolvedToolItemIds[0]}, 1(Column)={_resolvedToolItemIds[1]}, 2(Square4x4)={_resolvedToolItemIds[2]}",
                DebugType.FindMoongchi,
                this);
        }

        private void InitChildren()
        {
            DebugTool.Log("[FindMoongchiPopup] 하위 UI 초기화", DebugType.FindMoongchi, this);

            _mainPanel?.Init();
            _gamePanel?.Init();
            _missionPanel?.Init();
            _shopPanel?.Init();
            _purchasePopup?.Init();
            _noticePopup?.Init();
            _errorPopup?.Init();

            CloseAllModal();
        }

        private void BindEvents()
        {
            DebugTool.Log("[FindMoongchiPopup] 이벤트 바인딩", DebugType.FindMoongchi, this);

            if (_mainPanel != null)
            {
                _mainPanel.OnGameButtonClicked -= HandleGameButtonClicked;
                _mainPanel.OnMissionButtonClicked -= HandleMissionButtonClicked;
                _mainPanel.OnShopButtonClicked -= HandleShopButtonClicked;
                _mainPanel.OnCloseButtonClicked -= HandleCloseButtonClicked;

                _mainPanel.OnGameButtonClicked += HandleGameButtonClicked;
                _mainPanel.OnMissionButtonClicked += HandleMissionButtonClicked;
                _mainPanel.OnShopButtonClicked += HandleShopButtonClicked;
                _mainPanel.OnCloseButtonClicked += HandleCloseButtonClicked;
            }
            else
            {
                DebugTool.Warning("[FindMoongchiPopup] MainPanel이 없어 메인 버튼 이벤트를 바인딩하지 못했습니다.", DebugType.FindMoongchi, this);
            }

            if (_gamePanel != null)
            {
                _gamePanel.OnBackButtonClicked -= HandleBackToMain;
                _gamePanel.OnToolDropped -= HandleToolDropped;
                _gamePanel.OnBackButtonClicked += HandleBackToMain;
                _gamePanel.OnToolDropped += HandleToolDropped;
            }
            else
            {
                DebugTool.Warning("[FindMoongchiPopup] GamePanel이 없어 게임 이벤트를 바인딩하지 못했습니다.", DebugType.FindMoongchi, this);
            }

            if (_missionPanel != null)
            {
                _missionPanel.OnBackButtonClicked -= HandleBackToMain;
                _missionPanel.OnClaimMissionClicked -= HandleClaimMissionClicked;
                _missionPanel.OnBackButtonClicked += HandleBackToMain;
                _missionPanel.OnClaimMissionClicked += HandleClaimMissionClicked;
            }
            else
            {
                DebugTool.Warning("[FindMoongchiPopup] MissionPanel이 없어 미션 이벤트를 바인딩하지 못했습니다.", DebugType.FindMoongchi, this);
            }

            if (_shopPanel != null)
            {
                _shopPanel.OnBackButtonClicked -= HandleBackToMain;
                _shopPanel.OnShopItemClicked -= HandleShopItemClicked;
                _shopPanel.OnBackButtonClicked += HandleBackToMain;
                _shopPanel.OnShopItemClicked += HandleShopItemClicked;
            }
            else
            {
                DebugTool.Warning("[FindMoongchiPopup] ShopPanel이 없어 상점 이벤트를 바인딩하지 못했습니다.", DebugType.FindMoongchi, this);
            }
        }

        private void InitGameLogic()
        {
            _gameLogic.SetToolItemIds(_resolvedToolItemIds);

            int stageId = ResolveTemporaryStageId();
            _gameLogic.LoadStage(stageId);
            _currentStageId = _gameLogic.CurrentStageId;
            _currentWeek = _gameLogic.CurrentWeek;

            DebugTool.Log(
                $"[FindMoongchiPopup] 미니게임 임시 스테이지 로드: StageId={_gameLogic.CurrentStageId}, Week={_gameLogic.CurrentWeek}, TargetCount={_gameLogic.TargetCount}",
                DebugType.FindMoongchi,
                this);
        }

        private int ResolveTemporaryStageId()
        {
            if (_useRandomStageCycleMock)
            {
                string seed = GetTemporaryStageSeed();
                int stageId = FindMoongchiGameLogic.ResolveStageIdFromCycle(
                    _currentWeek,
                    seed,
                    _temporaryCycleIndex,
                    _temporaryCycleNumber);

                DebugTool.Log(
                    $"[FindMoongchiPopup] 임시 랜덤 스테이지 선택: Week={_currentWeek}, CycleIndex={_temporaryCycleIndex}, StageId={stageId}, Seed={seed}",
                    DebugType.FindMoongchi,
                    this);

                return stageId;
            }

            if (_currentStageId > 0)
                return _currentStageId;

            return _currentWeek <= 1 ? 1 : 6;
        }

        private string GetTemporaryStageSeed()
        {
            if (!string.IsNullOrWhiteSpace(_temporaryStageSeedOverride))
                return _temporaryStageSeedOverride;

            if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.CurrentUserId))
                return AuthManager.Instance.CurrentUserId;

            return SystemInfo.deviceUniqueIdentifier;
        }

        private void HandleDataLoadCompleted()
        {
            DebugTool.Log("[FindMoongchiPopup] 데이터 로드 완료 이벤트 수신", DebugType.FindMoongchi, this);

            if (!_initialized)
                return;

            RefreshCurrentPanel();
        }

        private void RefreshCurrentPanel()
        {
            switch (_currentPanelType)
            {
                case FindMoongchiPanelType.Game:
                    RefreshGamePanel();
                    break;
                case FindMoongchiPanelType.Mission:
                    RefreshMissionPanel();
                    break;
                case FindMoongchiPanelType.Shop:
                    RefreshShopPanel();
                    break;
            }
        }

        private void HandleGameButtonClicked()
        {
            DebugTool.Log("[FindMoongchiPopup] 게임 패널 열기 요청", DebugType.FindMoongchi, this);
            ShowPanel(FindMoongchiPanelType.Game);
        }

        private void HandleMissionButtonClicked()
        {
            DebugTool.Log("[FindMoongchiPopup] 미션 패널 열기 요청", DebugType.FindMoongchi, this);
            ShowPanel(FindMoongchiPanelType.Mission);
        }

        private void HandleShopButtonClicked()
        {
            DebugTool.Log("[FindMoongchiPopup] 상점 패널 열기 요청", DebugType.FindMoongchi, this);
            ShowPanel(FindMoongchiPanelType.Shop);
        }

        private void HandleBackToMain()
        {
            DebugTool.Log("[FindMoongchiPopup] 메인 패널로 복귀", DebugType.FindMoongchi, this);
            ShowPanel(FindMoongchiPanelType.Main);
        }

        private void HandleCloseButtonClicked()
        {
            DebugTool.Log("[FindMoongchiPopup] 닫기 버튼 요청", DebugType.FindMoongchi, this);
            ClosePopup();
        }

        private async void HandleToolDropped(int toolItemId, int tileIndex)
        {
            DebugTool.Log($"[FindMoongchiPopup] 도구 드롭 요청: ToolId={toolItemId}, Tile={tileIndex}", DebugType.FindMoongchi, this);

            if (_isUsingTool)
            {
                DebugTool.Warning("[FindMoongchiPopup] 이미 도구 사용 처리 중입니다.", DebugType.FindMoongchi, this);
                return;
            }

            if (_isStageClearWaitingForRestart)
            {
                DebugTool.Log("[FindMoongchiPopup] 클리어 완료 후 다시하기 대기 중이라 도구 사용을 막습니다.", DebugType.FindMoongchi, this);
                OpenStageClearNotice();
                return;
            }

            if (_searchChance <= 0)
            {
                DebugTool.Warning("[FindMoongchiPopup] 탐색 기회 부족", DebugType.FindMoongchi, this);
                OpenNotice("탐색 기회가 없습니다.");
                return;
            }

            int boardItemCount = FindMoongchiMergeBoardBridge.GetBoardItemCountById(toolItemId);

            if (boardItemCount <= 0)
            {
                DebugTool.Warning($"[FindMoongchiPopup] 보드에 탐색 도구 없음: ToolId={toolItemId}", DebugType.FindMoongchi, this);
                OpenNotice("보유한 탐색 도구가 없습니다.");
                return;
            }

            _isUsingTool = true;

            try
            {
                bool consumed = await FindMoongchiMergeBoardBridge.ConsumeBoardItemByIdAsync(toolItemId, 1);

                if (!consumed)
                {
                    DebugTool.Warning($"[FindMoongchiPopup] 탐색 도구 소비 실패: ToolId={toolItemId}", DebugType.FindMoongchi, this);
                    OpenError("탐색 도구 소비에 실패했습니다.", 1001);
                    return;
                }

                _searchChance = Mathf.Max(0, _searchChance - 1);

                FindMoongchiUseToolResult result = _gameLogic.UseTool(toolItemId, tileIndex);

                DebugTool.Log(
                    $"[FindMoongchiPopup] 도구 사용 성공: ToolId={toolItemId}, 기준 Tile={tileIndex}, 공개 대상={result.AffectedTileIndices.Count}, 신규 공개={result.NewlyRevealedTileIndices.Count}, 발견 목표={result.NewlyFoundTargets.Count}, 남은 탐색 기회={_searchChance}",
                    DebugType.FindMoongchi,
                    this);

                foreach (FindMoongchiTargetRuntimeData foundTarget in result.NewlyFoundTargets)
                    DebugTool.Log($"[FindMoongchiPopup] 목표물 발견: {foundTarget.TargetName}({foundTarget.TargetId})", DebugType.FindMoongchi, this);

                RefreshGamePanel(true);

                if (result.IsStageCleared)
                    HandleStageCleared();
            }
            finally
            {
                _isUsingTool = false;
            }
        }

        private void HandleStageCleared()
        {
            if (_isStageClearWaitingForRestart)
                return;

            _isStageClearWaitingForRestart = true;

            DebugTool.Log(
                $"[FindMoongchiPopup] 스테이지 클리어. 현재 공개 상태를 유지하고 다시하기 입력을 기다립니다. StageId={_gameLogic.CurrentStageId}",
                DebugType.FindMoongchi,
                this);

            OpenStageClearNotice();
        }

        private void OpenStageClearNotice()
        {
            OpenNotice("스테이지를 클리어했습니다.", "다시하기", HandleStageClearRestartConfirmed);
        }

        private void HandleStageClearRestartConfirmed()
        {
            DebugTool.Log("[FindMoongchiPopup] 스테이지 클리어 다시하기 확인", DebugType.FindMoongchi, this);

            _isStageClearWaitingForRestart = false;
            AdvanceTemporaryStageCycle();
        }

        private void AdvanceTemporaryStageCycle()
        {
            int nextStageId;

            if (_useRandomStageCycleMock)
            {
                _temporaryCycleIndex++;

                int stageCount = FindMoongchiGameLogic.GetStageIdsForWeek(_currentWeek).Count;
                if (stageCount > 0 && _temporaryCycleIndex >= stageCount)
                {
                    _temporaryCycleIndex = 0;
                    _temporaryCycleNumber++;
                    DebugTool.Log($"[FindMoongchiPopup] 임시 스테이지 사이클 완료. 다음 사이클 시작: Cycle={_temporaryCycleNumber}", DebugType.FindMoongchi, this);
                }

                nextStageId = ResolveTemporaryStageId();
            }
            else
            {
                nextStageId = _currentStageId;
            }

            _gameLogic.LoadStage(nextStageId);
            _currentStageId = _gameLogic.CurrentStageId;
            _currentWeek = _gameLogic.CurrentWeek;

            DebugTool.Log($"[FindMoongchiPopup] 다시하기 후 스테이지 로드: StageId={_currentStageId}, Week={_currentWeek}", DebugType.FindMoongchi, this);
            RefreshGamePanel(false);
        }

        private void HandleClaimMissionClicked(int missionId)
        {
            DebugTool.Log($"[FindMoongchiPopup] 미션 보상 수령 요청: MissionId={missionId}", DebugType.FindMoongchi, this);

            FindMoongchiMissionViewData mission = BuildMissionViewDataById(missionId);

            if (mission == null)
            {
                OpenError("미션 정보를 찾을 수 없습니다.", 2001);
                return;
            }

            if (mission.State == FindMoongchiMissionSlotState.Claimed)
            {
                OpenNotice("이미 수령한 미션입니다.");
                return;
            }

            if (mission.State != FindMoongchiMissionSlotState.Completed)
            {
                OpenNotice("아직 완료하지 않은 미션입니다.");
                return;
            }

            ApplyMissionReward(mission.Reward1);
            ApplyMissionReward(mission.Reward2);
            _claimedMissionIds.Add(missionId);

            DebugTool.Log($"[FindMoongchiPopup] 미션 보상 수령 완료: MissionId={missionId}, EventCoin={_eventCoin}", DebugType.FindMoongchi, this);
            OpenNotice("미션 보상을 수령했습니다.");
            RefreshMissionPanel();
        }

        private void HandleShopItemClicked(FindMoongchiShopViewData data)
        {
            if (data != null)
                DebugTool.Log($"[FindMoongchiPopup] 상점 상품 클릭: ShopItemId={data.ShopItemId}, ProductId={data.ProductId}", DebugType.FindMoongchi, this);

            if (data == null)
                return;

            int maxBuyCount = GetMaxBuyCount(data);

            if (maxBuyCount <= 0)
            {
                if (data.IsSoldOut)
                    OpenNotice("구매 횟수를 모두 사용했습니다.");
                else
                    OpenNotice("이벤트 재화가 부족합니다.");

                return;
            }

            DebugTool.Log($"[FindMoongchiPopup] 구매 팝업 열기: ShopItemId={data.ShopItemId}, MaxBuy={maxBuyCount}", DebugType.FindMoongchi, this);
            OpenModalLayer();
            _purchasePopup?.Open(data, maxBuyCount, HandlePurchaseConfirmed);
        }

        private void HandlePurchaseConfirmed(FindMoongchiShopViewData data, int count)
        {
            if (data != null)
                DebugTool.Log($"[FindMoongchiPopup] 구매 확정 요청: ShopItemId={data.ShopItemId}, Count={count}", DebugType.FindMoongchi, this);

            if (data == null || count <= 0)
                return;

            int totalCost = data.CostAmount * count;

            if (_eventCoin < totalCost)
            {
                OpenNotice("이벤트 재화가 부족합니다.");
                return;
            }

            if (data.HasLimit && data.PurchasedCount + count > data.LimitCount)
            {
                OpenNotice("구매 가능 횟수를 초과했습니다.");
                return;
            }

            _eventCoin -= totalCost;

            if (!_shopPurchaseCounts.ContainsKey(data.ShopItemId))
                _shopPurchaseCounts[data.ShopItemId] = 0;

            _shopPurchaseCounts[data.ShopItemId] += count;

            DebugTool.Log($"[FindMoongchiPopup] 구매 완료: ShopItemId={data.ShopItemId}, Count={count}, 남은 이벤트 재화={_eventCoin}", DebugType.FindMoongchi, this);
            _purchasePopup?.Close();
            OpenNotice("구매가 완료되었습니다.");
            RefreshShopPanel();
        }

        private void RefreshGamePanel(bool animateNewReveals = false)
        {
            FindMoongchiGameViewData data = BuildGameViewData();
            DebugTool.Log($"[FindMoongchiPopup] 게임 패널 갱신: Stage={_gameLogic.CurrentStageId}, Board={data.BoardWidth}x{data.BoardHeight}, 탐색기회={data.SearchChance}, 공개타일={data.RevealedTileIndices.Count}, 도구={data.Tools.Count}, 목표이미지={data.TargetVisuals.Count}, 발견목표={_gameLogic.FoundTargetCount}/{_gameLogic.TargetCount}, 연출={animateNewReveals}", DebugType.FindMoongchi, this);
            _gamePanel?.SetData(data, animateNewReveals);
        }

        private FindMoongchiGameViewData BuildGameViewData()
        {
            FindMoongchiGameViewData data = new FindMoongchiGameViewData
            {
                BoardWidth = _gameLogic.BoardWidth,
                BoardHeight = _gameLogic.BoardHeight,
                CurrentWeek = _currentWeek,
                RemainTimeText = _remainTimeText,
                SearchChance = _searchChance,
                EnergySpendProgress = _energySpendProgress,
                EnergySpendTarget = FindMoongchiConstants.EnergySpendTarget,
                RevealedTileIndices = _gameLogic.GetRevealedTileSet()
            };

            foreach (int toolId in _resolvedToolItemIds)
            {
                data.Tools.Add(new FindMoongchiToolViewData
                {
                    ToolItemId = toolId,
                    ToolName = GetItemName(toolId),
                    Icon = GetItemSprite(toolId),
                    Count = FindMoongchiMergeBoardBridge.GetBoardItemCountById(toolId),
                    IsUsable = _searchChance > 0
                });
            }

            foreach (FindMoongchiTargetRuntimeData target in _gameLogic.Targets)
            {
                data.TargetHints.Add(new FindMoongchiTargetHintViewData
                {
                    TargetName = target.TargetName,
                    Icon = null,
                    IconKey = target.IconKey,
                    IsFound = target.IsFound
                });

                data.TargetVisuals.Add(new FindMoongchiTargetVisualViewData
                {
                    TargetId = target.TargetId,
                    TargetName = target.TargetName,
                    Icon = null,
                    IconKey = target.IconKey,
                    IsFound = target.IsFound,
                    CellIndices = new List<int>(target.CellIndices)
                });
            }

            return data;
        }

        private void RefreshMissionPanel()
        {
            List<FindMoongchiMissionViewData> dailyMissions = BuildMissionViewDataList(MoongchiMissionType.DAILY);
            List<FindMoongchiMissionViewData> weeklyMissions = BuildWeeklyMissionViewDataList();

            DebugTool.Log(
                $"[FindMoongchiPopup] 미션 패널 갱신: Daily={dailyMissions.Count}, Weekly={weeklyMissions.Count}, 수령완료={_claimedMissionIds.Count}",
                DebugType.FindMoongchi,
                this);

            _missionPanel?.SetData(dailyMissions, weeklyMissions);
        }

        private List<FindMoongchiMissionViewData> BuildMissionViewDataList(MoongchiMissionType missionType)
        {
            List<FindMoongchiMissionViewData> result = new List<FindMoongchiMissionViewData>();

            if (_dataManager == null)
            {
                DebugTool.Warning($"[FindMoongchiPopup] DataManager가 없어 미션 데이터를 만들 수 없습니다. Type={missionType}", DebugType.FindMoongchi, this);
                return result;
            }

            IReadOnlyList<MoongchiMissionData> missions = _dataManager.GetMissionsByType(missionType);

            for (int i = 0; i < missions.Count; i++)
            {
                MoongchiMissionData mission = missions[i];

                if (mission == null)
                    continue;

                result.Add(BuildMissionViewData(mission));
            }

            return result;
        }

        private List<FindMoongchiMissionViewData> BuildWeeklyMissionViewDataList()
        {
            List<FindMoongchiMissionViewData> result = new List<FindMoongchiMissionViewData>();

            if (_dataManager == null)
            {
                DebugTool.Warning("[FindMoongchiPopup] DataManager가 없어 주간 미션 데이터를 만들 수 없습니다.", DebugType.FindMoongchi, this);
                return result;
            }

            IReadOnlyList<MoongchiMissionData> weeklyMissions = _dataManager.GetWeeklyMissions();

            for (int i = 0; i < weeklyMissions.Count; i++)
            {
                MoongchiMissionData mission = weeklyMissions[i];

                if (mission == null)
                    continue;

                result.Add(BuildMissionViewData(mission));
            }

            return result;
        }

        private FindMoongchiMissionViewData BuildMissionViewDataById(int missionId)
        {
            MoongchiMissionSO missionSO = MissionSO;

            if (missionSO == null || !missionSO.TryGetMission(missionId, out MoongchiMissionData mission))
                return null;

            return BuildMissionViewData(mission);
        }

        private FindMoongchiMissionViewData BuildMissionViewData(MoongchiMissionData mission)
        {
            int currentAmount = _completedMissionMock ? mission.TargetAmount : 0;
            FindMoongchiMissionSlotState state = FindMoongchiMissionSlotState.InProgress;

            if (_claimedMissionIds.Contains(mission.ID))
                state = FindMoongchiMissionSlotState.Claimed;
            else if (currentAmount >= mission.TargetAmount)
                state = FindMoongchiMissionSlotState.Completed;

            return new FindMoongchiMissionViewData
            {
                MissionId = mission.ID,
                MissionType = mission.MissionType,
                MissionDescription = mission.MissionContent,
                CurrentAmount = currentAmount,
                TargetAmount = mission.TargetAmount,
                State = state,
                Reward1 = mission.Reward1,
                Reward2 = mission.Reward2
            };
        }

        private void RefreshShopPanel()
        {
            List<FindMoongchiShopViewData> itemProducts = new List<FindMoongchiShopViewData>();
            List<FindMoongchiShopViewData> profileProducts = new List<FindMoongchiShopViewData>();
            MoongchiShopSO shopSO = ShopSO;

            if (shopSO != null)
            {
                foreach (MoongchiShopItemData item in shopSO.ShopItems)
                {
                    if (item == null)
                        continue;

                    FindMoongchiShopViewData data = BuildShopViewData(item);

                    if (data.ProductType == MoongchiProductType.PROFILE)
                        profileProducts.Add(data);
                    else
                        itemProducts.Add(data);
                }
            }
            else
            {
                DebugTool.Warning("[FindMoongchiPopup] ShopSO가 없어 상점 데이터를 만들 수 없습니다.", DebugType.FindMoongchi, this);
            }

            DebugTool.Log($"[FindMoongchiPopup] 상점 패널 갱신: 이벤트재화={_eventCoin}, 아이템={itemProducts.Count}, 프로필={profileProducts.Count}", DebugType.FindMoongchi, this);
            _shopPanel?.SetData(_eventCoin, itemProducts, profileProducts);
        }

        private FindMoongchiShopViewData BuildShopViewData(MoongchiShopItemData item)
        {
            _shopPurchaseCounts.TryGetValue(item.ID, out int purchasedCount);

            return new FindMoongchiShopViewData
            {
                ShopItemId = item.ID,
                ProductType = item.ProductType,
                ProductId = item.ProductID,
                ProductName = GetProductName(item.ProductType, item.ProductID),
                Icon = GetProductSprite(item.ProductType, item.ProductID),
                Quantity = item.Quantity,
                CostType = item.Cost,
                CostAmount = item.CostAmount,
                LimitCount = item.LimitCount,
                PurchasedCount = purchasedCount
            };
        }

        private int GetMaxBuyCount(FindMoongchiShopViewData data)
        {
            if (data == null || data.CostAmount <= 0)
                return 0;

            int affordableCount = _eventCoin / data.CostAmount;
            int remainingLimit = data.HasLimit ? data.RemainingLimit : int.MaxValue;
            return Mathf.Max(0, Mathf.Min(affordableCount, remainingLimit));
        }

        private void ApplyMissionReward(MoongchiRewardData reward)
        {
            if (reward == null || !reward.IsValid)
                return;

            switch (reward.RewardType)
            {
                case MoongchiCurrencyType.EVENT_COIN:
                    _eventCoin += reward.RewardAmount;
                    DebugTool.Log($"[FindMoongchiPopup] 이벤트 재화 보상 반영: +{reward.RewardAmount}, Current={_eventCoin}", DebugType.FindMoongchi, this);
                    break;
                case MoongchiCurrencyType.ENERGY:
                    DebugTool.Log($"[FindMoongchiPopup] 에너지 보상 지급 예정: {reward.RewardAmount}", DebugType.FindMoongchi, this);
                    break;
            }
        }

        private string GetProductName(MoongchiProductType productType, int productId)
        {
            if (productType == MoongchiProductType.ITEM)
                return GetItemName(productId);

            MoongchiProfileSO profileSO = ProfileSO;
            if (productType == MoongchiProductType.PROFILE && profileSO != null && profileSO.TryGetProfile(productId, out MoongchiProfileData profile))
                return profile.ProfileName;

            if (productType == MoongchiProductType.CURRENCY)
            {
                return productId switch
                {
                    1 => "에너지",
                    2 => "골드",
                    3 => "이벤트 코인",
                    _ => $"재화 {productId}"
                };
            }

            return $"{productType} {productId}";
        }

        private Sprite GetProductSprite(MoongchiProductType productType, int productId)
        {
            if (productType == MoongchiProductType.ITEM)
                return GetItemSprite(productId);

            return null;
        }

        private string GetItemName(int itemId)
        {
            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out ItemData itemData) &&
                itemData != null &&
                !string.IsNullOrEmpty(itemData.ItemName))
            {
                return itemData.ItemName;
            }

            return itemId.ToString();
        }

        private Sprite GetItemSprite(int itemId)
        {
            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out ItemData itemData) &&
                itemData != null)
            {
                return itemData.ItemSprite;
            }

            return null;
        }

        private void OpenModalLayer()
        {
            if (_modalLayer != null)
                _modalLayer.SetActive(true);
        }

        private void CloseAllModal()
        {
            _purchasePopup?.Close();
            _noticePopup?.Close();
            _errorPopup?.Close();

            if (_modalLayer != null)
                _modalLayer.SetActive(false);
        }

        private static void SetActive(MonoBehaviour target, bool isActive)
        {
            if (target != null)
                target.gameObject.SetActive(isActive);
        }

        private void OnDestroy()
        {
            if (_dataManager != null)
                _dataManager.OnLoadCompleted -= HandleDataLoadCompleted;

            if (_mainPanel != null)
            {
                _mainPanel.OnGameButtonClicked -= HandleGameButtonClicked;
                _mainPanel.OnMissionButtonClicked -= HandleMissionButtonClicked;
                _mainPanel.OnShopButtonClicked -= HandleShopButtonClicked;
                _mainPanel.OnCloseButtonClicked -= HandleCloseButtonClicked;
            }

            if (_gamePanel != null)
            {
                _gamePanel.OnBackButtonClicked -= HandleBackToMain;
                _gamePanel.OnToolDropped -= HandleToolDropped;
            }

            if (_missionPanel != null)
            {
                _missionPanel.OnBackButtonClicked -= HandleBackToMain;
                _missionPanel.OnClaimMissionClicked -= HandleClaimMissionClicked;
            }

            if (_shopPanel != null)
            {
                _shopPanel.OnBackButtonClicked -= HandleBackToMain;
                _shopPanel.OnShopItemClicked -= HandleShopItemClicked;
            }
        }
    }
}
