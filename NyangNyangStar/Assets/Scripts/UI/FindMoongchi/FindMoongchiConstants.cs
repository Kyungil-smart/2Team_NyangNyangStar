using System.Collections.Generic;

namespace UI.FindMoongchi
{
    public static class FindMoongchiConstants
    {
        public const int BoardWidth = 7;
        public const int BoardHeight = 9;
        public const int TileCount = BoardWidth * BoardHeight;

        // 도구 ID 기본값입니다. 실제 사용 ID는 FindMoongchiPopup 인스펙터 배열에서 관리합니다.
        // 순서 고정: 0 = 가로 한 줄, 1 = 세로 한 줄, 2 = 4x4 사각형.
        public const int ToolId01 = 10004;
        public const int ToolId02 = 10020;
        public const int ToolId03 = 10019;
        public const int ToolSlotCount = 3;

        public const int DailySearchChance = 10;
        public const int EnergySpendTarget = 20;

        public static readonly int[] DefaultToolItemIds =
        {
            ToolId01,
            ToolId02,
            ToolId03
        };

        public static readonly IReadOnlyList<int> ToolIds = DefaultToolItemIds;
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
