using System;
using Core.Managers;
using Data.ScriptableObjects.NyangQuariumSO;
using UnityEngine;
using Util;

namespace UI.NyangQuarium.Quest
{
    // Addressable로 NyangQuariumQuestPopUp을 미리 올려 두고, 열 때 활성화합니다.
    public sealed class NyangQuariumQuestPopupOpener : MonoBehaviour
    {
        public static NyangQuariumQuestPopupOpener Instance { get; private set; }

        private NyangQuariumQuestPopUp _cachedPopup;
        private bool _isLoading;
        private Action _pendingOpenAction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            EnsurePreloaded();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Open(NyangQuariumQuestData quest)
        {
            if (quest == null)
            {
                DebugTool.Warning("[NyangQuariumQuestPopupOpener] 표시할 퀘스트가 없습니다.", DebugType.UI);
                return;
            }

            if (Instance != null)
            {
                Instance.OpenPopup(quest);
                return;
            }

            DebugTool.Warning(
                "[NyangQuariumQuestPopupOpener] 씬에 NyangQuariumQuestPopupOpener가 없습니다.",
                DebugType.UI);
        }

        public void EnsurePreloaded(Action onReady = null)
        {
            if (_cachedPopup != null)
            {
                onReady?.Invoke();
                return;
            }

            if (onReady != null)
                _pendingOpenAction += onReady;

            if (_isLoading)
                return;

            _isLoading = true;

            GameManager.UI.ShowPopupUI<NyangQuariumQuestPopUp>(
                KeyContainer.Prefabs.NyangQuariumQuestPopUp,
                popup =>
                {
                    _isLoading = false;
                    _cachedPopup = popup;
                    _cachedPopup.ConfigureDirectLifecycle(false);
                    _cachedPopup.HideImmediately();

                    Action readyAction = _pendingOpenAction;
                    _pendingOpenAction = null;
                    readyAction?.Invoke();
                },
                setActive: false,
                onFailed: failedKey =>
                {
                    _isLoading = false;
                    _pendingOpenAction = null;

                    DebugTool.Warning(
                        $"[NyangQuariumQuestPopupOpener] 팝업 로드 실패. Key:{failedKey}",
                        DebugType.UI,
                        this);
                });
        }

        public void OpenPopup(NyangQuariumQuestData quest)
        {
            if (quest == null)
                return;

            EnsurePreloaded(() => ShowPopup(quest));
        }

        private void ShowPopup(NyangQuariumQuestData quest)
        {
            if (_cachedPopup == null)
                return;

            _cachedPopup.BindQuest(quest);
            _cachedPopup.OpenWhenReady();
        }

        public static void RefreshOpenPopup()
        {
            if (Instance?._cachedPopup == null || !Instance._cachedPopup.gameObject.activeInHierarchy)
                return;

            Instance._cachedPopup.RefreshBoundQuestView();
        }
    }
}
