using Data.ScriptableObjects.NyangQuariumSO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Data.ScriptableObjects.NyangQuariumSO
{
    // 퀘스트가 어느 분류에 속하는지 구분
    // 시트의 questSection 컬럼과 매칭

    public enum NyangQuariumQuestSection
    {
        None,
        Daily,
        Main,
        Sub
    }

    // 퀘스트가 어떤 시스템과 연결되는지 구분
    // questType
    public enum NyangQuariumQuestType
    {
        None,
        Story,
        Merge,
        Housing
    }
}

[Serializable]
public class NyangQuariumQuestData
{
    // 퀘스트 id
    [SerializeField] private int _id;

    // 퀘스트 구분
    [SerializeField] private NyangQuariumQuestSection _questSection; // 퀘스트 구분

    // 퀘스트 제목 string Key : Q_NAMES; 
    [SerializeField] private string _questNameKey;

    // 실제 한글 문구는 QuestStringSO에서 찾음
    [SerializeField] private string _questDescKey;

    // 선행 퀘스트 ID
    [SerializeField] private int _preQuestId;

    // 퀘스트 타입
    [SerializeField] private NyangQuariumQuestType _questType;

    // 첫 번째 퀘스트 조건
    [SerializeField] private string _questCondition1;

    // 첫 번째 조건에 필요한 수량
    [SerializeField] private int _conditionAmount1;

    // 두 번째 퀘스트 조건
    [SerializeField] private string _questCondition2;

    // 두 번째 조건에 필요한 수량
    [SerializeField] private int _conditionAmount2;

    // 보상 테이블 ID
    // 냥쿠_퀘스트 보상 테이블 id 참조
    [SerializeField] private int _questRewardId;

    // 퀘스트 테이블에 적힌 보상
    [SerializeField] private int _rewardAmount;

    // getter
    public int ID => _id;
    public NyangQuariumQuestSection QuestSection => _questSection;
    public string QuestNameKey => _questNameKey;
    public string QuestDescKey => _questDescKey;
    public int PreQuestId => _preQuestId;
    public NyangQuariumQuestType QuestType => _questType;
    public string QuestCondition1 => _questCondition1;
    public int ConditionAmount1 => _conditionAmount1;
    public string QuestCondition2 => _questCondition2;
    public int ConditionAmount2 => _conditionAmount2;
    public int QuestRewardId => _questRewardId;
    public int RewardAmount => _rewardAmount;

    // 선행 퀘스트 있는 지 확인
    public bool HasPreQuest => _preQuestId > 0;

    // 두 번째 조건이 있는지 확인 할 때 사용
    public bool HasCondition2 => !string.IsNullOrWhiteSpace(_questCondition2);

    // 보상 Id 와 보상량이 모두 유효한지 
    public bool HasReward => _questRewardId > 0 && _rewardAmount > 0;

    // 시트에서 파싱한 값을 한 번에 넣기 위한 생성자
    public NyangQuariumQuestData(
        int id,
        NyangQuariumQuestSection questSection,
        string questNameKey,
        string questDescKey,
        int preQuestId,
        NyangQuariumQuestType questType,
        string questCondition1,
        int conditionAmount1,
        string questCondition2,
        int conditionAmount2,
        int questRewardId,
        int rewardAmount)
    {
        _id = id;
        _questSection = questSection;
        _questNameKey = questNameKey;
        _questDescKey = questDescKey;
        _preQuestId = preQuestId;
        _questType = questType;
        _questCondition1 = questCondition1;
        _conditionAmount1 = conditionAmount1;
        _questCondition2 = questCondition2;
        _conditionAmount2 = conditionAmount2;
        _questRewardId = questRewardId;
        _rewardAmount = rewardAmount;
    }

}