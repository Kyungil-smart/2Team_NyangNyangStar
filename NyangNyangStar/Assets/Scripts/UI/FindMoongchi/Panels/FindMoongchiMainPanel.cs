using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiMainPanel : MonoBehaviour
    {
        [SerializeField] private Button _gameButton;
        [SerializeField] private Button _missionButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _closeButton;

        public event Action OnGameButtonClicked;
        public event Action OnMissionButtonClicked;
        public event Action OnShopButtonClicked;
        public event Action OnCloseButtonClicked;

        public void Init()
        {
            BindButton(_gameButton, HandleGameButtonClicked);
            BindButton(_missionButton, HandleMissionButtonClicked);
            BindButton(_shopButton, HandleShopButtonClicked);
            BindButton(_closeButton, HandleCloseButtonClicked);
        }

        private void HandleGameButtonClicked()
        {
            DebugTool.Log("[FindMoongchiMainPanel] 게임 버튼 클릭", DebugType.FindMoongchi, this);
            OnGameButtonClicked?.Invoke();
        }

        private void HandleMissionButtonClicked()
        {
            DebugTool.Log("[FindMoongchiMainPanel] 미션 버튼 클릭", DebugType.FindMoongchi, this);
            OnMissionButtonClicked?.Invoke();
        }

        private void HandleShopButtonClicked()
        {
            DebugTool.Log("[FindMoongchiMainPanel] 상점 버튼 클릭", DebugType.FindMoongchi, this);
            OnShopButtonClicked?.Invoke();
        }
        private void HandleCloseButtonClicked()
        {
            DebugTool.Log("[FindMoongchiMainPanel] 닫기 버튼 클릭", DebugType.FindMoongchi, this);
            OnCloseButtonClicked?.Invoke();
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                DebugTool.Warning("[FindMoongchiMainPanel] 버튼 연결이 비어있습니다.", DebugType.FindMoongchi);
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
        }

        private void OnDestroy()
        {
            UnbindButton(_gameButton, HandleGameButtonClicked);
            UnbindButton(_missionButton, HandleMissionButtonClicked);
            UnbindButton(_shopButton, HandleShopButtonClicked);
            UnbindButton(_closeButton, HandleCloseButtonClicked);
        }
    }
}
