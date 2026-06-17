using System.Collections.Generic;
using System.Threading.Tasks;
using UI.MergeBoard;
using UnityEngine;

namespace UI.FindMoongchi
{
    public static class FindMoongchiMergeBoardBridge
    {
        private static BoardSystem _cachedBoardSystem;
        private static readonly Dictionary<int, int> _lastBoardItemCounts = new();

        public static int GetBoardItemCountById(int itemId)
        {
            if (itemId <= 0)
            {
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] 유효하지 않은 ItemId입니다: {itemId}", DebugType.FindMoongchi);
                return 0;
            }

            BoardSystem boardSystem = ResolveBoardSystem();

            if (boardSystem == null)
            {
                int cachedCount = GetCachedCount(itemId);
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] BoardSystem을 찾지 못했습니다. 캐시 수량 사용: ItemId={itemId}, Count={cachedCount}", DebugType.FindMoongchi);
                return cachedCount;
            }

            int currentCount = boardSystem.GetItemCountById(itemId);

            if (!boardSystem.IsServerDataLoaded)
            {
                int cachedCount = GetCachedCount(itemId);

                // 보드가 저장/재로드 중이면 IsServerDataLoaded가 false가 될 수 있다.
                // 이때 0으로 덮어쓰면 FindMoongchi 도구 UI가 전부 0개로 표시된다.
                // 로컬 보드 딕셔너리에 값이 있으면 그 값을 사용하고, 로컬 값도 0이면 마지막 정상 수량을 유지한다.
                if (currentCount <= 0 && cachedCount > 0)
                {
                    DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 로드 플래그 false / 캐시 수량 유지: ItemId={itemId}, Count={cachedCount}", DebugType.FindMoongchi);
                    return cachedCount;
                }

                CacheCount(itemId, currentCount);
                DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 로드 플래그 false / 로컬 보드 수량 사용: ItemId={itemId}, Count={currentCount}", DebugType.FindMoongchi);
                return currentCount;
            }

            CacheCount(itemId, currentCount);
            DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 아이템 수량 조회: ItemId={itemId}, Count={currentCount}", DebugType.FindMoongchi);
            return currentCount;
        }

        public static async Task<bool> ConsumeBoardItemByIdAsync(int itemId, int count = 1)
        {
            if (itemId <= 0)
            {
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] 유효하지 않은 소비 ItemId입니다: {itemId}", DebugType.FindMoongchi);
                return false;
            }

            BoardSystem boardSystem = ResolveBoardSystem();

            if (boardSystem == null || !boardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning("[FindMoongchiMergeBoardBridge] 보드 데이터 로드 전이라 탐색 도구를 소비할 수 없습니다.", DebugType.FindMoongchi);
                return false;
            }

            int safeCount = Mathf.Max(1, count);
            DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 아이템 소비 요청: ItemId={itemId}, Count={safeCount}", DebugType.FindMoongchi);

            int consumedCount = await boardSystem.ConsumeItemsByIdAsync(itemId, safeCount);
            bool success = consumedCount == safeCount;

            if (success)
            {
                int currentCount = boardSystem.GetItemCountById(itemId);
                CacheCount(itemId, currentCount);
            }

            DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 아이템 소비 결과: ItemId={itemId}, 요청={safeCount}, 소비={consumedCount}, Success={success}", DebugType.FindMoongchi);
            return success;
        }

        public static void ClearCache()
        {
            _cachedBoardSystem = null;
            _lastBoardItemCounts.Clear();
            DebugTool.Log("[FindMoongchiMergeBoardBridge] 보드 수량 캐시 초기화", DebugType.FindMoongchi);
        }

        private static BoardSystem ResolveBoardSystem()
        {
            if (_cachedBoardSystem != null)
                return _cachedBoardSystem;

            _cachedBoardSystem = Object.FindFirstObjectByType<BoardSystem>();

            if (_cachedBoardSystem != null)
                return _cachedBoardSystem;

            BoardSystem[] boardSystems = Resources.FindObjectsOfTypeAll<BoardSystem>();

            for (int i = 0; i < boardSystems.Length; i++)
            {
                BoardSystem boardSystem = boardSystems[i];

                if (boardSystem == null || boardSystem.gameObject == null)
                    continue;

                if (!boardSystem.gameObject.scene.IsValid())
                    continue;

                _cachedBoardSystem = boardSystem;
                DebugTool.Log($"[FindMoongchiMergeBoardBridge] 비활성 포함 BoardSystem 캐시: {_cachedBoardSystem.name}", DebugType.FindMoongchi);
                return _cachedBoardSystem;
            }

            return null;
        }

        private static void CacheCount(int itemId, int count)
        {
            _lastBoardItemCounts[itemId] = Mathf.Max(0, count);
        }

        private static int GetCachedCount(int itemId)
        {
            return _lastBoardItemCounts.TryGetValue(itemId, out int count)
                ? count
                : 0;
        }
    }
}
