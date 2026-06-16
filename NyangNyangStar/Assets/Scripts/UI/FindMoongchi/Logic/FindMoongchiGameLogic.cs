using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiGameLogic
    {
        private readonly HashSet<int> _revealedTileIndices = new();
        private readonly List<FindMoongchiTargetRuntimeData> _targets = new();
        private readonly int[] _toolItemIds = new int[FindMoongchiConstants.ToolSlotCount];

        public int CurrentStageId { get; private set; }
        public int CurrentWeek { get; private set; }
        public int BoardWidth { get; private set; } = FindMoongchiConstants.BoardWidth;
        public int BoardHeight { get; private set; } = FindMoongchiConstants.BoardHeight;
        public int TileCount => BoardWidth * BoardHeight;
        public int RevealedTileCount => _revealedTileIndices.Count;
        public int TargetCount => _targets.Count;
        public int FoundTargetCount => _targets.Count(target => target.IsFound);
        public bool IsStageCleared => _targets.Count > 0 && _targets.All(target => target.IsFound);
        public IReadOnlyList<FindMoongchiTargetRuntimeData> Targets => _targets;
        public IReadOnlyList<int> ToolItemIds => _toolItemIds;

        public FindMoongchiGameLogic()
        {
            ResetToolItemIdsToDefault();
        }

        public void SetToolItemIds(IReadOnlyList<int> toolItemIds)
        {
            ApplyToolItemIds(toolItemIds, _toolItemIds);

            DebugTool.Log(
                $"[FindMoongchiGameLogic] 도구 ID 적용: Row={_toolItemIds[0]}, Column={_toolItemIds[1]}, Square4x4={_toolItemIds[2]}",
                DebugType.FindMoongchi);
        }

        public void LoadStage(int stageId)
        {
            FindMoongchiStageRuntimeData stage = CreateTemporaryStage(stageId);

            CurrentStageId = stage.StageId;
            CurrentWeek = stage.Week;
            BoardWidth = stage.BoardWidth;
            BoardHeight = stage.BoardHeight;

            _revealedTileIndices.Clear();
            _targets.Clear();
            _targets.AddRange(stage.Targets);

            DebugTool.Log(
                $"[FindMoongchiGameLogic] 스테이지 로드 완료: Stage={CurrentStageId}, Board={BoardWidth}x{BoardHeight}, Target={_targets.Count}",
                DebugType.FindMoongchi);
        }

        public FindMoongchiUseToolResult UseTool(int toolItemId, int originTileIndex)
        {
            FindMoongchiUseToolResult result = new FindMoongchiUseToolResult
            {
                ToolItemId = toolItemId,
                OriginTileIndex = originTileIndex
            };

            List<int> affectedTiles = CalculateAffectedTiles(toolItemId, originTileIndex, BoardWidth, BoardHeight, _toolItemIds);
            result.AffectedTileIndices.AddRange(affectedTiles);

            foreach (int tileIndex in affectedTiles)
            {
                if (_revealedTileIndices.Add(tileIndex))
                    result.NewlyRevealedTileIndices.Add(tileIndex);
            }

            foreach (FindMoongchiTargetRuntimeData target in _targets)
            {
                if (target.RefreshFoundState(_revealedTileIndices))
                    result.NewlyFoundTargets.Add(target);
            }

            result.IsStageCleared = IsStageCleared;
            return result;
        }

        public HashSet<int> GetRevealedTileSet()
        {
            return new HashSet<int>(_revealedTileIndices);
        }

        public static List<int> CalculateAffectedTiles(int toolItemId, int tileIndex, int boardWidth, int boardHeight)
        {
            return CalculateAffectedTiles(toolItemId, tileIndex, boardWidth, boardHeight, FindMoongchiConstants.ToolIds);
        }

        public static List<int> CalculateAffectedTiles(
            int toolItemId,
            int tileIndex,
            int boardWidth,
            int boardHeight,
            IReadOnlyList<int> toolItemIds)
        {
            List<int> result = new List<int>();

            if (boardWidth <= 0 || boardHeight <= 0)
                return result;

            int tileCount = boardWidth * boardHeight;

            if (tileIndex < 0 || tileIndex >= tileCount)
                return result;

            int row = tileIndex / boardWidth;
            int column = tileIndex % boardWidth;
            FindMoongchiToolPattern pattern = GetPattern(toolItemId, toolItemIds);

            switch (pattern)
            {
                case FindMoongchiToolPattern.Row:
                    AddRowAffectedTiles(result, row, boardWidth);
                    break;

                case FindMoongchiToolPattern.Column:
                    AddColumnAffectedTiles(result, column, boardWidth, boardHeight);
                    break;

                case FindMoongchiToolPattern.Square4x4:
                    AddRectangleAffectedTiles(
                        result,
                        originColumn: column,
                        originRow: row,
                        areaWidth: 4,
                        areaHeight: 4,
                        anchorX: 1,
                        anchorY: 2,
                        boardWidth,
                        boardHeight);
                    break;
            }

            return result;
        }

        public static FindMoongchiToolPattern GetPattern(int toolItemId)
        {
            return GetPattern(toolItemId, FindMoongchiConstants.ToolIds);
        }

        public static FindMoongchiToolPattern GetPattern(int toolItemId, IReadOnlyList<int> toolItemIds)
        {
            int[] resolvedToolIds = new int[FindMoongchiConstants.ToolSlotCount];
            ApplyToolItemIds(toolItemIds, resolvedToolIds);

            if (toolItemId == resolvedToolIds[0])
                return FindMoongchiToolPattern.Row;

            if (toolItemId == resolvedToolIds[1])
                return FindMoongchiToolPattern.Column;

            if (toolItemId == resolvedToolIds[2])
                return FindMoongchiToolPattern.Square4x4;

            DebugTool.Warning($"[FindMoongchiGameLogic] 등록되지 않은 도구 ID입니다. 기본 Row 패턴으로 처리합니다. ToolId={toolItemId}", DebugType.FindMoongchi);
            return FindMoongchiToolPattern.Row;
        }

        private static void AddRowAffectedTiles(List<int> result, int row, int boardWidth)
        {
            for (int x = 0; x < boardWidth; x++)
                result.Add(row * boardWidth + x);
        }

        private static void AddColumnAffectedTiles(List<int> result, int column, int boardWidth, int boardHeight)
        {
            for (int y = 0; y < boardHeight; y++)
                result.Add(y * boardWidth + column);
        }

        private static void AddRectangleAffectedTiles(
            List<int> result,
            int originColumn,
            int originRow,
            int areaWidth,
            int areaHeight,
            int anchorX,
            int anchorY,
            int boardWidth,
            int boardHeight)
        {
            int safeAreaWidth = Mathf.Min(areaWidth, boardWidth);
            int safeAreaHeight = Mathf.Min(areaHeight, boardHeight);

            int startColumn = Mathf.Clamp(originColumn - anchorX, 0, boardWidth - safeAreaWidth);
            int startRow = Mathf.Clamp(originRow - anchorY, 0, boardHeight - safeAreaHeight);

            for (int y = 0; y < safeAreaHeight; y++)
            {
                for (int x = 0; x < safeAreaWidth; x++)
                    result.Add((startRow + y) * boardWidth + (startColumn + x));
            }
        }

        private void ResetToolItemIdsToDefault()
        {
            ApplyToolItemIds(FindMoongchiConstants.DefaultToolItemIds, _toolItemIds);
        }

        private static void ApplyToolItemIds(IReadOnlyList<int> source, int[] target)
        {
            if (target == null || target.Length < FindMoongchiConstants.ToolSlotCount)
                return;

            IReadOnlyList<int> defaults = FindMoongchiConstants.DefaultToolItemIds;

            for (int i = 0; i < FindMoongchiConstants.ToolSlotCount; i++)
            {
                int value = source != null && i < source.Count ? source[i] : 0;

                if (value <= 0)
                    value = defaults[i];

                target[i] = value;
            }
        }

        private static FindMoongchiStageRuntimeData CreateTemporaryStage(int stageId)
        {
            return stageId switch
            {
                9 => CreateStage09(),
                _ => CreateStage08()
            };
        }

        private static FindMoongchiStageRuntimeData CreateStage08()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                8,
                1,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    new(8001, "뭉치", true, CreateRectangleCells(1, 1, 3, 3, width, height), tileCount),
                    new(8002, "스마트폰", false, CreateRectangleCells(5, 2, 1, 3, width, height), tileCount),
                    new(8003, "리모컨", false, CreateRectangleCells(0, 6, 4, 1, width, height), tileCount)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage09()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                9,
                2,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    new(9001, "뭉치", true, CreateRectangleCells(3, 1, 3, 3, width, height), tileCount),
                    new(9002, "다이어리", false, CreateRectangleCells(0, 3, 2, 3, width, height), tileCount),
                    new(9003, "열쇠", false, CreateRectangleCells(5, 6, 2, 2, width, height), tileCount)
                });
        }

        private static List<int> CreateRectangleCells(int startColumn, int startRow, int width, int height, int boardWidth, int boardHeight)
        {
            List<int> cells = new List<int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int column = startColumn + x;
                    int row = startRow + y;

                    if (column < 0 || column >= boardWidth)
                        continue;

                    if (row < 0 || row >= boardHeight)
                        continue;

                    cells.Add(row * boardWidth + column);
                }
            }

            return cells;
        }
    }
}
