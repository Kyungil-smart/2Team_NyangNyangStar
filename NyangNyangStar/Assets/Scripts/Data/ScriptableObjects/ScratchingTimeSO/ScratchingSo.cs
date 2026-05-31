using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [CreateAssetMenu(fileName = "ScratchingTime", menuName = "SO/Data/ScratchingTimeSO", order = 0)]
    public class ScratchingSo : SoBase, ISheetParsable
    {
        [Header("스크래칭 타임 스테이지 데이터")]
        [Tooltip("단계, 일일 내구도, 일일 경험치, 주간 내구도, 주간 경험치에 대한 정보")]
        [SerializeField] private List<ScratchingData> _scratchingData = new();
        public List<ScratchingData> ScratchingData => _scratchingData;

        private Dictionary<int, ScratchingData> _dataDict = new();
        private int _count = 1;

        
        public override void Init()
            => ClearData();

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
            ScratchingData data = new(cols[0].Trim(),
                int.Parse(cols[1].Trim()), int.Parse(cols[2].Trim()),
                int.Parse(cols[3].Trim()), int.Parse(cols[4].Trim()));

            _scratchingData.Add(data);
            _dataDict.Add(_count++, data);
        }

        /// <summary>
        /// 일일 스테이지 스크래쳐에 데미지를 적용하고 남은 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">데미지를 적용할 스테이지 값</param>
        /// <param name="damage">적용할 데미지 값</param>
        /// <returns>데미지 적용 후 남은 일일 스테이지 현재 내구도</returns>
        public int DamagedDailyStage(int stage, int damage)
        {
            if (!IsContainsKey(stage))
                return 0;

            _dataDict[stage].CurrentDailyDurability -= damage;
            return _dataDict[stage].CurrentDailyDurability;
        }

        /// <summary>
        /// 주간 스테이지 스크래쳐에 데미지를 적용하고 남은 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">데미지를 적용할 스테이지 값</param>
        /// <param name="damage">적용할 데미지 값</param>
        /// <returns>데미지 적용 후 남은 주간 스테이지 현재 내구도</returns>
        public int DamagedWeeklyStage(int stage, int damage)
        {
            if (!IsContainsKey(stage))
                return 0;

            _dataDict[stage].CurrentWeeklyDurability -= damage;
            return _dataDict[stage].CurrentWeeklyDurability;
        }

        /// <summary>
        /// 지정한 일일 스테이지의 최대 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <returns>해당 일일 스테이지의 최대 내구도</returns>
        public int GetMaxDailyDurability(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].MaxDailyDurability;
        }

        /// <summary>
        /// 지정한 일일 스테이지의 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <returns>해당 일일 스테이지의 현재 내구도</returns>
        public int GetCurrentDailyDurability(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].CurrentDailyDurability;
        }

        /// <summary>
        /// 지정한 일일 스테이지의 클리어 경험치를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <returns>해당 일일 스테이지의 클리어 경험치</returns>
        public int GetDailyClearExp(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].DailyClearExp;
        }

        /// <summary>
        /// 지정한 주간 스테이지의 최대 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <returns>해당 주간 스테이지의 최대 내구도</returns>
        public int GetMaxWeeklyDurability(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].MaxWeeklyDurability;
        }

        /// <summary>
        /// 지정한 주간 스테이지의 현재 내구도를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <returns>해당 주간 스테이지의 현재 내구도</returns>
        public int GetCurrentWeeklyDurability(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].CurrentWeeklyDurability;
        }

        /// <summary>
        /// 지정한 주간 스테이지의 클리어 경험치를 반환합니다.
        /// </summary>
        /// <param name="stage">조회할 스테이지 값</param>
        /// <returns>해당 주간 스테이지의 클리어 경험치</returns>
        public int GetWeeklyClearExp(int stage)
        {
            if (!IsContainsKey(stage))
                return 0;

            return _dataDict[stage].WeeklyClearExp;
        }

        /// <summary>
        /// 지정한 스테이지 데이터가 딕셔너리에 존재하는지 확인합니다.
        /// </summary>
        /// <param name="stage">확인할 스테이지 값</param>
        /// <returns>스테이지 데이터가 존재하면 true, 존재하지 않으면 false</returns>
        private bool IsContainsKey(int stage)
        {
            if (!_dataDict.ContainsKey(stage))
            {
                DebugTool.Warning($"{stage} 존재하지 않는 스테이지 입니다.", DebugType.Data);
                return false;
            }

            return true;
        }
        
        public void PrintData()
        {
            StringBuilder log = new();
            log.AppendLine("[ScratchingTimeSO 로드 완료]");

            foreach (ScratchingData data in _scratchingData)
            {
                log.AppendLine($"[{data.Stage}] " +
                               $"일일 내구도 = {data.MaxDailyDurability}, " +
                               $"일일 경험치 = {data.DailyClearExp}, " +
                               $"주간 내구도 = {data.MaxWeeklyDurability}, " +
                               $"주간 경험치 = {data.WeeklyClearExp}");
            }

            DebugTool.Log(log.ToString(), DebugType.Data);
        }
    }
}