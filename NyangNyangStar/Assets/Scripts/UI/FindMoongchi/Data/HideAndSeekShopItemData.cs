using System;
using UnityEngine;

namespace Data.ScriptableObjects.HideAndSeekSO
{
    // 상점/보상 테이블의 한 행을 나타내는 데이터
    // 컬럼 순서 : id, productType, productID, quantity, cost, costAmount, limitCount

    [Serializable]
    public class HideAndSeekShopItemData
    {
        [SerializeField] private int _id;
        [SerializeField] private HideAndSeekProductType _productType;
        [SerializeField] private int _productID;
        [SerializeField] private int _quantity;
        [SerializeField] private HideAndSeekCurrencyType _cost;
        [SerializeField] private int _costAmount;
        [SerializeField] private int _limitCount;

        public int ID => _id;
        public HideAndSeekProductType ProductType => _productType;
        public int ProductID => _productID;
        public int Quantity => _quantity;
        public HideAndSeekCurrencyType Cost => _cost;
        public int CostAmount => _costAmount;
        public int LimitCount => _limitCount;


        // limitCount가 0이면 무제한, 1 이상이면 해당 횟수까지만 구매 가능
        public bool HasPurchaseLimit => _limitCount > 0;

        public HideAndSeekShopItemData(
            int id,
            HideAndSeekProductType productType,
            int productID,
            int quantity,
            HideAndSeekCurrencyType cost,
            int costAmount,
            int limitCount)
        {
            _id = id;
            _productType = productType;
            _productID = productID;
            _quantity = quantity;
            _cost = cost;
            _costAmount = costAmount;
            _limitCount = limitCount;
        }
    }
}