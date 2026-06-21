using System;
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

        private Action _onClosed;

        public void Init()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(Close);
                _confirmButton.onClick.AddListener(Close);
            }

            gameObject.SetActive(false);
        }

        public void Open(string message, int errorCode = 0, Action onClosed = null)
        {
            DebugTool.Warning($"[ErrorPopupView] 열기: {message}, Code={errorCode}", DebugType.FindMoongchi, this);

            _onClosed = onClosed;

            if (_noticeText != null)
                _noticeText.text = message;

            if (_errorCodeText != null)
                _errorCodeText.text = errorCode > 0 ? $"에러 코드: {errorCode}" : string.Empty;

            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (gameObject.activeSelf)
                DebugTool.Log("[ErrorPopupView] 닫기", DebugType.FindMoongchi, this);

            gameObject.SetActive(false);

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(Close);
        }
    }
}
