using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiMergeBoardItemResolver
    {
        public string GetItemName(int itemId)
        {
            if (TryGetItem(itemId, out ItemData itemData) &&
                !string.IsNullOrEmpty(itemData.ItemName))
            {
                return itemData.ItemName;
            }

            return itemId.ToString();
        }

        public Sprite GetItemSprite(int itemId)
        {
            return TryGetItem(itemId, out ItemData itemData)
                ? itemData.ItemSprite
                : null;
        }

        public int GetItemCount(int itemId)
        {
            return FindMoongchiMergeBoardBridge.GetBoardItemCountById(itemId);
        }

        private static bool TryGetItem(int itemId, out ItemData itemData)
        {
            itemData = null;

            return LocalDataAccess.Instance?.Game != null &&
                   LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out itemData) &&
                   itemData != null;
        }
    }
}
