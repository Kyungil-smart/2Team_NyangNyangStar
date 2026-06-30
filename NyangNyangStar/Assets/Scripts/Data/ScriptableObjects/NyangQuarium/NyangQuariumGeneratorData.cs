using System;
using UnityEngine;

[Serializable]
public class NyangQuariumGeneratorData
{
    [SerializeField] private int _generatorId;
    [SerializeField] private string _generatorName;
    [SerializeField] private GeneratorType _generatorType;
    [SerializeField] private int _level;
    [SerializeField] private int _spawnCountPerCharge;
    [SerializeField] private int _rechargeMinutes;
    [SerializeField] private int _maxChargeCount;
    [SerializeField] private float _stage1SpawnRate;
    [SerializeField] private float _stage2SpawnRate;
    [SerializeField] private float _stage3SpawnRate;

    public int GeneratorId => _generatorId;
    public string GeneratorName => _generatorName;
    public GeneratorType GeneratorType => _generatorType;
    public int Level => _level;
    public int SpawnCountPerCharge => _spawnCountPerCharge;
    public int RechargeMinutes => _rechargeMinutes;
    public int MaxChargeCount => _maxChargeCount;
    public float Stage1SpawnRate => _stage1SpawnRate;
    public float Stage2SpawnRate => _stage2SpawnRate;
    public float Stage3SpawnRate => _stage3SpawnRate;

    public NyangQuariumGeneratorData(
        int generatorId,
        string generatorName,
        GeneratorType generatorType,
        int level,
        int spawnCountPerCharge,
        int rechargeMinutes,
        int maxChargeCount,
        float stage1SpawnRate,
        float stage2SpawnRate,
        float stage3SpawnRate)
    {
        _generatorId = generatorId;
        _generatorName = generatorName;
        _generatorType = generatorType;
        _level = level;
        _spawnCountPerCharge = spawnCountPerCharge;
        _rechargeMinutes = rechargeMinutes;
        _maxChargeCount = maxChargeCount;
        _stage1SpawnRate = stage1SpawnRate;
        _stage2SpawnRate = stage2SpawnRate;
        _stage3SpawnRate = stage3SpawnRate;
    }
}

public enum GeneratorType
{
    None,
    Freshwater,
    BrackishWater,
    Saltwater,
    Decoration
}

public static class GeneratorTypeExtensions
{
    public static FishType ToFishType(this GeneratorType generatorType)
    {
        switch (generatorType)
        {
            case GeneratorType.Freshwater:
                return FishType.Freshwater;
            case GeneratorType.BrackishWater:
                return FishType.BrackishWater;
            case GeneratorType.Saltwater:
                return FishType.Saltwater;
            case GeneratorType.Decoration:
                return FishType.Environments;
            default:
                return FishType.None;
        }
    }
}
