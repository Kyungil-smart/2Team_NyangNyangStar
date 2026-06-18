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
                $"[FindMoongchiGameLogic] 스테이지 로드 완료: Stage={CurrentStageId}, Week={CurrentWeek}, Board={BoardWidth}x{BoardHeight}, Target={_targets.Count}",
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

        public static IReadOnlyList<int> GetStageIdsForWeek(int week)
        {
            return week <= 1
                ? new[] { 1, 2, 3, 4, 5 }
                : new[] { 6, 7, 8, 9, 10 };
        }

        public static List<int> CreateShuffledStageCycle(int week, string userKey, int cycleNumber = 0)
        {
            IReadOnlyList<int> source = GetStageIdsForWeek(week);
            List<int> result = new List<int>(source);

            int seed = CreateStableSeed(userKey, week, cycleNumber);
            System.Random random = new System.Random(seed);

            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            DebugTool.Log(
                $"[FindMoongchiGameLogic] 주차 스테이지 셔플 생성: Week={week}, Cycle={cycleNumber}, Seed={seed}, Order={string.Join(",", result)}",
                DebugType.FindMoongchi);

            return result;
        }

        public static int ResolveStageIdFromCycle(int week, string userKey, int cycleIndex, int cycleNumber = 0)
        {
            List<int> cycle = CreateShuffledStageCycle(week, userKey, cycleNumber);

            if (cycle.Count == 0)
                return week <= 1 ? 1 : 6;

            int safeIndex = Mathf.Abs(cycleIndex) % cycle.Count;
            return cycle[safeIndex];
        }

        private static int CreateStableSeed(string userKey, int week, int cycleNumber)
        {
            unchecked
            {
                int hash = 23;
                string safeUserKey = string.IsNullOrEmpty(userKey) ? "FindMoongchiDefaultUser" : userKey;

                for (int i = 0; i < safeUserKey.Length; i++)
                    hash = hash * 31 + safeUserKey[i];

                hash = hash * 31 + week;
                hash = hash * 31 + cycleNumber;
                return hash;
            }
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
                1 => CreateStage01(),
                2 => CreateStage02(),
                3 => CreateStage03(),
                4 => CreateStage04(),
                5 => CreateStage05(),
                6 => CreateStage06(),
                7 => CreateStage07(),
                8 => CreateStage08(),
                9 => CreateStage09(),
                10 => CreateStage10(),
                _ => CreateStage01()
            };
        }

        private static FindMoongchiStageRuntimeData CreateStage01()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                1,
                1,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(1001, "뭉치", true, CreateRectangleCells(1, 0, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(1002, "스마트폰", false, CreateRectangleCells(5, 2, 2, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetSmartphone),
                    CreateTarget(1003, "무선 이어폰 케이스", false, CreateRectangleCells(6, 6, 1, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetEarbuds)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage02()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                2,
                1,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(2001, "뭉치", true, CreateRectangleCells(1, 0, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(2002, "다이어리", false, CreateRectangleCells(5, 2, 2, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetDiary),
                    CreateTarget(2003, "빗", false, CreateRectangleCells(0, 5, 1, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetHairbrush)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage03()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                3,
                1,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(3001, "뭉치", true, CreateRectangleCells(4, 4, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(3002, "양말", false, CreateCellsFromOffsets(0, 0, width, height, (0,0), (0,1), (0,2), (1,2)), tileCount, FindMoongchiSpriteKeys.TargetSocks),
                    CreateTarget(3003, "볼펜", false, CreateRectangleCells(2, 6, 1, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetPen)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage04()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                4,
                1,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(4001, "뭉치", true, CreateRectangleCells(2, 4, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(4002, "TV 리모컨", false, CreateRectangleCells(3, 0, 1, 4, width, height), tileCount, FindMoongchiSpriteKeys.TargetRemoteControl),
                    CreateTarget(4003, "안경", false, CreateRectangleCells(4, 8, 3, 1, width, height), tileCount, FindMoongchiSpriteKeys.TargetGlasses)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage05()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                5,
                1,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(5001, "뭉치", true, CreateRectangleCells(4, 5, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(5002, "열쇠 꾸러미", false, CreateRectangleCells(1, 4, 2, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetKeys),
                    CreateTarget(5003, "두툼한 지갑", false, CreateRectangleCells(3, 1, 2, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetWallet)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage06()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                6,
                2,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(6001, "뭉치", true, CreateRectangleCells(3, 2, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(6002, "모자", false, CreateRectangleCells(0, 3, 2, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetCap),
                    CreateTarget(6003, "립밤", false, CreateRectangleCells(6, 0, 1, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetLipBalm)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage07()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                7,
                2,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(7001, "뭉치", true, CreateRectangleCells(0, 3, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(7002, "접이식 에코백", false, CreateRectangleCells(3, 0, 2, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetEcoBag),
                    CreateTarget(7003, "보조배터리", false, CreateRectangleCells(5, 4, 2, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetPowerBank)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage08()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                8,
                2,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(8001, "뭉치", true, CreateRectangleCells(4, 1, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(8002, "충전 케이블", false, CreateCellsFromOffsets(0, 6, width, height, (0,0), (0,1), (0,2), (1,2), (2,2)), tileCount, FindMoongchiSpriteKeys.TargetChargingCable),
                    CreateTarget(8003, "핸드크림", false, CreateRectangleCells(3, 3, 1, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetHandCream)
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
                    CreateTarget(9001, "뭉치", true, CreateRectangleCells(1, 4, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(9002, "무선 마우스", false, CreateRectangleCells(4, 0, 2, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetMouse),
                    CreateTarget(9003, "사원증", false, CreateRectangleCells(5, 4, 1, 4, width, height), tileCount, FindMoongchiSpriteKeys.TargetIDCard)
                });
        }

        private static FindMoongchiStageRuntimeData CreateStage10()
        {
            const int width = FindMoongchiConstants.BoardWidth;
            const int height = FindMoongchiConstants.BoardHeight;
            int tileCount = width * height;

            return new FindMoongchiStageRuntimeData(
                10,
                2,
                width,
                height,
                new List<FindMoongchiTargetRuntimeData>
                {
                    CreateTarget(10001, "뭉치", true, CreateRectangleCells(4, 5, 3, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetMoongchi),
                    CreateTarget(10002, "휴대용 물티슈", false, CreateRectangleCells(3, 1, 2, 3, width, height), tileCount, FindMoongchiSpriteKeys.TargetWetWipes),
                    CreateTarget(10003, "손톱깎이", false, CreateRectangleCells(0, 7, 1, 2, width, height), tileCount, FindMoongchiSpriteKeys.TargetNailClipper)
                });
        }

        private static FindMoongchiTargetRuntimeData CreateTarget(
            int targetId,
            string targetName,
            bool isMainTarget,
            IEnumerable<int> cellIndices,
            int maxTileCount,
            string iconKey)
        {
            return new FindMoongchiTargetRuntimeData(targetId, targetName, isMainTarget, cellIndices, maxTileCount, iconKey);
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

        private static List<int> CreateCellsFromOffsets(int startColumn, int startRow, int boardWidth, int boardHeight, params (int x, int y)[] offsets)
        {
            List<int> cells = new List<int>();

            if (offsets == null)
                return cells;

            for (int i = 0; i < offsets.Length; i++)
            {
                int column = startColumn + offsets[i].x;
                int row = startRow + offsets[i].y;

                if (column < 0 || column >= boardWidth)
                    continue;

                if (row < 0 || row >= boardHeight)
                    continue;

                cells.Add(row * boardWidth + column);
            }

            return cells;
        }
    }
}
