using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class NyangNyangSnapPoseData
{
    [Header("포즈 데이터")]
    [SerializeField] private int _id;

    [Header("포즈 이름")]
    [SerializeField] private string _poseName;

    [Header("포즈 등급")]
    [SerializeField] private NyangNyangSnapPoseGrade _poseGrade;

    [Header("포즈 점수")]
    [SerializeField] private int _poseScore;

    [Header(" 도구 ID")]
    [SerializeField] private int _toolId;

    [Header("고양이")]
    [SerializeField] private string _poseDataId;

    [Header("애니메이션 경로")]
    [SerializeField] private string _animationPath;

    public int ID => _id;
    public string PoseName => _poseName;
    public NyangNyangSnapPoseGrade PoseGrade => _poseGrade;
    public int PoseScore => _poseScore;
    public int ToolId => _toolId;
    public string PoseDataId => _poseDataId;
    public string AnimationPath => _animationPath;

    public NyangNyangSnapPoseData(int id, string poseName, NyangNyangSnapPoseGrade poseGrade, int poseScore, int toolId, string poseDataId, string animationPath)
    {
        _id = id;
        _poseName = poseName;
        _poseGrade = poseGrade;
        _poseScore = poseScore;
        _toolId = toolId;
        _poseDataId = poseDataId;
        _animationPath = animationPath;
    }


}

public enum NyangNyangSnapPoseGrade
{
    None,
    Normal,
    Rare,
    Epic
}
