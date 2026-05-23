using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class UiManager: ISubManager
    {
        private int _order = 10;
        private Stack<UIPopup> _popupStack = new();
        private UIScene _uiScene;
        
        private GameObject _root;
        public GameObject Root => _root;
        
        public void Init()
        {
            _root = GameObject.Find("@UI_Root");

            if (_root == null)
            {
                _root = new GameObject { name = "@UI_Root" };
                Object.DontDestroyOnLoad(_root);
            }
            
            DebugTool.Log("UI 매니저 초기화 완료", DebugType.Game);
        }

        public void SetCanvas(GameObject go, bool sort = true)
        {
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                canvas = go.AddComponent<Canvas>();
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = sort;

            if (sort)
            {
                canvas.sortingOrder = _order;
                _order++;
            }
            else
                canvas.sortingOrder = 0;
        }

        public bool ShowSceneUI<T>(string name = null, Action<T> onLoaded = null) where T : UIScene
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            return GameManager.Addressable.TryLoadPrefab(name,
                uiPrefab =>
                {
                    T uiScene = uiPrefab.GetComponent<T>();

                    if (uiScene == null)
                        uiScene = uiPrefab.AddComponent<T>();

                    _uiScene = uiScene;

                    uiPrefab.transform.SetParent(_root.transform, false);

                    SetCanvas(uiPrefab, sort: false);

                    uiScene.Init();

                    DebugTool.Log($"{typeof(T).Name} Scene UI 로드 완료", DebugType.UI);

                    onLoaded?.Invoke(uiScene);
                },
                failedKey =>
                {
                    DebugTool.Warning($"{failedKey} : Scene UI 로드 실패", DebugType.Missing);
                });
        }

        public void ClosePopupUI(UIPopup popup)
        {
            if (_popupStack.Count == 0)
                return;

            if (_popupStack.Peek() != popup)
            {
                //GameManager.Addressable.TryReleasePrefab();
                _order--;
                return;
            }
            
            ClosePopupUI();
        }

        public void ClosePopupUI()
        {
            if (_popupStack.Count == 0)
                return;

            UIPopup popup = _popupStack.Pop();
            //GameManager.Addressable.TryReleasePrefab(popup.gameObject);
            _order--;
            DebugTool.Log($"{popup.name} : 팝업 창 닫힘 / 순서 : {_order}", DebugType.UI);
        }
    

        public void Clear()
        {
            if (_root == null) return;
            
            Object.DestroyImmediate(_root);
            _root = null;
            
            DebugTool.Log("UI 매니저 제거 완료", DebugType.Game);
        }
    }
}