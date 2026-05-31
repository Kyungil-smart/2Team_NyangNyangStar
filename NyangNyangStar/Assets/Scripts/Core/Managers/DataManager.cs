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

        // 로그인 완료 되면 호출
        public void LoadSheets()
        {
            if(_sheetLoader != null)
                if (_root == null)
                {
                    GameManager.Init();
                    _sheetLoader.DataLoad();
                }
        }

        // 데이터 매니저 제거 시 호출
        public void ClearSheets()
        {
            if(_sheetLoader != null)
                _sheetLoader.ClearDatas();
        }

        public void GetSheetLoaderPrefab()
        {
            GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.SheetLoader,
                loadPrefab =>
                {
                     _sheetLoader = loadPrefab.GetComponent<SheetLoader>();
                    if (_sheetLoader == null)
                    {
                        DebugTool.Warning($"{loadPrefab.name}에 시트 로더 컴포넌트가 없습니다.", DebugType.Missing);
                        return;
                    }
                    
                    loadPrefab.transform.SetParent(_root.transform, false);
                    
                    DebugTool.Log($"{loadPrefab.name} : 시트 로더 로드 완료", DebugType.Data);
                },
                failedKey =>
                {
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
            
            DebugTool.Log("데이터 매니저 제거 완료", DebugType.Game);
        }
    }
}
