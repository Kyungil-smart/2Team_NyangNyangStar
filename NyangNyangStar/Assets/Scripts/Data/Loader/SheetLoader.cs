using UnityEngine;

namespace Data.Loader
{
    public class SheetLoader : MonoBehaviour
    {

        //[Header("Player Class")]
        //public SheetData _classSheet;
        //[SerializeField] private List<PlayerClassDataSO> _classDataList;
        //private Dictionary<int, PlayerClassDataSO> _classDataDictionary = new();

        //[Header("Zombie Stat")]
        //public SheetData _zombieStatSheet;
        //[SerializeField] private List<ZombieStatSO> _zombieStatDataList;
        //private Dictionary<int, ZombieStatSO> _zombieStatDataDictionary = new();

        //[Header("Wave Info")]
        //public SheetData _waveInfoSheet;
        //[SerializeField] private List<WaveInfoSO> _waveInfoDataList;
        //private Dictionary<int, WaveInfoSO> _waveInfoDataDictionary = new();
        //[SerializeField] private WaveInfoTableSO _waveInfoTable;

        //[Header("Wave Spawn Table")]
        //public SheetData _waveSpawnSheet;
        //[SerializeField] private WaveSpawnTableSO _waveSpawnTable;

        //[Header("Player Upgrade")]
        //public SheetData _playerUpgradeSheet;
        //[SerializeField] private PlayerUpgradeTableSO _playerUpgradeTable;

        //[Header("Team Upgrade")]
        //public SheetData _teamUpgradeSheet;
        //[SerializeField] private TeamUpgradeTableSO _teamUpgradeTable;

        [SerializeField] private int _pendingSheetCount;
        public int PendingSHeetCount => _pendingSheetCount;

        private void Awake()
        {
            //_classDataDictionary = InitDict(_classDataList);
            //_zombieStatDataDictionary = InitDict(_zombieStatDataList);
            //_waveInfoDataDictionary = InitDict(_waveInfoDataList);
        }

        public void DataLoad()
        {
            if (LocalDataAccess.Instance == null)
            {
                DebugTool.Error(
                    "[DataManager] LocalDataAccess.Instance가 없음. 씬에 LocalDataAccess GameObject를 추가하세요.",
                    DebugType.Data, this);
                return;
            }

            //LoadSheetData(_classSheet, _classDataList, _classDataDictionary, onComplete: () =>
            //{
            //    LocalDataAccess.Instance.Game.RegisterClasses(_classDataDictionary);
            //    OnSheetCompleted();
            //});

            //LoadSheetData(_zombieStatSheet, _zombieStatDataList, _zombieStatDataDictionary, onComplete: () =>
            //{
            //    LocalDataAccess.Instance.Game.RegisterZombieStats(_zombieStatDataDictionary);
            //    OnSheetCompleted();
            //});

            //LoadSheetData(_waveInfoSheet, _waveInfoDataList, _waveInfoDataDictionary, onComplete: () =>
            //{
            //    if (_waveInfoTable != null)
            //    {
            //        _waveInfoTable.Build(_waveInfoDataList);
            //        LocalDataAccess.Instance.Game.RegisterWaveInfoTable(_waveInfoTable);
            //    }
            //    else
            //    {
            //        DebugTool.Error(
            //            "[DataManager] _waveInfoTable이 인스펙터에 미할당",
            //            DebugType.Data, this);
            //    }
            //    OnSheetCompleted();
            //});

            //LoadWaveSpawnTable(_waveSpawnSheet, _waveSpawnTable, onComplete: () =>
            //{
            //    LocalDataAccess.Instance.Game.RegisterWaveSpawnTable(_waveSpawnTable);
            //    OnSheetCompleted();
            //});

            //LoadPlayerUpgradeTable(_playerUpgradeSheet, _playerUpgradeTable, onComplete: () =>
            //{
            //    LocalDataAccess.Instance.Game.RegisterPlayerUpgradeTable(_playerUpgradeTable);
            //    OnSheetCompleted();
            //});

            //LoadTeamUpgradeTable(_teamUpgradeSheet, _teamUpgradeTable, onComplete: () =>
            //{
            //    LocalDataAccess.Instance.Game.RegisterTeamUpgradeTable(_teamUpgradeTable);
            //    OnSheetCompleted();
            //});
        }

        private void OnSheetCompleted()
        {
            _pendingSheetCount--;
            DebugTool.Log(
                $"[DataManager] 시트 완료 카운터: 남음 {_pendingSheetCount}",
                DebugType.Data, this);

            if (_pendingSheetCount <= 0)
            {
                LocalDataAccess.Instance.Game.MarkReady();
            }
        }


        private Dictionary<int, T> InitDict<T>(List<T> list)
            where T : ScriptableObject, ISheetParsable
        {
            if (list == null || list.Count == 0)
            {
                DebugTool.Warning(
                    $"[{typeof(T).Name}] 리스트 비어있음 - 빈 사전 반환",
                    DebugType.Data, this);
                return new Dictionary<int, T>();
            }

            return list.ToDictionary(x => x.Id);
        }


        private void LoadSheetData<T>(
            SheetData sheet,
            List<T> list,
            Dictionary<int, T> dict,
            int headerRowCount = 1,
            Action onComplete = null
        ) where T : ScriptableObject, ISheetParsable
        {
            StartCoroutine(sheet.Load((split, lines) =>
            {
                if (lines == null)
                {
                    DebugTool.Error(
                        $"[{typeof(T).Name}] 시트 로드 실패 - lines가 null",
                        DebugType.Data, this);
                    onComplete?.Invoke();
                    return;
                }

                for (int i = headerRowCount; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    string[] cols = line.Split(split);

                    if (cols.Length == 0 || !int.TryParse(cols[0], out int id))
                    {
                        DebugTool.Error(
                            $"[{typeof(T).Name}] {i}번째 줄 ID 파싱 실패: '{(cols.Length > 0 ? cols[0] : "(empty)")}'",
                            DebugType.Data, this);
                        continue;
                    }

                    T data;
                    if (dict.TryGetValue(id, out var existing))
                    {
                        data = existing;
                    }
                    else
                    {
                        data = ScriptableObject.CreateInstance<T>();
                        data.name = $"{typeof(T).Name}_{id}";
                        dict.Add(id, data);
                        list.Add(data);
                        DebugTool.Warning(
                            $"[{typeof(T).Name}] ID {id} 사전에 없어서 새 인스턴스 생성",
                            DebugType.Data, this);
                    }

                    data.SetData(cols);
                }

                DebugTool.Log(
                    $"[{typeof(T).Name}] 시트 로드 완료 (총 {dict.Count}건)",
                    DebugType.Data, this);
                onComplete?.Invoke();
            }));
        }


        //private void LoadWaveSpawnTable(
        //    SheetData sheet,
        //    WaveSpawnTableSO table,
        //    int headerRowCount = 1,
        //    Action onComplete = null)
        //{
        //    if (table == null)
        //    {
        //        DebugTool.Error(
        //            "[DataManager] _waveSpawnTable이 인스펙터에 미할당",
        //            DebugType.Data, this);
        //        onComplete?.Invoke();
        //        return;
        //    }

        //    StartCoroutine(sheet.Load((split, lines) =>
        //    {
        //        if (lines == null)
        //        {
        //            DebugTool.Error(
        //                "[DataManager] WaveSpawn 시트 로드 실패 - lines가 null",
        //                DebugType.Data, this);
        //            onComplete?.Invoke();
        //            return;
        //        }

        //        table.LoadFromSheet(split, lines, headerRowCount);
        //        onComplete?.Invoke();
        //    }));
        //}


        //private void LoadPlayerUpgradeTable(
        //    SheetData sheet,
        //    PlayerUpgradeTableSO table,
        //    int headerRowCount = 1,
        //    Action onComplete = null)
        //{
        //    if (table == null)
        //    {
        //        DebugTool.Error(
        //            "[DataManager] _playerUpgradeTable이 인스펙터에 미할당",
        //            DebugType.Data, this);
        //        onComplete?.Invoke();
        //        return;
        //    }

        //    StartCoroutine(sheet.Load((split, lines) =>
        //    {
        //        if (lines == null)
        //        {
        //            DebugTool.Error(
        //                "[DataManager] PlayerUpgrade 시트 로드 실패 - lines가 null",
        //                DebugType.Data, this);
        //            onComplete?.Invoke();
        //            return;
        //        }

        //        table.LoadFromSheet(split, lines, headerRowCount);
        //        onComplete?.Invoke();
        //    }));
        //}


        //private void LoadTeamUpgradeTable(
        //    SheetData sheet,
        //    TeamUpgradeTableSO table,
        //    int headerRowCount = 1,
        //    Action onComplete = null)
        //{
        //    if (table == null)
        //    {
        //        DebugTool.Error(
        //            "[DataManager] _teamUpgradeTable이 인스펙터에 미할당",
        //            DebugType.Data, this);
        //        onComplete?.Invoke();
        //        return;
        //    }

        //    StartCoroutine(sheet.Load((split, lines) =>
        //    {
        //        if (lines == null)
        //        {
        //            DebugTool.Error(
        //                "[DataManager] TeamUpgrade 시트 로드 실패 - lines가 null",
        //                DebugType.Data, this);
        //            onComplete?.Invoke();
        //            return;
        //        }

        //        table.LoadFromSheet(split, lines, headerRowCount);
        //        onComplete?.Invoke();
        //    }));
        //} 
    }
}