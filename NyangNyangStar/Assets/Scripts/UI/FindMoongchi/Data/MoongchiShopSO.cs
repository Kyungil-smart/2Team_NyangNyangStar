using System;
using System.Collections.Generic;
using System.Text;
using Data.ScriptableObjects;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{

    // 뭉치를 찾아라 이벤트 상점 데이터입니다.
    // Google Sheet의 상점/보상 테이블을 파싱해서 보관

    [CreateAssetMenu(fileName = "MoongchiShopSO", menuName = "SO/FindMoongchi/MoongchiShopSO", order = 0)]
    public class MoongchiShopSO : SoBase, ISheetParsable
    {
        [Header("뭉치를 찾아라 상점 데이터")]
        [SerializeField] private List<MoongchiShopItemData> _shopItems = new();

        // 런타임 조회용 캐시
        //id로 상품을 빠르게 찾기 위해 사용
        private readonly Dictionary<int, MoongchiShopItemData> _shopItemByID = new();

        public IReadOnlyList<MoongchiShopItemData> ShopItems => _shopItems;

        private void OnEnable()
        {
            RebuildDictionary();
        }

        public override void Init()
        {
            ClearData();
        }

        public void ClearData()
        {
            _shopItems.Clear();
            _shopItemByID.Clear();
        }


        // 시트 한 줄을 상점 상품 데이터로 변환
        // 헤더 행이나 잘못된 행은 id 파싱 실패로 스근하게 무시

        public void SetData(string[] cols)
        {
            if (cols == null || cols.Length < 7)
                return;

            if (!TryParseInt(GetColumn(cols, 0), out int id) || id <= 0)
                return;

            MoongchiProductType productType = ParseEnum(GetColumn(cols, 1), MoongchiProductType.None);
            int productID = ParseIntOrDefault(GetColumn(cols, 2));
            int quantity = ParseIntOrDefault(GetColumn(cols, 3));
            MoongchiCurrencyType cost = ParseEnum(GetColumn(cols, 4), MoongchiCurrencyType.None);
            int costAmount = ParseIntOrDefault(GetColumn(cols, 5));
            int limitCount = ParseIntOrDefault(GetColumn(cols, 6));

            if (productType == MoongchiProductType.None || cost == MoongchiCurrencyType.None)
                return;

            MoongchiShopItemData data = new MoongchiShopItemData(
                id,
                productType,
                productID,
                quantity,
                cost,
                costAmount,
                limitCount);

            AddOrUpdate(data);
        }

        public bool TryGetShopItem(int id, out MoongchiShopItemData data)
        {
            RebuildDictionaryIfNeeded();
            return _shopItemByID.TryGetValue(id, out data);
        }

        public void PrintData()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[MoongchiShopSO] 로드된 상점 데이터: {_shopItems.Count}");

            foreach (MoongchiShopItemData data in _shopItems)
            {
                builder.AppendLine(
                    $"ID:{data.ID}, Type:{data.ProductType}, ProductID:{data.ProductID}, " +
                    $"Quantity:{data.Quantity}, Cost:{data.Cost}, CostAmount:{data.CostAmount}, Limit:{data.LimitCount}");
            }

            DebugTool.Log(builder.ToString(), DebugType.Data, this);
        }

        private void AddOrUpdate(MoongchiShopItemData data)
        {
            RebuildDictionaryIfNeeded();

            if (_shopItemByID.ContainsKey(data.ID))
            {
                for (int i = 0; i < _shopItems.Count; i++)
                {
                    if (_shopItems[i].ID != data.ID)
                        continue;

                    _shopItems[i] = data;
                    _shopItemByID[data.ID] = data;
                    return;
                }
            }

            _shopItems.Add(data);
            _shopItemByID[data.ID] = data;
        }

        private void RebuildDictionaryIfNeeded()
        {
            if (_shopItemByID.Count == _shopItems.Count)
                return;

            RebuildDictionary();
        }

        private void RebuildDictionary()
        {
            _shopItemByID.Clear();

            foreach (MoongchiShopItemData data in _shopItems)
            {
                if (data == null || data.ID <= 0)
                    continue;

                _shopItemByID[data.ID] = data;
            }
        }

        private static string GetColumn(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        private static int ParseIntOrDefault(string value, int defaultValue = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Enum.TryParse(value, true, out T result) ? result : defaultValue;
        }
    }
}