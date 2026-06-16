using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiShopPanel : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _eventCoinCountText;
        [SerializeField] private Transform _itemGridRoot;
        [SerializeField] private Transform _profileGridRoot;
        [SerializeField] private ShopSlotView _shopSlotPrefab;

        private readonly List<ShopSlotView> _itemSlotViews = new();
        private readonly List<ShopSlotView> _profileSlotViews = new();

        public event Action OnBackButtonClicked;
        public event Action<FindMoongchiShopViewData> OnShopItemClicked;

        public void Init()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            ResolveExistingSlots();
        }

        public void SetData(int eventCoin, IReadOnlyList<FindMoongchiShopViewData> itemProducts, IReadOnlyList<FindMoongchiShopViewData> profileProducts)
        {
            if (_eventCoinCountText != null)
                _eventCoinCountText.text = eventCoin.ToString();

            SetSlots(_itemSlotViews, _itemGridRoot, itemProducts);
            SetSlots(_profileSlotViews, _profileGridRoot, profileProducts);
        }

        private void ResolveExistingSlots()
        {
            if (_itemGridRoot != null && _itemSlotViews.Count == 0)
                _itemSlotViews.AddRange(_itemGridRoot.GetComponentsInChildren<ShopSlotView>(true));

            if (_profileGridRoot != null && _profileSlotViews.Count == 0)
                _profileSlotViews.AddRange(_profileGridRoot.GetComponentsInChildren<ShopSlotView>(true));
        }

        private void SetSlots(List<ShopSlotView> slotViews, Transform parent, IReadOnlyList<FindMoongchiShopViewData> products)
        {
            EnsureSlotCount(slotViews, parent, products?.Count ?? 0);

            for (int i = 0; i < slotViews.Count; i++)
            {
                FindMoongchiShopViewData data = products != null && i < products.Count ? products[i] : null;
                slotViews[i].SetData(data, HandleShopItemClicked);
            }
        }

        private void EnsureSlotCount(List<ShopSlotView> slotViews, Transform parent, int count)
        {
            if (_shopSlotPrefab == null || parent == null)
                return;

            while (slotViews.Count < count)
            {
                ShopSlotView slot = Instantiate(_shopSlotPrefab, parent, false);
                slot.name = $"ShopSlot_{slotViews.Count}";
                slotViews.Add(slot);
            }
        }

        private void HandleBackButtonClicked()
        {
            OnBackButtonClicked?.Invoke();
        }

        private void HandleShopItemClicked(FindMoongchiShopViewData data)
        {
            OnShopItemClicked?.Invoke(data);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }
    }
}
