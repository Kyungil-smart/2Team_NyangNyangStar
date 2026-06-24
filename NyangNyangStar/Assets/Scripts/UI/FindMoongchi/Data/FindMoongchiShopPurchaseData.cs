using System;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 상점 상품별 구매 이력 데이터
    // ShopItemID를 맵 키로 사용, 상품별 누적 구매 횟수 저장
    [Serializable]
    public class FindMoongchiShopPurchaseData
    {
        [FirestoreMapKey]
        [SerializeField] private int _shopItemID;

        [SerializeField] private int _purchaseCount;

        public int ShopItemID => _shopItemID;
        public int PurchaseCount => _purchaseCount;

        public FindMoongchiShopPurchaseData()
        {
        }

        public FindMoongchiShopPurchaseData(int shopItemID, int purchaseCount)
        {
            _shopItemID = shopItemID;
            _purchaseCount = Mathf.Max(0, purchaseCount);
        }

        public void AddPurchaseCount(int count)
        {
            if (count <= 0)
                return;

            _purchaseCount = Mathf.Max(0, _purchaseCount + count);
        }

        public void SetPurchaseCount(int purchaseCount)
        {
            _purchaseCount = Mathf.Max(0, purchaseCount);
        }
    }
}
