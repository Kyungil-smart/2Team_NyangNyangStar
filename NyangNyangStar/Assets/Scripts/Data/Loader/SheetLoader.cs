using System;
using Data.Parsing;
using Data.LibrarySystem;
using Data.ScriptableObjects;
using Data.ScriptableObjects.KeyContainer;
using Services.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Data.Loader
{
    public class SheetLoader : MonoBehaviour
    {
        [Header("Key Container")]
        public List<SheetData> keyContainers;
        [SerializeField] private List<KeyContainerSo> keyLists = new(); 
        private Dictionary<int, KeyContainerSo> _keyContainerDict = new();

        [SerializeField] private int _pendingSheetCount;
        public int PendingSheetCount => _pendingSheetCount;

        public void DataLoad()
        {
            if (LocalDataAccess.Instance == null)
            {
                DebugTool.Error(
                    "[DataManager] LocalDataAccess.Instance가 없음. 씬에 LocalDataAccess GameObject를 추가하세요.",
                    DebugType.Data, this);
                return;
            }

            LoadKeyContainerData(keyContainers, keyLists, _keyContainerDict, onComplete: () =>
            {
                LocalDataAccess.Instance.Game.RegisterKeyContainers(_keyContainerDict);
                OnSheetCompleted();
            });
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
                    if (string.IsNullOrEmpty(line)) 
                        continue;

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

                        if (cols.Length < 2)
                        {
                            DebugTool.Warning(
                                $"[KeyContainer] {orderIndex}번 시트 {row}번째 줄 컬럼 부족: {line}",
                                DebugType.Data,
                                this);

                            continue;
                        }

                        if (cols.Length > 5)
                        {
                            DebugTool.Warning($"컬럼 개수가 {cols.Length} 입니다.\n" +
                                              $"불필요한 데이터가 있는지 확인 바랍니다.", DebugType.Data);
                            return;
                        }

                        string key = cols[0].Trim();
                        string fileName = cols[1].Trim();
                        string usage = cols[2].Trim();
                        string buildType = cols[3].Trim();
                        string imageType = cols[4].Trim();
                        
                        KeyData data = KeyDataMapping(key, fileName, usage, buildType, imageType);

                        log.AppendLine($"{row}번째 : Key = {data.Key} | {data.FileName} | " +
                                       $"{data.Usage} | {data.BuildType} | " +
                                       $"{data.ImageType.ToString()}");
                        
                        keyContainer.AddData(data);
                    }

                    DebugTool.Log(
                        $"[KeyContainer] {orderIndex}번 시트 로드 완료 / 총 {keyContainer.KeyDatas.Count}건",
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
                string buildtype, string ImageType)
            {
                if(string.IsNullOrEmpty(key) ||  string.IsNullOrEmpty(fileName) || 
                   string.IsNullOrEmpty(usage) || string.IsNullOrEmpty(buildtype) || 
                   string.IsNullOrEmpty(ImageType))
                    return null;

                KeyData data = new();
                
                data.Key = key;
                data.FileName = fileName;
                data.Usage = usage;
                data.BuildType = convertBuildType(buildtype);
                data.ImageType = convertImageType(ImageType);
                
                return data;
            }

            BuildType convertBuildType(string buildType)
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
            
            ImageType convertImageType(string imageType)
            {
                switch (imageType)
                {
                    case "Sprite" :
                        return ImageType.Sprite;
                    case "UI" :
                        return ImageType.UI;
                    default :
                        return ImageType.None;
                }
            }
        }
        
        public void ClearDatas()
        {
            StopAllCoroutines();

            _pendingSheetCount = 0;

            foreach (KeyContainerSo keyContainer in keyLists)
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