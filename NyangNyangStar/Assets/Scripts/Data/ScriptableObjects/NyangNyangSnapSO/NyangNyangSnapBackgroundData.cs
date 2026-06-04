using System;
using UnityEngine;

[Serializable]
public class NyangNyangSnapBackgroundData
{
    [Header("배경 ID")]
    [SerializeField] private int _backgroundId;
    [Header("배경 이름")]
    [SerializeField] private string _backgroundName;
    [Header("배경 등급")]
    [SerializeField] private BackgroundGrade _backgroundGrade;
    [Header("스테이지")]
    [SerializeField] private int _stage;
    [Header("배경 Key")]
    [SerializeField] private string _backgroundKey;

    public int BackgroundId => _backgroundId;
    public string BackgroundName => _backgroundName;
    public BackgroundGrade BackgroundGrade => _backgroundGrade;
    public int Stage => _stage;
    public string BackgroundKey => _backgroundKey;

    public NyangNyangSnapBackgroundData(
        int backgroundId, 
        string backgroundName, 
        BackgroundGrade backgroundGrade, 
        int stage, 
        string backgroundKey)
    {
        _backgroundId = backgroundId;
        _backgroundName = backgroundName;
        _backgroundGrade = backgroundGrade;
        _stage = stage;
        _backgroundKey = backgroundKey;
    }
}

public enum BackgroundGrade 
{
    None,
    Normal,
    Rare,
    Epic
}
