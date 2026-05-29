using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [CreateAssetMenu(fileName = "ScratchingTime", menuName = "SO/Data/ScratchingTimeSO", order = 0)]
    public class ScratchingSheetLoadSo : SheetLoadSoBase, ISheetParsable
    {
        [SerializeField] private List<ScratchingData> _scratchingDatas = new();
        public List<ScratchingData> ScratchingDatas => _scratchingDatas;

        private Dictionary<int, ScratchingData> _dataDict = new();
        private int _count = 1;

        public override void Init()
            => ClearData();
        
        public void ClearData()
        {
            _scratchingDatas.Clear();
            _dataDict.Clear();
        }

        public void SetData(string[] cols)
        {
            ScratchingData data = new (cols[0].Trim(),
                    int.Parse(cols[1].Trim()), int.Parse(cols[2].Trim()),
                    int.Parse(cols[3].Trim()), int.Parse(cols[4].Trim()));
            
            _scratchingDatas.Add(data);
            _dataDict.Add(_count++, data);
        }

        /// <summary>
        /// 원하는 일일 스테이지의 내구도 반환
        /// </summary>
        /// <param name="stage">필요한 스테이지 값</param>
        public int GetDailyDurability(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].DailyDurability;
        }

        /// <summary>
        /// 원하는 일일 스테이지의 획득 경험치 반환
        /// </summary>
        /// <param name="stage">필요한 스테이지 값</param>
        public int GetDailyClearExp(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;
            
            return _dataDict[stage].DailyClearExp;
        }

        /// <summary>
        /// 원하는 일일 스테이지의 내구도 반환
        /// </summary>
        /// <param name="stage">필요한 스테이지 값</param>
        public int GetWeeklyDurability(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;
            return _dataDict[stage].WeeklyDurability;
        }

        /// <summary>
        /// 원하는 주간 스테이지의 내구도 반환
        /// </summary>
        /// <param name="stage">필요한 스테이지 값</param>
        public int GetWeeklyClearExp(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;
            return _dataDict[stage].WeeklyClearExp;
        }

        /// <summary>
        /// 원하는 주간 스테이지의 내구도 반환
        /// </summary>
        /// <param name="stage">필요한 스테이지 값</param>
        private bool IsContainsKey(int stage)
        {
            if (!_dataDict.ContainsKey(stage))
            {
                DebugTool.Warning($"{stage} 존재하지 않는 스테이지 입니다.", DebugType.Data);
                return false;
            }
            
            return true;
        }

        public void PrintDatas()
        {
            StringBuilder log = new();
            log.AppendLine("[ScratchingTimeSO 로드 완료]");

            foreach (ScratchingData data in _scratchingDatas)
            {
                log.AppendLine($"[{data.Stage}] " +
                           $"일일 내구도 = {data.DailyDurability}, " +
                           $"일일 경험치 = {data.DailyClearExp}, " +
                           $"주간 내구도 = {data.WeeklyDurability}, " +
                           $"주간 경험치 = {data.WeeklyClearExp}");
            }
            
            DebugTool.Log(log.ToString(), DebugType.Data);
        }
    }
}