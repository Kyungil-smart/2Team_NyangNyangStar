using Core.Managers;
using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NyangQuariumGenerator", menuName = "SO/NyangQuarium/NyangQuariumGeneratorSO", order = 1)]
public class NyangQuariumGeneratorSO : SoBase, ISheetParsable
{
    private const int RequiredColumnCount = 10;

    [Header("생성기 데이터")]
    [Tooltip("id, 이름, 타입, 레벨, 1회 충전 개수, 1회 충전 시간(분), 최대 충전 개수, 1~3단계 생성확률")]
    [SerializeField] private List<NyangQuariumGeneratorData> _generators = new();

    private readonly Dictionary<int, NyangQuariumGeneratorData> _generatorDict = new();

    public IReadOnlyList<NyangQuariumGeneratorData> Generators => _generators;
    public int DataCount => _generators?.Count ?? 0;

    public override void Init() => ClearData();

    public void ClearData()
    {
        _generators.Clear();
        _generatorDict.Clear();
    }

    public void SetData(string[] cols)
    {
        if (cols == null || cols.Length < RequiredColumnCount)
        {
            DebugTool.Warning(
                $"[NyangQuariumGeneratorSO] 컬럼 수가 부족합니다. 필요: {RequiredColumnCount}, 실제: {cols?.Length ?? 0}",
                DebugType.Data,
                this);
            return;
        }

        if (!TryParseInt(cols[0], out int generatorId) || generatorId <= 0)
        {
            DebugTool.Warning($"[NyangQuariumGeneratorSO] 잘못된 id: {cols[0]}", DebugType.Data, this);
            return;
        }

        GeneratorType generatorType = ParseGeneratorType(cols[2]);
        if (generatorType == GeneratorType.None)
        {
            DebugTool.Warning($"[NyangQuariumGeneratorSO] 잘못된 타입: {cols[2]}", DebugType.Data, this);
            return;
        }

        if (!TryParseInt(cols[3], out int level) ||
            !TryParseInt(cols[4], out int spawnCountPerCharge) ||
            !TryParseInt(cols[5], out int rechargeMinutes) ||
            !TryParseInt(cols[6], out int maxChargeCount) ||
            !TryParseFloat(cols[7], out float stage1SpawnRate) ||
            !TryParseFloat(cols[8], out float stage2SpawnRate) ||
            !TryParseFloat(cols[9], out float stage3SpawnRate))
        {
            DebugTool.Warning($"[NyangQuariumGeneratorSO] 숫자 파싱 실패 / id:{generatorId}", DebugType.Data, this);
            return;
        }

        NyangQuariumGeneratorData data = new(
            generatorId,
            cols[1].Trim(),
            generatorType,
            level,
            spawnCountPerCharge,
            rechargeMinutes,
            maxChargeCount,
            stage1SpawnRate,
            stage2SpawnRate,
            stage3SpawnRate);

        _generators.Add(data);
        _generatorDict[generatorId] = data;
    }

    public void SortData()
    {
        _generators.Sort((a, b) => a.GeneratorId.CompareTo(b.GeneratorId));
    }

    public bool TryGetById(int generatorId, out NyangQuariumGeneratorData generatorData)
    {
        RebuildDictionaryIfNeeded();
        return _generatorDict.TryGetValue(generatorId, out generatorData);
    }

    public int RollSpawnStage(NyangQuariumGeneratorData generatorData)
    {
        if (generatorData == null)
            return 1;

        float totalRate = generatorData.Stage1SpawnRate +
                          generatorData.Stage2SpawnRate +
                          generatorData.Stage3SpawnRate;

        if (totalRate <= 0f)
            return 1;

        float roll = Random.Range(0f, totalRate);
        float cumulative = generatorData.Stage1SpawnRate;

        if (roll < cumulative)
            return 1;

        cumulative += generatorData.Stage2SpawnRate;
        if (roll < cumulative)
            return 2;

        return 3;
    }

    public void PrintData()
    {
        StringBuilder log = new();
        log.AppendLine("[NyangQuariumGeneratorSO 로드 완료]");

        foreach (NyangQuariumGeneratorData generatorData in _generators)
        {
            log.AppendLine(
                $"생성기 ID: {generatorData.GeneratorId}, " +
                $"이름: {generatorData.GeneratorName}, " +
                $"타입: {generatorData.GeneratorType}, " +
                $"레벨: {generatorData.Level}, " +
                $"1회 충전: {generatorData.SpawnCountPerCharge}개 / {generatorData.RechargeMinutes}분, " +
                $"최대 충전: {generatorData.MaxChargeCount}, " +
                $"확률: {generatorData.Stage1SpawnRate}/{generatorData.Stage2SpawnRate}/{generatorData.Stage3SpawnRate}");
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }

    private void RebuildDictionaryIfNeeded()
    {
        if (_generatorDict.Count == _generators.Count)
            return;

        _generatorDict.Clear();

        for (int i = 0; i < _generators.Count; i++)
        {
            NyangQuariumGeneratorData data = _generators[i];
            if (data == null || data.GeneratorId <= 0)
                continue;

            _generatorDict[data.GeneratorId] = data;
        }
    }

    private static GeneratorType ParseGeneratorType(string type)
    {
        switch (type.Trim())
        {
            case "담수":
                return GeneratorType.Freshwater;
            case "기수":
                return GeneratorType.BrackishWater;
            case "해수":
                return GeneratorType.Saltwater;
            case "꾸미기":
                return GeneratorType.Decoration;
            default:
                return GeneratorType.None;
        }
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
