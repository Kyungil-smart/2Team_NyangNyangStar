using System;

namespace UI.NyangQuarium.Quest
{
    public static class NyangQuariumQuestConditionUtil
    {
        public static bool IsCoinCondition(string condition)
        {
            return condition != null &&
                   (condition.Equals("Coin", StringComparison.OrdinalIgnoreCase) ||
                    condition.Equals("코인", StringComparison.OrdinalIgnoreCase));
        }
    }
}
