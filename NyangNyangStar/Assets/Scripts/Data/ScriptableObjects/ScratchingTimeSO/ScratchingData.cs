using System;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
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
            set => _currentDailyDurability = Math.Max(value, 0);
        }
        [Header("스테이지 클리어 경험치")]
        [SerializeField] private int _dailyClearExp;
        public int DailyClearExp { get => _dailyClearExp; set => _dailyClearExp = value; }
        
        [Space(15)] [Header("주간 스테이지")]
        [Header("스크래쳐 최대 내구도")]
        [SerializeField] private int _maxWeeklyDurability;
        public int MaxWeeklyDurability { get => _maxWeeklyDurability; set => _maxWeeklyDurability = value; }
        [Header("스크래쳐 현재 내구도")]
        [SerializeField] private int _currentWeeklyDurability;

        public int CurrentWeeklyDurability
        {
            get => _currentWeeklyDurability;
            set => _currentWeeklyDurability = Math.Max(value, 0);
        }
        [Header("스테이지 클리어 경험치")]
        [SerializeField] private int _weeklyClearExp;
        public int WeeklyClearExp { get => _weeklyClearExp; set => _weeklyClearExp = value; }
        
        /// <summary>
        /// 스크래칭 타임 데이터 생성자
        /// </summary>
        /// <param name="stage">스테이지</param>
        /// <param name="maxDailyDurability">일일 스크래쳐 최대 내구도</param>
        /// <param name="dailyClearExp">일일 스테이지 클리어 경험치</param>
        /// <param name="maxWeeklyDurability">주간 스크래쳐 최대 내구도</param>
        /// <param name="weeklyClearExp">주간 스테이지 클리어 경험치</param>
        public ScratchingData(string stage, 
            int maxDailyDurability, int dailyClearExp, 
            int maxWeeklyDurability, int weeklyClearExp)
        {
            _stage = stage;
            _maxDailyDurability = maxDailyDurability;
            _currentDailyDurability = _maxDailyDurability;
            _dailyClearExp = dailyClearExp;
            _maxWeeklyDurability = maxWeeklyDurability;
            _currentWeeklyDurability = _maxWeeklyDurability;
            _weeklyClearExp = weeklyClearExp;
        }
    }
}