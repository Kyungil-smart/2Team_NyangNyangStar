using System;
using Services.Enums;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    /// <summary>
    /// 스크래칭 타임의 단계별 일일/주간 스테이지 데이터를 보관합니다.
    /// </summary>
    [Serializable]
    public class ScratchingData
    {
        [Header("스테이지")]
        [SerializeField] private string _stage;
        public string Stage { get => _stage; private set => _stage = value; }

        [Space(15)] [Header("일일 스테이지")]
        [Header("스크래쳐 최대 내구도")]
        [SerializeField] private int _maxDailyDurability;
        public int MaxDailyDurability { get => _maxDailyDurability; set => _maxDailyDurability = value; }

        [Header("스크래쳐 현재 내구도")]
        [SerializeField] private int _currentDailyDurability;
        public int CurrentDailyDurability
        {
            get => _currentDailyDurability;
            set
            {
                _currentDailyDurability = Math.Max(value, 0);
                OnScratcherDamaged?.Invoke();

                if (_currentDailyDurability <= 0)
                    OnScratcherDestroyed?.Invoke();
            }
        }

        [Header("스테이지 클리어 경험치")]
        [SerializeField] private int _dailyClearExp;
        public int DailyClearExp { get => _dailyClearExp; set => _dailyClearExp = value; }

        [Header("일일 최대 도전 횟수")]
        [SerializeField] private int _maxDailyChallengeCount = 4;
        public int MaxDailyChallengeCount => _maxDailyChallengeCount;

        [Header("일일 남은 도전 횟수")]
        [SerializeField] private int _remainingDailyChallengeCount;
        public int RemainingDailyChallengeCount => _remainingDailyChallengeCount;

        [Space(15)] [Header("주간 스테이지")]
        [Header("스크래쳐 최대 내구도")]
        [SerializeField] private int _maxWeeklyDurability;
        public int MaxWeeklyDurability { get => _maxWeeklyDurability; set => _maxWeeklyDurability = value; }

        [Header("스크래쳐 현재 내구도")]
        [SerializeField] private int _currentWeeklyDurability;
        public int CurrentWeeklyDurability
        {
            get => _currentWeeklyDurability;
            set
            {
                _currentWeeklyDurability = Math.Max(value, 0);
                OnScratcherDamaged?.Invoke();

                if (_currentWeeklyDurability <= 0)
                    OnScratcherDestroyed?.Invoke();
            }
        }

        [Header("스테이지 클리어 경험치")]
        [SerializeField] private int _weeklyClearExp;
        public int WeeklyClearExp { get => _weeklyClearExp; set => _weeklyClearExp = value; }

        [Header("주간 최대 도전 횟수")]
        [SerializeField] private int _maxWeeklyChallengeCount = 1;
        public int MaxWeeklyChallengeCount => _maxWeeklyChallengeCount;

        [Header("주간 남은 도전 횟수")]
        [SerializeField] private int _remainingWeeklyChallengeCount;
        public int RemainingWeeklyChallengeCount => _remainingWeeklyChallengeCount;

        /// <summary>
        /// 스크래쳐 내구도가 변경될 때 호출됩니다.
        /// </summary>
        public Action OnScratcherDamaged;

        /// <summary>
        /// 스크래쳐 내구도가 0 이하가 되었을 때 호출됩니다.
        /// </summary>
        public Action OnScratcherDestroyed;

        /// <summary>
        /// 스크래칭 타임 스테이지 데이터를 생성합니다.
        /// </summary>
        /// <param name="stage">스테이지 이름 또는 단계 표시 값</param>
        /// <param name="maxDailyDurability">일일 스크래쳐 최대 내구도</param>
        /// <param name="dailyClearExp">일일 스테이지 클리어 경험치</param>
        /// <param name="maxWeeklyDurability">주간 스크래쳐 최대 내구도</param>
        /// <param name="weeklyClearExp">주간 스테이지 클리어 경험치</param>
        public ScratchingData(
            string stage,
            int maxDailyDurability,
            int dailyClearExp,
            int maxWeeklyDurability,
            int weeklyClearExp)
        {
            _stage = stage;
            _maxDailyDurability = maxDailyDurability;
            _dailyClearExp = dailyClearExp;
            _maxWeeklyDurability = maxWeeklyDurability;
            _weeklyClearExp = weeklyClearExp;

            ResetChallengeCount(StageType.Daily);
            ResetChallengeCount(StageType.Weekly);
        }

        /// <summary>
        /// 지정한 스테이지 타입의 최대 도전 횟수를 반환합니다.
        /// </summary>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 최대 도전 횟수</returns>
        public int GetMaxChallengeCount(StageType stageType)
        {
            return stageType switch
            {
                StageType.Daily => _maxDailyChallengeCount,
                StageType.Weekly => _maxWeeklyChallengeCount,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 반환합니다.
        /// </summary>
        /// <param name="stageType">조회할 스테이지 타입</param>
        /// <returns>해당 스테이지 타입의 남은 도전 횟수</returns>
        public int GetRemainingChallengeCount(StageType stageType)
        {
            return stageType switch
            {
                StageType.Daily => _remainingDailyChallengeCount,
                StageType.Weekly => _remainingWeeklyChallengeCount,
                _ => 0
            };
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 설정합니다.
        /// Firestore에서 불러온 진행 데이터를 적용할 때 사용합니다.
        /// </summary>
        /// <param name="stageType">설정할 스테이지 타입</param>
        /// <param name="remainingCount">남은 도전 횟수</param>
        public void SetRemainingChallengeCount(StageType stageType, int remainingCount)
        {
            switch (stageType)
            {
                case StageType.Daily:
                    _remainingDailyChallengeCount = Mathf.Clamp(remainingCount, 0, _maxDailyChallengeCount);
                    break;

                case StageType.Weekly:
                    _remainingWeeklyChallengeCount = Mathf.Clamp(remainingCount, 0, _maxWeeklyChallengeCount);
                    break;
            }
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 최대 도전 횟수로 초기화합니다.
        /// </summary>
        /// <param name="stageType">초기화할 스테이지 타입</param>
        public void ResetChallengeCount(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.Daily:
                    _remainingDailyChallengeCount = _maxDailyChallengeCount;
                    break;

                case StageType.Weekly:
                    _remainingWeeklyChallengeCount = _maxWeeklyChallengeCount;
                    break;
            }
        }

        /// <summary>
        /// 지정한 스테이지 타입의 남은 도전 횟수를 1 감소시킵니다.
        /// </summary>
        /// <param name="stageType">감소시킬 스테이지 타입</param>
        /// <returns>도전 횟수 감소에 성공하면 true, 남은 횟수가 없으면 false</returns>
        public bool TryDecreaseChallengeCount(StageType stageType)
        {
            if (GetRemainingChallengeCount(stageType) <= 0)
                return false;

            switch (stageType)
            {
                case StageType.Daily:
                    _remainingDailyChallengeCount = Math.Max(_remainingDailyChallengeCount - 1, 0);
                    return true;

                case StageType.Weekly:
                    _remainingWeeklyChallengeCount = Math.Max(_remainingWeeklyChallengeCount - 1, 0);
                    return true;

                default:
                    return false;
            }
        }
    }
}
