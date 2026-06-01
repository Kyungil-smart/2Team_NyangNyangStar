using Data.ScriptableObjects.ScratchingTimeSO;
using Core.Managers;
using Data.LibrarySystem;
using Services.Enums;
using UI;
using UnityEngine;

/// <summary>
/// 스크래칭 타임의 선택 상태, 화면 전환, 결과 팝업 흐름을 관리합니다.
/// 전투 입력과 흥미도 감소 로직은 포함하지 않습니다.
/// </summary>
public class ScratchingTimeManager : UIBase
{
    [Header("Data")]
    [SerializeField] private ScratchingSo _scratching;

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
    private bool _isInitialized;
    private bool _isWaitingForDataReady;

    private ScratchingBattleController CurrentBattleController =>
        _selectedStageType == StageType.Weekly
            ? _weeklyStageController ?? _battleController
            : _dailyStageController ?? _battleController;

    private void Awake()
    {
        EnsureRequiredDataLoad();
        Init();
    }

    /// <summary>
    /// 스크래칭 타임 UI 이벤트를 등록합니다.
    /// </summary>
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

    /// <summary>
    /// 스크래칭 타임 UI 이벤트를 해제합니다.
    /// </summary>
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
    }

    /// <summary>
    /// 스크래칭 타임 UI와 뭉치 정보를 초기 상태로 출력합니다.
    /// </summary>
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

        if (!IsRequiredDataReady())
        {
            EnsureRequiredDataLoad();
            DebugTool.Warning("스크래칭 타임 데이터가 아직 준비되지 않았습니다.", DebugType.ScratchingTime);
            return;
        }

        if (!_scratching.IsOpenedBySchedule(stage))
        {
            DebugTool.Warning($"{stage} 단계는 아직 개방되지 않았습니다.", DebugType.ScratchingTime);
            _selectionController?.ShowErrorPanel();
            return;
        }

        _selectionController?.HideErrorPanel();
        _selectedStage = stage;
        _selectedStageType = StageType.None;
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
            DebugTool.Warning("스크래칭 타임 데이터가 아직 준비되지 않았습니다.", DebugType.ScratchingTime);
            return;
        }

        if (!HasSelectedStage || stageType == StageType.None)
        {
            DebugTool.Warning("스테이지를 시작할 수 없는 상태입니다.", DebugType.ScratchingTime);
            return;
        }

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
        CurrentBattleController?.Show();
        CurrentBattleController?.SetInterestAmount(_scratching.TimeLimit, _scratching.TimeLimit);

        RefreshBattleInfo();

        DebugTool.Log($"스테이지 시작 : {_selectedStage} / {_selectedStageType}", DebugType.ScratchingTime);
    }

    /// <summary>
    /// 스크래치 이펙트가 발생한 클릭을 현재 스테이지 공격으로 처리합니다.
    /// </summary>
    private void AttackCurrentScratcher()
    {
        if (!_isStarted || _selectedStageType == StageType.None)
            return;

        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        if (maxDurability <= 0)
        {
            DebugTool.Warning("스크래쳐 내구도 데이터가 없어 공격을 처리할 수 없습니다.", DebugType.ScratchingTime);
            return;
        }

        int damage = _moongchiStatController != null ? _moongchiStatController.MoongchiAttack() : 0;

        if (damage <= 0)
            return;

        int currentDurability = _scratching.TakeDamageOnScratcher(_selectedStage, _selectedStageType, damage);

        CurrentBattleController?.SetDurabilityAmount(currentDurability, maxDurability);

        if (currentDurability <= 0)
            ShowStageClearResult();
    }

    /// <summary>
    /// 제한 시간 안에 스크래쳐 내구도를 0으로 만들지 못하면 실패 처리합니다.
    /// </summary>
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

    /// <summary>
    /// 현재 스테이지를 클리어 처리합니다.
    /// 클리어 시에만 남은 도전 횟수를 차감하고 뭉치 경험치를 지급합니다.
    /// </summary>
    public void ShowStageClearResult()
    {
        if (!_isStarted)
            return;

        int clearExp = _scratching.GetClearExp(_selectedStage, _selectedStageType);

        _scratching.RecordStageClear(_selectedStage, _selectedStageType);
        _moongchiStatController?.IncreaseExp(clearExp);

        _isStarted = false;
        StopBattleInterestDrain();

        bool canRetry = CanStart(_selectedStageType);
        _resultPopupController?.ShowResultPopup(true, canRetry);

        DebugTool.Log($"스테이지 클리어 : {_selectedStage} / {_selectedStageType} / EXP {clearExp}", DebugType.ScratchingTime);
    }

    /// <summary>
    /// 현재 스테이지를 실패 처리합니다.
    /// 실패 시 도전 횟수는 차감하지 않습니다.
    /// </summary>
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

    /// <summary>
    /// 결과 팝업에서 선택 화면으로 돌아갑니다.
    /// 스크래칭 타임을 닫기 전까지 선택된 단계는 유지합니다.
    /// </summary>
    private void BackToSelection()
    {
        BackToSelectionKeepingSelectedStage();
    }

    /// <summary>
    /// 전투 화면 닫기 버튼을 눌렀을 때 전투만 종료하고 선택 화면으로 돌아갑니다.
    /// 도전 횟수와 클리어 기록은 변경하지 않고, 선택된 단계는 유지합니다.
    /// </summary>
    private void BackToSelectionFromBattle()
    {
        BackToSelectionKeepingSelectedStage();
    }

    /// <summary>
    /// 스크래칭 타임 이벤트 UI를 다시 엽니다.
    /// 외부 UI에서 이벤트 버튼을 눌렀을 때 호출할 수 있습니다.
    /// </summary>
    public void OpenEventView()
    {
        EnsureRequiredDataLoad();
        ResetAllState();
    }

    /// <summary>
    /// 스크래칭 타임 이벤트 UI 전체를 닫고 선택 상태를 초기화합니다.
    /// </summary>
    private void CloseEventView()
    {
        _isStarted = false;
        StopBattleInterestDrain();
        _selectedStage = 0;
        _selectedStageType = StageType.None;

        _resultPopupController?.HidePopup();
        HideBattleScreens();
        _selectionController?.Hide();
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

        _resultPopupController?.HidePopup();
        StartStage(_selectedStageType);
    }

    /// <summary>
    /// 선택된 단계를 유지한 채 선택 화면으로 돌아갑니다.
    /// 전투 상태와 선택된 스테이지 타입만 초기화합니다.
    /// </summary>
    private void BackToSelectionKeepingSelectedStage()
    {
        _isStarted = false;
        StopBattleInterestDrain();
        _selectedStageType = StageType.None;

        _resultPopupController?.HidePopup();
        HideBattleScreens();
        _selectionController?.Show();
        _selectionController?.HideErrorPanel();

        RefreshStageButtonUnlockState();

        if (HasSelectedStage)
            RefreshSelectionInfo();
        else
            _selectionController?.InitView();
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

        if (IsRequiredDataReady())
        {
            UnsubscribeDataReady();
            return;
        }

        SubscribeDataReady();
        GameManager.Data.LoadSheets();
    }

    private static void EnsureLocalDataAccess()
    {
        if (LocalDataAccess.Instance != null)
            return;

        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
    }

    private bool IsRequiredDataReady()
    {
        return _scratching != null && _scratching.HasData;
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
        UnsubscribeDataReady();

        if (!gameObject.activeInHierarchy)
            return;

        ResetAllState();
        _moongchiStatController?.PrintMoongchiStat();
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

        controller.OnCloseClicked -= BackToSelectionFromBattle;
        controller.OnCloseClicked += BackToSelectionFromBattle;
    }

    private void UnregisterBattleCloseEvent(ScratchingBattleController controller)
    {
        if (controller == null)
            return;

        controller.OnCloseClicked -= BackToSelectionFromBattle;
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

    private void StopBattleInterestDrain()
    {
        _battleController?.StopInterestDrain();
        _dailyStageController?.StopInterestDrain();
        _weeklyStageController?.StopInterestDrain();
    }
}
