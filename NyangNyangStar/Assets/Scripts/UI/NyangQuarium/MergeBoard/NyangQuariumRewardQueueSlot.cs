using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    public sealed class NyangQuariumRewardQueueSlot : MonoBehaviour, IPointerClickHandler
    {
        private NyangQuariumRewardQueue _rewardQueue;
        private Image _itemImage;
        private Button _button;
        private UI.UISpriteController _spriteController;
        private Color _emptyColor;
        private bool _isTopSlot;
        private bool _hasItem;

        public void Init(NyangQuariumRewardQueue rewardQueue, bool isTopSlot)
        {
            _rewardQueue = rewardQueue;
            _isTopSlot = isTopSlot;
            BindImage();
            BindButton();
            SetItem(NyangQuariumBoardItem.Empty);
        }

        public void SetItem(NyangQuariumBoardItem item)
        {
            BindImage();

            _hasItem = item != null && item.HasItem;

            if (_itemImage == null)
                return;

            _spriteController?.ClearSprite();

            if (!_hasItem)
            {
                _itemImage.sprite = null;
                _itemImage.color = _emptyColor;
                _itemImage.enabled = false;
                _itemImage.gameObject.SetActive(false);
                _itemImage.raycastTarget = _button != null;
                return;
            }

            ItemData itemData = item.ItemData;
            _itemImage.gameObject.SetActive(true);
            _itemImage.color = Color.white;
            _itemImage.enabled = true;
            _itemImage.raycastTarget = _button != null;

            if (itemData.ItemSprite != null)
            {
                _itemImage.sprite = itemData.ItemSprite;
                return;
            }

            _itemImage.sprite = null;

            if (NyangQuariumItemGenerator.TryGetGeneratorSpriteKey(itemData, out string addressableKey))
            {
                _spriteController ??= new UI.UISpriteController(_itemImage);
                _spriteController.ChangeSprite(addressableKey);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null)
                return;

            HandleClick();
        }

        private void HandleClick()
        {
            if (!_isTopSlot || !_hasItem || _rewardQueue == null)
                return;

            _rewardQueue.TryMoveTopItemToBoard();
        }

        private void BindImage()
        {
            if (_itemImage != null)
                return;

            _itemImage = FindButtonImage();

            if (_itemImage == null)
                _itemImage = FindChildImage();

            if (_itemImage == null)
                return;

            _emptyColor = _itemImage.color;
        }

        private Image FindButtonImage()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                Image image = button.GetComponent<Image>();
                if (image != null)
                    return image;
            }

            return null;
        }

        private Image FindChildImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.gameObject == gameObject)
                    continue;

                return image;
            }

            return null;
        }

        private void BindButton()
        {
            Button button = null;

            if (_itemImage != null)
                button = _itemImage.GetComponent<Button>();

            if (button == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0)
                    button = buttons[0];
            }

            if (_button == button)
                return;

            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);

            _button = button;

            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);

            _spriteController?.Dispose();
            _spriteController = null;
        }
    }
}
