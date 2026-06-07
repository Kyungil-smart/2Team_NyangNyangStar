using System;
using UnityEngine;

[Serializable]
public class NyangNyangSnapCaptureRecord
{
    public Sprite CapturedSprite { get; private set; }
    public NyangNyangSnapPoseData PoseData { get; private set; }
    public NyangNyangSnapScoreResult ScoreResult { get; private set; }

    public int TotalScore => ScoreResult != null ? ScoreResult.TotalScore : 0;

    public NyangNyangSnapCaptureRecord(
        Sprite capturedSprite,
        NyangNyangSnapPoseData poseData,
        NyangNyangSnapScoreResult scoreResult)
    {
        CapturedSprite = capturedSprite;
        PoseData = poseData;
        ScoreResult = scoreResult;
    }
}