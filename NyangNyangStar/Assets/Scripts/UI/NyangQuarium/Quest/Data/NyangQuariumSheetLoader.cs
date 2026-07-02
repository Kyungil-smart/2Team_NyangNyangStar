using System;
using Data.Parsing;
using Data.ScriptableObjects;
using Data.ScriptableObjects.NyangQuariumSO;
using UnityEngine;

namespace UI.NyangQuarium.Quest
{
    // 냥쿠아리움 전용 시트 로더 (퀘스트, 생성기, 물고기, 경험치)
    public class NyangQuariumSheetLoader : MonoBehaviour
    {
        public static NyangQuariumSheetLoader Instance { get; private set; }

        [Header("냥쿠_퀘스트 테이블")]
        [SerializeField] private SheetData _questURL;
        [SerializeField] private NyangQuariumQuestSO _questSO;
        [SerializeField] private int _questHeaderRowCount = 3;

        [Header("냥쿠_퀘스트 보상 테이블")]
        [SerializeField] private SheetData _questRewardURL;
        [SerializeField] private NyangQuariumQuestRewardSO _questRewardSO;
        [SerializeField] private int _questRewardHeaderRowCount = 3;

        [Header("냥쿠_퀘스트 스트링 테이블")]
        [SerializeField] private SheetData _questStringURL;
        [SerializeField] private NyangQuariumQuestStringSO _questStringSO;
        [SerializeField] private int _questStringHeaderRowCount = 1;

        [Header("냥쿠_생성기 테이블")]
        [SerializeField] private SheetData _generatorURL;
        [SerializeField] private NyangQuariumGeneratorSO _generatorSO;
        [SerializeField] private int _generatorHeaderRowCount = 1;

        [Header("냥쿠_물고기 테이블")]
        [SerializeField] private SheetData _fishURL;
        [SerializeField] private NyangQuariumFishSO _fishSO;
        [SerializeField] private int _fishHeaderRowCount = 1;

        [Header("냥쿠_경험치 테이블")]
        [SerializeField] private SheetData _expItemURL;
        [SerializeField] private NyangQuariumExpItemSO _expItemSO;
        [SerializeField] private int _expItemHeaderRowCount = 1;

        [Header("냥쿠_수조 레벨 테이블")]
        [SerializeField] private SheetData _aquariumLevelURL;
        [SerializeField] private NyangQuariumAquariumLevelSO _aquariumLevelSO;
        [SerializeField] private int _aquariumLevelHeaderRowCount = 1;

        [Header("옵션")]
        [SerializeField] private bool _loadOnStart = true;

        private int _pendingSheetCount;

        public bool IsReady { get; private set; }

        public NyangQuariumQuestSO QuestSO => _questSO;
        public NyangQuariumQuestRewardSO QuestRewardSO => _questRewardSO;
        public NyangQuariumQuestStringSO QuestStringSO => _questStringSO;
        public NyangQuariumGeneratorSO GeneratorSO => _generatorSO;
        public NyangQuariumFishSO FishSO => _fishSO;
        public NyangQuariumExpItemSO ExpItemSO => _expItemSO;
        public NyangQuariumAquariumLevelSO AquariumLevelSO => _aquariumLevelSO;

        // 시트 로드 완료 시 QuestManager 등에서 구독
        public event Action OnLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (_loadOnStart)
                DataLoad();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // 수동 호출도 가능 (Login Scene_TEST 등)
        public void DataLoad()
        {
            IsReady = false;
            StopAllCoroutines();
            _pendingSheetCount = 7;

            DebugTool.Log("[NyangQuariumSheetLoader] 시트 로드 시작", DebugType.Data, this);

            LoadSheetData(_questURL, _questSO, _questHeaderRowCount, "퀘스트", () =>
            {
                _questSO?.PrintData();
                OnSheetCompleted("냥쿠_퀘스트 테이블 로드 완료");
            });

            LoadSheetData(_questRewardURL, _questRewardSO, _questRewardHeaderRowCount, "퀘스트 보상", () =>
            {
                _questRewardSO?.PrintData();
                OnSheetCompleted("냥쿠_퀘스트 보상 테이블 로드 완료");
            });

            LoadSheetData(_questStringURL, _questStringSO, _questStringHeaderRowCount, "퀘스트 스트링", () =>
            {
                _questStringSO?.PrintData();
                OnSheetCompleted("냥쿠_퀘스트 스트링 테이블 로드 완료");
            });

            LoadSheetData(_generatorURL, _generatorSO, _generatorHeaderRowCount, "생성기", () =>
            {
                _generatorSO?.SortData();
                _generatorSO?.PrintData();
                OnSheetCompleted("냥쿠_생성기 테이블 로드 완료");
            });

            LoadSheetData(_fishURL, _fishSO, _fishHeaderRowCount, "물고기", () =>
            {
                _fishSO?.SortData();
                _fishSO?.PrintData();
                OnSheetCompleted("냥쿠_물고기 테이블 로드 완료");
            });

            LoadSheetData(_expItemURL, _expItemSO, _expItemHeaderRowCount, "경험치", () =>
            {
                _expItemSO?.SortData();
                _expItemSO?.PrintData();
                OnSheetCompleted("냥쿠_경험치 테이블 로드 완료");
            });

            LoadSheetData(_aquariumLevelURL, _aquariumLevelSO, _aquariumLevelHeaderRowCount, "수조 레벨", () =>
            {
                _aquariumLevelSO?.SortData();
                _aquariumLevelSO?.PrintData();
                OnSheetCompleted("냥쿠_수조 레벨 테이블 로드 완료");
            });
        }

        public void ClearData()
        {
            StopAllCoroutines();
            IsReady = false;
            _pendingSheetCount = 0;

            _questSO?.ClearData();
            _questRewardSO?.ClearData();
            _questStringSO?.ClearData();
            _generatorSO?.ClearData();
            _fishSO?.ClearData();
            _expItemSO?.ClearData();
            _aquariumLevelSO?.ClearData();

            DebugTool.Log("[NyangQuariumSheetLoader] 캐싱된 시트 데이터 제거 완료", DebugType.Data, this);
        }

        private void OnSheetCompleted(string message)
        {
            _pendingSheetCount--;

            DebugTool.Log(
                $"[NyangQuariumSheetLoader] {message} / 남음 {_pendingSheetCount}",
                DebugType.Data,
                this);

            if (_pendingSheetCount > 0)
                return;

            IsReady = true;
            DebugTool.Log("[NyangQuariumSheetLoader] 모든 시트 로드 완료", DebugType.Data, this);
            OnLoadCompleted?.Invoke();
        }

        private void LoadSheetData<T>(
            SheetData sheet,
            T targetSo,
            int headerRowCount,
            string label,
            Action onComplete)
            where T : SoBase, ISheetParsable
        {
            if (targetSo == null)
            {
                DebugTool.Error($"[NyangQuariumSheetLoader] {label} SO가 연결되지 않았습니다.", DebugType.Data, this);
                onComplete?.Invoke();
                return;
            }

            targetSo.Init();

            if (string.IsNullOrEmpty(sheet.URL))
            {
                DebugTool.Warning($"[NyangQuariumSheetLoader] {label} Sheet URL이 비어있습니다.", DebugType.Data, this);
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(sheet.Load((split, lines) =>
            {
                if (lines == null)
                {
                    DebugTool.Error($"[NyangQuariumSheetLoader] {label} 시트 로드 실패", DebugType.Data, this);
                    onComplete?.Invoke();
                    return;
                }

                for (int i = headerRowCount; i < lines.Length; i++)
                {
                    string line = TrimSheetLine(lines[i]);

                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] cols = line.Split(split);

                    if (cols.Length == 0)
                        continue;

                    targetSo.SetData(cols);
                }

                DebugTool.Log($"[NyangQuariumSheetLoader] {label} 파싱 완료", DebugType.Data, this);
                onComplete?.Invoke();
            }));
        }

        private static string TrimSheetLine(string line)
        {
            return string.IsNullOrEmpty(line)
                ? string.Empty
                : line.TrimEnd('\r', '\n');
        }
    }
}
