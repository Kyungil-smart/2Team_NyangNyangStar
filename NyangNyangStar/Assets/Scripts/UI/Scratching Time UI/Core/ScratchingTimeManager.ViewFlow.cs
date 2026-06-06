using Services.Enums;

// 선택,전투,결과 화면 전환 및 UI 갱신
public partial class ScratchingTimeManager
{
    // 스크래칭 타임 이벤트 UI를 다시 엶
    // 외부 UI에서 이벤트 버튼을 눌렀을 때 호출 가능
    public void OpenEventView()
    {
        EnsureRequiredDataLoad();
        ResetAllState();
    }


    // 스크래칭 타임 이벤트 UI 전체를 닫고 선택 상태를 초기화
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

        // 선택 화면이 보이는 경우 닫기 애니메이션 후 비활성화
        if (_selectionController != null && _selectionController.gameObject.activeInHierarchy)
        {
            _selectionController.Hide(FinishCloseEventView);
            return;
        }

        FinishCloseEventView();
    }


    // 닫기 애니메이션 완료 후 매니저 오브젝트 비활성화
    private void FinishCloseEventView()
    {
        _selectionController?.Hide();
        _isUiTransitioning = false;
        gameObject.SetActive(false);
    }


    // 결과 팝업에서 같은 단계와 타입으로 다시 시작
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


    // 선택된 단계를 유지한 채 선택 화면으로 돌아감
    // 결과 팝업 돌아가기,전투 Exit 버튼 모두 이 메서드로 처리
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


    // 전투 화면 닫힌 뒤 선택 화면 표시 및 카드 정보 갱신
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


    // 선택 화면의 일일 / 주간 카드 정보를 현재 선택 단계 기준으로 갱신
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


    // 전투 화면의 스테이지 제목과 내구도 게이지를 갱신
    private void RefreshBattleInfo()
    {
        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        int currentDurability = _scratching.GetCurrentDurability(_selectedStage, _selectedStageType);

        CurrentBattleController?.PrintBattleStageTitle(_selectedStage, _selectedStageType);
        CurrentBattleController?.SetDurabilityAmount(currentDurability, maxDurability);
    }


    // 외부 전투 로직에서 내구도 UI만 다시 그릴 때 사용
    public void RefreshDurabilityView()
    {
        if (!_isStarted)
            return;

        RefreshBattleInfo();
    }


    // 외부 흥미도 로직에서 흥미도 게이지 UI만 갱신할 때 사용
    public void SetInterestView(float currentInterest, float maxInterest)
    {
        CurrentBattleController?.SetInterestAmount(currentInterest, maxInterest);
    }


    // 잠긴 단계도 에러 패널을 띄울 수 있도록 단계 버튼 입력은 유지
    private void RefreshStageButtonUnlockState()
    {
        for (int stage = 1; stage <= 4; stage++)
            _selectionController?.SetStageButtonInteractable(stage, true);
    }


    // 스테이지 시작 가능 여부를 확인 (도전 횟수,개방 여부)
    private bool CanStart(StageType stageType)
    {
        return HasSelectedStage && stageType != StageType.None && _scratching.CanChallenge(_selectedStage, stageType);
    }


    // 스크래칭 타임을 최초 선택 화면 상태로 되돌림
    // 스크래칭 타임을 새로 열 때 사용하며, 선택된 단계도 초기화
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
}
