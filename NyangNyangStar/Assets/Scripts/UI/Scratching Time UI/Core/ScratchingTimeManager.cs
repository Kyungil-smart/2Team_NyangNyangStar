using System;
using System.Collections;
using Data.Parsing;
using Data.ScriptableObjects.ScratchingTimeSO;
using Core.Managers;
using Data.LibrarySystem;
using Services.Enums;
using UI;
using UnityEngine;

// 스크래칭 타임의 선택 상태, 화면 전환, 결과 팝업 흐름을 관리
public class ScratchingTimeManager : UIBase
{
    [Header("Data")]
    [SerializeField] private ScratchingSo _scratching;
    [SerializeField] private ScratchingProgressSO _scratchingProgress;
    [Tooltip("스크래칭 밸런스 시트 URL. SheetLoader.prefab scratchingURL과 동일하게 유지")]
    [SerializeField]
    private string _scratchingSheetFallbackUrl =
        "https://docs.google.com/spreadsheets/d/1hD2NCEzCWr2aml-_aqHsHZu_8HeZ0xCLpMDt_A9NfoU/edit?gid=0#gid=0";

    [Header("Controller")]
    [SerializeField] private ScratchingTimeController _selectionController;
    [SerializeField] private ScratchingBattleController _battleController;
    [SerializeField] private ScratchingBattleController _dailyStageController;
    [SerializeField] private ScratchingBattleController _weeklyStageController;
    [SerializeField] private ResultPopupController _resultPopupController;
    [SerializeField] private MoongchiStatController _moongchiStatController;

    [Header("Current Stage State")]
    [SerializeField] private int _selectedStage;
    [SerializeField] private StageType _selectedStageType = StageType.None;
    [SerializeField] private bool _isStarted;

    private bool HasSelectedStage => _selectedStage > 0;
    private bool _isUiTransitioning;
    private bool _isInitialized;
    private bool _isWaitingForDataReady;
    private Coroutine _scratchingDataLoadCoroutine;

    private const int ScratchingSheetHeaderRowCount = 1;

    private ScratchingBattleController CurrentBattleController =>
        _selectedStageType == StageType.Weekly
            ? _weeklyStageController ?? _battleController
            : _dailyStageController ?? _battleController;

    private void Awake()
    {
        EnsureRequiredDataLoad();
        Init();
    }
    
    // 스크래칭 타임 UI 이벤트를 등록
    private void OnEnable()
    {
        if (_selectionController != null)
        {
            _selectionController.OnCloseClicked += CloseEventView;
            _selectionController.OnStageSelected += SelectStage;
            _selectionController.OnStageStartClicked += StartStage;
        }

        RegisterBattleCloseEvent(_battleController);
        RegisterBattleCloseEvent(_dailyStageController);
        RegisterBattleCloseEvent(_weeklyStageController);
        RegisterScratchAttackEvent(_battleController);
        RegisterScratchAttackEvent(_dailyStageController);
        RegisterScratchAttackEvent(_weeklyStageController);
        RegisterInterestDepletedEvent(_battleController);
        RegisterInterestDepletedEvent(_dailyStageController);
        RegisterInterestDepletedEvent(_weeklyStageController);

        if (_resultPopupController != null)
        {
            _resultPopupController.OnBackClicked += BackToSelection;
            _resultPopupController.OnRetryClicked += RetryStage;
        }
    }

    // 이벤트 해제
    private void OnDisable()
    {
        if (_selectionController != null)
        {
            _selectionController.OnCloseClicked -= CloseEventView;
            _selectionController.OnStageSelected -= SelectStage;
            _selectionController.OnStageStartClicked -= StartStage;
        }

        UnregisterBattleCloseEvent(_battleController);
        UnregisterBattleCloseEvent(_dailyStageController);
        UnregisterBattleCloseEvent(_weeklyStageController);
        UnregisterScratchAttackEvent(_battleController);
        UnregisterScratchAttackEvent(_dailyStageController);
        UnregisterScratchAttackEvent(_weeklyStageController);
        UnregisterInterestDepletedEvent(_battleController);
        UnregisterInterestDepletedEvent(_dailyStageController);
        UnregisterInterestDepletedEvent(_weeklyStageController);

        if (_resultPopupController != null)
        {
            _resultPopupController.OnBackClicked -= BackToSelection;
            _resultPopupController.OnRetryClicked -= RetryStage;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeDataReady();
        StopScratchingDataLoad();
    }

    
    // 스크래칭 타임 UI와 뭉치 정보를 초기 상태로 출력
    private void Start()
    {
        ResetAllState();
        _moongchiStatController?.PrintMoongchiStat();
    }

    /// <summary>
    /// 스테이지 단계를 선택하고 일일/주간 카드 정보를 갱신합니다.
    /// </summary>
    /// <param name="stage">선택한 스테이지 단계</param>
    private void SelectStage(int stage)
    {
        if (_isStarted)
            return;

        if (_scratching != null && !_scratching.IsOpenedBySchedule(stage))
        {
            DebugTool.Warning($"{stage} 단계는 아직 개방되지 않았습니다.", DebugType.ScratchingTime);
            _selectionController?.ShowErrorPanel();
            return;
        }

        if (!IsRequiredDataReady())
        {
            EnsureRequiredDataLoad();
            DebugTool.Warning("스크래칭 타임 시트 데이터 로드 중입니다.", DebugType.ScratchingTime);
            return;
        }

        _selectionController?.HideErrorPanel();
        _selectedStage = stage;
        _selectedStageType = StageType.None;
        RefreshSelectionInfo();
    }

    /// <summary>
    /// 선택한 단계의 일일 또는 주간 스테이지를 시작합니다.
    /// </summary>
    /// <param name="stageType">시작할 스테이지 타입</param>
    private void StartStage(StageType stageType)
    {
        if (!IsRequiredDataReady())
        {
            EnsureRequiredDataLoad();
            DebugTool.Warning("스크래칭 타임 시트 데이터 로드 중입니다.", DebugType.ScratchingTime);
            return;
        }

        if (!HasSelectedStage || stageType == StageType.None)
        {
            DebugTool.Warning("스테이지를 시작할 수 없는 상태입니다.", DebugType.ScratchingTime);
            return;
        }

        if (!CanStart(stageType))
        {
            DebugTool.Warning("남은 도전 횟수가 없거나 아직 개방되지 않은 스테이지입니다.", DebugType.ScratchingTime);
            RefreshSelectionInfo();
            return;
        }

        if (!_scratching.TryConsumeChallengeCount(_selectedStage, stageType))
        {
            DebugTool.Warning("남은 도전 횟수가 없어 스테이지를 시작할 수 없습니다.", DebugType.ScratchingTime);
            RefreshSelectionInfo();
            return;
        }

        SaveScratchingProgressToServer();

        _selectedStageType = stageType;

        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        if (maxDurability <= 0)
        {
            DebugTool.Warning("스크래쳐 내구도 데이터가 없어 스테이지를 시작할 수 없습니다.", DebugType.ScratchingTime);
            _selectedStageType = StageType.None;
            return;
        }

        _isStarted = true;

        _scratching.StageStart(_selectedStage, _selectedStageType);

        _resultPopupController?.HidePopup();
        _selectionController?.Hide();
        HideBattleScreens();
        CurrentBattleController?.ConfigureInterestDrain(_scratching.TimeLimit);
        CurrentBattleController?.SetInterestAmount(_scratching.TimeLimit, _scratching.TimeLimit);
        CurrentBattleController?.Show();

        RefreshBattleInfo();

        DebugTool.Log($"스테이지 시작 : {_selectedStage} / {_selectedStageType}", DebugType.ScratchingTime);
    }

    
    // 스크래치 이펙트가 발생한 클릭을 현재 스테이지 공격으로 처리
    
    private void AttackCurrentScratcher(Vector2 screenPosition)
    {
        if (!_isStarted || _selectedStageType == StageType.None)
            return;

        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        if (maxDurability <= 0)
        {
            DebugTool.Warning("스크래쳐 내구도 데이터가 없어 공격을 처리할 수 없습니다.", DebugType.ScratchingTime);
            return;
        }

        int damage = 0;
        bool isCritical = false;

        if (_moongchiStatController != null)
        {
            damage = _moongchiStatController.MoongchiAttack(out isCritical);
            CurrentBattleController?.ScratchEffectPool?.SpawnEffect(screenPosition, isCritical);
        }

        if (damage <= 0)
            return;

        int currentDurability = _scratching.TakeDamageOnScratcher(_selectedStage, _selectedStageType, damage);

        CurrentBattleController?.SetDurabilityAmount(currentDurability, maxDurability);

        if (currentDurability <= 0)
            ShowStageClearResult();
    }

    
    // 제한 시간 안에 스크래쳐 내구도를 0으로 만들지 못하면 실패 처리
    private void FailCurrentStageOnInterestDepleted()
    {
        if (!_isStarted || _selectedStageType == StageType.None)
            return;

        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        if (maxDurability <= 0)
        {
            DebugTool.Warning("스크래쳐 내구도 데이터가 없어 실패/클리어를 판정할 수 없습니다.", DebugType.ScratchingTime);
            return;
        }

        int currentDurability = _scratching.GetCurrentDurability(_selectedStage, _selectedStageType);

        if (currentDurability <= 0)
            ShowStageClearResult();
        else
            ShowStageFailResult();
    }

    
    // 현재 스테이지를 클리어 처리
    // 도전 횟수는 START 시점에 차감되며, 클리어 시 경험치와 클리어 기록만 갱신
    public void ShowStageClearResult()
    {
        if (!_isStarted)
            return;

        int clearExp = _scratching.GetClearExp(_selectedStage, _selectedStageType);

        _scratching.RecordStageClear(_selectedStage, _selectedStageType);
        SaveScratchingProgressToServer();
        _moongchiStatController?.IncreaseExp(clearExp);

        _isStarted = false;
        StopBattleInterestDrain();

        bool canRetry = CanStart(_selectedStageType);
        _resultPopupController?.ShowResultPopup(true, canRetry);

        DebugTool.Log($"스테이지 클리어 : {_selectedStage} / {_selectedStageType} / EXP {clearExp}", DebugType.ScratchingTime);
    }

    
    // 현재 스테이지를 실패 처리
    // 도전 횟수는 START 시점에 이미 차감된 상태
    
    public void ShowStageFailResult()
    {
        if (!_isStarted)
            return;

        _isStarted = false;
        StopBattleInterestDrain();

        bool canRetry = CanStart(_selectedStageType);
        _resultPopupController?.ShowResultPopup(false, canRetry);

        DebugTool.Log($"스테이지 실패 : {_selectedStage} / {_selectedStageType}", DebugType.ScratchingTime);
    }

    
    // 스크래칭 타임 이벤트 UI를 다시 엽니다.
    // 외부 UI에서 이벤트 버튼을 눌렀을 때 호출할 수 있습니다.
    
    public void OpenEventView()
    {
        EnsureRequiredDataLoad();
        ResetAllState();
    }

    
    /// 스크래칭 타임 이벤트 UI 전체를 닫고 선택 상태를 초기화
    
    private void CloseEventView()
    {
        if (_isUiTransitioning)
            return;

        _isStarted = false;
        StopBattleInterestDrain();
        _selectedStage = 0;
        _selectedStageType = StageType.None;
        _isUiTransitioning = true;

        _resultPopupController?.HidePopup();
        HideBattleScreens();

        if (_selectionController != null && _selectionController.gameObject.activeInHierarchy)
        {
            _selectionController.Hide(FinishCloseEventView);
            return;
        }

        FinishCloseEventView();
    }

    private void FinishCloseEventView()
    {
        _selectionController?.Hide();
        _isUiTransitioning = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 결과 팝업에서 같은 단계와 타입으로 다시 시작합니다.
    /// </summary>
    private void RetryStage()
    {
        if (!CanStart(_selectedStageType))
        {
            DebugTool.Warning("남은 도전 횟수가 없거나 아직 개방되지 않은 스테이지입니다.", DebugType.ScratchingTime);
            _resultPopupController?.SetRetryButtonInteractable(false);
            return;
        }

        _resultPopupController?.HidePopup(() => StartStage(_selectedStageType));
    }

    /// <summary>
    /// 선택된 단계를 유지한 채 선택 화면으로 돌아갑니다.
    /// 결과 팝업 돌아가기·전투 Exit 버튼 모두 이 메서드로 처리합니다.
    /// </summary>
    private void BackToSelection()
    {
        if (_isUiTransitioning)
            return;

        ScratchingBattleController visibleBattle = GetVisibleBattleController();

        _isStarted = false;
        StopBattleInterestDrain();
        _selectedStageType = StageType.None;
        _isUiTransitioning = true;

        _resultPopupController?.HidePopup(() =>
        {
            if (visibleBattle != null && visibleBattle.gameObject.activeInHierarchy)
            {
                visibleBattle.Hide(ShowSelectionAfterBattleClosed);
                return;
            }

            HideBattleScreens();
            ShowSelectionAfterBattleClosed();
        });
    }

    private void ShowSelectionAfterBattleClosed()
    {
        HideBattleScreens();
        _selectionController?.Show();
        _selectionController?.HideErrorPanel();

        RefreshStageButtonUnlockState();

        if (HasSelectedStage)
            RefreshSelectionInfo();
        else
            _selectionController?.InitView();

        _isUiTransitioning = false;
    }

    /// <summary>
    /// 선택 화면의 일일/주간 카드 정보를 현재 선택 단계 기준으로 갱신합니다.
    /// </summary>
    private void RefreshSelectionInfo()
    {
        if (!HasSelectedStage)
            return;

        int dailyRemainingCount = _scratching.GetRemainingChallengeCount(_selectedStage, StageType.Daily);
        int dailyMaxCount = _scratching.GetMaxChallengeCount(_selectedStage, StageType.Daily);
        int dailyClearExp = _scratching.GetClearExp(_selectedStage, StageType.Daily);
        bool canStartDaily = CanStart(StageType.Daily);

        int weeklyRemainingCount = _scratching.GetRemainingChallengeCount(_selectedStage, StageType.Weekly);
        int weeklyMaxCount = _scratching.GetMaxChallengeCount(_selectedStage, StageType.Weekly);
        int weeklyClearExp = _scratching.GetClearExp(_selectedStage, StageType.Weekly);
        bool canStartWeekly = CanStart(StageType.Weekly);

        _selectionController?.PrintStageCardInfo(
            dailyRemainingCount,
            dailyMaxCount,
            canStartDaily,
            weeklyRemainingCount,
            weeklyMaxCount,
            canStartWeekly);
    }

    /// <summary>
    /// 전투 화면의 스테이지 제목과 내구도 게이지를 갱신합니다.
    /// </summary>
    private void RefreshBattleInfo()
    {
        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        int currentDurability = _scratching.GetCurrentDurability(_selectedStage, _selectedStageType);

        CurrentBattleController?.PrintBattleStageTitle(_selectedStage, _selectedStageType);
        CurrentBattleController?.SetDurabilityAmount(currentDurability, maxDurability);
    }

    /// <summary>
    /// 외부 전투 로직에서 내구도 UI만 다시 그릴 때 사용합니다.
    /// </summary>
    public void RefreshDurabilityView()
    {
        if (!_isStarted)
            return;

        RefreshBattleInfo();
    }

    /// <summary>
    /// 외부 흥미도 로직에서 흥미도 게이지 UI만 갱신할 때 사용합니다.
    /// </summary>
    /// <param name="currentInterest">현재 흥미도 값</param>
    /// <param name="maxInterest">최대 흥미도 값</param>
    public void SetInterestView(float currentInterest, float maxInterest)
    {
        CurrentBattleController?.SetInterestAmount(currentInterest, maxInterest);
    }

    /// <summary>
    /// 잠긴 단계도 에러 패널을 띄울 수 있도록 단계 버튼 입력은 유지합니다.
    /// </summary>
    private void RefreshStageButtonUnlockState()
    {
        for (int stage = 1; stage <= 4; stage++)
            _selectionController?.SetStageButtonInteractable(stage, true);
    }

    /// <summary>
    /// 스테이지 시작 가능 여부를 확인합니다.
    /// </summary>
    /// <param name="stageType">확인할 스테이지 타입</param>
    /// <returns>선택 단계와 도전 횟수 조건을 만족하면 true</returns>
    private bool CanStart(StageType stageType)
    {
        return HasSelectedStage && stageType != StageType.None && _scratching.CanChallenge(_selectedStage, stageType);
    }

    /// <summary>
    /// 스크래칭 타임을 최초 선택 화면 상태로 되돌립니다.
    /// 스크래칭 타임을 새로 열 때 사용하며, 선택된 단계도 초기화합니다.
    /// </summary>
    private void ResetAllState()
    {
        _isUiTransitioning = false;
        _isStarted = false;
        _selectedStage = 0;
        _selectedStageType = StageType.None;

        _resultPopupController?.HidePopup();
        HideBattleScreens();
        _selectionController?.Show();
        _selectionController?.InitView();
        RefreshStageButtonUnlockState();
    }

    private void EnsureRequiredDataLoad()
    {
        GameManager.Init();
        EnsureLocalDataAccess();

        if (_scratching == null)
        {
            DebugTool.Warning(
                "ScratchingSo 참조가 비어 있습니다. ScratchingTimeScreen 프리팹의 Scratching Time Manager에 SO를 연결하세요.",
                DebugType.ScratchingTime);
            return;
        }

        if (IsRequiredDataReady())
        {
            UnsubscribeDataReady();
            StopScratchingDataLoad();
            return;
        }

        SubscribeDataReady();
        StartScratchingDataLoad();
    }

    private void StartScratchingDataLoad()
    {
        if (_scratchingDataLoadCoroutine != null)
            return;

        _scratchingDataLoadCoroutine = StartCoroutine(LoadScratchingDataRoutine());
    }

    private void StopScratchingDataLoad()
    {
        if (_scratchingDataLoadCoroutine == null)
            return;

        StopCoroutine(_scratchingDataLoadCoroutine);
        _scratchingDataLoadCoroutine = null;
    }

    /// <summary>
    /// 스크래칭 시트만 즉시 요청합니다. SheetLoader 긴 대기 없이 네트워크 1회로 완료합니다.
    /// </summary>
    private IEnumerator LoadScratchingDataRoutine()
    {
        if (IsRequiredDataReady())
        {
            OnScratchingDataLoaded();
            yield break;
        }

        yield return LoadScratchingSheetDirectly();

        _scratchingDataLoadCoroutine = null;

        if (IsRequiredDataReady())
            OnScratchingDataLoaded();
        else
            DebugTool.Warning(
                "스크래칭 타임 시트 로드에 실패했습니다. Fallback URL·시트 공개 설정을 확인하세요.",
                DebugType.ScratchingTime);
    }

    private void OnScratchingDataLoaded()
    {
        UnsubscribeDataReady();

        if (!gameObject.activeInHierarchy)
            return;

        ResetAllState();
        LoadScratchingProgressFromServer();
        _moongchiStatController?.PrintMoongchiStat();

        if (HasSelectedStage)
            RefreshSelectionInfo();
    }

    /// <summary>
    /// Google Sheets에서 스크래칭 밸런스만 받아 SO에 채웁니다.
    /// </summary>
    private IEnumerator LoadScratchingSheetDirectly()
    {
        if (_scratching == null || string.IsNullOrWhiteSpace(_scratchingSheetFallbackUrl))
            yield break;

        _scratching.Init();
        SheetData sheet = new(_scratchingSheetFallbackUrl.Trim(), SheetType.TSV);

        yield return sheet.Load(ParseScratchingSheetLines);
    }

    private void ParseScratchingSheetLines(char split, string[] lines)
    {
        if (lines == null || lines.Length <= ScratchingSheetHeaderRowCount)
            return;

        for (int i = ScratchingSheetHeaderRowCount; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] cols = line.Split(split);
            if (cols.Length < 5)
                continue;

            _scratching.SetData(cols);
        }
    }

    private static void EnsureLocalDataAccess()
    {
        if (LocalDataAccess.Instance != null)
            return;

        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
    }

    private bool IsRequiredDataReady()
        => _scratching != null && _scratching.HasData;

    private void RefreshInitialSelectionInfo()
    {
        if (HasSelectedStage)
        {
            RefreshSelectionInfo();
            return;
        }

        _selectedStage = 1;
        _selectedStageType = StageType.None;
        RefreshSelectionInfo();
    }

    private async void LoadScratchingProgressFromServer()
    {
        if (_scratchingProgress == null || _scratching == null)
            return;

        try
        {
            await _scratchingProgress.UpdateFromServerAsync(false);
            _scratchingProgress.ApplyTo(_scratching);
            RefreshStageButtonUnlockState();
            RefreshInitialSelectionInfo();
        }
        catch (Exception e)
        {
            DebugTool.Warning($"ScratchingProgressSO 불러오기 실패: {e.Message}", DebugType.ScratchingTime);
        }
    }

    private async void SaveScratchingProgressToServer()
    {
        if (_scratchingProgress == null || _scratching == null)
            return;

        _scratchingProgress.CaptureFrom(_scratching);

        try
        {
            await _scratchingProgress.UpdateDataAsync();
        }
        catch (Exception e)
        {
            DebugTool.Warning($"ScratchingProgressSO 저장 실패: {e.Message}", DebugType.ScratchingTime);
        }
    }

    private void SubscribeDataReady()
    {
        if (LocalDataAccess.Instance == null || _isWaitingForDataReady)
            return;

        LocalDataAccess.Instance.Game.OnReady += HandleDataReady;
        _isWaitingForDataReady = true;
    }

    private void UnsubscribeDataReady()
    {
        if (LocalDataAccess.Instance != null && _isWaitingForDataReady)
            LocalDataAccess.Instance.Game.OnReady -= HandleDataReady;

        _isWaitingForDataReady = false;
    }

    private void HandleDataReady()
    {
        if (!IsRequiredDataReady())
            return;

        OnScratchingDataLoaded();
    }

    public override void Init()
    {
        if (_isInitialized)
            return;

        // ScratchingTimeUI Addressables 스프라이트 적용임
        GetOrAddController<ScratchingTimeUISprite>("ScratchingTimeUI")?.Init();

        _selectionController ??= GetOrAddController<ScratchingTimeController>("ScratchingTimeUI");
        _dailyStageController ??= GetOrAddController<ScratchingBattleController>("ScratchingDailyStageUI");
        _weeklyStageController ??= GetOrAddController<ScratchingBattleController>("ScratchingWeekStageUI");
        _battleController ??= _dailyStageController;
        _resultPopupController ??= GetOrAddController<ResultPopupController>("ResultPopup");
        _moongchiStatController ??= GetOrAddController<MoongchiStatController>("ScratchingTimeUI");

        _selectionController?.Init();
        _dailyStageController?.Init();
        _weeklyStageController?.Init();
        _resultPopupController?.Init();
        _moongchiStatController?.Init();

        _isInitialized = true;
    }

    private T GetOrAddController<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            DebugTool.Warning($"{childName} 오브젝트를 찾을 수 없습니다.", DebugType.ScratchingTime);
            return null;
        }

        T controller = child.GetComponent<T>();

        if (controller == null)
            controller = child.gameObject.AddComponent<T>();

        return controller;
    }

    private void RegisterBattleCloseEvent(ScratchingBattleController controller)
    {
        if (controller == null)
            return;

        controller.OnCloseClicked -= BackToSelection;
        controller.OnCloseClicked += BackToSelection;
    }

    private void UnregisterBattleCloseEvent(ScratchingBattleController controller)
    {
        if (controller == null)
            return;

        controller.OnCloseClicked -= BackToSelection;
    }

    private void RegisterScratchAttackEvent(ScratchingBattleController controller)
    {
        if (controller?.ScratchEffectPool == null)
            return;

        controller.ScratchEffectPool.OnScratchClicked -= AttackCurrentScratcher;
        controller.ScratchEffectPool.OnScratchClicked += AttackCurrentScratcher;
    }

    private void UnregisterScratchAttackEvent(ScratchingBattleController controller)
    {
        if (controller?.ScratchEffectPool == null)
            return;

        controller.ScratchEffectPool.OnScratchClicked -= AttackCurrentScratcher;
    }

    private void RegisterInterestDepletedEvent(ScratchingBattleController controller)
    {
        if (controller?.InterestController == null)
            return;

        controller.InterestController.OnDepleted -= FailCurrentStageOnInterestDepleted;
        controller.InterestController.OnDepleted += FailCurrentStageOnInterestDepleted;
    }

    private void UnregisterInterestDepletedEvent(ScratchingBattleController controller)
    {
        if (controller?.InterestController == null)
            return;

        controller.InterestController.OnDepleted -= FailCurrentStageOnInterestDepleted;
    }

    private void HideBattleScreens()
    {
        _battleController?.Hide();
        _dailyStageController?.Hide();
        _weeklyStageController?.Hide();
    }

    private ScratchingBattleController GetVisibleBattleController()
    {
        if (_weeklyStageController != null && _weeklyStageController.gameObject.activeInHierarchy)
            return _weeklyStageController;

        if (_dailyStageController != null && _dailyStageController.gameObject.activeInHierarchy)
            return _dailyStageController;

        if (_battleController != null && _battleController.gameObject.activeInHierarchy)
            return _battleController;

        return null;
    }

    private void StopBattleInterestDrain()
    {
        _battleController?.StopInterestDrain();
        _dailyStageController?.StopInterestDrain();
        _weeklyStageController?.StopInterestDrain();
    }
}
