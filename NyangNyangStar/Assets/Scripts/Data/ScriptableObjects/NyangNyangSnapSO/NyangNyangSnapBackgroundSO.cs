using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NyangNyangSnapBackground", menuName = "SO/Data/NyangNyangSnapBackgroundSO", order = 0)]

public class NyangNyangSnapBackgroundSO : SoBase, ISheetParsable
{
    [Header("냥냥스냅 배경 데이터")]
    [Tooltip("배경ID, 배경 이름, 배경 등급, 스테이지, 배경 Key")]
    [SerializeField] private List<NyangNyangSnapBackgroundData> _backgroundData = new();
    private Dictionary<int, List<NyangNyangSnapBackgroundData>> _backgroundDataDic = new();

    public override void Init() => ClearData();

    public void ClearData()
    {
        _backgroundData.Clear();
        _backgroundDataDic.Clear();
    }

    public void SetData(string[] cols)
    {
        NyangNyangSnapBackgroundData data = new(
            int.Parse(cols[0].Trim()),
            cols[1].Trim(),
            ConvertGrade(cols[2].Trim()),
            int.Parse(cols[3].Trim()),
            cols[4].Trim());

        _backgroundData.Add(data);

        if(!_backgroundDataDic.ContainsKey(data.Stage))
            _backgroundDataDic[data.Stage] = new List<NyangNyangSnapBackgroundData>();

        _backgroundDataDic[data.Stage].Add(data);
    }

    private BackgroundGrade ConvertGrade(string grade)
    {
        switch(grade) {
            case "normal":
                return BackgroundGrade.Normal;
            case "rare":
                return BackgroundGrade.Rare;
            case "epic":
                return BackgroundGrade.Epic;
            default:
                return BackgroundGrade.None;
        }
    }

    public string GetRandomBackgroundKey(int stage)
    {
        if (!_backgroundDataDic.ContainsKey(stage)) return string.Empty;

        List<NyangNyangSnapBackgroundData> backgrounds = _backgroundDataDic[stage];

        if(backgrounds == null) return string.Empty;

        int randomIndex = Random.Range(0, backgrounds.Count);
        return backgrounds[randomIndex].BackgroundKey;
    }

    public void PrintData()
    {
        StringBuilder log = new ();
        log.AppendLine("[NyangNyangSnapBackgroundSO 로드 완료]");

        foreach (NyangNyangSnapBackgroundData backgroundData in _backgroundData)
        {
            log.AppendLine($"배경 ID: {backgroundData.BackgroundId}" + 
                           $"배경 이름: {backgroundData.BackgroundName}, " +
                           $"배경 등급: {backgroundData.BackgroundGrade}" + 
                           $"스테이지: {backgroundData.Stage}" + 
                           $"배경 Key: {backgroundData.BackgroundKey}");
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }
}
