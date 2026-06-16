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
            DebugTool.Log("[FindMoongchiShopPanel] 초기화 시작", DebugType.FindMoongchi, this);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            ResolveExistingSlots();
            DebugTool.Log($"[FindMoongchiShopPanel] 초기화 완료: 아이템 슬롯={_itemSlotViews.Count}, 프로필 슬롯={_profileSlotViews.Count}", DebugType.FindMoongchi, this);
        }

        public void SetData(int eventCoin, IReadOnlyList<FindMoongchiShopViewData> itemProducts, IReadOnlyList<FindMoongchiShopViewData> profileProducts)
        {
            DebugTool.Log($"[FindMoongchiShopPanel] 상점 데이터 적용: 이벤트재화={eventCoin}, 아이템={itemProducts?.Count ?? 0}, 프로필={profileProducts?.Count ?? 0}", DebugType.FindMoongchi, this);

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

            DebugTool.Log($"[FindMoongchiShopPanel] 기존 상점 슬롯 조회: 아이템={_itemSlotViews.Count}, 프로필={_profileSlotViews.Count}", DebugType.FindMoongchi, this);
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
            if (slotViews.Count >= count)
                return;

            if (_shopSlotPrefab == null || parent == null)
            {
                DebugTool.Warning("[FindMoongchiShopPanel] ShopSlotPrefab 또는 GridRoot가 없어 슬롯을 생성할 수 없습니다.", DebugType.FindMoongchi, this);
                return;
            }

            while (slotViews.Count < count)
            {
                ShopSlotView slot = Instantiate(_shopSlotPrefab, parent, false);
                slot.name = $"ShopSlot_{slotViews.Count}";
                slotViews.Add(slot);
            }

            DebugTool.Log($"[FindMoongchiShopPanel] 상점 슬롯 수 보정 완료: {slotViews.Count}/{count}", DebugType.FindMoongchi, this);
        }

        private void HandleBackButtonClicked()
        {
            DebugTool.Log("[FindMoongchiShopPanel] 뒤로가기 버튼 클릭", DebugType.FindMoongchi, this);
            OnBackButtonClicked?.Invoke();
        }

        private void HandleShopItemClicked(FindMoongchiShopViewData data)
        {
            if (data != null)
                DebugTool.Log($"[FindMoongchiShopPanel] 상점 슬롯 클릭: ShopItemId={data.ShopItemId}", DebugType.FindMoongchi, this);

            OnShopItemClicked?.Invoke(data);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }
    }
}
