using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Util;

[CreateAssetMenu(
    fileName = "NyangNyangSnapTool",
    menuName = "SO/Data/NyangNyangSnapToolSO",
    order = 2)]
public class NyangNyangSnapToolSO : SoBase, ISheetParsable
{
    [Header("냥냥스냅 도구 데이터")]
    [Tooltip("아이템 ID, 아이템 유형, 아이템 범위")]
    [SerializeField] private List<NyangNyangSnapToolData> _toolData = new();

    private readonly Dictionary<int, NyangNyangSnapToolData> _toolDataByItemID = new();

    public override void Init()
    {
        ClearData();
    }

    public void ClearData()
    {
        _toolData.Clear();
        _toolDataByItemID.Clear();
    }

    public void SetData(string[] cols)
    {
        if (cols == null || cols.Length < 3)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapToolSO] 도구 데이터 컬럼 수 부족 / cols.Length: {(cols == null ? -1 : cols.Length)}",
                DebugType.Data
            );
            return;
        }

        int itemID = int.Parse(cols[0].Trim());
        NyangNyangSnapToolType toolType = ConvertToolType(cols[1].Trim());
        int itemRange = int.Parse(cols[2].Trim());

        NyangNyangSnapToolData data = new NyangNyangSnapToolData(itemID, toolType, itemRange);

        _toolData.Add(data);
        _toolDataByItemID[data.ItemID] = data;

        DebugTool.Log(
            $"[NyangNyangSnapToolSO] 도구 데이터 추가 / ItemID: {data.ItemID}, Type: {data.ItemToolType}, Range: {data.ItemRange}",
            DebugType.Data
        );
    }

    private NyangNyangSnapToolType ConvertToolType(string type)
    {
        switch (type.ToLower())
        {
            case "toy":
                return NyangNyangSnapToolType.Toy;

            case "food":
                return NyangNyangSnapToolType.Food;

            case "snack":
                return NyangNyangSnapToolType.Snack;

            default:
                DebugTool.Warning($"[NyangNyangSnapToolSO] 알 수 없는 도구 타입: {type}", DebugType.Data);
                return NyangNyangSnapToolType.None;
        }
    }

    public bool TryGetToolDataByItemID(int itemID, out NyangNyangSnapToolData data)
    {
        EnsureLookupReady();
        return _toolDataByItemID.TryGetValue(itemID, out data);
    }

    public NyangNyangSnapToolData GetRandomToolData()
    {
        EnsureLookupReady();

        if (_toolData == null || _toolData.Count == 0)
        {
            DebugTool.Warning("[NyangNyangSnapToolSO] 랜덤으로 가져올 도구 데이터가 없습니다.", DebugType.Data);
            return null;
        }

        int randomIndex = Random.Range(0, _toolData.Count);
        return _toolData[randomIndex];
    }

    public void PrintData()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[NyangNyangSnapToolSO] 로드된 도구 데이터");

        foreach (NyangNyangSnapToolData data in _toolData)
        {
            builder.AppendLine(
                $"ItemID: {data.ItemID}, " +
                $"Type: {data.ItemToolType}, " +
                $"Range: {data.ItemRange}"
            );
        }

        DebugTool.Log(builder.ToString(), DebugType.Data);
    }

    private void EnsureLookupReady()
    {
        if (_toolDataByItemID.Count > 0 || _toolData == null || _toolData.Count == 0)
            return;

        foreach (NyangNyangSnapToolData data in _toolData)
        {
            if (data == null)
                continue;

            _toolDataByItemID[data.ItemID] = data;
        }
    }
}
