using System.Collections.Generic;
using System.Linq;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiTargetRuntimeData
    {
        private readonly HashSet<int> _cellIndices = new();

        public int TargetId { get; }
        public string TargetName { get; }
        public bool IsMainTarget { get; }
        public bool IsFound { get; private set; }
        public IReadOnlyCollection<int> CellIndices => _cellIndices;

        public FindMoongchiTargetRuntimeData(
            int targetId,
            string targetName,
            bool isMainTarget,
            IEnumerable<int> cellIndices,
            int maxTileCount = FindMoongchiConstants.TileCount)
        {
            TargetId = targetId;
            TargetName = targetName;
            IsMainTarget = isMainTarget;

            if (cellIndices == null)
                return;

            foreach (int cellIndex in cellIndices)
            {
                if (cellIndex < 0 || cellIndex >= maxTileCount)
                    continue;

                _cellIndices.Add(cellIndex);
            }
        }

        public bool RefreshFoundState(HashSet<int> revealedTileIndices)
        {
            if (IsFound)
                return false;

            if (_cellIndices.Count == 0 || revealedTileIndices == null)
                return false;

            bool isNowFound = _cellIndices.All(revealedTileIndices.Contains);

            if (!isNowFound)
                return false;

            IsFound = true;
            return true;
        }
    }
}
