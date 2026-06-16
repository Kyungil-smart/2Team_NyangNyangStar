using System.Collections.Generic;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiUseToolResult
    {
        public int ToolItemId { get; set; }
        public int OriginTileIndex { get; set; }
        public List<int> AffectedTileIndices { get; } = new();
        public List<int> NewlyRevealedTileIndices { get; } = new();
        public List<FindMoongchiTargetRuntimeData> NewlyFoundTargets { get; } = new();
        public bool IsStageCleared { get; set; }
    }
}
