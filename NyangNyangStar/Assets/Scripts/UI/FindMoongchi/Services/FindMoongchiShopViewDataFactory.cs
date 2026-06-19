using System;
using System.Collections.Generic;
using Data.ScriptableObjects.MoongchiSO;
using UnityEngine;

namespace UI.FindMoongchi
{
    /// <summary>
    /// 상점 정적 데이터와 진행 데이터를 UI 표시용 데이터로 변환합니다.
    /// FindMoongchiPopup이 상품 이름/아이콘/구매 횟수 계산까지 직접 담당하지 않도록 분리한 클래스입니다.
    /// </summary>
    public sealed class FindMoongchiShopViewDataFactory
    {
        private readonly FindMoongchiDataManager _dataManager;
        private readonly FindMoongchiProgressController _progressController;
        private readonly bool _isProgressReady;
        private readonly Dictionary<int, int> _localPurchaseCounts;
        private readonly Func<int, string> _getItemName;
        private readonly Func<int, Sprite> _getItemSprite;

        public FindMoongchiShopViewDataFactory(
            FindMoongchiDataManager dataManager,
            FindMoongchiProgressController progressController,
            bool isProgressReady,
            Dictionary<int, int> localPurchaseCounts,
            Func<int, string> getItemName,
            Func<int, Sprite> getItemSprite)
        {
            _dataManager = dataManager;
            _progressController = progressController;
            _isProgressReady = isProgressReady;
            _localPurchaseCounts = localPurchaseCounts;
            _getItemName = getItemName;
            _getItemSprite = getItemSprite;
        }

        public void BuildProducts(
            List<FindMoongchiShopViewData> itemProducts,
            List<FindMoongchiShopViewData> profileProducts)
        {
            itemProducts?.Clear();
            profileProducts?.Clear();

            if (_dataManager == null)
            {
                DebugTool.Warning("[FindMoongchiShopViewDataFactory] DataManager가 없어 상점 데이터를 만들 수 없습니다.", DebugType.FindMoongchi);
                return;
            }

            IReadOnlyList<MoongchiShopItemData> shopItems = _dataManager.GetShopItems();

            for (int i = 0; i < shopItems.Count; i++)
            {
                MoongchiShopItemData item = shopItems[i];

                if (item == null)
                    continue;

                FindMoongchiShopViewData data = Build(item);

                if (data.ProductType == MoongchiProductType.PROFILE)
                    profileProducts?.Add(data);
                else
                    itemProducts?.Add(data);
            }
        }

        public FindMoongchiShopViewData Build(MoongchiShopItemData item)
        {
            if (item == null)
                return null;

            int purchasedCount = GetPurchasedCount(item.ID);

            return new FindMoongchiShopViewData
            {
                ShopItemId = item.ID,
                ProductType = item.ProductType,
                ProductId = item.ProductID,
                ProductName = GetProductName(item.ProductType, item.ProductID),
                Icon = GetProductSprite(item.ProductType, item.ProductID),
                IconKey = GetProductIconKey(item.ProductType, item.ProductID),
                Quantity = item.Quantity,
                // 상점 구매 비용은 항상 이벤트 코인으로 처리합니다.
                // 시트의 Cost 값이 다른 재화로 들어와도 UI/구매 검증은 이벤트 코인 기준입니다.
                CostType = MoongchiCurrencyType.EVENT_COIN,
                CostAmount = item.CostAmount,
                LimitCount = item.LimitCount,
                PurchasedCount = purchasedCount
            };
        }

        private int GetPurchasedCount(int shopItemId)
        {
            if (_isProgressReady && _progressController != null)
                return _progressController.GetShopPurchaseCount(shopItemId);

            return _localPurchaseCounts != null && _localPurchaseCounts.TryGetValue(shopItemId, out int localPurchasedCount)
                ? localPurchasedCount
                : 0;
        }

        public string GetProductName(MoongchiProductType productType, int productId)
        {
            if (productType == MoongchiProductType.ITEM)
                return _getItemName?.Invoke(productId) ?? productId.ToString();

            if (productType == MoongchiProductType.PROFILE &&
                _dataManager != null &&
                _dataManager.TryGetProfile(productId, out MoongchiProfileData profile) &&
                profile != null &&
                !string.IsNullOrEmpty(profile.ProfileName))
            {
                return profile.ProfileName;
            }

            if (productType == MoongchiProductType.CURRENCY)
                return GetCurrencyProductName(productId);

            return $"{productType} {productId}";
        }

        public Sprite GetProductSprite(MoongchiProductType productType, int productId)
        {
            if (productType == MoongchiProductType.ITEM)
                return _getItemSprite?.Invoke(productId);

            return null;
        }

        public string GetProductIconKey(MoongchiProductType productType, int productId)
        {
            if (productType == MoongchiProductType.CURRENCY)
                return GetCurrencyProductIconKey(productId);

            if (productType == MoongchiProductType.PROFILE &&
                _dataManager != null &&
                _dataManager.TryGetProfile(productId, out MoongchiProfileData profile) &&
                profile != null)
            {
                return profile.AddressableKey;
            }

            return null;
        }

        public static string GetCurrencyProductName(int productId)
        {
            return productId switch
            {
                1 => "골드",
                2 => "보석",
                3 => "에너지",
                _ => $"재화 {productId}"
            };
        }

        public static string GetCurrencyProductIconKey(int productId)
        {
            return productId switch
            {
                1 => FindMoongchiSpriteKeys.CommonGoldIcon,
                2 => FindMoongchiSpriteKeys.CommonGemIcon,
                3 => FindMoongchiSpriteKeys.CommonEnergyIcon,
                _ => null
            };
        }
    }
}
