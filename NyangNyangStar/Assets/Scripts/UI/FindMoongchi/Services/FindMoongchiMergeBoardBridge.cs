using System.Threading.Tasks;
using UI.MergeBoard;
using UnityEngine;

namespace UI.FindMoongchi
{
    public static class FindMoongchiMergeBoardBridge
    {
        public static int GetBoardItemCountById(int itemId)
        {
            if (itemId <= 0)
            {
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] 유효하지 않은 ItemId입니다: {itemId}", DebugType.FindMoongchi);
                return 0;
            }

            BoardSystem boardSystem = Object.FindFirstObjectByType<BoardSystem>();

            if (boardSystem == null || !boardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] 보드 데이터 로드 전 수량 조회: ItemId={itemId}", DebugType.FindMoongchi);
                return 0;
            }

            int count = boardSystem.GetItemCountById(itemId);
            DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 아이템 수량 조회: ItemId={itemId}, Count={count}", DebugType.FindMoongchi);
            return count;
        }

        public static async Task<bool> ConsumeBoardItemByIdAsync(int itemId, int count = 1)
        {
            if (itemId <= 0)
            {
                DebugTool.Warning($"[FindMoongchiMergeBoardBridge] 유효하지 않은 소비 ItemId입니다: {itemId}", DebugType.FindMoongchi);
                return false;
            }

            BoardSystem boardSystem = Object.FindFirstObjectByType<BoardSystem>();

            if (boardSystem == null || !boardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning("보드 데이터 로드 전이라 탐색 도구를 소비할 수 없습니다.", DebugType.FindMoongchi);
                return false;
            }

            int safeCount = Mathf.Max(1, count);
            DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 아이템 소비 요청: ItemId={itemId}, Count={safeCount}", DebugType.FindMoongchi);

            int consumedCount = await boardSystem.ConsumeItemsByIdAsync(itemId, safeCount);
            bool success = consumedCount == safeCount;
            DebugTool.Log($"[FindMoongchiMergeBoardBridge] 보드 아이템 소비 결과: ItemId={itemId}, 요청={safeCount}, 소비={consumedCount}, Success={success}", DebugType.FindMoongchi);
            return success;
        }
    }
}
