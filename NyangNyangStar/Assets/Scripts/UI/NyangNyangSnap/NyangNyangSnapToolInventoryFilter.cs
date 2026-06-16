using System.Collections.Generic;
using Data.ScriptableObjects.MergeBoard;

public static class NyangNyangSnapToolInventoryFilter
{
    /// <summary>
    /// MergeBoard 슬롯 중 냥냥스냅 장난감만 가져옵니다.
    /// 같은 ItemID가 여러 슬롯에 있으면 하나로 묶어서 Count로 표시합니다.
    /// </summary>
    public static List<NyangNyangSnapInventoryItem> GetToyItems(
        Dictionary<int, ItemData> boardSlots,
        NyangNyangSnapToolSO toolSO)
    {
        return GetItemsByTypes(
            boardSlots,
            toolSO,
            NyangNyangSnapToolType.Toy
        );
    }

    /// <summary>
    /// MergeBoard 슬롯 중 냥냥스냅 간식/음식만 가져옵니다.
    /// 같은 ItemID가 여러 슬롯에 있으면 하나로 묶어서 Count로 표시합니다.
    /// </summary>
    public static List<NyangNyangSnapInventoryItem> GetSnackItems(
        Dictionary<int, ItemData> boardSlots,
        NyangNyangSnapToolSO toolSO)
    {
        return GetItemsByTypes(
            boardSlots,
            toolSO,
            NyangNyangSnapToolType.Snack,
            NyangNyangSnapToolType.Food
        );
    }

    /// <summary>
    /// MergeBoard에서 원하는 냥냥스냅 도구 타입만 필터링합니다.
    /// </summary>
    private static List<NyangNyangSnapInventoryItem> GetItemsByTypes(
        Dictionary<int, ItemData> boardSlots,
        NyangNyangSnapToolSO toolSO,
        params NyangNyangSnapToolType[] targetTypes)
    {
        List<NyangNyangSnapInventoryItem> result = new();

        if (boardSlots == null || boardSlots.Count == 0)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToolInventoryFilter] MergeBoard 슬롯 데이터가 비어있습니다.",
                DebugType.UI
            );
            return result;
        }

        if (toolSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapToolInventoryFilter] NyangNyangSnapToolSO가 없습니다.",
                DebugType.UI
            );
            return result;
        }

        Dictionary<int, List<int>> slotNumbersByItemId = new();
        Dictionary<int, ItemData> itemDataByItemId = new();
        Dictionary<int, NyangNyangSnapToolData> toolDataByItemId = new();

        foreach (var pair in boardSlots)
        {
            int slotNumber = pair.Key;
            ItemData itemData = pair.Value;

            if (itemData == null || !itemData.HasItem)
                continue;

            if (!toolSO.TryGetToolDataByItemID(itemData.ItemID, out NyangNyangSnapToolData toolData))
                continue;

            if (!ContainsToolType(targetTypes, toolData.ItemToolType))
                continue;

            if (!slotNumbersByItemId.ContainsKey(itemData.ItemID))
            {
                slotNumbersByItemId[itemData.ItemID] = new List<int>();
                itemDataByItemId[itemData.ItemID] = itemData;
                toolDataByItemId[itemData.ItemID] = toolData;
            }

            slotNumbersByItemId[itemData.ItemID].Add(slotNumber);
        }

        foreach (var pair in slotNumbersByItemId)
        {
            int itemId = pair.Key;
            List<int> slotNumbers = pair.Value;

            result.Add(new NyangNyangSnapInventoryItem(
                slotNumbers,
                itemDataByItemId[itemId],
                slotNumbers.Count,
                toolDataByItemId[itemId]
            ));
        }

        DebugTool.Log(
            $"[NyangNyangSnapToolInventoryFilter] MergeBoard 필터링 완료 / 개수:{result.Count}",
            DebugType.UI
        );

        return result;
    }

    /// <summary>
    /// 대상 타입 목록에 현재 타입이 포함되어 있는지 확인합니다.
    /// </summary>
    private static bool ContainsToolType(
        NyangNyangSnapToolType[] targetTypes,
        NyangNyangSnapToolType currentType)
    {
        if (targetTypes == null || targetTypes.Length == 0)
            return false;

        for (int i = 0; i < targetTypes.Length; i++)
        {
            if (targetTypes[i] == currentType)
                return true;
        }

        return false;
    }
}