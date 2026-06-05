using System;

[Serializable]
public class NyangNyangSnapScoreResult
{
    private const int MaxTotalScore = 100;
    private const int MaxPoseScore = 40;
    private const int MaxCompositionScore = 30;
    private const int MaxReactionScore = 30;

    public int PoseScore { get; private set; }
    public int CompositionScore { get; private set; }
    public int ReactionScore { get; private set; }
    public int TotalScore { get; private set; }

    public float PoseGaugeValue => PoseScore / (float)MaxPoseScore;
    public float CompositionGaugeValue => CompositionScore / (float)MaxCompositionScore;
    public float ReactionGaugeValue => ReactionScore / (float)MaxReactionScore;
    public float TotalGaugeValue => TotalScore / (float)MaxTotalScore;

    public NyangNyangSnapScoreResult(
        int poseScore,
        int compositionScore,
        int reactionScore)
    {
        PoseScore = poseScore;
        CompositionScore = compositionScore;
        ReactionScore = reactionScore;
        TotalScore = PoseScore + CompositionScore + ReactionScore;
    }
}