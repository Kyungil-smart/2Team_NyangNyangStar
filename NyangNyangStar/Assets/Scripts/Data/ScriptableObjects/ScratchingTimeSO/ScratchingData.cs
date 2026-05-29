using System;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [Serializable]
    public class ScratchingData
    {
        [SerializeField] private string _stage;
        public string Stage { get => _stage; private set => _stage = value; }
        [SerializeField] private int _dailyDurability;
        public int DailyDurability { get => _dailyDurability; private set => _dailyDurability = value; }
        [SerializeField] private int _dailyClearExp;
        public int DailyClearExp { get => _dailyClearExp; private set => _dailyClearExp = value; }
        [SerializeField] private int _weeklyDurability;
        public int WeeklyDurability { get => _weeklyDurability; private set => _weeklyDurability = value; }
        [SerializeField] private int _weeklyClearExp;
        public int WeeklyClearExp { get => _weeklyClearExp; private set => _weeklyClearExp = value; }
        
        public ScratchingData(string stage, 
            int dailyDurability, int dailyClearExp, 
            int weeklyDurability, int weeklyClearExp)
        {
            _stage = stage;
            _dailyDurability = dailyDurability;
            _dailyClearExp = dailyClearExp;
            _weeklyDurability = weeklyDurability;
            _weeklyClearExp = weeklyClearExp;
        }
    }
}