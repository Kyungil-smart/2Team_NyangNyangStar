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
                return 0;

            BoardSystem boardSystem = Object.FindFirstObjectByType<BoardSystem>();

            if (boardSystem == null || !boardSystem.IsServerDataLoaded)
                return 0;

            return boardSystem.GetItemCountById(itemId);
        }

        public static async Task<bool> ConsumeBoardItemByIdAsync(int itemId, int count = 1)
        {
            if (itemId <= 0)
                return false;

            BoardSystem boardSystem = Object.FindFirstObjectByType<BoardSystem>();

            if (boardSystem == null || !boardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning("보드 데이터 로드 전이라 탐색 도구를 소비할 수 없습니다.", DebugType.Board);
                return false;
            }

            int consumedCount = await boardSystem.ConsumeItemsByIdAsync(itemId, Mathf.Max(1, count));
            return consumedCount == Mathf.Max(1, count);
        }
    }
}
