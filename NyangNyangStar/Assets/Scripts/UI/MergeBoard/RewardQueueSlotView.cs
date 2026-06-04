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

        public void Init(BoardRewardQueue rewardQueue, bool isTopSlot)
        {
            _rewardQueue = rewardQueue;
            _isTopSlot = isTopSlot;
        }

        public void SetItem(ItemData itemData)
        {
            bool hasItem = itemData != null && itemData.HasItem;

            gameObject.SetActive(hasItem);

            if (_itemSlotImage == null)
                return;

            _itemSlotImage.gameObject.SetActive(hasItem);
            _itemSlotImage.enabled = hasItem;
            _itemSlotImage.raycastTarget = hasItem;
            _itemSlotImage.sprite = hasItem ? itemData.ItemSprite : null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isTopSlot)
                return;

            DebugTool.Log("보상 큐 최상단 슬롯 클릭", DebugType.Board, this);

            _rewardQueue.TryMoveTopItemToBoard();
        }
    }
}
