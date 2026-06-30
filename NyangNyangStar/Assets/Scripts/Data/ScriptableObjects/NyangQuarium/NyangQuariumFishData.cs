using System;
using UnityEngine;

[Serializable]
public class NyangQuariumFishData
{
    [Header("물고기 ID")]
    [SerializeField] private int _fishId;
    [Header("타입")]
    [SerializeField] private FishType _fishType;
    [Header("레벨")]
    [SerializeField] private int _level;
    [Header("종류")]
    [SerializeField] private string _category;
    [Header("물고기 이름")]
    [SerializeField] private string _fishName;
    [Header("물고기 설명")]
    [SerializeField] private string _fishDescription;
    [Header("물고기 Key")]
    [SerializeField] private string _fishKey;

    public int FishId => _fishId;
    public FishType FishType => _fishType;
    public int Level => _level;
    public string Category => _category;
    public string FishName => _fishName;
    public string FishDescription => _fishDescription;
    public string FishKey => _fishKey;

    public NyangQuariumFishData(
        int fishId,
        FishType fishType,
        int level,
        string category,
        string fishName,
        string fishDescription,
        string fishKey)
    {
        _fishId = fishId;
        _fishType = fishType;
        _level = level;
        _category = category;
        _fishName = fishName;
        _fishDescription = fishDescription;
        _fishKey = fishKey;
    }
}

 public enum FishType
{
    None,
    Freshwater,
    Saltwater,
    BrackishWater,
    Environments
}