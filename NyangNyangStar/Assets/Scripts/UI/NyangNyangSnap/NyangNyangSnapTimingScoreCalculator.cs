using UnityEngine;

public class NyangNyangSnapTimingScoreCalculator
{
    private const int DefaultScore = 5;
    private const int OptimalCutScore = 20;
    private const int OptimalCutFrameIndex = 3;

    public int Calculate(NyangNyangSnapCatSpriteAnimator animator)
    {
        if (animator == null || !animator.IsPoseAnimation)
            return DefaultScore;

        float elapsedTime = animator.AnimationElapsedTime;
        float frameDuration = animator.CurrentFrameDuration;
        float animationDuration = animator.AnimationDuration;

        if (frameDuration <= 0f || animationDuration <= 0f)
            return DefaultScore;

        float enterDuration =
            OptimalCutFrameIndex * frameDuration;

        float peakEndTime =
            enterDuration + frameDuration;

        // POSE 진입 구간: 5 + 5 × (진행 시간 / 진입 전체 시간)
        if (elapsedTime < enterDuration)
        {
            float enterProgress = Mathf.Clamp01(
                elapsedTime / enterDuration
            );

            float enterScore =
                DefaultScore + 5f * enterProgress;

            return Mathf.FloorToInt(enterScore);
        }

        // 4번째 Sprite가 출력되는 절정 구간
        if (elapsedTime < peakEndTime)
            return OptimalCutScore;

        float exitDuration =
            animationDuration - peakEndTime;

        if (exitDuration <= 0f)
            return OptimalCutScore;

        float elapsedAfterPeak =
            elapsedTime - peakEndTime;

        float exitProgress = Mathf.Clamp01(
            elapsedAfterPeak / exitDuration
        );

        // POSE 종료 구간: 20 - 15 × (절정 이후 지난 시간 / 종료 전체 시간)
        float exitScore =
            OptimalCutScore - 15f * exitProgress;

        return Mathf.FloorToInt(exitScore);
    }
}
