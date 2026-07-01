using System;
using System.Collections.Generic;
using System.Text;
using Data.ScriptableObjects;
using UnityEngine;

namespace Data.ScriptableObjects.NyangQuariumSO
{

    // SheetLoader가 ISheetParsable.SetData(string[] cols)를 호출해서
    // 시트 한 줄씩 데이터를 채워 넣음
    [CreateAssetMenu(fileName = "NyangQuariumQuestSO", menuName = "SO/NyangQuarium/NyangQuariumQuestSO", order = 1)]
    public class NyangQuariumQuestSO : SoBase, ISheetParsable
    {
        // 시트에서 읽어온 퀘스트 목록
        // 인스펙터에서 확인할 수 있도록 SerializeField로 둡니다.
        [Header("냥쿠아리움 퀘스트 테이블")]
        [SerializeField] private List<NyangQuariumQuestData> _quests = new();

        // 퀘스트 ID로 빠르게 찾기 위한 캐시 딕셔너리
        // 런타임 조회용이라 SerializeField는 붙이지 않음
        private readonly Dictionary<int, NyangQuariumQuestData> _questById = new();

        // 외부에서는 리스트를 읽기 전용으로만 접근하게 함
        public IReadOnlyList<NyangQuariumQuestData> Quests => _quests;

        // SO가 활성화될 때 딕셔너리를 다시 구성
        // 에디터에서 데이터를 확인하거나 플레이 시작 시 캐시가 비는 상황을 방지
        private void OnEnable()
        {
            RebuildDictionary();
        }

        // SoBase에서 요구하는 초기화 함수
        // 시트를 다시 로드하기 전에 기존 데이터를 비움
        public override void Init()
        {
            ClearData();
        }

        // ISheetParsable에서 요구하는 함수
        // 저장된 퀘스트 목록과 캐시를 모두 초기화
        public void ClearData()
        {
            _quests.Clear();
            _questById.Clear();
        }

        // 시트 한 줄을 받아서 NyangQuariumQuestData로 변환
        public void SetData(string[] cols)
        {
            if (cols == null || cols.Length < 7)
                return;

            // 0번 컬럼: id
            // ID가 없거나 0 이하라면 유효하지 않은 행으로 보고 무시
            if (!TryParseInt(GetColumn(cols, 0), out int id) || id <= 0)
                return;

            // 1번 컬럼: questSection
            // daily / main / sub 값을 enum으로 변환하기
            NyangQuariumQuestSection questSection =
                ParseEnum(GetColumn(cols, 1), NyangQuariumQuestSection.None);

            string questNameKey;
            string questDescKey;

            string column4 = GetColumn(cols, 4);
            string column5 = GetColumn(cols, 5);
            string column6 = GetColumn(cols, 6);

            int preQuestId;
            NyangQuariumQuestType questType;
            int condition1Index;
            int amount1Index;
            int condition2Index;
            int amount2Index;
            int rewardIdIndex;
            int rewardAmountIndex;

            // sub merge: id | sub | name | desc | merge | condition1 | amount1
            // 예: 43004 | sub | Q_NAME4 | Q_DESC4 | merge | 42001 | 2
            if (TryParseQuestType(column4, out questType))
            {
                questNameKey = GetColumn(cols, 2);
                questDescKey = GetColumn(cols, 3);
                preQuestId = 0;
                condition1Index = 5;
                amount1Index = 6;
                condition2Index = 7;
                amount2Index = 8;
                rewardIdIndex = 9;
                rewardAmountIndex = 10;
            }
            // main + questSubject: id | section | subject | name | desc | preQuest | type | condition1 | amount1
            // 예: 43001 | main | Q_Subject1 | Q_NAME1 | Q_DESC1 | - | story | 10033 | 1
            else if (TryParseQuestType(column6, out questType))
            {
                questNameKey = GetColumn(cols, 3);
                questDescKey = GetColumn(cols, 4);
                preQuestId = ParseQuestId(column5);
                condition1Index = 7;
                amount1Index = 8;
                condition2Index = 9;
                amount2Index = 10;
                rewardIdIndex = 11;
                rewardAmountIndex = 12;
            }
            // legacy main: id | section | name | desc | preQuest | type | condition1 | amount1
            else
            {
                questNameKey = GetColumn(cols, 2);
                questDescKey = GetColumn(cols, 3);
                preQuestId = ParseQuestId(column4);
                questType = ParseEnum(column5, NyangQuariumQuestType.None);
                condition1Index = 6;
                amount1Index = 7;
                condition2Index = 8;
                amount2Index = 9;
                rewardIdIndex = 10;
                rewardAmountIndex = 11;
            }

            string questCondition1 = NormalizeEmptyValue(GetColumn(cols, condition1Index));
            int conditionAmount1 = ParseIntOrDefault(GetColumn(cols, amount1Index));
            string questCondition2 = NormalizeEmptyValue(GetColumn(cols, condition2Index));
            int conditionAmount2 = ParseIntOrDefault(GetColumn(cols, amount2Index));
            int questRewardId = ParseIntOrDefault(GetColumn(cols, rewardIdIndex));
            int rewardAmount = ParseIntOrDefault(GetColumn(cols, rewardAmountIndex));

            // enum 값이 잘못되면 None으로 들어오므로 해당 행은 무시하기
            if (questSection == NyangQuariumQuestSection.None || questType == NyangQuariumQuestType.None)
                return;

            // 이름/설명 키가 없으면 UI에 출력할 수 없으므로 무시하기
            if (string.IsNullOrWhiteSpace(questNameKey) || string.IsNullOrWhiteSpace(questDescKey))
                return;

            // 파싱한 값으로 데이터 객체를 생성하기
            NyangQuariumQuestData data = new NyangQuariumQuestData(
                id,
                questSection,
                questNameKey,
                questDescKey,
                preQuestId,
                questType,
                questCondition1,
                conditionAmount1,
                questCondition2,
                conditionAmount2,
                questRewardId,
                rewardAmount);

            // 같은 ID가 있으면 교체하고, 없으면 새로 추가하기
            AddOrUpdate(data);
        }

        // 퀘스트 ID로 데이터 하나를 조회하기
        public bool TryGetQuest(int id, out NyangQuariumQuestData data)
        {
            RebuildDictionaryIfNeeded();
            return _questById.TryGetValue(id, out data);
        }

        // daily / main / sub 같은 섹션 기준으로 퀘스트 목록을 가져오기
        public List<NyangQuariumQuestData> GetQuestsBySection(NyangQuariumQuestSection section)
        {
            List<NyangQuariumQuestData> result = new();

            for (int i = 0; i < _quests.Count; i++)
            {
                NyangQuariumQuestData quest = _quests[i];

                if (quest != null && quest.QuestSection == section)
                    result.Add(quest);
            }

            return result;
        }

        // story / merge / housing 같은 타입 기준으로 퀘스트 목록을 가져오기
        public List<NyangQuariumQuestData> GetQuestsByType(NyangQuariumQuestType type)
        {
            List<NyangQuariumQuestData> result = new();

            for (int i = 0; i < _quests.Count; i++)
            {
                NyangQuariumQuestData quest = _quests[i];

                if (quest != null && quest.QuestType == type)
                    result.Add(quest);
            }

            return result;
        }

        // 시트 로드 후 디버그 로그로 데이터가 잘 들어왔는지 확인할 때 사용
        public void PrintData()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[NyangQuariumQuestSO] 로드된 퀘스트 데이터: {_quests.Count}");

            for (int i = 0; i < _quests.Count; i++)
            {
                NyangQuariumQuestData quest = _quests[i];

                if (quest == null)
                    continue;

                builder.AppendLine(
                    $"ID:{quest.ID}, Section:{quest.QuestSection}, Type:{quest.QuestType}, " +
                    $"Name:{quest.QuestNameKey}, Desc:{quest.QuestDescKey}, PreQuest:{quest.PreQuestId}, " +
                    $"Condition1:{quest.QuestCondition1} x{quest.ConditionAmount1}, " +
                    $"Condition2:{quest.QuestCondition2} x{quest.ConditionAmount2}, " +
                    $"Reward:{quest.QuestRewardId} x{quest.RewardAmount}");
            }

            DebugTool.Log(builder.ToString(), DebugType.Data, this);
        }

        // 같은 ID가 이미 있으면 기존 데이터를 교체하고,
        // 없으면 리스트와 딕셔너리에 새로 추가하기
        private void AddOrUpdate(NyangQuariumQuestData data)
        {
            if (data == null || data.ID <= 0)
                return;

            RebuildDictionaryIfNeeded();

            if (_questById.ContainsKey(data.ID))
            {
                for (int i = 0; i < _quests.Count; i++)
                {
                    if (_quests[i] == null || _quests[i].ID != data.ID)
                        continue;

                    _quests[i] = data;
                    _questById[data.ID] = data;
                    return;
                }
            }

            _quests.Add(data);
            _questById[data.ID] = data;
        }

        // 리스트 개수와 딕셔너리 개수가 다르면 캐시가 깨진 것으로 보고 다시 만들기
        private void RebuildDictionaryIfNeeded()
        {
            if (_questById.Count == _quests.Count)
                return;

            RebuildDictionary();
        }

        // 퀘스트 리스트를 기준으로 ID 조회용 딕셔너리를 다시 만들기
        private void RebuildDictionary()
        {
            _questById.Clear();

            for (int i = 0; i < _quests.Count; i++)
            {
                NyangQuariumQuestData data = _quests[i];

                if (data == null || data.ID <= 0)
                    continue;

                _questById[data.ID] = data;
            }
        }

        // 배열에서 특정 컬럼을 안전하게 가져오기
        private static string GetColumn(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }

        // 시트에서 빈 값처럼 쓰는 "-", 공백, null을 빈 문자열로 통일
        private static string NormalizeEmptyValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return string.Empty;

            return value.Trim();
        }

        // 선행 퀘스트 ID 파싱용
        
        private static int ParseQuestId(string value)
        {
            value = NormalizeEmptyValue(value);
            return ParseIntOrDefault(value);
        }

        // 문자열을 int로 변환할 수 있는지 확인
        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        // 문자열을 int로 변환
        // 실패하면 defaultValue를 반환
        private static int ParseIntOrDefault(string value, int defaultValue = 0)
        {
            value = NormalizeEmptyValue(value);
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        // 문자열을 enum으로 변환
        // 대소문자는 무시하고, 실패하면 defaultValue를 반환
        private static T ParseEnum<T>(string value, T defaultValue) where T : struct
        {
            value = NormalizeEmptyValue(value);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return Enum.TryParse(value, true, out T result) ? result : defaultValue;
        }

        private static bool TryParseQuestType(string value, out NyangQuariumQuestType questType)
        {
            value = NormalizeEmptyValue(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                questType = NyangQuariumQuestType.None;
                return false;
            }

            // E열 선행 퀘스트 ID(43001 등)는 Enum.TryParse가 통과할 수 있어 questType으로 오인됨
            if (int.TryParse(value, out _))
            {
                questType = NyangQuariumQuestType.None;
                return false;
            }

            if (!Enum.TryParse(value, true, out questType) ||
                questType == NyangQuariumQuestType.None ||
                !Enum.IsDefined(typeof(NyangQuariumQuestType), questType))
            {
                questType = NyangQuariumQuestType.None;
                return false;
            }

            return true;
        }
    }
}