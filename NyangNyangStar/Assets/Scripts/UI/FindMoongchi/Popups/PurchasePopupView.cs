using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class PurchasePopupView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _purchaseCountText;
        [SerializeField] private TMP_Text _costAmountText;
        [Tooltip("총 가격 옆 재화 아이콘입니다. 비워두면 자식 중 CoinIcon을 자동 검색합니다.")]
        [SerializeField] private Image _costIconImage;

        [Header("Buttons")]
        [SerializeField] private Button _minButton;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _maxButton;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Button _cancelButton;

        private UISpriteController _itemIconController;
        private UISpriteController _costIconController;
        private FindMoongchiShopViewData _data;
        private Action<FindMoongchiShopViewData, int> _onPurchaseConfirmed;
        private int _count;
        private int _maxCount;

        private void Awake()
        {
            if (_itemIcon != null)
                _itemIconController = new UISpriteController(_itemIcon);

            if (_costIconImage == null)
                _costIconImage = FindChildImage(transform, "CoinIcon");

            if (_costIconImage != null)
                _costIconController = new UISpriteController(_costIconImage);
        }

        public void Init()
        {
            BindButtons();
            gameObject.SetActive(false);
        }

        public void Open(FindMoongchiShopViewData data, int maxCount, Action<FindMoongchiShopViewData, int> onPurchaseConfirmed)
        {
            DebugTool.Log($"[PurchasePopupView] 구매 팝업 열기: ShopItemId={data?.ShopItemId}, MaxCount={maxCount}", DebugType.FindMoongchi, this);
            _data = data;
            _onPurchaseConfirmed = onPurchaseConfirmed;
            _maxCount = Mathf.Max(0, maxCount);
            _count = _maxCount > 0 ? 1 : 0;

            SetIcon(data);
            SetCostIcon();

            if (_nameText != null)
                _nameText.text = data == null ? string.Empty : BuildProductName(data);

            RefreshTexts();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (gameObject.activeSelf)
                DebugTool.Log("[PurchasePopupView] 구매 팝업 닫기", DebugType.FindMoongchi, this);

            gameObject.SetActive(false);
            _data = null;
            _onPurchaseConfirmed = null;
            _count = 0;
            _maxCount = 0;
        }

        private void SetIcon(FindMoongchiShopViewData data)
        {
            if (_itemIcon == null)
                return;

            if (data != null && !string.IsNullOrWhiteSpace(data.IconKey))
            {
                _itemIcon.enabled = true;

                if (_itemIconController == null)
                    _itemIconController = new UISpriteController(_itemIcon);

                _itemIconController.ChangeSprite(data.IconKey);
                return;
            }

            _itemIcon.sprite = data?.Icon;
            _itemIcon.enabled = data?.Icon != null;
        }

        private void SetCostIcon()
        {
            if (_costIconImage == null)
                return;

            // 구매 팝업의 필요 재화도 항상 이벤트 코인으로 표시합니다.
            string key = FindMoongchiSpriteKeys.EventCoinIcon;

            if (string.IsNullOrWhiteSpace(key))
            {
                _costIconImage.enabled = false;
                return;
            }

            _costIconImage.enabled = true;
            _costIconImage.sprite = null;
            _costIconImage.color = Color.white;
            _costIconImage.preserveAspect = true;

            if (_costIconController == null)
                _costIconController = new UISpriteController(_costIconImage);

            _costIconController.ChangeSprite(key);
        }

        private void BindButtons()
        {
            Bind(_minButton, SetMin);
            Bind(_minusButton, Decrease);
            Bind(_plusButton, Increase);
            Bind(_maxButton, SetMax);
            Bind(_cancelButton, Close);
            Bind(_purchaseButton, ConfirmPurchase);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void SetMin()
        {
            _count = _maxCount > 0 ? 1 : 0;
            RefreshTexts();
        }

        private void Decrease()
        {
            _count = Mathf.Max(_maxCount > 0 ? 1 : 0, _count - 1);
            RefreshTexts();
        }

        private void Increase()
        {
            _count = Mathf.Min(_maxCount, _count + 1);
            RefreshTexts();
        }

        private void SetMax()
        {
            _count = _maxCount;
            RefreshTexts();
        }

        private void ConfirmPurchase()
        {
            if (_data == null || _count <= 0)
                return;

            DebugTool.Log($"[PurchasePopupView] 구매 버튼 클릭: ShopItemId={_data.ShopItemId}, Count={_count}", DebugType.FindMoongchi, this);

            _onPurchaseConfirmed?.Invoke(_data, _count);
        }

        private void RefreshTexts()
        {
            if (_purchaseCountText != null)
                _purchaseCountText.text = _count.ToString();

            int totalCost = _data != null ? _data.CostAmount * _count : 0;

            if (_costAmountText != null)
                _costAmountText.text = totalCost.ToString();

            if (_purchaseButton != null)
                _purchaseButton.interactable = _data != null && _count > 0 && _maxCount > 0;
        }

        private static string BuildProductName(FindMoongchiShopViewData data)
        {
            string productName = string.IsNullOrEmpty(data.ProductName)
                ? $"{data.ProductType} {data.ProductId}"
                : data.ProductName;

            return data.Quantity > 1 ? $"{productName} x{data.Quantity}" : productName;
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == childName && child.TryGetComponent(out Image image))
                    return image;

                Image nested = FindChildImage(child, childName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void OnDestroy()
        {
            Unbind(_minButton, SetMin);
            Unbind(_minusButton, Decrease);
            Unbind(_plusButton, Increase);
            Unbind(_maxButton, SetMax);
            Unbind(_cancelButton, Close);
            Unbind(_purchaseButton, ConfirmPurchase);

            _itemIconController?.Dispose();
            _costIconController?.Dispose();
        }

        private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
        }
    }
}
