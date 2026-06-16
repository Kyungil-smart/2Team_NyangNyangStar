using System;
using TMPro;
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

        [Header("Buttons")]
        [SerializeField] private Button _minButton;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _maxButton;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Button _cancelButton;

        private FindMoongchiShopViewData _data;
        private Action<FindMoongchiShopViewData, int> _onPurchaseConfirmed;
        private int _count;
        private int _maxCount;

        public void Init()
        {
            BindButtons();
            gameObject.SetActive(false);
        }

        public void Open(FindMoongchiShopViewData data, int maxCount, Action<FindMoongchiShopViewData, int> onPurchaseConfirmed)
        {
            _data = data;
            _onPurchaseConfirmed = onPurchaseConfirmed;
            _maxCount = Mathf.Max(0, maxCount);
            _count = _maxCount > 0 ? 1 : 0;

            if (_itemIcon != null)
            {
                _itemIcon.sprite = data?.Icon;
                _itemIcon.enabled = data?.Icon != null;
            }

            if (_nameText != null)
                _nameText.text = data == null ? string.Empty : BuildProductName(data);

            RefreshTexts();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _data = null;
            _onPurchaseConfirmed = null;
            _count = 0;
            _maxCount = 0;
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

        private void OnDestroy()
        {
            Unbind(_minButton, SetMin);
            Unbind(_minusButton, Decrease);
            Unbind(_plusButton, Increase);
            Unbind(_maxButton, SetMax);
            Unbind(_cancelButton, Close);
            Unbind(_purchaseButton, ConfirmPurchase);
        }

        private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
        }
    }
}
