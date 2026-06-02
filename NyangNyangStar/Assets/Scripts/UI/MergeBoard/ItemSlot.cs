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

        public ItemData ItemData { get; private set; }
        public bool HasItem => ItemData.HasItem;

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
            ItemData = itemData;

            _number.text = _slotIndex.ToString();
            _item.color = itemData.Color;

            if (_item != null)
                _item.gameObject.SetActive(ItemData.HasItem);
            
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

    public struct ItemData
    {
        public bool HasItem;
        public int SlotNumber;
        public string ItemName;
        public int ItemLevel;
        public Color Color;

        public static ItemData Empty => new ItemData(false, 0, null, 0, new Color(0,0,0, 1));

        public ItemData(int slotNumber, string itemName, int itemLevel, Color color)
        {
            HasItem = true;
            SlotNumber = slotNumber;
            ItemName = itemName;
            ItemLevel = itemLevel;
            Color = color;
        }

        private ItemData(bool hasItem, int slotNumber, string itemName, int itemLevel, Color color)
        {
            HasItem = hasItem;
            SlotNumber = slotNumber;
            ItemName = itemName;
            ItemLevel = itemLevel;
            Color = color;
        }
    }
}