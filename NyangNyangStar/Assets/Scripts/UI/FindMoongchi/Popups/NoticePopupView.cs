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
            if (_noticeText != null)
                _noticeText.text = message;

            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(Close);
        }
    }
}
