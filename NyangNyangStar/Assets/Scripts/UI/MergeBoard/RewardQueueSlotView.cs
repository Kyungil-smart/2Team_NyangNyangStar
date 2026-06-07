using Data.ScriptableObjects.MergeBoard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class RewardQueueSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _itemSlotImage;

        private BoardRewardQueue _rewardQueue;
        private bool _isTopSlot;
        private bool _hasItem;

        public void Init(BoardRewardQueue rewardQueue, bool isTopSlot)
        {
            _rewardQueue = rewardQueue;
            _isTopSlot = isTopSlot;
        }

        public void SetItem(ItemData itemData)
        {
            _hasItem = itemData != null && itemData.HasItem;

            if (_itemSlotImage == null)
                return;

            _itemSlotImage.gameObject.SetActive(_hasItem);
            _itemSlotImage.enabled = _hasItem;
            _itemSlotImage.raycastTarget = false;
            _itemSlotImage.sprite = _hasItem ? itemData.ItemSprite : null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isTopSlot || !_hasItem)
                return;

            if (_rewardQueue == null)
                return;

            _rewardQueue.TryMoveTopItemToBoard();
        }
    }
}
