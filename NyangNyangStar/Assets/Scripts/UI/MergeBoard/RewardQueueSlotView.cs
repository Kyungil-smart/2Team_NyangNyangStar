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
            {
                if (_itemSlotImage != null)
                {
                    _itemSlotImage.sprite = null;
                    _itemSlotImage.enabled = false;
                }

                if (_itemText != null)
                    _itemText.text = string.Empty;

                return;
            }

            if (_itemSlotImage != null)
            {
                _itemSlotImage.enabled = true;
                _itemSlotImage.raycastTarget = true;
                _itemSlotImage.sprite = itemData.ItemSprite;
            }

            if (_itemText != null)
            {
                _itemText.text = itemData.Amount > 1
                    ? $"#{itemData.ItemID}\nx{itemData.Amount}"
                    : $"#{itemData.ItemID}";
            }
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