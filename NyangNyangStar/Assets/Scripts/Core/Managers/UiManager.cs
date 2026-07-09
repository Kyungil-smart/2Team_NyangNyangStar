using UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class UiManager : ISubManager
    {
        private const int PopupStartorder = 10;
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private int _order = PopupStartorder;

        private Stack<UIPopup> _popupStack = new();
        private UIScene _uiScene;
        private string _uiSceneKey;
        private GameObject _root;

        private bool _isCleared;
        private int _sessionVersion;

        public void Init()
        {
            _isCleared = false;
            EnsureRoot();

            DebugTool.Log("UI 매니저 초기화 완료", DebugType.Game);
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            _root = GameObject.Find("@UI_Root");

            if (_root == null)
                _root = new GameObject { name = "@UI_Root" };
        }

        public void SetCanvas(GameObject go, bool sort = true)
        {
            Canvas[] canvases = go.GetComponentsInChildren<Canvas>(true);

            if (canvases.Length == 0)
            {
                Canvas canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvases = new[] { canvas };
            }

            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                canvas.overrideSorting = sort;
                canvas.sortingOrder = sort ? _order++ : 0;
            }

            ConfigureCanvasScalers(go);
        }

        private static void ConfigureCanvasScalers(GameObject go)
        {
            if (go == null)
                return;

            Canvas[] canvases = go.GetComponentsInChildren<Canvas>(true);

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                    continue;

                CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
                if (canvasScaler == null)
                    canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();

                ConfigureCanvasScaler(canvasScaler);
            }
        }

        private static void ConfigureCanvasScaler(CanvasScaler canvasScaler)
        {
            if (canvasScaler == null)
                return;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = ReferenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasScaler.matchWidthOrHeight = 0f;
        }

        public void ShowSceneUI<T>(string name = null, Action<T> onLoaded = null) where T : UIScene
        {
            EnsureRoot();

            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            int requestVersion = _sessionVersion;

            GameManager.Addressable.LoadPrefab(name,
                uiPrefab =>
                {
                    if (_isCleared || requestVersion != _sessionVersion)
                    {
                        if (uiPrefab != null)
                            ReleaseLoadedPrefab(name, uiPrefab);

                        return;
                    }

                    EnsureRoot();

                    if (uiPrefab == null || _root == null)
                        return;

                    T uiScene = uiPrefab.GetComponent<T>();

                    if (uiScene == null)
                        uiScene = uiPrefab.AddComponent<T>();

                    _uiScene = uiScene;
                    _uiSceneKey = name;

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

        public void ShowPopupUI<T>(
            string name = null,
            Action<T> onLoaded = null,
            bool setActive = true,
            Action<string> onFailed = null,
            UIPopupCloseMode closeMode = UIPopupCloseMode.Auto) where T : UIPopup
        {
            EnsureRoot();

            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            int requestVersion = _sessionVersion;

            GameManager.Addressable.LoadPrefab(name,
                uiPrefab =>
                {
                    if (_isCleared || requestVersion != _sessionVersion)
                    {
                        if (uiPrefab != null)
                            ReleaseLoadedPrefab(name, uiPrefab);

                        return;
                    }

                    EnsureRoot();

                    if (uiPrefab == null || _root == null)
                        return;

                    T popup = uiPrefab.GetComponent<T>();

                    if (popup == null)
                        popup = uiPrefab.AddComponent<T>();

                    UIPopupCloseMode resolvedCloseMode = ResolveCloseMode(closeMode, setActive);
                    popup.SetAddressableKey(name);
                    popup.SetCloseMode(resolvedCloseMode);

                    if (resolvedCloseMode == UIPopupCloseMode.Release)
                        _popupStack.Push(popup);

                    uiPrefab.transform.SetParent(_root.transform, false);

                    SetCanvas(uiPrefab);

                    popup.Init();

                    popup.gameObject.SetActive(setActive);

                    DebugTool.Log($"{popup.name} : 팝업 창 열림 / 현재 팝업 수 : {_popupStack.Count}", DebugType.UI);

                    onLoaded?.Invoke(popup);
                },
                failedKey =>
                {
                    DebugTool.Warning($"{failedKey} : 팝업 UI 로드 실패", DebugType.Missing);
                    onFailed?.Invoke(failedKey);
                });
        }

        public bool ClosePopupUI()
        {
            PrunePopupStack();

            if (_popupStack.Count == 0)
                return false;

            return ClosePopupUI(_popupStack.Peek());
        }

        public bool ClosePopupUI(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            PrunePopupStack();

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

            if (targetPopup == null)
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

            PrunePopupStack();

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

            ReleaseLoadedPrefab(key, popupObject);

            popups.RemoveAt(index);
            _popupStack.Clear();

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                if (popups[i] != null)
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

        private static UIPopupCloseMode ResolveCloseMode(UIPopupCloseMode closeMode, bool setActive)
        {
            if (closeMode != UIPopupCloseMode.Auto)
                return closeMode;

            return setActive ? UIPopupCloseMode.Release : UIPopupCloseMode.Hide;
        }

        private void PrunePopupStack()
        {
            if (_popupStack.Count == 0)
                return;

            List<UIPopup> popups = _popupStack.ToList();
            popups.RemoveAll(popup => popup == null);

            _popupStack.Clear();

            for (int i = popups.Count - 1; i >= 0; i--)
                _popupStack.Push(popups[i]);
        }

        private static void ReleaseLoadedPrefab(string key, GameObject prefab)
        {
            if (prefab == null)
                return;

            if (GameManager.Addressable != null)
                GameManager.Addressable.ReleasePrefabOrDestroy(key, prefab);
            else
                Object.Destroy(prefab);
        }

        private void ReleaseLoadedUiInstances()
        {
            if (_root == null)
                return;

            HashSet<GameObject> releasedObjects = new();

            UIPopup[] popups = _root.GetComponentsInChildren<UIPopup>(true);
            foreach (UIPopup popup in popups)
            {
                if (popup == null || popup.gameObject == null || !releasedObjects.Add(popup.gameObject))
                    continue;

                ReleaseLoadedPrefab(popup.AddressableKey, popup.gameObject);
            }

            if (_uiScene != null && _uiScene.gameObject != null && releasedObjects.Add(_uiScene.gameObject))
                ReleaseLoadedPrefab(_uiSceneKey, _uiScene.gameObject);
        }

        public void Clear()
        {
            _isCleared = true;
            _sessionVersion++;

            ReleaseLoadedUiInstances();
            _popupStack.Clear();
            _uiScene = null;
            _uiSceneKey = null;

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _order = PopupStartorder;

            DebugTool.Log("UI 매니저 제거 완료", DebugType.Game);
        }
    }
}
