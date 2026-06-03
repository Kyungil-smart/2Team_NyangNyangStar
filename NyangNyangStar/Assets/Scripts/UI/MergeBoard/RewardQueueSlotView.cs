using Data.ScriptableObjects.MergeBoard;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class RewardQueueSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _itemSlotImage;
        [SerializeField] private TMP_Text _itemText;

        private BoardRewardQueue _rewardQueue;
        private bool _isTopSlot;

        public void Init(BoardRewardQueue rewardQueue, bool isTopSlot)
        {
            _rewardQueue = rewardQueue;
            _isTopSlot = isTopSlot;
        }

        public void SetItem(ItemData itemData)
        {
            bool hasItem = itemData != null && itemData.HasItem;

            gameObject.SetActive(hasItem);

            if (!hasItem)
                return;

            if (_itemSlotImage != null)
            {
                _itemSlotImage.sprite = itemData.ItemSprite;
                _itemSlotImage.enabled = itemData.ItemSprite != null;
            }

            if (_itemText != null)
            {
                _itemText.text = itemData.Amount > 1
                    ? itemData.Amount.ToString()
                    : string.Empty;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isTopSlot)
                return;

            _rewardQueue.TryMoveTopItemToBoard();
        }
    }
}