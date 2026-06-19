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

            if (resourcesSO != null)
                PlayerResourceManager.Instance.Bind(resourcesSO);

            bool granted = await PlayerResourceManager.Instance.AddEnergyAsync(amount);

            if (granted)
            {
                DebugTool.Log($"[FindMoongchiRewardGrantService] 에너지 지급: +{amount}", DebugType.Data);
                return true;
            }

            DebugTool.Warning("[FindMoongchiRewardGrantService] PlayerResourceManager를 통해 에너지를 지급하지 못했습니다.", DebugType.Data);
            return false;
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

            if (resourcesSO != null)
                PlayerResourceManager.Instance.Bind(resourcesSO);

            bool granted = await PlayerResourceManager.Instance.AddCoinAsync(amount);

            if (granted)
            {
                DebugTool.Log($"[FindMoongchiRewardGrantService] 코인 지급: +{amount}", DebugType.Data);
                return true;
            }

            DebugTool.Warning("[FindMoongchiRewardGrantService] PlayerResourceManager를 통해 코인을 지급하지 못했습니다.", DebugType.Data);
            return false;
        }

        private static async Task<bool> GrantMergeBoardItemAsync(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0)
                return false;

            MergeBoardItemService itemService = ResolveMergeBoardItemService();

            if (itemService == null)
            {
                DebugTool.Warning("[FindMoongchiRewardGrantService] MergeBoardItemService가 없어 아이템을 지급할 수 없습니다.", DebugType.Data);
                return false;
            }

            bool granted = await itemService.AddItemByIdAsync(itemId, count);

            if (granted)
                DebugTool.Log($"[FindMoongchiRewardGrantService] 아이템 지급: ItemId={itemId}, Count={count}", DebugType.Data);

            return granted;
        }

        private static MergeBoardItemService ResolveMergeBoardItemService()
        {
            if (MergeBoardItemService.Instance != null)
                return MergeBoardItemService.Instance;

            MergeBoardItemService activeService = Object.FindFirstObjectByType<MergeBoardItemService>();

            if (activeService != null)
                return activeService;

            MergeBoardItemService[] services = Resources.FindObjectsOfTypeAll<MergeBoardItemService>();

            for (int i = 0; i < services.Length; i++)
            {
                MergeBoardItemService service = services[i];

                if (service == null || service.gameObject == null)
                    continue;

                if (!service.gameObject.scene.IsValid())
                    continue;

                return service;
            }

            return null;
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
