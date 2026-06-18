using UnityEngine;
using Util;

public class NyangNyangSnapScoreCalculator
{
    private const int MaxPoseScore = 25;
    private const int MaxCompositionScore = 25;
    private const int MaxBackgroundScore = 25;
    private const int MaxTimingScore = 25;

    public NyangNyangSnapScoreResult Calculate(
        NyangNyangSnapPoseData poseData,
        float compositionRate,
        int backgroundScore,
        float timingRate)
    {
        int calculatedPoseScore =
            CalculatePoseScore(poseData);

        int calculatedCompositionScore =
            CalculateRateScore(
                compositionRate,
                MaxCompositionScore
            );

        int calculatedBackgroundScore =
            Mathf.Clamp(
                backgroundScore,
                0,
                MaxBackgroundScore
            );

        int calculatedTimingScore =
            CalculateRateScore(
                timingRate,
                MaxTimingScore
            );

        NyangNyangSnapScoreResult result = new(
            calculatedPoseScore,
            calculatedCompositionScore,
            calculatedBackgroundScore,
            calculatedTimingScore
        );

        DebugTool.Log(
            $"[냥냥스냅 점수 계산] " +
            $"포즈: {result.PoseScore}/{MaxPoseScore}, " +
            $"구도: {result.CompositionScore}/{MaxCompositionScore}, " +
            $"배경: {result.BackgroundScore}/{MaxBackgroundScore}, " +
            $"타이밍: {result.TimingScore}/{MaxTimingScore}, " +
            $"총점: {result.TotalScore}/100",
            DebugType.Game
        );

        return result;
    }

    private int CalculatePoseScore(
        NyangNyangSnapPoseData poseData)
    {
        if (poseData == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapScoreCalculator] " +
                "포즈 데이터가 없어 포즈 점수를 0점으로 처리합니다.",
                DebugType.Game
            );

            return 0;
        }

        // Sheets의 포즈 점수가 이미 0~25점 기준이므로 그대로 사용합니다.
        return Mathf.Clamp(
            poseData.PoseScore,
            0,
            MaxPoseScore
        );
    }

    private int CalculateRateScore(
        float rate,
        int maxScore)
    {
        float clampedRate =
            Mathf.Clamp01(rate);

        return Mathf.RoundToInt(
            clampedRate * maxScore
        );
    }
}