using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class NoticePopupView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _noticeText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmButtonText;

        [Header("Text")]
        [SerializeField] private string _defaultConfirmButtonText = "확인";

        private Action _onConfirmClicked;

        public void Init()
        {
            if (_confirmButtonText == null && _confirmButton != null)
                _confirmButtonText = _confirmButton.GetComponentInChildren<TMP_Text>(true);

            if (string.IsNullOrWhiteSpace(_defaultConfirmButtonText))
                _defaultConfirmButtonText = _confirmButtonText != null && !string.IsNullOrWhiteSpace(_confirmButtonText.text)
                    ? _confirmButtonText.text
                    : "확인";

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
                _confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
            }

            gameObject.SetActive(false);
        }

        public void SetDefaultConfirmButtonText(string buttonText)
        {
            if (string.IsNullOrWhiteSpace(buttonText))
                return;

            _defaultConfirmButtonText = buttonText;
        }

        public void Open(string message)
        {
            Open(message, null, null);
        }

        public void Open(string message, Action onConfirmClicked)
        {
            Open(message, null, onConfirmClicked);
        }

        public void Open(string message, string confirmButtonText, Action onConfirmClicked)
        {
            DebugTool.Log($"[NoticePopupView] 열기: {message}", DebugType.FindMoongchi, this);

            _onConfirmClicked = onConfirmClicked;

            if (_noticeText != null)
                _noticeText.text = message;

            if (_confirmButtonText != null)
                _confirmButtonText.text = string.IsNullOrWhiteSpace(confirmButtonText)
                    ? _defaultConfirmButtonText
                    : confirmButtonText;

            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (gameObject.activeSelf)
                DebugTool.Log("[NoticePopupView] 닫기", DebugType.FindMoongchi, this);

            gameObject.SetActive(false);
        }

        private void HandleConfirmButtonClicked()
        {
            Action callback = _onConfirmClicked;
            _onConfirmClicked = null;

            Close();
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
        }
    }
}
