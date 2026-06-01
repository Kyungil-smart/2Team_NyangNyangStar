using Data.ScriptableObjects.ScratchingTimeSO;
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
    [SerializeField] private ResultPopupController _resultPopupController;
    [SerializeField] private MoongchiStatController _moongchiStatController;

    [Header("Current Stage State")]
    [SerializeField] private int _selectedStage;
    [SerializeField] private StageType _selectedStageType = StageType.None;
    [SerializeField] private bool _isStarted;

    private bool HasSelectedStage => _selectedStage > 0;

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

        if (_battleController != null)
            _battleController.OnCloseClicked += BackToSelectionFromBattle;

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

        if (_battleController != null)
            _battleController.OnCloseClicked -= BackToSelectionFromBattle;

        if (_resultPopupController != null)
        {
            _resultPopupController.OnBackClicked -= BackToSelection;
            _resultPopupController.OnRetryClicked -= RetryStage;
        }
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

        if (!_scratching.IsOpenedBySchedule(stage))
        {
            DebugTool.Warning($"{stage} 단계는 아직 개방되지 않았습니다.", DebugType.ScratchingTime);
            return;
        }

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
        if (!CanStart(stageType))
        {
            DebugTool.Warning("스테이지를 시작할 수 없는 상태입니다.", DebugType.ScratchingTime);
            return;
        }

        _selectedStageType = stageType;
        _isStarted = true;

        _scratching.StageStart(_selectedStage, _selectedStageType);

        _resultPopupController?.HidePopup();
        _selectionController?.Hide();
        _battleController?.Show();
        _battleController?.SetInterestAmount(_scratching.TimeLimit, _scratching.TimeLimit);

        RefreshBattleInfo();

        DebugTool.Log($"스테이지 시작 : {_selectedStage} / {_selectedStageType}", DebugType.ScratchingTime);
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
        ResetAllState();
    }

    /// <summary>
    /// 스크래칭 타임 이벤트 UI 전체를 닫고 선택 상태를 초기화합니다.
    /// </summary>
    private void CloseEventView()
    {
        _isStarted = false;
        _selectedStage = 0;
        _selectedStageType = StageType.None;

        _resultPopupController?.HidePopup();
        _battleController?.Hide();
        _selectionController?.Hide();
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
        _selectedStageType = StageType.None;

        _resultPopupController?.HidePopup();
        _battleController?.Hide();
        _selectionController?.Show();

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

        _battleController?.PrintBattleStageTitle(_selectedStage, _selectedStageType);
        _battleController?.SetDurabilityAmount(currentDurability, maxDurability);
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
        _battleController?.SetInterestAmount(currentInterest, maxInterest);
    }

    /// <summary>
    /// 이벤트 주차 기준으로 개방된 단계 버튼만 선택 가능하도록 갱신합니다.
    /// </summary>
    private void RefreshStageButtonUnlockState()
    {
        int openedStage = _scratching.GetOpenedStageBySchedule();

        for (int stage = 1; stage <= 4; stage++)
            _selectionController?.SetStageButtonInteractable(stage, stage <= openedStage);
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
        _battleController?.Hide();
        _selectionController?.Show();
        _selectionController?.InitView();
        RefreshStageButtonUnlockState();
    }

    public override void Init()
    {
        
    }
}
