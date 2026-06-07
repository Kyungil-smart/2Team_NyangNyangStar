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
        private bool _loadSheetsWhenReady;
        private bool _isLoadingSheetLoader;
    
        public void Init()
        {
            _root = GameObject.Find("@Data");
            
            if (_root == null)
            {
                _root = new GameObject { name = "@Data" };
                Object.DontDestroyOnLoad(_root);
            }

            GetSheetLoaderPrefab();

            DebugTool.Log("데이터 매니저 초기화 완료", DebugType.Game);
        }

        public void LoadSheets()
        {
            if (_sheetLoader == null)
            {
                _loadSheetsWhenReady = true;
                GetSheetLoaderPrefab();
                DebugTool.Log("SheetLoader 로드 대기 중 - 로드 완료 후 시트를 자동 로드합니다.", DebugType.Data);
                return;
            }

            _loadSheetsWhenReady = false;
            _sheetLoader.DataLoad();
        }

        public void ClearSheets()
        {
            _loadSheetsWhenReady = false;
            
            if (_sheetLoader != null)
                _sheetLoader.ClearDatas();
        }

        public void GetSheetLoaderPrefab()
        {
            if (_sheetLoader != null || _isLoadingSheetLoader)
                return;

            _isLoadingSheetLoader = true;
            
            GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.SheetLoader,
                loadPrefab =>
                {
                    _isLoadingSheetLoader = false;

                    if (loadPrefab == null)
                    {
                        DebugTool.Warning($"{KeyContainer.Prefabs.SheetLoader} : 로드된 SheetLoader 인스턴스가 없습니다.", DebugType.Missing);
                        return;
                    }
                    
                    _sheetLoader = loadPrefab.GetComponent<SheetLoader>();
                    if (_sheetLoader == null)
                    {
                        DebugTool.Warning($"{loadPrefab.name}에 시트 로더 컴포넌트가 없습니다.", DebugType.Missing);
                        return;
                    }
                    
                    loadPrefab.transform.SetParent(_root.transform, false);
                    DebugTool.Log($"{loadPrefab.name} : 시트 로더 로드 완료", DebugType.Data);

                    if (_loadSheetsWhenReady)
                        LoadSheets();
                },
                failedKey =>
                {
                    _isLoadingSheetLoader = false;
                    DebugTool.Warning($"{failedKey} : 시트 로더 로드 실패", DebugType.Missing);
                });
        }

        public void Clear()
        {
            if (_root == null) 
                return;

            ClearSheets();
            
            Object.Destroy(_root);
            _root = null;
            _sheetLoader = null;
            _isLoadingSheetLoader = false;
            
            DebugTool.Log("데이터 매니저 제거 완료", DebugType.Game);
        }
    }
}
