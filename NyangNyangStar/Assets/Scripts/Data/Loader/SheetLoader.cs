using Core.Managers;
using System;
using Data.Parsing;
using Data.LibrarySystem;
using Data.ScriptableObjects;
using Data.ScriptableObjects.KeyContainerSO;
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

        [Space(8)] [SerializeField] private int _pendingSheetCount;
        public int PendingSheetCount => _pendingSheetCount;

        public void DataLoad()
        {
            if(GameManager.Data == null) GameManager.Init();
            if (LocalDataAccess.Instance == null)
            {
                DebugTool.Error(
                    "[DataManager] LocalDataAccess.Instance가 없음. 씬에 LocalDataAccess GameObject를 추가하세요.",
                    DebugType.Data, this);
                return;
            }

            LoadKeyContainerData(keyCotainerURL, keySo, _keyContainerDict, onComplete: () =>
            {
                LocalDataAccess.Instance.Game.RegisterKeyContainers(_keyContainerDict);
                foreach (KeyContainerSo so in keySo)
                {
                    if (so.DataCount <= 0)
                    {
                        DebugTool.Warning($"{so.name} : 데이터 입니다.", DebugType.Data);
                        continue;
                    }
                    so.RegisterAll();
                }
                OnSheetCompleted();
                
                KeyContainer.PrintKeys();
            });
            
            LoadSheetData(scratchingURL, scratchingSo, 1, () =>
                {
                    OnSheetCompleted();
                    
                    scratchingSo.PrintData();
                });
        }

        private void OnSheetCompleted()
        {
            _pendingSheetCount--;
            DebugTool.Log(
                $"[DataManager] 시트 완료 카운터: 남음 {_pendingSheetCount}",
                DebugType.Data, this);

            if (_pendingSheetCount <= 0)
                LocalDataAccess.Instance.Game.MarkReady();
        }

        private void LoadSheetData<T>(
             SheetData sheet,
            T targetSo,
            int headerRowCount = 1,
            Action onComplete = null
        ) where T : SoBase, ISheetParsable
        {
            if(targetSo == null)
            {
                DebugTool.Error($"[{typeof(T).Name}] SO 가 없습니다.", DebugType.Data, this);
                onComplete?.Invoke();
                return;
            }
            
            // 로드 전 SO 초기화
            targetSo.Init();

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
                        string line = lines[i].Trim();
                        if(string.IsNullOrEmpty(line))
                            continue;
                        
                        string[] cols = line.Split(split);
                        if (cols.Length == 0)
                            continue;
                        
                        // 각 행의 문자열 배열을 SO의 SetData로 던져서 
                        // SO가 스스로 파싱하고 리스트에 추가하도록 위임
                        targetSo.SetData(cols);
                    }
                    DebugTool.Log($"[{typeof(T).Name}] 컨테이너 시트 데이터 로드 완료", DebugType.Data, this);
                    onComplete?.Invoke();
                }
            ));
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

                        CompleteOne();
                        return;
                    }

                    for (int row = headerRowCount; row < lines.Length; row++)
                    {
                        string line = lines[row].Trim();

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
                            return;
                        }

                        string key = cols[0].Trim();
                        string fileName = cols[1].Trim();
                        string usage = cols[2].Trim();
                        string groupType = cols[3].Trim();
                        string labelType = cols[4].Trim();
                        string buildType = cols[5].Trim();
                        
                        KeyData data = KeyDataMapping(key, fileName, usage, groupType, labelType, buildType);

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

                    CompleteOne();
                }));
            }

            void CompleteOne()
            {
                completedCount++;

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
                if(string.IsNullOrEmpty(key) ||  string.IsNullOrEmpty(fileName) || 
                   string.IsNullOrEmpty(usage) || string.IsNullOrEmpty(buildType) || 
                   string.IsNullOrEmpty(labelType))
                    return null;

                KeyData data = new() { 
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
                    case "Local" :
                        return BuildType.Local;
                    case "Remote" :
                        return BuildType.Remote;
                    default :
                        return BuildType.None;
                }
            }
            
            LabelType ConvertLabelType(string lableType)
            {
                switch (lableType)
                {
                    case "Sprite" :
                        return LabelType.Sprite;
                    case "Audio" :
                        return LabelType.Audio;
                    default :
                        return LabelType.None;
                }
            }
            AddressableGroupType ConvertGroupType(string groupType)
            {
                switch (groupType)
                {
                    case "Common":
                        return  AddressableGroupType.Common;
                    case "Main":
                        return AddressableGroupType.Main;
                    case "Nyangstagram":
                        return AddressableGroupType.Nyangstagram;
                    case "Scratching" :
                        return AddressableGroupType.Scratching;
                    default:
                        return AddressableGroupType.None;
                }
            }
        }
        
        public void ClearDatas()
        {
            StopAllCoroutines();

            _pendingSheetCount = 0;

            foreach (KeyContainerSo keyContainer in keySo)
            {
                if (keyContainer == null)
                    continue;

                keyContainer.ClearData();
            }

            _keyContainerDict.Clear();

            DebugTool.Log(
                "[SheetLoader] 캐싱된 시트 데이터 제거 완료",
                DebugType.Data,
                this);
        }
    }
}