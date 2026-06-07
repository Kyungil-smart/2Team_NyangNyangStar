using System.Collections.Generic;
using Services.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    
    // 스크래칭 타임의 스테이지 데이터, 도전 횟수, 단계 개방 상태를 관리
    [CreateAssetMenu(fileName = "ScratchingTime", menuName = "SO/Data/ScratchingTimeSO", order = 0)]
    public partial class ScratchingSo : SoBase, ISheetParsable
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

        
        // 스크래칭 타임 데이터를 초기화
        public override void Init()
            => ClearData();

        
        // 시트에서 로드한 스테이지 데이터와 딕셔너리를 초기화
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

    }
}
