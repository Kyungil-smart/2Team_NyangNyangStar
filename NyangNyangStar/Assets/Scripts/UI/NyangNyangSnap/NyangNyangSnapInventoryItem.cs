using System.Collections.Generic;
using Data.ScriptableObjects.MergeBoard;

public class NyangNyangSnapInventoryItem
{
    public IReadOnlyList<int> SlotNumbers { get; }
    public ItemData ItemData { get; }
    public int Count { get; }
    public NyangNyangSnapToolData ToolData { get; }

    public int SlotNumber => SlotNumbers != null && SlotNumbers.Count > 0 ? SlotNumbers[0] : 0;
    public int ItemID => ItemData != null ? ItemData.ItemID : 0;
    public string ItemName => ItemData != null ? ItemData.ItemName : string.Empty;
    public string AddressableKey => ItemData != null ? ItemData.AddressableKey : string.Empty;
    public NyangNyangSnapToolType ToolType => ToolData != null ? ToolData.ItemToolType : NyangNyangSnapToolType.None;

    public NyangNyangSnapInventoryItem(
        IReadOnlyList<int> slotNumbers,
        ItemData itemData,
        int count,
        NyangNyangSnapToolData toolData)
    {
        SlotNumbers = slotNumbers;
        ItemData = itemData;
        Count = count;
        ToolData = toolData;
    }
}