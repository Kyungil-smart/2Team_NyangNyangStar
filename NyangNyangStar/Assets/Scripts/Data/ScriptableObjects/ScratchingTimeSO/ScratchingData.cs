using System;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [Serializable]
    public class ScratchingData
    {
        [SerializeField] private string stage;
        public string Stage { get => stage; set => stage = value; }
        [SerializeField] private int dailyDurability;
        public int DailyDurability { get => dailyDurability; set => dailyDurability = value; }
        [SerializeField] private int dailyClearExp;
        public int DailyClearExp { get => dailyClearExp; set => dailyClearExp = value; }
        [SerializeField] private int weeklyDurability;
        public int WeeklyDurability { get => weeklyDurability; set => weeklyDurability = value; }
        [SerializeField] private int weeklyClearExp;
        public int WeeklyClearExp { get => weeklyClearExp; set => weeklyClearExp = value; }
    }
}