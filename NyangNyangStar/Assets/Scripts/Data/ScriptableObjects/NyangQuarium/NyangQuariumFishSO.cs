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

    public IReadOnlyList<NyangQuariumFishData> FishData => _fishData;

    public override void Init() => ClearData();

    public void ClearData()
    {
        _fishData.Clear();
    }

    public void SetData(string[] cols)
    {
        NyangQuariumFishData data = new(
            int.Parse(cols[0].Trim()),
            ParseFishType(cols[1].Trim()),
            int.Parse(cols[2].Trim()),
            ParsePlaceableType(cols[3].Trim()),
            cols[4].Trim(),
            cols[5].Trim(),
            cols[6].Trim());

        _fishData.Add(data);
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

    private PlaceableType ParsePlaceableType(string type)
    {
        switch (type)
        {
            case "물고기":
                return PlaceableType.Fish;
            case "돌":
                return PlaceableType.Stone;
            case "풀":
                return PlaceableType.Plant;
            case "바다":
                return PlaceableType.Marine;
            case "산호":
                return PlaceableType.Coral;
            case "은신처":
                return PlaceableType.Shelter;
            default:
                return PlaceableType.None;
        }
    }

    public void SortData()
    {
        _fishData.Sort((a, b) =>
        {
            int fishTypeCompare = a.FishType.CompareTo(b.FishType);
            if (fishTypeCompare != 0)
                return fishTypeCompare;

            if (a.FishType == FishType.Environments)
            {
                int placeableCompare = a.PlaceableType.CompareTo(b.PlaceableType);
                if (placeableCompare != 0)
                    return placeableCompare;
            }

            return a.Level.CompareTo(b.Level);
        });
    }

    public void PrintData()
    {
        StringBuilder log = new();
        log.AppendLine("[NyangQuariumFishSO 로드 완료]");

        foreach (NyangQuariumFishData fishData in _fishData)
        {
            log.AppendLine($"물고기 ID: {fishData.FishId}, " +
                           $"타입: {fishData.FishType}, " +
                           $"레벨: {fishData.Level}, " +
                           $"종류: {fishData.PlaceableType}, " +
                           $"이름: {fishData.FishName}, " +
                           $"설명: {fishData.FishDescription}, " +
                           $"물고기 Key: {fishData.FishKey}");
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }
}
