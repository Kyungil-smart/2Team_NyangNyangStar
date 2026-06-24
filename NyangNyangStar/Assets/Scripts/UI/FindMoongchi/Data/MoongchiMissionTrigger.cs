namespace Data.ScriptableObjects.MoongchiSO
{
    // 미션 시트 MissionContent 컬럼과 매칭되는 트리거 종류
    // 시트에 enum 이름을 그대로 넣거나, MoongchiMissionTriggerResolver 키워드 규칙을 사용합니다.
    public enum MoongchiMissionTrigger
    {
        None,
        EnergySpend,
        UseSearchTool,
        OpenTile,
        FindTarget,
        FindMoongchi,
        StageClear
    }
}
