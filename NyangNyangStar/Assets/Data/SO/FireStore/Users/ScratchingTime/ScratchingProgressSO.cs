using Data.ScriptableObjects.ScratchingTimeSO;
using Services.Enums;
using UnityEngine;

[FirestorePath("Users/{userId}/ScratchingTime/{docId}")]
[CreateAssetMenu(fileName = "ScratchingProgressSO", menuName = "ScriptableObjects/ScratchingProgressSO")]
public class ScratchingProgressSO : BaseFireStore
{
    [Header("일일 도전 남은 횟수")] [SerializeField]
    private int _remainingDailyChallengeCount = 4;

    [Header("주간 도전 남은 횟수")] [SerializeField]
    private int _remainingWeeklyChallengeCount = 1;

    [Header("스케줄 기준 열린 스테이지")] [SerializeField]
    private int _openedStageBySchedule = 1;

    [Header("일일 최고 클리어 스테이지")] [SerializeField]
    private int _highestClearedDailyStage;

    [Header("주간 최고 클리어 스테이지")] [SerializeField]
    private int _highestClearedWeeklyStage;

    public int RemainingDailyChallengeCount => _remainingDailyChallengeCount;
    public int RemainingWeeklyChallengeCount => _remainingWeeklyChallengeCount;
    public int OpenedStageBySchedule => _openedStageBySchedule;
    public int HighestClearedDailyStage => _highestClearedDailyStage;
    public int HighestClearedWeeklyStage => _highestClearedWeeklyStage;

    public void ApplyTo(ScratchingSo scratching)
    {
        if (scratching == null)
            return;

        scratching.SetProgressData(
            _openedStageBySchedule,
            _remainingDailyChallengeCount,
            _remainingWeeklyChallengeCount,
            _highestClearedDailyStage,
            _highestClearedWeeklyStage);
    }

    public void CaptureFrom(ScratchingSo scratching)
    {
        if (scratching == null)
            return;

        _openedStageBySchedule = scratching.GetOpenedStageBySchedule();
        _remainingDailyChallengeCount = scratching.GetRemainingChallengeCount(StageType.Daily);
        _remainingWeeklyChallengeCount = scratching.GetRemainingChallengeCount(StageType.Weekly);
        _highestClearedDailyStage = scratching.GetHighestClearedStage(StageType.Daily);
        _highestClearedWeeklyStage = scratching.GetHighestClearedStage(StageType.Weekly);
    }
}