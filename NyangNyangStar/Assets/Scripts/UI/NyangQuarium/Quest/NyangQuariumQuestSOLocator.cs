using Data.Loader;
using Data.ScriptableObjects.NyangQuariumSO;
using UnityEngine;

namespace UI.NyangQuarium.Quest
{
    // SheetLoader / NyangQuariumSheetLoader 에서 퀘스트 관련 SO를 조회하는 헬퍼
    public static class NyangQuariumQuestSOLocator
    {
        // 퀘스트 SO 조회 (SheetLoader 우선, 없으면 NyangQuariumSheetLoader)
        public static bool TryResolveQuestSO(out NyangQuariumQuestSO questSO)
        {
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumQuestSO(out questSO))
                return questSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            questSO = quariumLoader != null ? quariumLoader.QuestSO : null;
            return questSO != null;
        }

        // 퀘스트 문자열 SO 조회
        public static NyangQuariumQuestStringSO ResolveQuestStringSO()
        {
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null &&
                sheetLoader.TryGetNyangQuariumQuestStringSO(out NyangQuariumQuestStringSO stringSO))
            {
                return stringSO;
            }

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            return quariumLoader != null ? quariumLoader.QuestStringSO : null;
        }

        // 퀘스트 보상 SO 조회
        public static NyangQuariumQuestRewardSO ResolveQuestRewardSO()
        {
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null &&
                sheetLoader.TryGetNyangQuariumQuestRewardSO(out NyangQuariumQuestRewardSO rewardSO))
            {
                return rewardSO;
            }

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            return quariumLoader != null ? quariumLoader.QuestRewardSO : null;
        }

        public static NyangQuariumAquariumLevelSO ResolveAquariumLevelSO()
        {
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null &&
                sheetLoader.TryGetNyangQuariumAquariumLevelSO(out NyangQuariumAquariumLevelSO aquariumLevelSO))
            {
                return aquariumLevelSO;
            }

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            return quariumLoader != null ? quariumLoader.AquariumLevelSO : null;
        }
    }
}
