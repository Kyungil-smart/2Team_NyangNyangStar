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
    // 냥쿠아리움 보상 대기열 슬롯 UI 관리
    public sealed class NyangQuariumRewardQueueSlot : MonoBehaviour, IPointerClickHandler
    {
        private NyangQuariumRewardQueue _rewardQueue;
        private Image _itemImage;
        private Button _button;
        private UI.UISpriteController _spriteController;
        private Color _emptyColor;

        // 같은 아이템 재세팅 시 스프라이트 재로드 방지 캐시
        private int _currentItemId;
        private string _currentAddressableKey;
        private Sprite _currentSprite;
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

            bool hasItem = item != null && item.HasItem;

            if (_itemImage == null)
                return;

            // 빈 슬롯 이미지 및 현재 표시 상태 초기화
            if (!hasItem)
            {
                ClearCurrentItem();
                _itemImage.sprite = null;
                _itemImage.color = _emptyColor;
                _itemImage.enabled = false;
                _itemImage.gameObject.SetActive(false);
                _itemImage.raycastTarget = _button != null;
                return;
            }

            ItemData itemData = item.ItemData;

            // 아이템 데이터 없음, 빈 슬롯과 동일 처리
            if (itemData == null)
            {
                ClearCurrentItem();
                _itemImage.sprite = null;
                _itemImage.color = _emptyColor;
                _itemImage.enabled = false;
                _itemImage.gameObject.SetActive(false);
                _itemImage.raycastTarget = _button != null;
                return;
            }

            _itemImage.gameObject.SetActive(true);
            _itemImage.enabled = true;
            _itemImage.raycastTarget = _button != null;

            // ItemData 직접 연결 스프라이트, addressable 로드 없이 표시
            if (itemData.ItemSprite != null)
            {
                if (_hasItem && _currentItemId == itemData.ItemID && _currentSprite == itemData.ItemSprite)
                    return;

                _spriteController?.ClearSprite();
                _itemImage.sprite = itemData.ItemSprite;
                _itemImage.color = Color.white;
                _hasItem = true;
                _currentItemId = itemData.ItemID;
                _currentSprite = itemData.ItemSprite;
                _currentAddressableKey = null;
                return;
            }

            // 생성기 아이템, addressable 키로 스프라이트 로드
            if (NyangQuariumItemGenerator.TryGetGeneratorSpriteKey(itemData, out string addressableKey))
            {
                // 같은 아이템 재로드 차단, 대기 칸 이미지 깜빡임 방지
                if (_hasItem && _currentItemId == itemData.ItemID && _currentAddressableKey == addressableKey)
                    return;

                _spriteController ??= new UI.UISpriteController(_itemImage);
                _spriteController.ClearSprite();
                _itemImage.sprite = null;
                _itemImage.color = Color.clear;
                _hasItem = true;
                _currentItemId = itemData.ItemID;
                _currentSprite = null;
                _currentAddressableKey = addressableKey;

                // 로드 완료 전 null 스프라이트 흰 네모 숨김
                _spriteController.ChangeSprite(addressableKey, onLoaded: () =>
                {
                    if (_itemImage != null && _itemImage.sprite != null)
                        _itemImage.color = Color.white;
                });
            }
            else
            {
                ClearCurrentItem();
                _itemImage.sprite = null;
                _itemImage.color = _emptyColor;
            }
        }

        // 현재 슬롯 표시 아이템 상태 및 addressable 핸들 초기화
        private void ClearCurrentItem()
        {
            _hasItem = false;
            _currentItemId = 0;
            _currentAddressableKey = null;
            _currentSprite = null;
            _spriteController?.ClearSprite();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null)
                return;

            HandleClick();
        }

        private void HandleClick()
        {
            // 대기열 맨 앞 슬롯만 머지보드 이동 허용
            if (!_isTopSlot || !_hasItem || _rewardQueue == null)
                return;

            _rewardQueue.TryMoveTopItemToBoard();
        }

        private void BindImage()
        {
            if (_itemImage != null)
                return;

            // 슬롯 프리팹의 버튼 이미지를 실제 아이템 아이콘으로 사용
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
