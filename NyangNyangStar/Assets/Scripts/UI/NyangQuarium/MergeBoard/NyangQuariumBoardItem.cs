using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumBoardItem -> 보드 슬롯에 올라가는 아이템 래퍼
    public sealed class NyangQuariumBoardItem
    {
        public static readonly NyangQuariumBoardItem Empty = new(ItemData.Empty);

        public ItemData ItemData { get; }
        public int Id => ItemData?.ItemID ?? 0;
        public string Name => ItemData?.ItemName ?? string.Empty;
        public int Level => ItemData?.ItemLevel ?? 0;
        public bool HasItem => ItemData != null && ItemData.HasItem;

        public NyangQuariumBoardItem(ItemData itemData)
        {
            ItemData = itemData?.Clone() ?? ItemData.Empty;
        }
    }
}
