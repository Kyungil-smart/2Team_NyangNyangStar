using System;
using System.Collections.Generic;
using System.Text;
using Data.ScriptableObjects;
using UnityEngine;

namespace Data.ScriptableObjects.HideAndSeekSO
{

    // 뭉치를 찾아라 일일/주간 미션 마스터 데이터입니다.
    // 일일 미션은 EVENT_COIN + ENERGY 보상, 주간 미션은 EVENT_COIN 보상을 기준으로 합니다.

    [CreateAssetMenu(fileName = "HideAndSeekMissionSO", menuName = "SO/FindMoongchi/HideAndSeekMissionSO", order = 1)]
    public class HideAndSeekMissionSO : SoBase, ISheetParsable
    {
        [Header("뭉치를 찾아라 미션 데이터")]
        [SerializeField] private List<HideAndSeekMissionData> _missions = new();

        // 미션 ID 기반 조회 캐시입니다.
        private readonly Dictionary<int, HideAndSeekMissionData> _missionByID = new();

        // 외부에서는 읽기만 가능하게 열어둠
        public IReadOnlyList<HideAndSeekMissionData> Missions => _missions;

        private void OnEnable()
        {
            // 에디터에서 켜질 때 캐시 다시 맞춰줌
            RebuildDictionary();
        }

        public override void Init()
        {
            // 시트 다시 받을 때 기존 데이터 초기화
            ClearData();
        }

        public void ClearData()
        {
            _missions.Clear();
            _missionByID.Clear();
        }


        // 시트 한 줄을 미션 데이터로 변환
        // 보상2가 비어 있는 주간 미션도 정상적으로 파싱

        public void SetData(string[] cols)
        {

            if (cols == null || cols.Length < 7)
                return;

            if (!TryParseInt(GetColumn(cols, 0), out int id) || id <= 0)
                return;

            // 1번째 칸을 가져와서 HideAndSeekMissionType enum으로 변환
            HideAndSeekMissionType missionType = ParseEnum(GetColumn(cols, 1), HideAndSeekMissionType.None);
            string missionContent = GetColumn(cols, 2);
            int targetAmount = ParseIntOrDefault(GetColumn(cols, 3));


            // cols.length >= 9 이면 두 번째 보상도 만들어야함
            HideAndSeekRewardData reward1 = CreateReward(GetColumn(cols, 4), GetColumn(cols, 5));
            HideAndSeekRewardData reward2 = cols.Length >= 8
                ? CreateReward(GetColumn(cols, 6), GetColumn(cols, 7))
                : null;

            if (missionType == HideAndSeekMissionType.None || string.IsNullOrEmpty(missionContent) || !reward1.IsValid)
                return;

            HideAndSeekMissionData data = new HideAndSeekMissionData(
                id,
                missionType,
                missionContent,
                targetAmount,
                reward1,
                reward2);

            AddOrUpdate(data);
        }

        public bool TryGetMission(int id, out HideAndSeekMissionData data)
        {
            // id로 미션 하나 빠르게 찾기
            RebuildDictionaryIfNeeded();
            return _missionByID.TryGetValue(id, out data);
        }

        public List<HideAndSeekMissionData> GetMissionsByType(HideAndSeekMissionType missionType)
        {
            // 일일, 주간 같은 타입 기준으로 미션 묶어서 가져오기
            List<HideAndSeekMissionData> result = new List<HideAndSeekMissionData>();

            foreach (HideAndSeekMissionData mission in _missions)
            {
                if (mission.MissionType == missionType)
                    result.Add(mission);
            }

            return result;
        }

        public void PrintData()
        {
            // 로드된 미션 데이터 확인용 로그
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[HideAndSeekMissionSO] 로드된 미션 데이터: {_missions.Count}");

            foreach (HideAndSeekMissionData data in _missions)
            {
                string reward2Text = data.HasReward2
                    ? $", Reward2:{data.Reward2.RewardType} {data.Reward2.RewardAmount}"
                    : string.Empty;

                builder.AppendLine(
                    $"ID:{data.ID}, Type:{data.MissionType}, Content:{data.MissionContent}, " +
                    $"Target:{data.TargetAmount}, Reward1:{data.Reward1.RewardType} {data.Reward1.RewardAmount}{reward2Text}");
            }

            DebugTool.Log(builder.ToString(), DebugType.Data, this);
        }

        private void AddOrUpdate(HideAndSeekMissionData data)
        {
            // 같은 id가 있으면 교체하고, 없으면 새로 추가
            RebuildDictionaryIfNeeded();

            if (_missionByID.ContainsKey(data.ID))
            {
                for (int i = 0; i < _missions.Count; i++)
                {
                    if (_missions[i].ID != data.ID)
                        continue;

                    _missions[i] = data;
                    _missionByID[data.ID] = data;
                    return;
                }
            }

            _missions.Add(data);
            _missionByID[data.ID] = data;
        }

        private void RebuildDictionaryIfNeeded()
        {
            // 리스트랑 캐시 개수가 다르면 누락된 걸로 보고 다시 만듦
            if (_missionByID.Count == _missions.Count)
                return;

            RebuildDictionary();
        }

        private void RebuildDictionary()
        {
            // 미션 id로 바로 찾을 수 있게 캐시 재구성
            _missionByID.Clear();

            foreach (HideAndSeekMissionData data in _missions)
            {
                if (data == null || data.ID <= 0)
                    continue;

                _missionByID[data.ID] = data;
            }
        }

        private static HideAndSeekRewardData CreateReward(string rewardTypeText, string rewardAmountText)
        {
            // 보상 타입과 수량 텍스트를 실제 보상 데이터로 변환
            HideAndSeekCurrencyType rewardType = ParseEnum(rewardTypeText, HideAndSeekCurrencyType.None);
            int rewardAmount = ParseIntOrDefault(rewardAmountText);

            return new HideAndSeekRewardData(rewardType, rewardAmount);
        }

        private static string GetColumn(string[] cols, int index)
        {
            // 시트 컬럼 값 안전하게 가져오기
            if (cols == null || index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        private static int ParseIntOrDefault(string value, int defaultValue = 0)
        {
            // 숫자로 못 바꾸면 기본값으로 처리
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct
        {
            // 문자열을 enum으로 바꾸고 실패하면 기본값
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Enum.TryParse(value, true, out T result) ? result : defaultValue;
        }
    }
}