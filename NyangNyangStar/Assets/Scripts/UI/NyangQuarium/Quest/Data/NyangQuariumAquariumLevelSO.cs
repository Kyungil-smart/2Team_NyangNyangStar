using Core.Managers;
using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NyangQuariumAquariumLevel", menuName = "SO/NyangQuarium/NyangQuariumAquariumLevelSO", order = 4)]
public class NyangQuariumAquariumLevelSO : SoBase, ISheetParsable
{
    private const int RequiredColumnCount = 5;
    private const int LevelIdOffset = 44000;

    [Header("수조 레벨 데이터")]
    [Tooltip("ID, Desc, Reaquire_Exp, Max_Placeable_Fish, Max_Placeable_Environment")]
    [SerializeField] private List<NyangQuariumAquariumLevelData> _aquariumLevels = new();

    private readonly Dictionary<int, NyangQuariumAquariumLevelData> _levelIdDict = new();
    private readonly Dictionary<int, NyangQuariumAquariumLevelData> _levelDict = new();

    public IReadOnlyList<NyangQuariumAquariumLevelData> AquariumLevels => _aquariumLevels;
    public int DataCount => _aquariumLevels?.Count ?? 0;

    public override void Init() => ClearData();

    public void ClearData()
    {
        _aquariumLevels.Clear();
        _levelIdDict.Clear();
        _levelDict.Clear();
    }

    public void SetData(string[] cols)
    {
        if (cols == null || cols.Length < RequiredColumnCount)
        {
            DebugTool.Warning(
                $"[NyangQuariumAquariumLevelSO] 컬럼 수가 부족합니다. 필요: {RequiredColumnCount}, 실제: {cols?.Length ?? 0}",
                DebugType.Data,
                this);
            return;
        }

        if (!TryParseInt(cols[0], out int levelId) || levelId <= 0)
        {
            DebugTool.Warning($"[NyangQuariumAquariumLevelSO] 잘못된 ID: {cols[0]}", DebugType.Data, this);
            return;
        }

        int level = levelId - LevelIdOffset;
        if (level <= 0)
        {
            DebugTool.Warning($"[NyangQuariumAquariumLevelSO] 잘못된 레벨 ID: {levelId}", DebugType.Data, this);
            return;
        }

        if (!TryParseInt(cols[2], out int requiredExp) ||
            !TryParseInt(cols[3], out int maxPlaceableFish) ||
            !TryParseInt(cols[4], out int maxPlaceableEnvironment))
        {
            DebugTool.Warning($"[NyangQuariumAquariumLevelSO] 숫자 파싱 실패 / ID: {levelId}", DebugType.Data, this);
            return;
        }

        NyangQuariumAquariumLevelData data = new(
            levelId,
            level,
            cols[1].Trim(),
            requiredExp,
            maxPlaceableFish,
            maxPlaceableEnvironment);

        _aquariumLevels.Add(data);
        _levelIdDict[levelId] = data;
        _levelDict[level] = data;
    }

    public void SortData()
    {
        _aquariumLevels.Sort((a, b) => a.Level.CompareTo(b.Level));
    }

    public bool TryGetById(int levelId, out NyangQuariumAquariumLevelData levelData)
    {
        RebuildDictionaryIfNeeded();
        return _levelIdDict.TryGetValue(levelId, out levelData);
    }

    public bool TryGetByLevel(int level, out NyangQuariumAquariumLevelData levelData)
    {
        RebuildDictionaryIfNeeded();
        return _levelDict.TryGetValue(level, out levelData);
    }

    public bool TryGetMaxPlaceableCount(
        int level,
        out int maxPlaceableFish,
        out int maxPlaceableEnvironment)
    {
        maxPlaceableFish = 0;
        maxPlaceableEnvironment = 0;

        if (!TryGetByLevel(level, out NyangQuariumAquariumLevelData levelData))
            return false;

        maxPlaceableFish = levelData.MaxPlaceableFish;
        maxPlaceableEnvironment = levelData.MaxPlaceableEnvironment;
        return true;
    }

    public bool IsMaxLevel(int level)
    {
        if (!TryGetByLevel(level, out NyangQuariumAquariumLevelData levelData))
            return false;

        return levelData.RequiredExp <= 0;
    }

    public void PrintData()
    {
        StringBuilder log = new();
        log.AppendLine("[NyangQuariumAquariumLevelSO 로드 완료]");

        foreach (NyangQuariumAquariumLevelData levelData in _aquariumLevels)
        {
            log.AppendLine(
                $"ID: {levelData.LevelId}, " +
                $"레벨: {levelData.Level}, " +
                $"설명: {levelData.Description}, " +
                $"필요 경험치: {levelData.RequiredExp}, " +
                $"최대 물고기 배치: {levelData.MaxPlaceableFish}, " +
                $"최대 자연 요소 배치: {levelData.MaxPlaceableEnvironment}");
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }

    private void RebuildDictionaryIfNeeded()
    {
        if (_levelIdDict.Count == _aquariumLevels.Count &&
            _levelDict.Count == _aquariumLevels.Count)
        {
            return;
        }

        _levelIdDict.Clear();
        _levelDict.Clear();

        for (int i = 0; i < _aquariumLevels.Count; i++)
        {
            NyangQuariumAquariumLevelData data = _aquariumLevels[i];

            if (data == null || data.LevelId <= 0 || data.Level <= 0)
                continue;

            _levelIdDict[data.LevelId] = data;
            _levelDict[data.Level] = data;
        }
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}