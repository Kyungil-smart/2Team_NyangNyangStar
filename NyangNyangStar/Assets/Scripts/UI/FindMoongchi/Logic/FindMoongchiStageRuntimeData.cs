using System.Collections.Generic;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiStageRuntimeData
    {
        public int StageId { get; }
        public int Week { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int TileCount => BoardWidth * BoardHeight;
        public IReadOnlyList<FindMoongchiTargetRuntimeData> Targets { get; }

        public FindMoongchiStageRuntimeData(
            int stageId,
            int week,
            int boardWidth,
            int boardHeight,
            IReadOnlyList<FindMoongchiTargetRuntimeData> targets)
        {
            StageId = stageId;
            Week = week;
            BoardWidth = boardWidth > 0 ? boardWidth : FindMoongchiConstants.BoardWidth;
            BoardHeight = boardHeight > 0 ? boardHeight : FindMoongchiConstants.BoardHeight;
            Targets = targets ?? new List<FindMoongchiTargetRuntimeData>();
        }
    }
}
