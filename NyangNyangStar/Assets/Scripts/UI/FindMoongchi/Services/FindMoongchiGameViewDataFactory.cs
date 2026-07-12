using System.Collections.Generic;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiGameViewDataFactory
    {
        private readonly FindMoongchiMergeBoardItemResolver _itemResolver;

        public FindMoongchiGameViewDataFactory(FindMoongchiMergeBoardItemResolver itemResolver)
        {
            _itemResolver = itemResolver;
        }

        public FindMoongchiGameViewData Build(
            FindMoongchiGameLogic gameLogic,
            IReadOnlyList<int> toolItemIds,
            int currentWeek,
            string remainTimeText,
            int searchChance,
            int energySpendProgress,
            bool debugInfiniteToolUse,
            int debugToolDisplayCount)
        {
            if (gameLogic == null)
                return new FindMoongchiGameViewData();

            int visibleSearchChance = debugInfiniteToolUse ? Mathf.Max(1, debugToolDisplayCount) : searchChance;

            FindMoongchiGameViewData data = new FindMoongchiGameViewData
            {
                StageId = gameLogic.CurrentStageId,
                BoardWidth = gameLogic.BoardWidth,
                BoardHeight = gameLogic.BoardHeight,
                CurrentWeek = currentWeek,
                RemainTimeText = remainTimeText,
                SearchChance = visibleSearchChance,
                EnergySpendProgress = energySpendProgress,
                EnergySpendTarget = FindMoongchiConstants.EnergySpendTarget,
                RevealedTileIndices = gameLogic.GetRevealedTileSet()
            };

            AddTools(data, toolItemIds, searchChance, debugInfiniteToolUse, debugToolDisplayCount);
            AddTargets(data, gameLogic.Targets);

            return data;
        }

        private void AddTools(
            FindMoongchiGameViewData data,
            IReadOnlyList<int> toolItemIds,
            int searchChance,
            bool debugInfiniteToolUse,
            int debugToolDisplayCount)
        {
            if (data == null || toolItemIds == null)
                return;

            for (int i = 0; i < toolItemIds.Count; i++)
            {
                int toolId = toolItemIds[i];
                int count = debugInfiniteToolUse
                    ? Mathf.Max(1, debugToolDisplayCount)
                    : _itemResolver?.GetItemCount(toolId) ?? 0;

                data.Tools.Add(new FindMoongchiToolViewData
                {
                    ToolItemId = toolId,
                    ToolName = _itemResolver?.GetItemName(toolId) ?? toolId.ToString(),
                    Icon = _itemResolver?.GetItemSprite(toolId),
                    Count = count,
                    IsUsable = debugInfiniteToolUse || searchChance > 0
                });
            }
        }

        private static void AddTargets(
            FindMoongchiGameViewData data,
            IReadOnlyList<FindMoongchiTargetRuntimeData> targets)
        {
            if (data == null || targets == null)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                FindMoongchiTargetRuntimeData target = targets[i];

                if (target == null)
                    continue;

                data.TargetHints.Add(new FindMoongchiTargetHintViewData
                {
                    TargetName = target.TargetName,
                    Icon = null,
                    IconKey = target.IconKey,
                    IsFound = target.IsFound
                });

                data.TargetVisuals.Add(new FindMoongchiTargetVisualViewData
                {
                    TargetId = target.TargetId,
                    TargetName = target.TargetName,
                    Icon = null,
                    IconKey = target.IconKey,
                    IsFound = target.IsFound,
                    CellIndices = new List<int>(target.CellIndices)
                });
            }
        }
    }
}
