using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class NoticePopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _noticeText;
        [SerializeField] private Button _confirmButton;

        public void Init()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(Close);
                _confirmButton.onClick.AddListener(Close);
            }

            gameObject.SetActive(false);
        }

        public void Open(string message)
        {
            DebugTool.Log($"[NoticePopupView] 열기: {message}", DebugType.FindMoongchi, this);

            if (_noticeText != null)
                _noticeText.text = message;

            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (gameObject.activeSelf)
                DebugTool.Log("[NoticePopupView] 닫기", DebugType.FindMoongchi, this);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(Close);
        }
    }
}
