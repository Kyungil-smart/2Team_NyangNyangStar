using UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class UiManager: ISubManager
    {
        private const int PopupStartorder = 10;
        private int _order = PopupStartorder;
        
        private Stack<UIPopup> _popupStack = new();
        
        private UIScene _uiScene;
        
        private GameObject _root;
        
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

                    foreach (var popupcomponent in _popupStack)
                    {
                        if (popup.gameObject == popupcomponent.gameObject)
                        {
                            DebugTool.Log($"{popup.gameObject.name} 이미 같은 오브젝트가 있습니다.", DebugType.UI);
                            return;
                        }
                    }
                    
                    _popupStack.Push(popup);

                    uiPrefab.transform.SetParent(_root.transform, false);
                    
                    SetCanvas(uiPrefab);
                    popup.Init();
                    popup.SetAddressableKey(name);

                    DebugTool.Log($"{popup.name} : 팝업 창 열림 / 현재 팝업 수 : {_popupStack.Count}", DebugType.UI);

                    onLoaded?.Invoke(popup);
                },
                failedKey =>
                {
                    DebugTool.Warning($"{failedKey} : 팝업 UI 로드 실패", DebugType.Missing);
                });
        }

        public bool ClosePopupUI()
        {
            if (_popupStack.Count == 0)
                return false;
            
            return ClosePopupUI(_popupStack.Peek());
        }

        public bool ClosePopupUI(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (_popupStack.Count == 0)
                return false;
            
            List<UIPopup> popups = new(_popupStack);

            UIPopup targetPopup = null;

            foreach (UIPopup popup in popups)
            {
                if (popup == null)
                    continue;

                if (popup.AddressableKey == key)
                {
                    targetPopup = popup;
                    break;
                }
            }
            if(targetPopup == null)
            {
                DebugTool.Warning($"{key} : 팝업 스택에 존재하지 않습니다.", DebugType.UI);
                return false;
            }

            return ClosePopupUI(targetPopup);
        }

        public bool ClosePopupUI(UIPopup popup)
        {
            if (popup == null)
                return false;
            
            if (_popupStack.Count == 0)
                return false;

            List<UIPopup> popups = _popupStack.ToList();
            
            int index = popups.FindIndex(target => ReferenceEquals(target, popup));

            if (index < 0)
            {
                DebugTool.Warning($"{popup.name} : 팝업 스택에 존재하지 않습니다.", DebugType.UI);
                return false;
            }

            string key = popup.AddressableKey;
            string popupName = popup.name;
            GameObject popupObject = popup.gameObject;

            if (!GameManager.Addressable.TryReleasePrefab(key, popupObject))
            {
                DebugTool.Warning($"{key} : 해당 UI를 닫을 수 없습니다.", DebugType.UI);
                return false;
            }
            
            popups.RemoveAt(index);
            _popupStack.Clear();

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                if(popups[i] != null)
                    _popupStack.Push(popups[i]);
            }
            
            RefreshPopupSortingOrder();
            DebugTool.Log($"{popupName} : 팝업 창 닫힘 / 현재 팝업 수 : {_popupStack.Count}", DebugType.UI);
            return true;
        }

        private void RefreshPopupSortingOrder()
        {
            UIPopup[] popups = _popupStack.ToArray();

            for (int i = popups.Length - 1; i >= 0; i--)
            {
                UIPopup popup = popups[i];

                if (popup == null)
                    continue;
                
                Canvas canvas = popup.GetComponentInParent<Canvas>();

                if (canvas == null)
                    continue;

                int orderIndex = popups.Length - 1 - i;
                canvas.sortingOrder = PopupStartorder + orderIndex;
            }
            
            _order = PopupStartorder + _popupStack.Count;
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