using Core.Managers;
using Data.ScriptableObjects.NyangQuariumSO;
using UnityEngine;
using Util;

namespace UI.NyangQuarium.Quest
{
    // Addressable 등록 전: Inspector에 NyangQuariumQuestPopUp 프리팹을 연결해 테스트
    // 퀘스트 클릭 -> 알람 뷰 -> 여기서 팝업을 열게 됨
    public sealed class NyangQuariumQuestPopupOpener : MonoBehaviour
    {
        public static NyangQuariumQuestPopupOpener Instance { get; private set; }

        [Header("Popup Prefab (Addressable 전 테스트용)")]
        [SerializeField] private NyangQuariumQuestPopUp _popupPrefab;

        private NyangQuariumQuestPopUp _activePopup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
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
                "[NyangQuariumQuestPopupOpener] 씬에 NyangQuariumQuestPopupOpener가 없습니다. " +
                "@NyangQuariumData 등에 컴포넌트를 추가하고 NyangQuariumQuestPopUp 프리팹을 연결",
                DebugType.UI);
        }

        public void OpenPopup(NyangQuariumQuestData quest)
        {
            if (quest == null)
                return;

            if (_popupPrefab == null)
            {
                DebugTool.Warning(
                    "[NyangQuariumQuestPopupOpener] Popup Prefab이 연결되지 않았습니다. " +
                    "NyangQuariumQuestPopUp 프리팹 을 Inspector에 연결하세요.",
                    DebugType.UI,
                    this);
                return;
            }

            if (_activePopup != null)
            {
                _activePopup.BindQuest(quest);
                _activePopup.gameObject.SetActive(true);
                _activePopup.PlayOpenAnimation();
                return;
            }

            Transform parent = FindUiRoot();
            NyangQuariumQuestPopUp popup = Instantiate(_popupPrefab, parent);
            popup.ConfigureDirectLifecycle(true);
            popup.gameObject.SetActive(true);
            popup.Init();
            popup.BindQuest(quest);
            popup.PlayOpenAnimation();

            _activePopup = popup;
        }

        private static Transform FindUiRoot()
        {
            GameObject root = GameObject.Find("@UI_Root");
            return root != null ? root.transform : null;
        }
    }
}
