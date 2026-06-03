using Data.ScriptableObjects.MergeBoard;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image _item;
        [SerializeField] private TMP_Text _number;

        private BoardSystem _boardSystem;
        private RectTransform _itemRect;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private CanvasGroup _canvasGroup;

        private int _slotIndex;
        private bool _isDragging;

        public int SlotNumber => _slotIndex;
        public ItemData ItemData { get; private set; } = ItemData.Empty;
        public bool HasItem => ItemData != null && ItemData.HasItem;

        public void Init(BoardSystem boardSystem, int slotIndex, ItemData itemData, int itemSize)
        {
            _boardSystem = boardSystem;
            _slotIndex = slotIndex;

            if (_item == null)
            {
                Debug.LogError($"{name} : _item이 연결되지 않았습니다.", this);
                return;
            }

            if (_number == null)
            {
                Debug.LogError($"{name} : _number가 연결되지 않았습니다.", this);
                return;
            }

            _canvas = GetComponentInParent<Canvas>();

            if (_canvas == null)
            {
                Debug.LogError($"{name} : 부모 Canvas를 찾을 수 없습니다.", this);
                return;
            }

            _canvasRect = _canvas.transform as RectTransform;

            _itemRect = _item.rectTransform;
            _itemRect.sizeDelta = new Vector2(itemSize, itemSize);

            _canvasGroup = _item.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = _item.gameObject.AddComponent<CanvasGroup>();

            SetItemData(itemData);
            ResetSiblingOrder();
        }

        public void SetItemData(ItemData itemData)
        {
            ItemData = itemData ?? ItemData.Empty;

            bool hasItem = ItemData.HasItem;

            if (_item != null)
            {
                _item.gameObject.SetActive(hasItem);
                _item.sprite = hasItem ? ItemData.ItemSprite : null;
            }

            if (_number != null)
            {
                _number.text = hasItem
                    ? $"#{ItemData.ItemNumber}"
                    : string.Empty;
            }

            ResetSiblingOrder();
        }

        public void ClearItem()
        {
            SetItemData(ItemData.Empty);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!HasItem)
                return;

            _isDragging = true;

            _canvasGroup.blocksRaycasts = false;
            _itemRect.SetParent(_canvas.transform, true);
            _itemRect.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                _itemRect.anchoredPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            _isDragging = false;

            _canvasGroup.blocksRaycasts = true;
            _itemRect.SetParent(transform, false);
            _itemRect.anchoredPosition = Vector2.zero;

            ResetSiblingOrder();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            ItemSlot fromSlot = eventData.pointerDrag.GetComponent<ItemSlot>();

            if (fromSlot == null)
                return;

            _boardSystem.MoveOrSwapItem(fromSlot, this);
        }

        private void ResetSiblingOrder()
        {
            if (_item != null)
                _item.transform.SetSiblingIndex(0);

            if (_number != null)
                _number.transform.SetAsLastSibling();
        }
    }
}
