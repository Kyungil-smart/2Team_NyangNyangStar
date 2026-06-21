using Core.Managers;
using System;
using Data.Parsing;
using Data.LibrarySystem;
using Data.ScriptableObjects;
using Data.ScriptableObjects.MoongchiSO;
using Data.ScriptableObjects.KeyContainerSO;
using Data.ScriptableObjects.MergeBoard;
using Data.ScriptableObjects.ScratchingTimeSO;
using Services.Enums;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Util;

namespace Data.Loader
{
    public class SheetLoader : MonoBehaviour
    {
        [Header("Key Container")]
        [SerializeField] private List<SheetData> keyCotainerURL = new();
        [SerializeField] private List<KeyContainerSo> keySo = new();
        private readonly Dictionary<int, KeyContainerSo> _keyContainerDict = new();

        [Space(8)] [Header("스크래칭 타임")]
        [SerializeField] private SheetData scratchingURL;
        [SerializeField] private ScratchingSo scratchingSo;

        [Space(8)] [Header("냥냥스냅 배경")]
        [SerializeField] private SheetData nyangNyangSnapBackgroundURL;
        [SerializeField] private NyangNyangSnapBackgroundSO nyangNyangSnapBackgroundSo;

        [Space(8)] [Header("머지 보드 아이템")]
        [SerializeField] private SheetData mergeBoardItemURL;
        [SerializeField] private ItemDatabaseSo itemDatabaseSo;

        [Space(8)] [Header("냥냥스냅 포즈")]
        [SerializeField] private SheetData nyangNyangSnapPoseURL;
        [SerializeField] private NyangNyangSnapPoseSO nyangNyangSnapPoseSo;

        [Space(8)]
        [Header("냥냥스냅 도구")]
        [SerializeField] private SheetData nyangNyangSnapToolURL;
        [SerializeField] private NyangNyangSnapToolSO nyangNyangSnapToolSo;

        [Space(8)] [Header("뭉치를 찾아라 상점/보상")]
        [SerializeField] private SheetData findMoongchiShopURL;
        [SerializeField] private MoongchiShopSO findMoongchiShopSo;

        [Space(8)] [Header("뭉치를 찾아라 미션")]
        [SerializeField] private SheetData findMoongchiMissionURL;
        [SerializeField] private MoongchiMissionSO findMoongchiMissionSo;

        [Space(8)] [Header("뭉치를 찾아라 프로필")]
        [SerializeField] private SheetData findMoongchiProfileURL;
        [SerializeField] private MoongchiProfileSO findMoongchiProfileSo;

        [Space(8)] [SerializeField] private int _pendingSheetCount;
        public int PendingSheetCount => _pendingSheetCount;

        public event Action<float, string> OnSheetLoadProgressChanged;

        private int _totalLoadStepCount;
        private int _completedLoadStepCount;

        public void DataLoad()
        {
            if (GameManager.Data == null)
                GameManager.Init();

            if (LocalDataAccess.Instance == null)
            {
                DebugTool.Error(
                    "[DataManager] LocalDataAccess.Instance가 없음. 씬에 LocalDataAccess GameObject를 추가하세요.",
                    DebugType.Data, this);
                return;
            }

            LocalDataAccess.Instance.Game.MarkNotReady();

            StopAllCoroutines();
            _pendingSheetCount = 0;
            _completedLoadStepCount = 0;
            _totalLoadStepCount = Mathf.Max(1, (keyCotainerURL?.Count ?? 0) + 8);

            ReportSheetProgress("시트 로드 시작");

            LoadKeyContainerData(keyCotainerURL, keySo, _keyContainerDict, onComplete: () =>
            {
                LocalDataAccess.Instance.Game.RegisterKeyContainers(_keyContainerDict);

                foreach (KeyContainerSo so in keySo)
                {
                    if (so == null)
                        continue;

                    if (so.DataCount <= 0)
                    {
                        DebugTool.Warning($"{so.name} : 데이터 입니다.", DebugType.Data);
                        continue;
                    }

                    so.RegisterAll();
                }

                KeyContainer.PrintKeys();
                LoadContentSheets();
            });
        }

        private void LoadContentSheets()
        {
            _pendingSheetCount = 8;

            LoadSheetData(scratchingURL, scratchingSo, 1, () =>
            {
                OnSheetCompleted("스크래칭 타임 시트 로드 완료");
                scratchingSo?.PrintData();
            });

            LoadSheetData(nyangNyangSnapBackgroundURL, nyangNyangSnapBackgroundSo, 3, () =>
            {
                OnSheetCompleted("냥냥스냅 배경 시트 로드 완료");
                nyangNyangSnapBackgroundSo?.PrintData();
            });

            LoadSheetData(mergeBoardItemURL, itemDatabaseSo, 1, () =>
            {
                if (itemDatabaseSo == null)
                {
                    OnSheetCompleted("머지 보드 아이템 시트 로드 완료");
                    return;
                }

                StartCoroutine(itemDatabaseSo.LoadItemSpritesCoroutine(() =>
                {
                    LocalDataAccess.Instance.Game.RegisterMergeBoardItemDatabase(itemDatabaseSo);
                    itemDatabaseSo.PrintData();
                    OnSheetCompleted("머지 보드 아이템 이미지 로드 완료");
                }));
            });

            LoadSheetData(nyangNyangSnapPoseURL, nyangNyangSnapPoseSo, 1, () =>
            {
                OnSheetCompleted("냥냥스냅 포즈 시트 로드 완료");
                nyangNyangSnapPoseSo?.PrintData();
            });

            LoadSheetData(nyangNyangSnapToolURL, nyangNyangSnapToolSo, 1, () =>
            {
                OnSheetCompleted("냥냥스냅 포즈 시트 로드 완료");
                nyangNyangSnapToolSo?.PrintData();
            });

            LoadSheetData(findMoongchiShopURL, findMoongchiShopSo, 2, () =>
            {
                OnSheetCompleted("뭉치를 찾아라 상점/보상 시트 로드 완료");
                findMoongchiShopSo?.PrintData();
            });

            LoadSheetData(findMoongchiMissionURL, findMoongchiMissionSo, 2, () =>
            {
                OnSheetCompleted("뭉치를 찾아라 미션 시트 로드 완료");
                findMoongchiMissionSo?.PrintData();
            });

            LoadSheetData(findMoongchiProfileURL, findMoongchiProfileSo, 2, () =>
            {
                OnSheetCompleted("뭉치를 찾아라 프로필 시트 로드 완료");
                findMoongchiProfileSo?.PrintData();
            });
        }

        private void OnSheetCompleted(string message)
        {
            _pendingSheetCount--;
            CompleteProgressStep(message);

            DebugTool.Log(
                $"[DataManager] 시트 완료 카운터: 남음 {_pendingSheetCount}",
                DebugType.Data, this);

            if (_pendingSheetCount <= 0)
            {
                ReportSheetProgress("시트 로드 완료");
                LocalDataAccess.Instance.Game.MarkReady();
            }
        }

        private void LoadSheetData<T>(
            SheetData sheet,
            T targetSo,
            int headerRowCount = 1,
            Action onComplete = null
        ) where T : SoBase, ISheetParsable
        {
            if (targetSo == null)
            {
                DebugTool.Error($"[{typeof(T).Name}] SO 가 없습니다.", DebugType.Data, this);
                onComplete?.Invoke();
                return;
            }

            targetSo.Init();

            if (string.IsNullOrEmpty(sheet.URL))
            {
                DebugTool.Warning($"[{typeof(T).Name}] SheetData URL이 비어있습니다.", DebugType.Data, this);
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(sheet.Load((split, lines) =>
            {
                if (lines == null)
                {
                    DebugTool.Error($"[{typeof(T).Name} 시트 로드 실패 - 라인이 없습니다.", DebugType.Data, this);
                    onComplete?.Invoke();
                    return;
                }

                for (int i = headerRowCount; i < lines.Length; i++)
                {
                    // 줄 끝 탭은 빈 컬럼 구분자라 Trim()으로 제거하면 TSV 컬럼 수가 줄어듭니다.
                    string line = TrimSheetLine(lines[i]);

                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] cols = line.Split(split);

                    if (cols.Length == 0)
                        continue;

                    targetSo.SetData(cols);
                }

                DebugTool.Log($"[{typeof(T).Name}] 컨테이너 시트 데이터 로드 완료", DebugType.Data, this);
                onComplete?.Invoke();
            }));
        }

        private void LoadKeyContainerData(
            List<SheetData> sheets,
            List<KeyContainerSo> keyContainerList,
            Dictionary<int, KeyContainerSo> dict,
            int headerRowCount = 1,
            Action onComplete = null)
        {
            if (sheets == null || sheets.Count == 0)
            {
                DebugTool.Warning(
                    "[KeyContainer] 로드할 SheetData 리스트가 비어있음",
                    DebugType.Data,
                    this);

                onComplete?.Invoke();
                return;
            }

            if (keyContainerList == null || keyContainerList.Count == 0)
            {
                DebugTool.Error(
                    "[KeyContainer] KeyContainerSo 리스트가 비어있음",
                    DebugType.Data,
                    this);

                onComplete?.Invoke();
                return;
            }

            if (sheets.Count != keyContainerList.Count)
            {
                DebugTool.Error(
                    $"[KeyContainer] SheetData 수와 KeyContainerSo 수가 다름 / SheetData: {sheets.Count}, SO: {keyContainerList.Count}",
                    DebugType.Data,
                    this);

                onComplete?.Invoke();
                return;
            }

            dict.Clear();

            int completedCount = 0;

            for (int i = 0; i < sheets.Count; i++)
            {
                int orderIndex = i;

                SheetData sheet = sheets[orderIndex];
                KeyContainerSo keyContainer = keyContainerList[orderIndex];

                keyContainer.ClearData();
                dict[orderIndex] = keyContainer;

                StringBuilder log = new();
                log.AppendLine($"[KeyContainerSO] {orderIndex} 번 SO 초기화");

                StartCoroutine(sheet.Load((split, lines) =>
                {
                    if (lines == null)
                    {
                        DebugTool.Error(
                            $"[KeyContainer] {orderIndex}번 시트 로드 실패 - lines가 null",
                            DebugType.Data,
                            this);

                        CompleteOne($"KeyContainer {orderIndex}번 시트 로드 실패");
                        return;
                    }

                    for (int row = headerRowCount; row < lines.Length; row++)
                    {
                        string line = TrimSheetLine(lines[row]);

                        if (string.IsNullOrEmpty(line))
                            continue;

                        string[] cols = line.Split(split);

                        if (cols.Length < 6)
                        {
                            DebugTool.Warning(
                                $"[KeyContainer] {orderIndex}번 시트 {row}번째 줄 컬럼 부족: {line}",
                                DebugType.Data,
                                this);

                            continue;
                        }

                        if (cols.Length > 6)
                        {
                            DebugTool.Warning($"컬럼 개수가 {cols.Length} 입니다.\n" +
                                             "불필요한 데이터가 있는지 확인 바랍니다.", DebugType.Data);
                            continue;
                        }

                        string key = cols[0].Trim();
                        string fileName = cols[1].Trim();
                        string usage = cols[2].Trim();
                        string groupType = cols[3].Trim();
                        string labelType = cols[4].Trim();
                        string buildType = cols[5].Trim();

                        KeyData data = KeyDataMapping(key, fileName, usage, groupType, labelType, buildType);

                        if (data == null)
                            continue;

                        log.AppendLine($"{row}번째 : Key = {data.Key} | {data.FileName} | " +
                                       $"{data.GroupType} | {data.Usage} | {data.BuildType} | " +
                                       $"{data.LabelType.ToString()}");

                        keyContainer.AddData(data);
                    }

                    DebugTool.Log(
                        $"[KeyContainer] {orderIndex}번 시트 로드 완료 / 총 {keyContainer.DataCount}건",
                        DebugType.Data,
                        this);

                    Debug.Log(log.ToString());

                    CompleteOne($"KeyContainer {orderIndex + 1}/{sheets.Count} 로드 완료");
                }));
            }

            void CompleteOne(string message)
            {
                completedCount++;
                CompleteProgressStep(message);

                if (completedCount >= sheets.Count)
                {
                    DebugTool.Log(
                        $"[KeyContainer] 모든 KeyContainer 시트 로드 완료 ({completedCount}/{sheets.Count})",
                        DebugType.Data,
                        this);

                    onComplete?.Invoke();
                }
            }

            KeyData KeyDataMapping(string key, string fileName, string usage,
                string groupType, string labelType, string buildType)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(fileName) ||
                    string.IsNullOrEmpty(usage) || string.IsNullOrEmpty(buildType) ||
                    string.IsNullOrEmpty(labelType))
                    return null;

                KeyData data = new()
                {
                    Key = key,
                    FileName = fileName,
                    Usage = usage,
                    GroupType = ConvertGroupType(groupType),
                    BuildType = ConvertBuildType(buildType),
                    LabelType = ConvertLabelType(labelType)
                };

                return data;
            }

            BuildType ConvertBuildType(string buildType)
            {
                switch (buildType)
                {
                    case "Local":
                        return BuildType.Local;
                    case "Remote":
                        return BuildType.Remote;
                    default:
                        return BuildType.None;
                }
            }

            LabelType ConvertLabelType(string labelType)
            {
                switch (labelType)
                {
                    case "Sprite":
                        return LabelType.Sprite;
                    case "Audio":
                        return LabelType.Audio;
                    default:
                        return LabelType.None;
                }
            }

            AddressableGroupType ConvertGroupType(string groupType)
            {
                switch (groupType)
                {
                    case "Common":
                        return AddressableGroupType.Common;
                    case "Main":
                        return AddressableGroupType.Main;
                    case "Nyangstagram":
                        return AddressableGroupType.Nyangstagram;
                    case "Scratching":
                        return AddressableGroupType.Scratching;
                    case "Snap":
                        return AddressableGroupType.Snap;
                    case "Finding":
                        return AddressableGroupType.Finding;
                    case "Items":
                        return AddressableGroupType.Items;
                    case "Nyangquarium":
                        return AddressableGroupType.Nyangquarium;
                    default:
                        return AddressableGroupType.None;
                }
            }
        }

        private void CompleteProgressStep(string message)
        {
            _completedLoadStepCount++;
            ReportSheetProgress(message);
        }

        private void ReportSheetProgress(string message)
        {
            float progress = _totalLoadStepCount <= 0
                ? 0f
                : (float)_completedLoadStepCount / _totalLoadStepCount;

            OnSheetLoadProgressChanged?.Invoke(Mathf.Clamp01(progress), message);
        }

        private static string TrimSheetLine(string line)
        {
            return string.IsNullOrEmpty(line)
                ? string.Empty
                : line.TrimEnd('\r', '\n');
        }

        public void ClearDatas()
        {
            StopAllCoroutines();

            _pendingSheetCount = 0;
            _completedLoadStepCount = 0;
            _totalLoadStepCount = 0;

            foreach (KeyContainerSo keyContainer in keySo)
            {
                if (keyContainer == null)
                    continue;

                keyContainer.ClearData();
            }

            scratchingSo?.ClearData();
            nyangNyangSnapBackgroundSo?.ClearData();
            itemDatabaseSo?.ClearData();
            nyangNyangSnapPoseSo?.ClearData();
            nyangNyangSnapToolSo?.ClearData();
            findMoongchiShopSo?.ClearData();
            findMoongchiMissionSo?.ClearData();
            findMoongchiProfileSo?.ClearData();

            _keyContainerDict.Clear();

            DebugTool.Log(
                "[SheetLoader] 캐싱된 시트 데이터 제거 완료",
                DebugType.Data,
                this);
        }
    }
}
