using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data.ScriptableObjects;
using Util;
using System.Text;

[CreateAssetMenu(
    fileName = "NyangNyangSnapTool",
    menuName = "SO/Data/NyangNyangSnapToolSO",
    order = 2)]
public class NyangNyangSnapToolSO : SoBase, ISheetParsable
{
    [Header("냥냥스냅 도구 데이터")]
    [Tooltip("도구ID, 아이템 ID, 아이템 유형, 아이템 범위")]
    [SerializeField] private List<NyangNyangSnapToolData> _toolData = new();

    private readonly Dictionary<int, NyangNyangSnapToolData> _toolDataByID = new();
    private readonly Dictionary<int , NyangNyangSnapToolData> _toolDataByItemID = new();

    public override void Init()
    {
        ClearData();
    }

    public void ClearData()
    {
        _toolData.Clear();
        _toolDataByID.Clear();
        _toolDataByItemID.Clear();
    }
    public void SetData(string[] cols)
    {
        if(cols ==null || cols.Length < 4)
        {
            return;
        }

        int id = int.Parse(cols[0]);
        int itemID = int.Parse(cols[1]);
        NyangNyangSnapToolType toolType = ConvertToolType(cols[2]);
        int itemRange = int.Parse(cols[3]);

        NyangNyangSnapToolData data = new NyangNyangSnapToolData(id, itemID, toolType, itemRange);

        _toolData.Add(data);
        _toolDataByID[data.ID] = data;
        _toolDataByItemID[data.ItemID] = data;
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
                return NyangNyangSnapToolType.None;
        }
    }

    public NyangNyangSnapToolData GetRandomToolData()
    {
        if(_toolData == null || _toolData.Count == 0)
        {
            return null;
        }
        int randomIndex = Random.Range(0, _toolData.Count);
        NyangNyangSnapToolData data = _toolData[randomIndex];

        return data;
    }

    public void PrintData()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[NyangNyangSnapToolSO] 로드된 도구 데이터");

        foreach (NyangNyangSnapToolData data in _toolData)
        {
            builder.AppendLine(
                $"ToolID:{data.ID}, " +
                $"ItemID:{data.ItemID}, " +
                $"Type:{data.ItemToolType}," +
                $" Range:{data.ItemRange}"
            );
        }

        DebugTool.Log(builder.ToString(), DebugType.Data);
    }

}
