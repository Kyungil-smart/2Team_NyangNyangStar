using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiToolSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _countText;

        [Tooltip("드래그 중 선택 표시용입니다. 기본 테두리라면 연결하지 마세요.")]
        [SerializeField] private GameObject _selectedOutLine;

        [Header("Color")]
        [SerializeField] private Color _enabledColor = Color.white;
        [SerializeField] private Color _disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        public int ToolItemId { get; private set; }
        public int Count { get; private set; }
        public bool IsUsable { get; private set; }

        public event Action<FindMoongchiToolSlotView, PointerEventData> OnBeginDragTool;
        public event Action<FindMoongchiToolSlotView, PointerEventData> OnDragTool;
        public event Action<FindMoongchiToolSlotView, PointerEventData> OnEndDragTool;

        public void SetData(FindMoongchiToolViewData data)
        {
            if (data == null)
            {
                ToolItemId = 0;
                Count = 0;
                IsUsable = false;

                SetText(_countText, "x0");
                SetIcon(null, false);
                SetSelected(false);
                return;
            }

            ToolItemId = data.ToolItemId;
            Count = Mathf.Max(0, data.Count);
            IsUsable = data.IsUsable && Count > 0;

            SetText(_countText, $"x{Count}");
            SetIcon(data.Icon, IsUsable);
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectedOutLine != null)
                _selectedOutLine.SetActive(isSelected);
        }

        private void SetIcon(Sprite sprite, bool isUsable)
        {
            if (_iconImage == null)
                return;

            _iconImage.sprite = sprite;
            _iconImage.color = isUsable ? _enabledColor : _disabledColor;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsUsable)
            {
                DebugTool.Log($"[FindMoongchiToolSlotView] 사용 불가 도구 드래그 차단: ToolId={ToolItemId}, Count={Count}", DebugType.FindMoongchi, this);
                return;
            }

            SetSelected(true);
            OnBeginDragTool?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsUsable)
                return;

            OnDragTool?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsUsable)
                return;

            SetSelected(false);
            OnEndDragTool?.Invoke(this, eventData);
        }
    }
}