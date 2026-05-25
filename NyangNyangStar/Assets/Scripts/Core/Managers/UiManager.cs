using UI;
using Services.AddressableKey;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
            
            CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
            if(canvasScaler == null)
                canvasScaler = go.AddComponent<CanvasScaler>();
            
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.matchWidthOrHeight = 0.5f;

            if (sort)
            {
                canvas.sortingOrder = _order;
                _order++;
            }
            else
                canvas.sortingOrder = 0;
        }

        public void ShowSceneUI<T>(string name = null, Action<T> onLoaded = null) where T : UIScene
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            GameManager.Addressable.LoadPrefab(name,
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

        public void ShowPopupUI<T>(string name = null, Action<T> onLoaded = null) where T : UIPopup
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            GameManager.Addressable.LoadPrefab(name,
                uiPrefab =>
                {
                    T popup = uiPrefab.GetComponent<T>();

                    if (popup == null)
                        popup = uiPrefab.AddComponent<T>();

                    _popupStack.Push(popup);

                    uiPrefab.transform.SetParent(_root.transform, false);
                    
                    SetCanvas(uiPrefab);
                    popup.Init();
                    popup.SetAddressableKey(name);

                    DebugTool.Log($"{popup.name} : 팝업 UI 생성", DebugType.UI);

                    onLoaded?.Invoke(popup);
                },
                failedKey =>
                {
                    DebugTool.Warning($"{failedKey} : 팝업 UI 로드 실패", DebugType.Missing);
                });
        }

        public void ClosePopupUI(UIPopup popup)
        {
            if (_popupStack.Count == 0)
                return;

            if (_popupStack.Peek() != popup)
            {
                DebugTool.Warning("최상단 팝업이 아니므로 닫을 수 업습니다.", DebugType.UI);
                return;
            }
            
            ClosePopupUI();
        }

        public void ClosePopupUI()
        {
            if (_popupStack.Count == 0)
                return;

            UIPopup popup = _popupStack.Peek();
            string key = popup.AddressableKey;

            if (!GameManager.Addressable.TryReleasePrefab(key, popup.gameObject))
            {
                DebugTool.Warning($"{key} : 해당 UI를 닫을 수 없습니다.", DebugType.UI);
                return;
            }
            
            _popupStack.Pop();
            
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