using System.Collections.Generic;
using Data.ScriptableObjects.MoongchiSO;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiGameViewData
    {
        public int StageId;
        public int BoardWidth = FindMoongchiConstants.BoardWidth;
        public int BoardHeight = FindMoongchiConstants.BoardHeight;
        public int CurrentWeek;
        public string RemainTimeText;
        public int SearchChance;
        public int EnergySpendProgress;
        public int EnergySpendTarget;
        public List<FindMoongchiToolViewData> Tools = new();
        public List<FindMoongchiTargetHintViewData> TargetHints = new();
        public List<FindMoongchiTargetVisualViewData> TargetVisuals = new();
        public HashSet<int> RevealedTileIndices = new();
    }

    public sealed class FindMoongchiToolViewData
    {
        public int ToolItemId;
        public string ToolName;
        public Sprite Icon;
        public string IconKey;
        public int Count;
        public bool IsUsable;
    }

    public sealed class FindMoongchiTargetHintViewData
    {
        public string TargetName;
        public Sprite Icon;
        public string IconKey;
        public bool IsFound;
    }


    public sealed class FindMoongchiTargetVisualViewData
    {
        public int TargetId;
        public string TargetName;
        public Sprite Icon;
        public string IconKey;
        public bool IsFound;
        public List<int> CellIndices = new();
    }

    public sealed class FindMoongchiMissionViewData
    {
        public int MissionId;
        public MoongchiMissionType MissionType;
        public string MissionDescription;
        public int CurrentAmount;
        public int TargetAmount;
        public FindMoongchiMissionSlotState State;
        public MoongchiRewardData Reward1;
        public MoongchiRewardData Reward2;
    }

    public sealed class FindMoongchiShopViewData
    {
        public int ShopItemId;
        public MoongchiProductType ProductType;
        public int ProductId;
        public string ProductName;
        public Sprite Icon;
        public string IconKey;
        public int Quantity;
        public MoongchiCurrencyType CostType;
        public string CostIconKey;
        public int CostAmount;
        public int LimitCount;
        public int PurchasedCount;

        public bool HasLimit => LimitCount > 0;
        public int RemainingLimit => HasLimit ? Mathf.Max(0, LimitCount - PurchasedCount) : int.MaxValue;
        public bool IsSoldOut => HasLimit && RemainingLimit <= 0;
    }
}
