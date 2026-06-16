using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class ErrorPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _noticeText;
        [SerializeField] private TMP_Text _errorCodeText;
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

        public void Open(string message, int errorCode = 0)
        {
            if (_noticeText != null)
                _noticeText.text = message;

            if (_errorCodeText != null)
                _errorCodeText.text = errorCode > 0 ? $"에러 코드: {errorCode}" : string.Empty;

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
