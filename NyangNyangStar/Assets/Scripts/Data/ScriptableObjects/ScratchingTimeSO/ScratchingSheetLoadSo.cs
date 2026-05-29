using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [CreateAssetMenu(fileName = "ScratchingTime", menuName = "SO/Data/ScratchingTimeSO", order = 0)]
    public class ScratchingSheetLoadSo : SheetLoadSoBase, ISheetParsable
    {
        [SerializeField] private List<ScratchingData> _scratchingDatas = new();
        public List<ScratchingData> ScratchingDatas => _scratchingDatas;

        private Dictionary<int, ScratchingData> _dataDict = new();
        
        public override void Init()
        {
            ClearData();
        }
        
        public void ClearData()
        {
            _scratchingDatas.Clear();
        }

        public void SetData(string[] cols)
        {
            // ScratchingData data = new {cols[0].Trim(), int.TryParse(cols[1], out int result),  int.Parse(cols[2]), int.Parse(cols[3]), int.Parse(cols[4])};
        }
    }
}