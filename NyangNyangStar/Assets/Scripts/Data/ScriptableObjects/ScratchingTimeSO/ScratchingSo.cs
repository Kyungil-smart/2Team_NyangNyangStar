using System;
using System.Collections.Generic;
using System.Text;
using Services.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    /// <summary>
    /// 스크래칭 타임의 스테이지 데이터, 도전 횟수, 단계 개방 상태를 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ScratchingTime", menuName = "SO/Data/ScratchingTimeSO", order = 0)]
    public class ScratchingSo : SoBase, ISheetParsable
    {
        [Header("스크래칭 타임 스테이지 데이터")]
        [Tooltip("단계, 일일 내구도, 일일 경험치, 주간 내구도, 주간 경험치에 대한 정보")]
        [FormerlySerializedAs("_scratchingDatas")]
        [SerializeField] private List<ScratchingData> _scratchingData = new();
        public List<ScratchingData> ScratchingData => _scratchingData;

        private readonly Dictionary<int, ScratchingData> _dataDict = new();
        private int _count = 1;

        [Header("도전 횟수 진행 데이터")]
        [Tooltip("Firestore 연동 전 임시로 보관하는 일일 남은 도전 횟수입니다.")]
        [SerializeField] private int _remainingDailyChallengeCount = 4;
        [Tooltip("Firestore 연동 전 임시로 보관하는 주간 남은 도전 횟수입니다.")]
        [SerializeField] private int _remainingWeeklyChallengeCount = 1;

        [Header("단계 개방")]
        [Tooltip("이벤트 진행 주차 또는 Firestore 데이터 기준으로 개방된 최대 단계입니다.")]
        [SerializeField] private int _openedStageBySchedule = 1;
        [Tooltip("일일 스테이지에서 클리어한 가장 높은 단계입니다. Firestore 연동 시 저장/로드 대상입니다.")]
        [SerializeField] private int _highestClearedDailyStage;
        [Tooltip("주간 스테이지에서 클리어한 가장 높은 단계입니다. Firestore 연동 시 저장/로드 대상입니다.")]
        [SerializeField] private int _highestClearedWeeklyStage;

        [Header("흥미도 (제한시간)")]
        [SerializeField] private float _timeLimit = 33f;
        public float TimeLimit => _timeLimit;
        public bool HasData
        {
            get
            {
                RebuildRuntimeDataIfNeeded();
                return _dataDict.Count > 0;
            }
        }

        private void OnEnable()
        {
            RebuildRuntimeDataIfNeeded();
        }

        /// <summary>
        /// 스크래칭 타임 데이터를 초기화합니다.
        /// </summary>
        public override void Init()
            => ClearData();

        /// <summary>
        /// 시트에서 로드한 스테이지 데이터와 딕셔너리를 초기화합니다.
        /// </summary>
        public void ClearData()
        {
            _scratchingData.Clear();
            _dataDict.Clear();
            _count = 1;
        }

        /// <summary>
        /// 시트에서 읽어온 문자열 배열 데이터를 스크래칭 타임 데이터로 변환하여 저장합니다.
        /// </summary>
        /// <param name="cols">스테이지, 일일 내구도, 일일 경험치, 주간 내구도, 주간 경험치 데이터 배열</param>
        public void SetData(string[] cols)
        {
            ScratchingData data = new(
                cols[0].Trim(),
                int.Parse(cols[1].Trim()),
                int.Parse(cols[2].Trim()),
                int.Parse(cols[3].Trim()),
                int.Parse(cols[4].Trim()));

            data.SetRemainingChallengeCount(StageType.Daily, _remainingDailyChallengeCount);
            data.SetRemainingChallengeCount(StageType.Weekly, _remainingWeeklyChallengeCount);

            _scratchingData.Add(data);
            _dataDict.Add(_count++, data);
        }

        /// <summary>
        /// Firestore에서 불러올 예정인 스크래칭 타임 진행 데이터를 적용합니다.
        /// </summary>
        /// <param name="openedStageBySchedule">이벤트 진행 주차 기준으로 개방된 최대 단계</param>
        /// <param name="remainingDailyChallengeCount">오늘 남은 일일 스테이지 도전 횟수</param>
        /// <param name="remainingWeeklyChallengeCount">이번 주 남은 주간 스테이지 도전 횟수</param>
        /// <param name="highestClearedDailyStage">일일 스테이지에서 클리어한 가장 높은 단계</param>
        /// <param name="highestClearedWeeklyStage">주간 스테이지에서 클리어한 가장 높은 단계</param>
        public void SetProgressData(
            int openedStageBySchedule,
            int remainingDailyChallengeCount,
            int remainingWeeklyChallengeCount,
            int highestClearedDailyStage,
            int highestClearedWeeklyStage)
        {
            _openedStageBySchedule = ClampStage(openedStageBySchedule);
            _remainingDailyChallengeCount = Mathf.Max(remainingDailyChallengeCount, 0);
            _remainingWeeklyChallengeCount = Mathf.Max(remainingWeeklyChallengeCount, 0);
            _highestClearedDailyStage = ClampClearedStage(highestClearedDailyStage);
            _highestClearedWeeklyStage = ClampClearedStage(highestClearedWeeklyStage);

            SetRemainingChallengeCountForAll(StageType.Daily, _remainingDailyChallengeCount);
            SetRemainingChallengeCountForAll(StageType.Weekly, _remainingWeeklyChallengeCount);
        }

        /// <summary>
        /// 이벤트 진행 주차 기준으로 개방된 최대 단계를 설정합니다.
        /// Firestore 연동 전에는 테스트 값으로 사용할 수 있습니다.
        /// </summary>
        /// <param name="openedStage">개방할 최대 단계</param>
        public void SetOpenedStageBySchedule(int openedStage)
        {
            _openedStageBySchedule = ClampStage(openedStage);
        }

        /// <summary>
        /// 지정한 스테이지 타입의 현재 내구도를 최대 내구도로 초기화합니다.
        /// </summary>
        /// <param name="stage">초기화를 적용할 스테이지 값</param>
        /// <param name="stageType">초기화할 스테이지 타입</param>
        public void StageStart(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return;

            switch (stageType)
            {
                case StageType.Daily:
                    _dataDict[stage].CurrentDailyDurability = _dataDict[stage].MaxDailyDurability;
                    break;

                case StageType.Weekly:
                    _dataDict[stage].CurrentWeeklyDurability = _dataDict[stage].MaxWeeklyDurability;
                    break;

                default:
                    DebugTool.Warning($"{stageType} 처리할 수 없는 스테이지 타입입니다.", DebugType.Data);
                    break;
            }
        }

        /// <summary>
        /// 지정한 스테이지 타입의 스크래쳐에 데미지를 적용하고 남은 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">데미지를 적용할 스테이지 값</param>
        /// <param name="stageType">데미지를 적용할 스테이지 타입</param>
        /// <param name="damage">적용할 데미지 값</param>
        /// <returns>데미지 적용 후 남은 현재 내구도</returns>
        public int TakeDamageOnScratcher(int stage, StageType stageType, int damage)
        {
            if (!IsContainsKey(stage))
                return 0;

            switch (stageType)
            {
                case StageType.Daily:
                    _dataDict[stage].CurrentDailyDurability -= damage;
                    return _dataDict[stage].CurrentDailyDurability;

                case StageType.Weekly:
                    _dataDict[stage].CurrentWeeklyDurability -= damage;
                    return _dataDict[stage].CurrentWeeklyDurability;

                default:
                    DebugTool.Warning($"{stageType} 처리할 수 없는 스테이지 타입입니다.", DebugType.Data);
                    return 0;
            }
        }

        /// <summary>
        /// 스테이지 클리어를 기록하고 남은 도전 횟수와 최고 클리어 단계를 갱신합니다.
        /// 도전 횟수는 클리어 시에만 차감됩니다.
        /// </summary>
        /// <param name="stage">클리어한 스테이지 값</param>
        /// <param name="stageType">클리어한 스테이지 타입</param>
        public void RecordStageClear(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return;

            int remainingCount = GetRemainingChallengeCount(stage, stageType);

            if (remainingCount <= 0)
                return;

            SetRemainingChallengeCountForAll(stageType, remainingCount - 1);

            switch (stageType)
            {
                case StageType.Daily:
                    _highestClearedDailyStage = Mathf.Max(_highestClearedDailyStage, stage);
                    break;

                case StageType.Weekly:
                    _highestClearedWeeklyStage = Mathf.Max(_highestClearedWeeklyStage, stage);
                    break;

                default:
                    DebugTool.Warning($"{stageType} 처리할 수 없는 스테이지 타입입니다.", DebugType.Data);
                    break;
            }
        }

        /// <summary>
        /// 지정한 스테이지 타입의 최대 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 최대 내구도</returns>
        public int GetMaxDurability(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return stageType switch
            {
                StageType.Daily => _dataDict[stage].MaxDailyDurability,
                StageType.Weekly => _dataDict[stage].MaxWeeklyDurability,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지 타입의 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 현재 내구도</returns>
        public int GetCurrentDurability(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return stageType switch
            {
                StageType.Daily => _dataDict[stage].CurrentDailyDurability,
                StageType.Weekly => _dataDict[stage].CurrentWeeklyDurability,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지 타입의 클리어 경험치를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 클리어 경험치</returns>
        public int GetClearExp(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return stageType switch
            {
                StageType.Daily => _dataDict[stage].DailyClearExp,
                StageType.Weekly => _dataDict[stage].WeeklyClearExp,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지 타입의 최대 도전 횟수를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 최대 도전 횟수</returns>
        public int GetMaxChallengeCount(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].GetMaxChallengeCount(stageType);
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 남은 도전 횟수</returns>
        public int GetRemainingChallengeCount(int stage, StageType stageType)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].GetRemainingChallengeCount(stageType);
        }

        /// <summary>
        /// 이벤트 진행 주차 기준으로 개방된 최대 단계를 반환합니다.
        /// </summary>
        /// <returns>이벤트 진행 주차 기준으로 개방된 최대 단계</returns>
        public int GetOpenedStageBySchedule()
            => _openedStageBySchedule;

        /// <summary>
        /// 지정한 스테이지 타입에서 클리어한 가장 높은 단계를 반환합니다.
        /// </summary>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>클리어한 가장 높은 단계</returns>
        public int GetHighestClearedStage(StageType stageType)
        {
            return stageType switch
            {
                StageType.Daily => _highestClearedDailyStage,
                StageType.Weekly => _highestClearedWeeklyStage,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지가 이벤트 주차 기준으로 개방되어 있는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <returns>이벤트 주차 기준으로 개방되어 있으면 true, 아니면 false</returns>
        public bool IsOpenedBySchedule(int stage)
            => stage > 0 && stage <= _openedStageBySchedule;

        /// <summary>
        /// 지정한 스테이지가 이전 단계 클리어 조건을 만족하는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <param name="stageType">확인할 스테이지 타입</param>
        /// <returns>이전 단계 클리어 조건을 만족하면 true, 아니면 false</returns>
        public bool IsStageUnlocked(int stage, StageType stageType)
        {
            if (stage <= 0)
                return false;

            if (!IsOpenedBySchedule(stage))
                return false;

            int highestClearedStage = GetHighestClearedStage(stageType);
            return stage <= highestClearedStage + 1;
        }

        /// <summary>
        /// 지정한 스테이지를 현재 도전할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <param name="stageType">확인할 스테이지 타입</param>
        /// <returns>단계 개방과 남은 도전 횟수 조건을 모두 만족하면 true, 아니면 false</returns>
        public bool CanChallenge(int stage, StageType stageType)
        {
            if (stageType == StageType.None)
                return false;

            if (!IsStageUnlocked(stage, stageType))
                return false;

            return GetRemainingChallengeCount(stage, stageType) > 0;
        }

        /// <summary>
        /// 지정한 스테이지의 내구도 변경 이벤트에 리스너를 등록합니다.
        /// </summary>
        /// <param name="stage">리스너를 등록할 스테이지 값</param>
        /// <param name="listener">등록할 리스너</param>
        public void AddDamagedListener(int stage, Action listener)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDamaged += listener;
        }

        /// <summary>
        /// 지정한 스테이지의 파괴 이벤트에 리스너를 등록합니다.
        /// </summary>
        /// <param name="stage">리스너를 등록할 스테이지 값</param>
        /// <param name="listener">등록할 리스너</param>
        public void AddDestroyedListener(int stage, Action listener)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDestroyed += listener;
        }

        /// <summary>
        /// 지정한 스테이지의 내구도 변경 이벤트 리스너를 모두 제거합니다.
        /// </summary>
        /// <param name="stage">리스너를 제거할 스테이지 값</param>
        public void RemoveAllDamagedListener(int stage)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDamaged = null;
        }

        /// <summary>
        /// 지정한 스테이지의 파괴 이벤트 리스너를 모두 제거합니다.
        /// </summary>
        /// <param name="stage">리스너를 제거할 스테이지 값</param>
        public void RemoveAllDestroyedListener(int stage)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDestroyed = null;
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 모든 스테이지 데이터에 동일하게 적용합니다.
        /// </summary>
        /// <param name="stageType">적용할 스테이지 타입</param>
        /// <param name="remainingCount">남은 도전 횟수</param>
        private void SetRemainingChallengeCountForAll(StageType stageType, int remainingCount)
        {
            foreach (ScratchingData data in _scratchingData)
                data.SetRemainingChallengeCount(stageType, remainingCount);

            switch (stageType)
            {
                case StageType.Daily:
                    _remainingDailyChallengeCount = Mathf.Max(remainingCount, 0);
                    break;

                case StageType.Weekly:
                    _remainingWeeklyChallengeCount = Mathf.Max(remainingCount, 0);
                    break;
            }
        }

        private void RebuildRuntimeDataIfNeeded()
        {
            if (_scratchingData == null || _scratchingData.Count == 0)
                return;

            if (_dataDict.Count == _scratchingData.Count)
                return;

            _dataDict.Clear();
            _count = 1;

            foreach (ScratchingData data in _scratchingData)
            {
                if (data == null)
                    continue;

                data.SetRemainingChallengeCount(StageType.Daily, _remainingDailyChallengeCount);
                data.SetRemainingChallengeCount(StageType.Weekly, _remainingWeeklyChallengeCount);
                _dataDict[_count++] = data;
            }
        }

        /// <summary>
        /// 지정한 스테이지 데이터가 딕셔너리에 존재하는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <returns>스테이지 데이터가 존재하면 true, 존재하지 않으면 false</returns>
        private bool IsContainsKey(int stage)
        {
            RebuildRuntimeDataIfNeeded();

            if (!_dataDict.ContainsKey(stage))
            {
                DebugTool.Warning($"{stage} 존재하지 않는 스테이지 입니다.", DebugType.Data);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 스테이지 값을 사용 가능한 범위로 제한합니다.
        /// </summary>
        /// <param name="stage">제한할 스테이지 값</param>
        /// <returns>사용 가능한 범위로 제한된 스테이지 값</returns>
        private int ClampStage(int stage)
            => Mathf.Clamp(stage, 1, Mathf.Max(_scratchingData.Count, 1));

        /// <summary>
        /// 클리어 단계 값을 사용 가능한 범위로 제한합니다.
        /// </summary>
        /// <param name="stage">제한할 클리어 단계 값</param>
        /// <returns>사용 가능한 범위로 제한된 클리어 단계 값</returns>
        private int ClampClearedStage(int stage)
            => Mathf.Clamp(stage, 0, Mathf.Max(_scratchingData.Count, 1));

        /// <summary>
        /// 로드된 스크래칭 타임 데이터를 로그로 출력합니다.
        /// </summary>
        public void PrintData()
        {
            StringBuilder log = new();
            log.AppendLine("[ScratchingTimeSO 로드 완료]");

            foreach (ScratchingData data in _scratchingData)
            {
                log.AppendLine($"[{data.Stage}] " +
                               $"일일 내구도 = {data.MaxDailyDurability}, " +
                               $"일일 경험치 = {data.DailyClearExp}, " +
                               $"일일 남은 횟수 = {data.RemainingDailyChallengeCount}/{data.MaxDailyChallengeCount}, " +
                               $"주간 내구도 = {data.MaxWeeklyDurability}, " +
                               $"주간 경험치 = {data.WeeklyClearExp}, " +
                               $"주간 남은 횟수 = {data.RemainingWeeklyChallengeCount}/{data.MaxWeeklyChallengeCount}");
            }

            DebugTool.Log(log.ToString(), DebugType.Data);
        }
    }
}
