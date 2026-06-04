using Data.ScriptableObjects.MergeBoard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class SpecialItemSlotView : MonoBehaviour
    {
        [SerializeField] private Image _itemImage;
        [SerializeField] private TMP_Text _itemText;

        public int SlotNumber { get; private set; }
        public SpecialItemSlotData SlotData { get; private set; } = SpecialItemSlotData.Empty(0);

        public void Init(int slotNumber)
        {
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
            }

            if (_itemText != null)
            {
                _itemText.text = hasItem
                    ? $"#{itemData.ItemID}\nx{SlotData.Count}"
                    : string.Empty;
            }
        }
    }
}
