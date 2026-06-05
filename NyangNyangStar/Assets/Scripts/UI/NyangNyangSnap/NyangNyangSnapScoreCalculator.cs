using UnityEngine;
using Util;

public class NyangNyangSnapScoreCalculator
{
    private const int MaxPoseScore = 40;
    private const int MaxCompositionScore = 30;
    private const int MaxReactionScore = 30;

    private const int MaxPoseRawScore = 20;

    public NyangNyangSnapScoreResult Calculate(
        NyangNyangSnapPoseData poseData,
        float compositionRate,
        float reactionRate)
    {
        int poseScore = CalculatePoseScore(poseData);
        int compositionScore = CalculateRateScore(compositionRate, MaxCompositionScore);
        int reactionScore = CalculateRateScore(reactionRate, MaxReactionScore);

        NyangNyangSnapScoreResult result = new(
            poseScore,
            compositionScore,
            reactionScore
        );

        DebugTool.Log(
            $"[냥냥스냅 점수 계산] " +
            $"포즈: {result.PoseScore}/{MaxPoseScore}, " +
            $"구도: {result.CompositionScore}/{MaxCompositionScore}, " +
            $"반응: {result.ReactionScore}/{MaxReactionScore}, " +
            $"총점: {result.TotalScore}/100",
            DebugType.Game
        );

        return result;
    }

    private int CalculatePoseScore(NyangNyangSnapPoseData poseData)
    {
        if (poseData == null)
        {
            DebugTool.Warning("[NyangNyangSnapScoreCalculator] 포즈 데이터가 없습니다. 포즈 점수는 0점 처리됩니다.", DebugType.Game);
            return 0;
        }

        float poseRate = Mathf.Clamp01((float)poseData.PoseScore / MaxPoseRawScore);
        return Mathf.RoundToInt(poseRate * MaxPoseScore);
    }

    private int CalculateRateScore(float rate, int maxScore)
    {
        float clampedRate = Mathf.Clamp01(rate);
        return Mathf.RoundToInt(clampedRate * maxScore);
    }
}