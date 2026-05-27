using System;

namespace Data.Modules
{
    public class GameDataModule
    {

        //private Dictionary<int, PlayerClassDataSO> _classes;
        //private Dictionary<int, ZombieStatSO>      _zombieStats;
        //private WaveInfoTableSO                    _waveInfoTable;
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
        //public void RegisterClasses(Dictionary<int, PlayerClassDataSO> dict)
        //{
        //    _classes = dict;
        //    DebugTool.Log($"[GameDataModule] Classes 등록 ({dict?.Count ?? 0}건)", DebugType.Data);
        //}

        //public void RegisterZombieStats(Dictionary<int, ZombieStatSO> dict)
        //{
        //    _zombieStats = dict;
        //    DebugTool.Log($"[GameDataModule] ZombieStats 등록 ({dict?.Count ?? 0}건)", DebugType.Data);
        //}

        //public void RegisterWaveInfoTable(WaveInfoTableSO table)
        //{
        //    _waveInfoTable = table;
        //    DebugTool.Log(
        //        $"[GameDataModule] WaveInfoTable 등록 (그룹 {table?.GroupCount ?? 0}개)",
        //        DebugType.Data);
        //}

        //public void RegisterWaveSpawnTable(WaveSpawnTableSO table)
        //{
        //    _waveSpawnTable = table;
        //    DebugTool.Log(
        //        $"[GameDataModule] WaveSpawnTable 등록 (그룹 {table?.GroupCount ?? 0}개)",
        //        DebugType.Data);
        //}

        //public void RegisterPlayerUpgradeTable(PlayerUpgradeTableSO table)
        //{
        //    _playerUpgradeTable = table;
        //    DebugTool.Log(
        //        $"[GameDataModule] PlayerUpgradeTable 등록 (그룹 {table?.GroupCount ?? 0}개)",
        //        DebugType.Data);
        //}

        //public void RegisterTeamUpgradeTable(TeamUpgradeTableSO table)
        //{
        //    _teamUpgradeTable = table;
        //    DebugTool.Log(
        //        $"[GameDataModule] TeamUpgradeTable 등록 (엔트리 {table?.EntryCount ?? 0}개)",
        //        DebugType.Data);
        //}


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

        // ─── 단일 조회 ────────────────────────────────────────────
        //public PlayerClassDataSO GetClass(int classId)
        //{
        //    if (!CheckReady(nameof(GetClass), classId)) return null;
        //    if (_classes == null || !_classes.TryGetValue(classId, out var data))
        //    {
        //        DebugTool.Warning($"[GameDataModule] ClassId {classId} 없음", DebugType.Data);
        //        return null;
        //    }
        //    return data;
        //}

        //public ZombieStatSO GetZombieStat(int zombieId)
        //{
        //    if (!CheckReady(nameof(GetZombieStat), zombieId)) return null;
        //    if (_zombieStats == null || !_zombieStats.TryGetValue(zombieId, out var data))
        //    {
        //        DebugTool.Warning($"[GameDataModule] ZombieId {zombieId} 없음", DebugType.Data);
        //        return null;
        //    }
        //    return data;
        //}

        ///// <summary>
        ///// 지정한 BattleNodeIndex의 모든 WaveInfoSO를 WaveIndex 오름차순으로 반환.
        ///// 없으면 빈 리스트(웨이브 0개도 정상 케이스).
        ///// 테이블 자체가 미등록 상태일 때만 Warning.
        ///// </summary>
        //public List<WaveInfoSO> GetWaveInfo(int battleNodeIndex)
        //{
        //    if (!CheckReady(nameof(GetWaveInfo), battleNodeIndex)) return new List<WaveInfoSO>();
        //    if (_waveInfoTable == null)
        //    {
        //        DebugTool.Warning("[GameDataModule] WaveInfoTable 미등록", DebugType.Data);
        //        return new List<WaveInfoSO>();
        //    }
        //    return _waveInfoTable.GetEntries(battleNodeIndex);
        //}

        ///// <summary>
        ///// WaveId의 스폰 엔트리들을 반환. 없으면 빈 리스트(스폰 0개도 정상 케이스).
        ///// 테이블 자체가 미등록 상태일 때만 Warning.
        ///// </summary>
        //public List<WaveSpawnEntry> GetWaveSpawns(int waveId)
        //{
        //    if (!CheckReady(nameof(GetWaveSpawns), waveId)) return new List<WaveSpawnEntry>();
        //    if (_waveSpawnTable == null)
        //    {
        //        DebugTool.Warning("[GameDataModule] WaveSpawnTable 미등록", DebugType.Data);
        //        return new List<WaveSpawnEntry>();
        //    }
        //    return _waveSpawnTable.GetEntries(waveId);
        //}

        ///// <summary>
        ///// ClassType 문자열로 업그레이드 데이터 조회. 대소문자 무관.
        ///// 사용 예: GetUpgrade("Melee").MaxHealth.ValuePerLevel
        ///// </summary>
        //public ClassUpgradeData GetUpgrade(string classType)
        //{
        //    if (!CheckReady(nameof(GetUpgrade), 0)) return null;
        //    if (_playerUpgradeTable == null)
        //    {
        //        DebugTool.Warning("[GameDataModule] PlayerUpgradeTable 미등록", DebugType.Data);
        //        return null;
        //    }
        //    return _playerUpgradeTable.GetUpgrade(classType);
        //}

        ///// <summary>WeaponType enum으로 업그레이드 데이터 조회.</summary>
        //public ClassUpgradeData GetUpgrade(WeaponType classType)
        //{
        //    if (!CheckReady(nameof(GetUpgrade), (int)classType)) return null;
        //    if (_playerUpgradeTable == null)
        //    {
        //        DebugTool.Warning("[GameDataModule] PlayerUpgradeTable 미등록", DebugType.Data);
        //        return null;
        //    }
        //    return _playerUpgradeTable.GetUpgrade(classType);
        //}

        ///// <summary>
        ///// UpgradeId로 팀 업그레이드 엔트리 조회.
        ///// 사용 예: GetTeamUpgrade(51009).UpgradeName
        ///// </summary>
        //public TeamUpgradeEntry GetTeamUpgrade(int upgradeId)
        //{
        //    if (!CheckReady(nameof(GetTeamUpgrade), upgradeId)) return null;
        //    if (_teamUpgradeTable == null)
        //    {
        //        DebugTool.Warning("[GameDataModule] TeamUpgradeTable 미등록", DebugType.Data);
        //        return null;
        //    }
        //    return _teamUpgradeTable.GetEntry(upgradeId);
        //}

        // ─── 전체 ID 순회 ────────────────────────────────────
        //public IEnumerable<int> GetAllClassIds()
        //    => _classes?.Keys ?? Enumerable.Empty<int>();

        //public IEnumerable<int> GetAllZombieIds()
        //    => _zombieStats?.Keys ?? Enumerable.Empty<int>();

        //public IEnumerable<int> GetAllBattleNodeIndices()
        //    => _waveInfoTable?.BattleNodeIndices ?? Enumerable.Empty<int>();

        //public IEnumerable<int> GetAllTeamUpgradeIds()
        //    => _teamUpgradeTable?.Ids ?? Enumerable.Empty<int>();


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
