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
    // NyangQuariumItemSlot -> 슬롯 1칸, 클릭하면 보드에 선택 전달
    public sealed class NyangQuariumItemSlot : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color BaseColor = new(1f, 1f, 1f, 0.25f);
        private static readonly Color SelectedColor = new(1f, 0.9f, 0.6f, 0.65f);

        private NyangQuariumItemBoard _board;
        private Image _backgroundImage;
        private Image _itemImage;
        private UI.UISpriteController _spriteController;

        public NyangQuariumBoardItem Item { get; private set; } = NyangQuariumBoardItem.Empty;
        public bool HasItem => Item != null && Item.HasItem;

        public void Init(NyangQuariumItemBoard board, Image backgroundImage, Image itemImage)
        {
            _board = board;
            _backgroundImage = backgroundImage;
            _itemImage = itemImage;

            if (_itemImage != null)
                _spriteController = new UI.UISpriteController(_itemImage);

            SetItem(NyangQuariumBoardItem.Empty);
            SetSelected(false);
        }

        // AddressableKey로 로드
        public void SetItem(NyangQuariumBoardItem item)
        {
            Item = item ?? NyangQuariumBoardItem.Empty;

            if (_itemImage == null)
                return;

            _spriteController?.ClearSprite();

            _itemImage.sprite = null;
            _itemImage.enabled = HasItem;
            _itemImage.gameObject.SetActive(HasItem);
            _itemImage.color = Color.white;

            if (!HasItem)
                return;

            ItemData itemData = Item.ItemData;

            if (itemData.ItemSprite != null)
            {
                _itemImage.sprite = itemData.ItemSprite;
                return;
            }

            if (NyangQuariumItemGenerator.TryGetGeneratorSpriteKey(itemData, out string addressableKey))
            {
                _spriteController ??= new UI.UISpriteController(_itemImage);
                _spriteController.ChangeSprite(addressableKey);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_backgroundImage == null)
                return;

            _backgroundImage.color = selected ? SelectedColor : BaseColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_board != null)
                _board.SelectSlot(this);
        }

        private void OnDestroy()
        {
            _spriteController?.Dispose();
            _spriteController = null;
        }
    }
}
