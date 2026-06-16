using System.Collections.Generic;

namespace UI.FindMoongchi
{
    public static class FindMoongchiConstants
    {
        public const int BoardWidth = 7;
        public const int BoardHeight = 9;
        public const int TileCount = BoardWidth * BoardHeight;

        public const int ToolId01 = 10004;
        public const int ToolId02 = 10020;
        public const int ToolId03 = 10019;

        public const int DailySearchChance = 2;
        public const int EnergySpendTarget = 20;

        public static readonly IReadOnlyList<int> ToolIds = new[]
        {
            ToolId01,
            ToolId02,
            ToolId03
        };
    }

    public enum FindMoongchiPanelType
    {
        Main,
        Game,
        Mission,
        Shop
    }

    public enum FindMoongchiToolPattern
    {
        Row,
        Column,
        Square4x4
    }

    public enum FindMoongchiMissionSlotState
    {
        InProgress,
        Completed,
        Claimed
    }
}
