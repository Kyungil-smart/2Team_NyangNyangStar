using System;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 상점/보상 테이블의 한 행을 나타내는 데이터
    // 컬럼 순서 : id, productType, productID, quantity, cost, costAmount, limitCount

    [Serializable]
    public class MoongchiShopItemData
    {
        [SerializeField] private int _id;
        [SerializeField] private MoongchiProductType _productType;
        [SerializeField] private int _productID;
        [SerializeField] private int _quantity;
        [SerializeField] private MoongchiCurrencyType _cost;
        [SerializeField] private int _costAmount;
        [SerializeField] private int _limitCount;

        public int ID => _id;
        public MoongchiProductType ProductType => _productType;
        public int ProductID => _productID;
        public int Quantity => _quantity;
        public MoongchiCurrencyType Cost => _cost;
        public int CostAmount => _costAmount;
        public int LimitCount => _limitCount;


        // limitCount가 0이면 무제한, 1 이상이면 해당 횟수까지만 구매 가능
        public bool HasPurchaseLimit => _limitCount > 0;

        public MoongchiShopItemData(
            int id,
            MoongchiProductType productType,
            int productID,
            int quantity,
            MoongchiCurrencyType cost,
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