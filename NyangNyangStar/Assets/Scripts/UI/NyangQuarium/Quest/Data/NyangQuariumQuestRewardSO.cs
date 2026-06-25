using System.Collections.Generic;
using System.Text;
using Data.ScriptableObjects;
using UnityEngine;

namespace Data.ScriptableObjects.NyangQuariumSO
{
    // 냥쿠_퀘스트 보상 테이블 전체를 담는 ScriptableObject입니다.
    // 퀘스트 테이블의 questReward 값이 이 SO의 id를 참조합니다.
    [CreateAssetMenu(fileName = "NyangQuariumQuestRewardSO", menuName = "SO/NyangQuarium/NyangQuariumQuestRewardSO", order = 2)]
    public class NyangQuariumQuestRewardSO : SoBase, ISheetParsable
    {
        // 시트에서 읽어온 보상 목록입니다.
        [Header("냥쿠아리움 퀘스트 보상 테이블")]
        [SerializeField] private List<NyangQuariumQuestRewardData> _rewards = new();

        // 보상 ID로 빠르게 찾기 위한 캐시 딕셔너리입니다.
        private readonly Dictionary<int, NyangQuariumQuestRewardData> _rewardById = new();

        // 외부에서는 읽기 전용으로만 접근합니다.
        public IReadOnlyList<NyangQuariumQuestRewardData> Rewards => _rewards;

        // SO 활성화 시 조회 캐시를 다시 구성합니다.
        private void OnEnable()
        {
            RebuildDictionary();
        }

        // 시트 로드 전 기존 데이터를 초기화합니다.
        public override void Init()
        {
            ClearData();
        }

        // 보상 목록과 캐시를 모두 비웁니다.
        public void ClearData()
        {
            _rewards.Clear();
            _rewardById.Clear();
        }

        // 시트 한 줄을 받아서 NyangQuariumQuestRewardData로 변환합니다.
        public void SetData(string[] cols)
        {
            // 보상 테이블은 최소 6개 컬럼이 필요합니다.
            if (cols == null || cols.Length < 6)
                return;

            // 0번 컬럼: id
            // ID가 없거나 0 이하라면 유효하지 않은 행으로 보고 무시합니다.
            if (!TryParseInt(GetColumn(cols, 0), out int id) || id <= 0)
                return;

            // 1번 컬럼: rewardItem1
            int rewardItem1 = ParseIntOrDefault(GetColumn(cols, 1));

            // 2번 컬럼: amount
            int rewardAmount1 = ParseIntOrDefault(GetColumn(cols, 2));

            // 3번 컬럼: rewardItem2
            int rewardItem2 = ParseIntOrDefault(GetColumn(cols, 3));

            // 4번 컬럼: amount
            int rewardAmount2 = ParseIntOrDefault(GetColumn(cols, 4));

            // 5번 컬럼: expAmount
            int expAmount = ParseIntOrDefault(GetColumn(cols, 5));

            // 파싱한 값으로 보상 데이터 객체를 생성합니다.
            NyangQuariumQuestRewardData data = new NyangQuariumQuestRewardData(
                id,
                rewardItem1,
                rewardAmount1,
                rewardItem2,
                rewardAmount2,
                expAmount);

            // 같은 ID가 있으면 교체하고, 없으면 새로 추가합니다.
            AddOrUpdate(data);
        }

        // 보상 ID로 데이터 하나를 조회합니다.
        public bool TryGetReward(int id, out NyangQuariumQuestRewardData data)
        {
            RebuildDictionaryIfNeeded();
            return _rewardById.TryGetValue(id, out data);
        }

        // 시트 로드 후 디버그 로그로 데이터가 잘 들어왔는지 확인할 때 사용합니다.
        public void PrintData()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[NyangQuariumQuestRewardSO] 로드된 보상 데이터: {_rewards.Count}");

            for (int i = 0; i < _rewards.Count; i++)
            {
                NyangQuariumQuestRewardData reward = _rewards[i];

                if (reward == null)
                    continue;

                builder.AppendLine(
                    $"ID:{reward.ID}, " +
                    $"Reward1:{reward.RewardItem1} x{reward.RewardAmount1}, " +
                    $"Reward2:{reward.RewardItem2} x{reward.RewardAmount2}, " +
                    $"Exp:{reward.ExpAmount}");
            }

            DebugTool.Log(builder.ToString(), DebugType.Data, this);
        }

        // 같은 ID가 이미 있으면 기존 데이터를 교체하고,
        // 없으면 리스트와 딕셔너리에 새로 추가합니다.
        private void AddOrUpdate(NyangQuariumQuestRewardData data)
        {
            if (data == null || data.ID <= 0)
                return;

            RebuildDictionaryIfNeeded();

            if (_rewardById.ContainsKey(data.ID))
            {
                for (int i = 0; i < _rewards.Count; i++)
                {
                    if (_rewards[i] == null || _rewards[i].ID != data.ID)
                        continue;

                    _rewards[i] = data;
                    _rewardById[data.ID] = data;
                    return;
                }
            }

            _rewards.Add(data);
            _rewardById[data.ID] = data;
        }

        // 리스트 개수와 딕셔너리 개수가 다르면 캐시가 깨진 것으로 보고 다시 만듭니다.
        private void RebuildDictionaryIfNeeded()
        {
            if (_rewardById.Count == _rewards.Count)
                return;

            RebuildDictionary();
        }

        // 보상 리스트를 기준으로 ID 조회용 딕셔너리를 다시 만듭니다.
        private void RebuildDictionary()
        {
            _rewardById.Clear();

            for (int i = 0; i < _rewards.Count; i++)
            {
                NyangQuariumQuestRewardData data = _rewards[i];

                if (data == null || data.ID <= 0)
                    continue;

                _rewardById[data.ID] = data;
            }
        }

        // 배열에서 특정 컬럼을 안전하게 가져옵니다.
        private static string GetColumn(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }

        // 문자열을 int로 변환할 수 있는지 확인합니다.
        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        // 문자열을 int로 변환합니다.
        // 빈 값이나 "-"는 defaultValue로 처리합니다.
        private static int ParseIntOrDefault(string value, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return defaultValue;

            return int.TryParse(value, out int result) ? result : defaultValue;
        }
    }
}