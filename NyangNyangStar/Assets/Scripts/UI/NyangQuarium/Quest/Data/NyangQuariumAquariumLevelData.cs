using System;
using UnityEngine;

[Serializable]
public class NyangQuariumAquariumLevelData
{
    [SerializeField] private int _levelId;
    [SerializeField] private int _level;
    [SerializeField] private string _description;
    [SerializeField] private int _requiredExp;
    [SerializeField] private int _maxPlaceableFish;
    [SerializeField] private int _maxPlaceableEnvironment;

    public int LevelId => _levelId;
    public int Level => _level;
    public string Description => _description;
    public int RequiredExp => _requiredExp;
    public int MaxPlaceableFish => _maxPlaceableFish;
    public int MaxPlaceableEnvironment => _maxPlaceableEnvironment;

    public NyangQuariumAquariumLevelData(
        int levelId,
        int level,
        string description,
        int requiredExp,
        int maxPlaceableFish,
        int maxPlaceableEnvironment)
    {
        _levelId = levelId;
        _level = level;
        _description = description;
        _requiredExp = requiredExp;
        _maxPlaceableFish = maxPlaceableFish;
        _maxPlaceableEnvironment = maxPlaceableEnvironment;
    }
}