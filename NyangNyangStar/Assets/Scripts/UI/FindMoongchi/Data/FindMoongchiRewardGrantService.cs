using System.Threading.Tasks;
using Core.Managers;
using Data.ScriptableObjects.MoongchiSO;
using UI.MergeBoard;
using UnityEngine;

namespace UI.FindMoongchi
{
    // 미션/상점 보상을 실제 유저 재화, 아이템, 프로필로 지급
    public static class FindMoongchiRewardGrantService
    {
        public static async Task<bool> GrantMissionRewardAsync(
            MoongchiRewardData reward,
            FindMoongchiProgressRuntimeData progress,
            ResourcesSO resourcesSO)
        {
            if (reward == null || !reward.IsValid)
                return true;

            return reward.RewardType switch
            {
                MoongchiCurrencyType.EVENT_COIN => GrantEventCoin(progress, reward.RewardAmount),
                MoongchiCurrencyType.ENERGY => await GrantEnergyAsync(resourcesSO, reward.RewardAmount),
                _ => false
            };
        }

        public static async Task<bool> GrantShopProductAsync(
            MoongchiShopItemData shopItem,
            int purchaseCount,
            FindMoongchiProgressRuntimeData progress,
            ResourcesSO resourcesSO)
        {
            if (shopItem == null || purchaseCount <= 0 || progress == null)
                return false;

            int totalQuantity = Mathf.Max(1, shopItem.Quantity) * purchaseCount;

            switch (shopItem.ProductType)
            {
                case MoongchiProductType.ITEM:
                    return await GrantMergeBoardItemAsync(shopItem.ProductID, totalQuantity);

                case MoongchiProductType.CURRENCY:
                    return await GrantShopCurrencyAsync(shopItem.ProductID, totalQuantity, progress, resourcesSO);

                case MoongchiProductType.PROFILE:
                    return GrantOwnedProfile(progress, shopItem.ProductID);

                default:
                    DebugTool.Warning(
                        $"[FindMoongchiRewardGrantService] 지원하지 않는 상품 타입: {shopItem.ProductType}",
                        DebugType.Data);
                    return false;
            }
        }

        public static async Task<bool> GrantEnergyAsync(ResourcesSO resourcesSO, int amount)
        {
            if (amount <= 0)
                return true;

            if (resourcesSO == null)
            {
                DebugTool.Warning("[FindMoongchiRewardGrantService] ResourcesSO가 연결되지 않아 에너지를 지급할 수 없습니다.", DebugType.Data);
                return false;
            }

            PlayerResourceManager.Instance.Bind(resourcesSO);
            bool granted = await PlayerResourceManager.Instance.AddEnergyAsync(amount);

            if (granted)
                DebugTool.Log($"[FindMoongchiRewardGrantService] 에너지 지급: +{amount}", DebugType.Data);

            return granted;
        }

        private static bool GrantEventCoin(FindMoongchiProgressRuntimeData progress, int amount)
        {
            if (progress == null || amount <= 0)
                return false;

            progress.EventCurrency += amount;
            return true;
        }

        private static async Task<bool> GrantShopCurrencyAsync(
            int currencyProductId,
            int amount,
            FindMoongchiProgressRuntimeData progress,
            ResourcesSO resourcesSO)
        {
            switch (currencyProductId)
            {
                case 1:
                    return await GrantEnergyAsync(resourcesSO, amount);
                case 2:
                    return await GrantCoinAsync(resourcesSO, amount);
                case 3:
                    return GrantEventCoin(progress, amount);
                default:
                    DebugTool.Warning(
                        $"[FindMoongchiRewardGrantService] 알 수 없는 재화 상품 ID: {currencyProductId}",
                        DebugType.Data);
                    return false;
            }
        }

        private static async Task<bool> GrantCoinAsync(ResourcesSO resourcesSO, int amount)
        {
            if (amount <= 0)
                return true;

            if (resourcesSO == null)
                return false;

            PlayerResourceManager.Instance.Bind(resourcesSO);
            return await PlayerResourceManager.Instance.AddCoinAsync(amount);
        }

        private static async Task<bool> GrantMergeBoardItemAsync(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0)
                return false;

            if (MergeBoardItemService.Instance == null)
            {
                DebugTool.Warning("[FindMoongchiRewardGrantService] MergeBoardItemService가 없어 아이템을 지급할 수 없습니다.", DebugType.Data);
                return false;
            }

            bool granted = await MergeBoardItemService.Instance.AddItemByIdAsync(itemId, count);

            if (granted)
                DebugTool.Log($"[FindMoongchiRewardGrantService] 아이템 지급: ItemId={itemId}, Count={count}", DebugType.Data);

            return granted;
        }

        private static bool GrantOwnedProfile(FindMoongchiProgressRuntimeData progress, int profileId)
        {
            if (profileId <= 0 || progress == null)
                return false;

            FindMoongchiProgressHelper.AddOwnedProfile(progress, profileId);
            DebugTool.Log($"[FindMoongchiRewardGrantService] 프로필 해금: ProfileId={profileId}", DebugType.Data);
            return true;
        }
    }
}
