using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NyangQuariumFish", menuName = "SO/NyangQuarium/NyangQuariumFishSO", order = 0)]

public class NyangQuariumFishSO : SoBase, ISheetParsable
{
    [Header("물고기 데이터")]
    [Tooltip("물고기ID, 타입, 레벨, 종류, 이름, 설명, 물고기 Key")]
    [SerializeField] private List<NyangQuariumFishData> _fishData = new();

    private readonly Dictionary<FishType, int> _fishCountByType = new(); // 타입별 물고기 수

    public IReadOnlyList<NyangQuariumFishData> FishData => _fishData;

    public override void Init() => ClearData();

    public void ClearData()
    {
        _fishData.Clear();
        _fishCountByType.Clear();
    }

    public void SetData(string[] cols)
    {
        NyangQuariumFishData data = new(
            int.Parse(cols[0].Trim()),
            ParseFishType(cols[1].Trim()),
            int.Parse(cols[2].Trim()),
            cols[3].Trim(),
            cols[4].Trim(),
            cols[5].Trim(),
            cols[6].Trim());

        _fishData.Add(data);

        if (!_fishCountByType.ContainsKey(data.FishType))
            _fishCountByType[data.FishType] = 0;

        _fishCountByType[data.FishType]++;
    }

    private FishType ParseFishType(string type)
    {
        switch (type)
        {
            case "담수":
                return FishType.Freshwater;
            case "해수":
                return FishType.Saltwater;
            case "기수":
                return FishType.BrackishWater;
            case "자연 요소":
                return FishType.Environments;
            default:
                return FishType.None;
        }
    }

    public void SortData()
    {
        _fishData.Sort((a, b) => a.Level.CompareTo(b.Level));
    }

    public int GetFishCount(FishType fishType) => _fishCountByType.TryGetValue(fishType, out int count) ? count : 0;

    public void PrintData()
    {
        StringBuilder log = new();
        log.AppendLine("[NyangQuariumFishSO 로드 완료]");

        foreach (NyangQuariumFishData fishData in _fishData)
        {
            log.AppendLine($"물고기 ID: {fishData.FishId}, " +
                           $"타입: {fishData.FishType}, " +
                           $"레벨: {fishData.Level}, " +
                           $"종류: {fishData.Category}, " +
                           $"이름: {fishData.FishName}, " +
                           $"설명: {fishData.FishDescription}, " +
                           $"물고기 Key: {fishData.FishKey}");
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }
}
