using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumMergeBoard 상단 Back / Home 버튼 연결
    public sealed class NyangQuariumMergeBoardNavigation : MonoBehaviour
    {
        private const string BackButtonName = "BackButton";
        private const string HomeButtonName = "HomeButton";

        private Button _backButton;
        private Button _homeButton;
        private bool _initialized;

        public void Init()
        {
            if (_initialized)
                return;

            _initialized = true;
            BindNavigationButtons();
        }

        private void BindNavigationButtons()
        {
            _backButton = FindButton(BackButtonName);
            _homeButton = FindButton(HomeButtonName);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
                _backButton.onClick.AddListener(OnBackClicked);
            }
            else
            {
                DebugTool.Warning("[NyangQuariumMergeBoardNavigation] BackButton을 찾지 못했습니다.", DebugType.UI, this);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeClicked);
                _homeButton.onClick.AddListener(OnHomeClicked);
            }
            else
            {
                DebugTool.Warning("[NyangQuariumMergeBoardNavigation] HomeButton을 찾지 못했습니다.", DebugType.UI, this);
            }
        }

        private Button FindButton(string buttonName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];

                if (child == null || !string.Equals(child.name, buttonName, System.StringComparison.Ordinal))
                    continue;

                if (!child.gameObject.activeInHierarchy)
                    continue;

                Button button = child.GetComponent<Button>();

                if (button != null)
                    return button;
            }

            return null;
        }

        private static void OnBackClicked()
        {
            if (NyangquariumMainUIManager.Active != null)
            {
                NyangquariumMainUIManager.Active.ReturnToMain();
                return;
            }

            DebugTool.Warning("[NyangQuariumMergeBoardNavigation] NyangquariumMainUIManager가 없어 뒤로 갈 수 없습니다.", DebugType.UI);
        }

        private static void OnHomeClicked()
        {
            if (NyangquariumMainUIManager.Active != null)
            {
                NyangquariumMainUIManager.Active.CloseMain();
                return;
            }

            DebugTool.Warning("[NyangQuariumMergeBoardNavigation] NyangquariumMainUIManager가 없어 메인으로 갈 수 없습니다.", DebugType.UI);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(OnBackClicked);

            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(OnHomeClicked);
        }
    }
}
