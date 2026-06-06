using UnityEngine;

// 전투 컨트롤러,이펙트 풀 이벤트 등록/해제 및 전투 UI 유틸
public partial class ScratchingTimeManager
{
    // 전투 화면 닫기(Exit) 버튼 → 선택 화면 복귀
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


    // 스크래치 터치 → AttackCurrentScratcher 연결
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


    // 흥미도(제한 시간) 소진 → 실패/클리어 판정 연결
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


    // 일일/주간/폴백 전투 화면 모두 숨김
    private void HideBattleScreens()
    {
        _battleController?.Hide();
        _dailyStageController?.Hide();
        _weeklyStageController?.Hide();
    }


    // 현재 화면에 보이는 전투 컨트롤러 반환 (주간 → 일일 → 폴백 순)
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


    // 모든 전투 화면의 흥미도 감소 코루틴 중단
    private void StopBattleInterestDrain()
    {
        _battleController?.StopInterestDrain();
        _dailyStageController?.StopInterestDrain();
        _weeklyStageController?.StopInterestDrain();
    }
}
