using Data.ScriptableObjects.MergeBoard;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class SpecialItemSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _itemImage;
        [SerializeField] private TMP_Text _countText;

        private SpecialItemBoardSystem _boardSystem;

        public int SlotNumber { get; private set; }
        public SpecialItemSlotData SlotData { get; private set; } = SpecialItemSlotData.Empty(0);
        public bool HasItem => SlotData != null && SlotData.HasItem;

        public void Init(SpecialItemBoardSystem boardSystem, int slotNumber)
        {
            _boardSystem = boardSystem;
            SlotNumber = slotNumber;
            SetSlotData(SpecialItemSlotData.Empty(slotNumber));
        }

        public void SetSlotData(SpecialItemSlotData slotData)
        {
            SlotData = slotData?.Clone() ?? SpecialItemSlotData.Empty(SlotNumber);

            bool hasItem = SlotData.HasItem;
            ItemData itemData = SlotData.ItemData;

            if (_itemImage != null)
            {
                _itemImage.gameObject.SetActive(hasItem);
                _itemImage.enabled = hasItem;
                _itemImage.sprite = hasItem ? itemData.ItemSprite : null;
                _itemImage.raycastTarget = false;
            }

            if (_countText != null)
            {
                _countText.gameObject.SetActive(hasItem);
                _countText.text = hasItem ? SlotData.Count.ToString() : string.Empty;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_boardSystem == null)
                return;

            if (!HasItem)
            {
                _boardSystem.ClearSelectedSlot();
                return;
            }

            _boardSystem.SelectSlot(this);
        }
    }
}
