using Data.ScriptableObjects.MergeBoard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Raycast")]
        [SerializeField] private Image _slotRaycastImage;

        [Header("Item")]
        [SerializeField] private Image _item;

        private BoardSystem _boardSystem;
        private RectTransform _itemRect;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private CanvasGroup _canvasGroup;

        private int _slotIndex;
        private bool _isDragging;
        private bool _ignoreClickOnce;

        public int SlotNumber => _slotIndex;
        public RectTransform RectTransform => transform as RectTransform;
        public ItemData ItemData { get; private set; } = ItemData.Empty;
        public bool HasItem => ItemData != null && ItemData.HasItem;

        public void Init(BoardSystem boardSystem, int slotIndex, ItemData itemData, int itemSize)
        {
            _boardSystem = boardSystem;
            _slotIndex = slotIndex;

            SetupRaycastImage();

            if (_item == null)
            {
                Debug.LogError($"{name} : _item이 연결되지 않았습니다.", this);
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

            _item.raycastTarget = false;
            SetItemData(itemData);
        }

        private void SetupRaycastImage()
        {
            if (_slotRaycastImage == null)
                _slotRaycastImage = GetComponent<Image>();

            if (_slotRaycastImage == null)
            {
                _slotRaycastImage = gameObject.AddComponent<Image>();
                _slotRaycastImage.color = new Color(1f, 1f, 1f, 0f);
            }

            _slotRaycastImage.raycastTarget = true;
        }

        public void SetItemData(ItemData itemData)
        {
            ItemData = itemData ?? ItemData.Empty;

            bool hasItem = ItemData.HasItem;

            if (_item == null)
                return;

            _item.gameObject.SetActive(hasItem);
            _item.enabled = hasItem;
            _item.sprite = hasItem ? ItemData.ItemSprite : null;
            _item.raycastTarget = false;
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
            _ignoreClickOnce = true;

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

            if (_boardSystem != null)
                _boardSystem.HandleDragEnd(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging)
                return;

            if (_ignoreClickOnce)
            {
                _ignoreClickOnce = false;
                return;
            }

            if (_boardSystem == null)
                return;

            if (!HasItem)
                return;

            _boardSystem.SelectSlot(this);
        }
    }
}
