using System;

[Serializable]
public class NyangNyangSnapScoreResult
{
    private const int MaxTotalScore = 100;

    private const int MaxPoseScore = 25;
    private const int MaxCompositionScore = 25;
    private const int MaxBackgroundScore = 25;
    private const int MaxTimingScore = 25;

    public int PoseScore { get; private set; }
    public int CompositionScore { get; private set; }
    public int BackgroundScore { get; private set; }
    public int TimingScore { get; private set; }
    public int TotalScore { get; private set; }

    public float PoseGaugeValue =>
        PoseScore / (float)MaxPoseScore;

    public float CompositionGaugeValue =>
        CompositionScore / (float)MaxCompositionScore;

    public float BackgroundGaugeValue =>
        BackgroundScore / (float)MaxBackgroundScore;

    public float TimingGaugeValue =>
        TimingScore / (float)MaxTimingScore;

    public float TotalGaugeValue =>
        TotalScore / (float)MaxTotalScore;

    public NyangNyangSnapScoreResult(
        int poseScore,
        int compositionScore,
        int backgroundScore,
        int timingScore)
    {
        PoseScore = Math.Clamp(
            poseScore,
            0,
            MaxPoseScore
        );

        CompositionScore = Math.Clamp(
            compositionScore,
            0,
            MaxCompositionScore
        );

        BackgroundScore = Math.Clamp(
            backgroundScore,
            0,
            MaxBackgroundScore
        );

        TimingScore = Math.Clamp(
            timingScore,
            0,
            MaxTimingScore
        );

        TotalScore =
            PoseScore +
            CompositionScore +
            BackgroundScore +
            TimingScore;
    }
}