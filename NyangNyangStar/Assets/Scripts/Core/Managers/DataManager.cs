using Data.Loader;
using UnityEngine;
using Util;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class DataManager : ISubManager
    {
        private GameObject _root;
        private SheetLoader _sheetLoader;
        // private bool _loadSheetsWhenReady;
        // private bool _isLoadingSheetLoader;
    
        public void Init()
        {
            _root = GameObject.Find("@Data");
            
            if (_root == null)
            {
                _root = new GameObject { name = "@Data" };
                Object.DontDestroyOnLoad(_root);
            }
            // EnsureLocalDataAccess();
            GetSheetLoaderPrefab();

            DebugTool.Log("데이터 매니저 초기화 완료", DebugType.Game);
        }

        // 로그인 완료 되면 호출
        public void LoadSheets()
        {
            // if (_sheetLoader == null)
            // {
            //     _loadSheetsWhenReady = true;
            //     GetSheetLoaderPrefab();
            //     return;
            // }
            // _loadSheetsWhenReady = false;
            // _sheetLoader.DataLoad();
            
            
            if(_sheetLoader != null)
                _sheetLoader.DataLoad();
        }

        // 데이터 매니저 제거 시 호출
        public void ClearSheets()
        {
            if(_sheetLoader != null)
                _sheetLoader.ClearDatas();
        }

        public void GetSheetLoaderPrefab()
        {
            
            // if (_sheetLoader != null || _isLoadingSheetLoader)
            //     return;
            // _isLoadingSheetLoader = true;
            
            
            GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.SheetLoader,
                loadPrefab =>
                {
                    // _isLoadingSheetLoader = false;
                    // if (loadPrefab == null)
                    // {
                    //     DebugTool.Warning($"{KeyContainer.Prefabs.SheetLoader} : 로드된 SheetLoader 인스턴스가 이미 제거되었습니다.", DebugType.Missing);
                    //     return;
                    // }
                    
                    
                     _sheetLoader = loadPrefab.GetComponent<SheetLoader>();
                    if (_sheetLoader == null)
                    {
                        DebugTool.Warning($"{loadPrefab.name}에 시트 로더 컴포넌트가 없습니다.", DebugType.Missing);
                        return;
                    }
                    
                    loadPrefab.transform.SetParent(_root.transform, false);
                    // if (_loadSheetsWhenReady)
                    //     LoadSheets();
                    
                    
                    DebugTool.Log($"{loadPrefab.name} : 시트 로더 로드 완료", DebugType.Data);
                },
                failedKey =>
                {
                    // _isLoadingSheetLoader = false;
                    DebugTool.Warning($"{failedKey} : 시트 로더 로드 실패", DebugType.Missing);
                });
        }
        
        // private static void EnsureLocalDataAccess()
        // {
        //     if (LocalDataAccess.Instance != null)
        //         return;
        //     new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
        // }

        public void Clear()
        {
            if (_root == null) 
                return;

            ClearSheets();
            
            Object.Destroy(_root);
            _root = null;
            
            DebugTool.Log("데이터 매니저 제거 완료", DebugType.Game);
        }
    }
}
