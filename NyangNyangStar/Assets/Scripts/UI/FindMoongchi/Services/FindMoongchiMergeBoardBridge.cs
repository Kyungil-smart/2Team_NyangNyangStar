using System;
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
            if (!IsValidItemId(itemId))
                return 0;

            if (TryGetOwnedCountFromItemService(itemId, out int serviceCount) && serviceCount > 0)
            {
                CacheCount(itemId, serviceCount);
                DebugTool.Log($"[FindMoongchiMergeBoardBridge] Service count: ItemId={itemId}, Count={serviceCount}", DebugType.FindMoongchi);
                return serviceCount;
            }

            BoardSystem boardSystem = ResolveBoardSystem();

            if (boardSystem == null)
            {
                int cachedCount = GetCachedCount(itemId);
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] BoardSystem missing. Using cached count: ItemId={itemId}, Count={cachedCount}", DebugType.FindMoongchi);
                return cachedCount;
            }

            int boardCount = boardSystem.GetItemCountById(itemId);

            if (!boardSystem.IsServerDataLoaded)
            {
                int cachedCount = GetCachedCount(itemId);

                if (boardCount <= 0 && cachedCount > 0)
                {
                    DebugTool.Log($"[FindMoongchiMergeBoardBridge] Board not loaded. Keeping cached count: ItemId={itemId}, Count={cachedCount}", DebugType.FindMoongchi);
                    return cachedCount;
                }

                CacheCount(itemId, boardCount);
                DebugTool.Log($"[FindMoongchiMergeBoardBridge] Board not loaded. Using local board count: ItemId={itemId}, Count={boardCount}", DebugType.FindMoongchi);
                return boardCount;
            }

            CacheCount(itemId, boardCount);
            DebugTool.Log($"[FindMoongchiMergeBoardBridge] Board count: ItemId={itemId}, Count={boardCount}", DebugType.FindMoongchi);
            return boardCount;
        }

        public static async Task<bool> ConsumeBoardItemByIdAsync(int itemId, int count = 1)
        {
            if (!IsValidItemId(itemId))
                return false;

            int safeCount = Mathf.Max(1, count);
            MergeBoardItemService itemService = MergeBoardItemService.Instance;

            if (itemService != null)
            {
                try
                {
                    await itemService.EnsureInventoryLoadedAsync();

                    int ownedCount = itemService.GetOwnedItemCount(itemId);

                    if (ownedCount >= safeCount)
                    {
                        bool consumed = await itemService.ConsumeItemByIdAsync(itemId, safeCount);

                        if (consumed)
                        {
                            CacheCount(itemId, itemService.GetOwnedItemCount(itemId));
                            DebugTool.Log($"[FindMoongchiMergeBoardBridge] Service consume success: ItemId={itemId}, Count={safeCount}", DebugType.FindMoongchi);
                            return true;
                        }

                        DebugTool.Warning($"[FindMoongchiMergeBoardBridge] Service consume failed: ItemId={itemId}, Count={safeCount}", DebugType.FindMoongchi);
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    DebugTool.Warning($"[FindMoongchiMergeBoardBridge] Service consume exception: {exception.Message}", DebugType.FindMoongchi);
                }
            }

            BoardSystem boardSystem = ResolveBoardSystem();

            if (boardSystem == null || !boardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning("[FindMoongchiMergeBoardBridge] Board data is not ready. Cannot consume FindMoongchi tool.", DebugType.FindMoongchi);
                return false;
            }

            DebugTool.Log($"[FindMoongchiMergeBoardBridge] Board consume request: ItemId={itemId}, Count={safeCount}", DebugType.FindMoongchi);

            int consumedCount = await boardSystem.ConsumeItemsByIdAsync(itemId, safeCount);
            bool success = consumedCount == safeCount;

            if (success)
                CacheCount(itemId, boardSystem.GetItemCountById(itemId));

            DebugTool.Log($"[FindMoongchiMergeBoardBridge] Board consume result: ItemId={itemId}, Request={safeCount}, Consumed={consumedCount}, Success={success}", DebugType.FindMoongchi);
            return success;
        }

        public static void ClearCache()
        {
            _cachedBoardSystem = null;
            _lastBoardItemCounts.Clear();
            DebugTool.Log("[FindMoongchiMergeBoardBridge] Count cache cleared.", DebugType.FindMoongchi);
        }

        private static bool IsValidItemId(int itemId)
        {
            if (itemId > 0)
                return true;

            DebugTool.Warning($"[FindMoongchiMergeBoardBridge] Invalid ItemId: {itemId}", DebugType.FindMoongchi);
            return false;
        }

        private static bool TryGetOwnedCountFromItemService(int itemId, out int count)
        {
            count = 0;

            MergeBoardItemService itemService = MergeBoardItemService.Instance;

            if (itemService == null)
                return false;

            count = Mathf.Max(0, itemService.GetOwnedItemCount(itemId));
            return true;
        }

        private static BoardSystem ResolveBoardSystem()
        {
            if (_cachedBoardSystem != null)
                return _cachedBoardSystem;

            _cachedBoardSystem = UnityEngine.Object.FindFirstObjectByType<BoardSystem>();

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
                DebugTool.Log($"[FindMoongchiMergeBoardBridge] Cached inactive BoardSystem: {_cachedBoardSystem.name}", DebugType.FindMoongchi);
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
