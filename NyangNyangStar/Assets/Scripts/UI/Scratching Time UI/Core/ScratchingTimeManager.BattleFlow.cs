using Core.Managers;
using Services.Enums;
using UnityEngine;

// 스테이지 선택,시작, 스크래치 공격, 클리어/실패 판정
public partial class ScratchingTimeManager
{
    // 스테이지 단계를 선택하고 일일/주간 카드 정보를 갱신
    private void SelectStage(int stage)
    {
        if (_isStarted)
            return;

        // 스케줄상 아직 개방되지 않은 단계
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


    // 선택한 단계의 일일 또는 주간 스테이지를 시작
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

        _selectedStageType = stageType;

        int maxDurability = _scratching.GetMaxDurability(_selectedStage, _selectedStageType);
        if (maxDurability <= 0)
        {
            DebugTool.Warning("스크래쳐 내구도 데이터가 없어 스테이지를 시작할 수 없습니다.", DebugType.ScratchingTime);
            _selectedStageType = StageType.None;
            return;
        }

        _isStarted = true;
        ResetAttackCooldown();

        _scratching.StageStart(_selectedStage, _selectedStageType);

        _resultPopupController?.HidePopup();
        _selectionController?.Hide();
        HideBattleScreens();
        CurrentBattleController?.ConfigureInterestDrain(_scratching.TimeLimit);
        CurrentBattleController?.SetInterestAmount(_scratching.TimeLimit, _scratching.TimeLimit);
        CurrentBattleController?.Show();

        RefreshBattleInfo();

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
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

        if (IsAttackCoolingDown())
            return;

        StartAttackCooldown();

        int damage = 0;
        bool isCritical = false;

        // 뭉치 공격력 계산 및 이펙트 표시
        if (_moongchiStatController != null)
        {
            damage = _moongchiStatController.MoongchiAttack(out isCritical);
            CurrentBattleController?.ScratchEffectPool?.SpawnEffect(screenPosition, isCritical);

            if (isCritical)
                GameManager.Audio.PlaySfx("ST_SFX_Critical");
            else
                GameManager.Audio.PlaySfx("ST_SFX_Normal");
        }

        if (damage <= 0)
            return;

        int currentDurability = _scratching.TakeDamageOnScratcher(_selectedStage, _selectedStageType, damage);

        CurrentBattleController?.SetDurabilityAmount(currentDurability, maxDurability);

        if (currentDurability <= 0)
            ShowStageClearResult();
    }

    private bool IsAttackCoolingDown()
    {
        return _attackCooldownSeconds > 0f &&
               Time.unscaledTime < _nextAttackAllowedTime;
    }

    private void StartAttackCooldown()
    {
        if (_attackCooldownSeconds <= 0f)
        {
            _nextAttackAllowedTime = 0f;
            return;
        }

        _nextAttackAllowedTime = Time.unscaledTime + _attackCooldownSeconds;
    }

    private void ResetAttackCooldown()
    {
        _nextAttackAllowedTime = 0f;
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

        // 시간 종료 직전에 내구도가 0이면 클리어로 처리
        if (currentDurability <= 0)
            ShowStageClearResult();
        else
            ShowStageFailResult();
    }


    // 현재 스테이지를 클리어 처리
    // 도전 횟수는 클리어 성공 시점에만 차감하고, 이후 경험치와 클리어 기록을 갱신
    public void ShowStageClearResult()
    {
        if (!_isStarted)
            return;

        int clearExp = _scratching.GetClearExp(_selectedStage, _selectedStageType);

        if (!_scratching.TryConsumeChallengeCount(_selectedStage, _selectedStageType))
        {
            DebugTool.Warning("남은 도전 횟수가 없어 클리어 보상을 처리할 수 없습니다.", DebugType.ScratchingTime);
            RefreshSelectionInfo();
            return;
        }

        _scratching.RecordStageClear(_selectedStage, _selectedStageType);
        SaveScratchingProgressToServer();
        _moongchiStatController?.IncreaseExp(clearExp);

        _scratchingReward?.GiveReward(_selectedStage, _selectedStageType);

        _isStarted = false;
        StopBattleInterestDrain();

        bool canRetry = CanStart(_selectedStageType);
        _resultPopupController?.ShowResultPopup(true, canRetry);
        
        GameManager.Audio.PlaySfx("ST_SFX_Clear");
        DebugTool.Log($"스테이지 클리어 : {_selectedStage} / {_selectedStageType} / EXP {clearExp}", DebugType.ScratchingTime);
    }


    // 현재 스테이지를 실패 처리
    public void ShowStageFailResult()
    {
        if (!_isStarted)
            return;

        _isStarted = false;
        StopBattleInterestDrain();

        bool canRetry = CanStart(_selectedStageType);
        _resultPopupController?.ShowResultPopup(false, canRetry);

        GameManager.Audio.PlaySfx("ST_SFX_Fail");
        DebugTool.Log($"스테이지 실패 : {_selectedStage} / {_selectedStageType}", DebugType.ScratchingTime);
    }
}
