using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Util;

[CreateAssetMenu(
    fileName = "NyangNyangSnapPose",
    menuName = "SO/Data/NyangNyangSnapPoseSO",
    order = 1)]
public class NyangNyangSnapPoseSO : SoBase, ISheetParsable
{
    [Header("냥냥스냅 포즈 데이터")]
    [Tooltip("아이디, 포즈 이름, 등급, 점수, 도구, 해당 고양이, 애니메이션 경로")]
    [SerializeField] private List<NyangNyangSnapPoseData> _poseData = new();

    [SerializeField]private Dictionary<int, NyangNyangSnapPoseData> _poseDataDic = new();
    [SerializeField]private Dictionary<int, List<NyangNyangSnapPoseData>> _toolPoseDataDic = new();

    public int DataCount => _poseData.Count;

    public override void Init() => ClearData();

    public void ClearData()
    {
        _poseData.Clear();
        _poseDataDic.Clear();
        _toolPoseDataDic.Clear();

        DebugTool.Log(
    $"[NyangNyangSnapPoseSO] ClearData 호출됨 / 기존 개수: {_poseData.Count}",
    DebugType.Data
);
    }

    public void SetData(string[] cols)
    {
        if (cols == null || cols.Length < 6)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapPoseSO] 포즈 데이터 컬럼 수가 부족합니다. cols.Length: {(cols == null ? -1 : cols.Length)}",
                DebugType.Data
            );
            return;
        }

        string animationPath = cols.Length >= 7 ? cols[6].Trim() : string.Empty;

        NyangNyangSnapPoseData data = new(
            int.Parse(cols[0].Trim()),
            cols[1].Trim(),
            ConvertGrade(cols[2].Trim()),
            int.Parse(cols[3].Trim()),
            int.Parse(cols[4].Trim()),
            cols[5].Trim(),
            animationPath
        );

        _poseData.Add(data);
        _poseDataDic[data.ID] = data;

        if (!_toolPoseDataDic.ContainsKey(data.ToolId))
            _toolPoseDataDic[data.ToolId] = new List<NyangNyangSnapPoseData>();

        _toolPoseDataDic[data.ToolId].Add(data);

        DebugTool.Log(
            $"[NyangNyangSnapPoseSO] 포즈 데이터 추가 완료 / 현재 개수: {_poseData.Count} / ID: {data.ID}",
            DebugType.Data
        );
    }

    private NyangNyangSnapPoseGrade ConvertGrade(string grade)
    {
        switch (grade)
        {
            case "normal":
                return NyangNyangSnapPoseGrade.Normal;

            case "rare":
                return NyangNyangSnapPoseGrade.Rare;

            case "epic":
                return NyangNyangSnapPoseGrade.Epic;

            default:
                return NyangNyangSnapPoseGrade.None;
        }
    }

    public NyangNyangSnapPoseData GetPoseData(int id)
    {
        if (_poseDataDic.TryGetValue(id, out NyangNyangSnapPoseData data))
            return data;

        DebugTool.Warning($"[NyangNyangSnapPoseSO] 포즈 데이터를 찾을 수 없습니다. ID: {id}", DebugType.Data);
        return null;
    }

    public bool TryGetPoseData(int id, out NyangNyangSnapPoseData data)
    {
        return _poseDataDic.TryGetValue(id, out data);
    }

    public List<NyangNyangSnapPoseData> GetPoseDataByTool(int toolId)
    {
        if (_toolPoseDataDic.TryGetValue(toolId, out List<NyangNyangSnapPoseData> list))
            return list;

        DebugTool.Warning($"[NyangNyangSnapPoseSO] 도구에 해당하는 포즈 데이터가 없습니다. ToolId: {toolId}", DebugType.Data);
        return null;
    }

    public NyangNyangSnapPoseData GetRandomPoseByTool(int toolId)
    {
        List<NyangNyangSnapPoseData> list = GetPoseDataByTool(toolId);

        if (list == null || list.Count == 0)
            return null;

        int randomIndex = Random.Range(0, list.Count);
        return list[randomIndex];
    }
    //데모용
    public NyangNyangSnapPoseData GetRandomPoseData()
    {
        DebugTool.Log(
            $"[NyangNyangSnapPoseSO] 현재 포즈 데이터 개수: {_poseData.Count}",
            DebugType.Data
        );

        if (_poseData == null || _poseData.Count == 0)
        {
            DebugTool.Warning("[NyangNyangSnapPoseSO] 랜덤으로 가져올 포즈 데이터가 없습니다.", DebugType.Data);
            return null;
        }

        int randomIndex = Random.Range(0, _poseData.Count);
        NyangNyangSnapPoseData randomPoseData = _poseData[randomIndex];

        DebugTool.Log(
            $"[NyangNyangSnapPoseSO] 랜덤 포즈 선택 완료 - " +
            $"ID: {randomPoseData.ID}, " +
            $"포즈: {randomPoseData.PoseName}, " +
            $"등급: {randomPoseData.PoseGrade}, " +
            $"원점수: {randomPoseData.PoseScore}",
            DebugType.Data
        );

        return randomPoseData;
    }

    public int GetMaxPoseScore()
    {
        int maxScore = 0;

        foreach (NyangNyangSnapPoseData data in _poseData)
        {
            if (data.PoseScore > maxScore)
                maxScore = data.PoseScore;
        }

        return maxScore;
    }

    public void PrintData()
    {
        StringBuilder log = new();
        log.AppendLine("[NyangNyangSnapPoseSO 로드 완료]");

        foreach (NyangNyangSnapPoseData data in _poseData)
        {
            log.AppendLine(
                $"ID: {data.ID}, " +
                $"포즈 이름: {data.PoseName}, " +
                $"등급: {data.PoseGrade}, " +
                $"점수: {data.PoseScore}, " +
                $"도구 ID: {data.ToolId}, " +
                $"고양이: {data.PoseDataId}, " +
                $"애니메이션 경로: {data.AnimationPath}"
            );
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }
}