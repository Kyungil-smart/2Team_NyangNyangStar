using Data.ScriptableObjects;
using System;
using System.Collections.Generic;

namespace Data.Modules
{
    public class GameDataModule
    {
        private Dictionary<int, KeyContainerSo> _keyContainerDict = new();
        //private WaveSpawnTableSO                   _waveSpawnTable;
        //private PlayerUpgradeTableSO               _playerUpgradeTable;
        //private TeamUpgradeTableSO                 _teamUpgradeTable;
        
        // ─── 상태 ────────────────────────────────────────────────
        public bool IsReady { get; private set; }

        private event Action _onReady;

        public event Action OnReady
        {
            add
            {
                _onReady += value;
                if (IsReady) value?.Invoke();
            }
            remove { _onReady -= value; }
        }

        // ─── 등록 (DataManager가 호출) ────────────────────────────
        public void RegisterKeyContainers(Dictionary<int, KeyContainerSo> dict)
        {
            _keyContainerDict = dict;
            DebugTool.Log($"[GameDataModule] KeyContainer 등록 ({dict?.Count ?? 0}개)", DebugType.Data);
        }

        public void MarkReady()
        {
            if (IsReady)
            {
                DebugTool.Warning("[GameDataModule] 이미 Ready 상태에서 MarkReady() 재호출", DebugType.Data);
                return;
            }
            IsReady = true;
            DebugTool.Log("[GameDataModule] 모든 데이터 준비 완료 (IsReady = true)", DebugType.Data);
            _onReady?.Invoke();
        }
        private bool CheckReady(string methodName, int id)
        {
            if (IsReady) return true;
            DebugTool.Warning(
                $"[GameDataModule] 미준비 상태에서 {methodName}({id}) 호출",
                DebugType.Data);
            return false;
        }

        public void ClearEvent()
            => _onReady = null;
    }
}
