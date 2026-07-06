using System;
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
        private bool _isCleared;
        private int _sessionVersion;

        public event Action<float, string> OnDataLoadProgressChanged;

        public void Init()
        {
            _isCleared = false;
            CreateRoot();

            DebugTool.Log("데이터 매니저 초기화 완료", DebugType.Game);
        }

        public void LoadSheets()
        {
            if (_isCleared)
                return;

            CreateRoot();

            if (_sheetLoader == null)
            {
                _loadSheetsWhenReady = true;
                GetSheetLoaderPrefab();
                DebugTool.Log("SheetLoader 로드 대기 중 - 로드 완료 후 시트를 자동 로드합니다.", DebugType.Data);
                return;
            }

            _loadSheetsWhenReady = false;
            ReportDataLoadProgress(0f, "시트 로드 시작");
            _sheetLoader.DataLoad();
        }

        public void ClearSheets()
        {
            _loadSheetsWhenReady = false;

            if (_sheetLoader != null)
                _sheetLoader.ClearDatas();
        }

        private void CreateRoot()
        {
            if (_root != null)
                return;

            _root = new GameObject("@Data");
            Object.DontDestroyOnLoad(_root);
        }

        private void GetSheetLoaderPrefab()
        {
            if (_sheetLoader != null || _isLoadingSheetLoader)
                return;

            CreateRoot();

            _isLoadingSheetLoader = true;
            int requestVersion = _sessionVersion;

            GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.SheetLoader,
                loadPrefab =>
                {
                    _isLoadingSheetLoader = false;

                    if (_isCleared || requestVersion != _sessionVersion)
                    {
                        if (loadPrefab != null)
                            ReleaseSheetLoaderPrefab(loadPrefab);

                        return;
                    }

                    CreateRoot();

                    if (loadPrefab == null)
                    {
                        DebugTool.Warning($"{KeyContainer.Prefabs.SheetLoader} : 로드된 SheetLoader 인스턴스가 없습니다.", DebugType.Missing);
                        return;
                    }

                    if (_root == null)
                    {
                        ReleaseSheetLoaderPrefab(loadPrefab);
                        return;
                    }

                    _sheetLoader = loadPrefab.GetComponent<SheetLoader>();

                    if (_sheetLoader == null)
                    {
                        DebugTool.Warning($"{loadPrefab.name}에 시트 로더 컴포넌트가 없습니다.", DebugType.Missing);
                        ReleaseSheetLoaderPrefab(loadPrefab);
                        return;
                    }

                    _sheetLoader.OnSheetLoadProgressChanged -= HandleSheetLoadProgress;
                    _sheetLoader.OnSheetLoadProgressChanged += HandleSheetLoadProgress;

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

        private void HandleSheetLoadProgress(float progress, string message)
        {
            ReportDataLoadProgress(progress, message);
        }

        private void ReportDataLoadProgress(float progress, string message)
        {
            OnDataLoadProgressChanged?.Invoke(Mathf.Clamp01(progress), message);
        }

        private static void ReleaseSheetLoaderPrefab(GameObject prefab)
        {
            if (prefab == null)
                return;

            if (GameManager.Addressable != null)
                GameManager.Addressable.ReleasePrefabOrDestroy(KeyContainer.Prefabs.SheetLoader, prefab);
            else
                Object.Destroy(prefab);
        }

        public void Clear()
        {
            _isCleared = true;
            _sessionVersion++;

            _loadSheetsWhenReady = false;
            _isLoadingSheetLoader = false;

            if (_sheetLoader != null)
            {
                _sheetLoader.OnSheetLoadProgressChanged -= HandleSheetLoadProgress;
                _sheetLoader.ClearDatas();
                ReleaseSheetLoaderPrefab(_sheetLoader.gameObject);
                _sheetLoader = null;
            }

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            OnDataLoadProgressChanged = null;

            DebugTool.Log("데이터 매니저 제거 완료", DebugType.Game);
        }
    }
}
