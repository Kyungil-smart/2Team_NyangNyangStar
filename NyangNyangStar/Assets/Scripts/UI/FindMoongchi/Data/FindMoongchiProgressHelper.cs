using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // FindMoongchiProgressRuntimeData 안의 리스트형 진행값을 다루는 static 유틸
    //
    // RuntimeData는 미션 / 상점 / 프로필 진행 정보를 List로 들고 있는데,
    // Firestore 맵 필드와 1:1로 맞추기 위해 ID 기준 조회, 추가, 갱신 로직을 여기 모아 둡니다.
    
    // FindMoongchiDataManager, FindMoongchiMissionTracker,
    // FindMoongchiProgressResetLogic, FindMoongchiRewardGrantService
    
    // UI(FindMoongchiPopup)는 FindMoongchiProgressController 
    public static class FindMoongchiProgressHelper
    {
        // --- 미션 진행도 ---

        // missionId에 해당하는 미션 진행 항목 조회. 없으면 null
        public static FindMoongchiMissionProgressData FindMissionProgress(
            FindMoongchiProgressRuntimeData progress,
            int missionId)
        {
            if (progress?.MissionProgresses == null)
                return null;

            List<FindMoongchiMissionProgressData> missions = progress.MissionProgresses;

            for (int i = 0; i < missions.Count; i++)
            {
                FindMoongchiMissionProgressData entry = missions[i];

                if (entry != null && entry.MissionID == missionId)
                    return entry;
            }

            return null;
        }

        // 미션 진행 항목 조회. 없으면 0 / 미수령 상태로 새 항목 생성
        public static FindMoongchiMissionProgressData GetOrCreateMissionProgress(
            FindMoongchiProgressRuntimeData progress,
            int missionId)
        {
            if (progress == null)
                return null;

            FindMoongchiMissionProgressData existing = FindMissionProgress(progress, missionId);

            if (existing != null)
                return existing;

            FindMoongchiMissionProgressData created = new FindMoongchiMissionProgressData(missionId, 0, false);
            progress.MissionProgresses.Add(created);
            return created;
        }

        // 미션 현재 진행 수치 (없으면 0)
        public static int GetMissionCurrentAmount(FindMoongchiProgressRuntimeData progress, int missionId)
        {
            FindMoongchiMissionProgressData entry = FindMissionProgress(progress, missionId);
            return entry?.CurrentAmount ?? 0;
        }

        // 미션 보상 수령 완료 여부
        public static bool IsMissionRewardClaimed(FindMoongchiProgressRuntimeData progress, int missionId)
        {
            FindMoongchiMissionProgressData entry = FindMissionProgress(progress, missionId);
            return entry != null && entry.IsRewardClaimed;
        }

        // --- 상점 구매 횟수 ---

        // shopItemId 누적 구매 횟수 (없으면 0)
        public static int GetShopPurchaseCount(FindMoongchiProgressRuntimeData progress, int shopItemId)
        {
            if (progress?.ShopPurchaseCounts == null)
                return 0;

            List<FindMoongchiShopPurchaseData> purchases = progress.ShopPurchaseCounts;

            for (int i = 0; i < purchases.Count; i++)
            {
                FindMoongchiShopPurchaseData entry = purchases[i];

                if (entry != null && entry.ShopItemID == shopItemId)
                    return entry.PurchaseCount;
            }

            return 0;
        }

        // shopItemId 구매 횟수 증가. 항목 없으면 새로 추가
        public static void AddShopPurchaseCount(FindMoongchiProgressRuntimeData progress, int shopItemId, int count)
        {
            if (progress == null || shopItemId <= 0 || count <= 0)
                return;

            List<FindMoongchiShopPurchaseData> purchases = progress.ShopPurchaseCounts;

            for (int i = 0; i < purchases.Count; i++)
            {
                FindMoongchiShopPurchaseData entry = purchases[i];

                if (entry == null || entry.ShopItemID != shopItemId)
                    continue;

                entry.AddPurchaseCount(count);
                return;
            }

            purchases.Add(new FindMoongchiShopPurchaseData(shopItemId, count));
        }

        // --- 미션 진행도 갱신 ---

        // 미션 진행도를 amount만큼 올림. targetAmount 초과 불가, 이미 보상 수령했으면 false
        public static bool TryAddMissionProgress(
            FindMoongchiProgressRuntimeData progress,
            int missionId,
            int amount,
            int targetAmount)
        {
            if (progress == null || missionId <= 0 || amount <= 0 || targetAmount <= 0)
                return false;

            if (IsMissionRewardClaimed(progress, missionId))
                return false;

            FindMoongchiMissionProgressData entry = GetOrCreateMissionProgress(progress, missionId);

            if (entry == null)
                return false;

            int nextAmount = Mathf.Min(entry.CurrentAmount + amount, targetAmount);

            if (nextAmount == entry.CurrentAmount)
                return false;

            entry.SetCurrentAmount(nextAmount);
            return true;
        }

        // 일/주간 리셋 시 해당 미션 진행 항목 삭제
        public static void RemoveMissionProgress(FindMoongchiProgressRuntimeData progress, int missionId)
        {
            if (progress?.MissionProgresses == null || missionId <= 0)
                return;

            List<FindMoongchiMissionProgressData> missions = progress.MissionProgresses;

            for (int i = missions.Count - 1; i >= 0; i--)
            {
                FindMoongchiMissionProgressData entry = missions[i];

                if (entry != null && entry.MissionID == missionId)
                    missions.RemoveAt(i);
            }
        }

        // --- 보유 프로필 ---

        // 상점에서 구매한 프로필 보유 여부
        public static bool HasOwnedProfile(FindMoongchiProgressRuntimeData progress, int profileId)
        {
            return progress?.OwnedProfileIDs != null && progress.OwnedProfileIDs.Contains(profileId);
        }

        // 프로필 보유 목록에 추가. 이미 있으면 false
        public static bool AddOwnedProfile(FindMoongchiProgressRuntimeData progress, int profileId)
        {
            if (progress == null || profileId <= 0)
                return false;

            if (progress.OwnedProfileIDs == null)
                progress.OwnedProfileIDs = new List<int>();

            if (progress.OwnedProfileIDs.Contains(profileId))
                return false;

            progress.OwnedProfileIDs.Add(profileId);
            return true;
        }
    }
}
