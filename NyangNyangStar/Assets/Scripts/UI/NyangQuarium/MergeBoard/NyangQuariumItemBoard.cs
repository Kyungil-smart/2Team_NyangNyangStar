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
    [DefaultExecutionOrder(-50)]
    public sealed class NyangQuariumItemBoard : MonoBehaviour
    {
        private const int Width = 7;
        private const int Height = 9;
        private const int SlotSize = 135;
        private const int SlotSpacing = 9;
        private const int ItemSize = 85;
        private const string SlotRootObjectName = "@Slot Root";

        private readonly List<NyangQuariumItemSlot> _slots = new();
        private NyangQuariumItemInfoPanel _infoPanel;
        private NyangQuariumItemSlot _selectedSlot;
        private Transform _slotRoot;
        private bool _initialized;

        private void Awake()
        {
            // BoardSystem.Start()보다 먼저 비활성화해서 메인 보드 @Slot Root 오염을 방지합니다.
            DisableMergeBoardSystem();
        }

        public void Init(NyangQuariumItemInfoPanel infoPanel)
        {
            _infoPanel = infoPanel;

            if (_initialized)
                return;

            // 메인 머지보드 BoardSystem 끄고 냥쿠 전용 슬롯으로 대체
            DisableMergeBoardSystem();
            SetupSlotRoot();
            GenerateSlots();
            ClearSelection();
            NyangQuariumMergeBoardInventoryService.RegisterBoard(this);
            _initialized = true;
        }

        private void OnDestroy()
        {
            NyangQuariumMergeBoardInventoryService.UnregisterBoard(this);
        }

        // InventoryService.GetOwnedFishCount — 슬롯 전체에서 fishId 개수 세기
        public int GetOwnedFishCount(int fishId)
        {
            if (fishId <= 0)
                return 0;

            int count = 0;

            for (int i = 0; i < _slots.Count; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot != null && slot.HasItem && slot.Item.Id == fishId)
                    count++;
            }

            return count;
        }

        // InventoryService.CopyOwnedFishEntries — 왼쪽 슬롯부터 maxCount개, FishSO에 없는 아이템은 스킵
        public void CopyOwnedFishEntries(
            List<NyangQuariumMergeBoardFishEntry> results,
            FishType aquariumType,
            int maxCount)
        {
            if (results == null)
                return;

            results.Clear();

            if (maxCount <= 0)
                return;

            for (int i = 0; i < _slots.Count && results.Count < maxCount; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem)
                    continue;

                NyangQuariumBoardItem item = slot.Item;

                if (!TryResolveFishData(item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (!NyangQuariumMergeBoardInventoryService.CanPlaceFish(
                        fishData.FishType,
                        aquariumType))
                {
                    continue;
                }

                results.Add(new NyangQuariumMergeBoardFishEntry(
                    i,
                    item.Id,
                    item.Level,
                    fishData.FishKey,
                    item.Name,
                    fishData.FishType,
                    item.ItemData?.ItemSprite));
            }
        }

        public int GetOwnedNatureCount(int itemId)
        {
            if (itemId <= 0)
                return 0;

            int count = 0;

            for (int i = 0; i < _slots.Count; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem || slot.Item.Id != itemId)
                    continue;

                if (!TryResolveFishData(slot.Item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                count++;
            }

            return count;
        }

        public void CopyOwnedNatureEntries(
            List<NyangQuariumMergeBoardNatureEntry> results,
            int maxCount)
        {
            if (results == null)
                return;

            results.Clear();

            if (maxCount <= 0)
                return;

            for (int i = 0; i < _slots.Count && results.Count < maxCount; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem)
                    continue;

                NyangQuariumBoardItem item = slot.Item;

                if (!TryResolveFishData(item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                results.Add(new NyangQuariumMergeBoardNatureEntry(
                    i,
                    item.Id,
                    item.Level,
                    fishData.FishKey,
                    item.Name,
                    item.ItemData?.ItemSprite));
            }
        }

        public bool TryConsumeNature(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0)
                return false;

            int remaining = count;

            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem || slot.Item.Id != itemId)
                    continue;

                if (!TryResolveFishData(slot.Item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                if (!TryClearSlot(i))
                    continue;

                remaining--;
            }

            return remaining <= 0;
        }

        public bool TryConsumeNatureAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;

            NyangQuariumItemSlot slot = _slots[slotIndex];

            if (slot == null || !slot.HasItem)
                return false;

            if (!TryResolveFishData(slot.Item.Id, out NyangQuariumFishData fishData))
                return false;

            return fishData.FishType == FishType.Environments && TryClearSlot(slotIndex);
        }

        // InventoryService.TryConsumeFish — fishId 같은 슬롯을 왼쪽부터 count만큼 비움
        public bool TryConsumeFish(int fishId, int count)
        {
            if (fishId <= 0 || count <= 0)
                return false;

            int remaining = count;

            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem || slot.Item.Id != fishId)
                    continue;

                if (!TryClearSlot(i))
                    continue;

                remaining--;
            }

            return remaining <= 0;
        }

        // 슬롯 1칸 비우기 — 수조 배치 후 머지보드에서 사라지는 처리
        public bool TryClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;

            NyangQuariumItemSlot slot = _slots[slotIndex];

            if (slot == null || !slot.HasItem)
                return false;

            if (_selectedSlot == slot)
                ClearSelection();

            slot.SetItem(NyangQuariumBoardItem.Empty);
            return true;
        }

        private static bool TryResolveFishData(int fishId, out NyangQuariumFishData fishData)
        {
            fishData = null;

            SheetLoader sheetLoader = UnityEngine.Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader == null ||
                !sheetLoader.TryGetNyangQuariumFishSO(out NyangQuariumFishSO fishSO) ||
                fishSO.FishData == null)
            {
                return false;
            }

            for (int i = 0; i < fishSO.FishData.Count; i++)
            {
                NyangQuariumFishData data = fishSO.FishData[i];

                if (data == null || data.FishId != fishId)
                    continue;

                fishData = data;
                return true;
            }

            return false;
        }

        // 왼쪽부터 빈 슬롯 찾아서 아이템 넣기
        public bool TryAddItem(NyangQuariumBoardItem item)
        {
            if (item == null || !item.HasItem)
                return false;

            for (int i = 0; i < _slots.Count; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];
                if (slot == null || slot.HasItem)
                    continue;

                slot.SetItem(item);
                NyangQuariumMergeBoardInventoryService.NotifyInventoryChanged();
                return true;
            }

            DebugTool.Warning("[NyangQuariumItemBoard] 빈 슬롯이 없습니다.", DebugType.UI, this);
            return false;
        }

        // 슬롯 클릭 시 선택 표시 + 하단 Info 패널 갱신
        public void SelectSlot(NyangQuariumItemSlot slot)
        {
            if (slot == null || !slot.HasItem)
            {
                ClearSelection();
                return;
            }

            if (_selectedSlot != null)
                _selectedSlot.SetSelected(false);

            _selectedSlot = slot;
            _selectedSlot.SetSelected(true);

            if (_infoPanel != null)
                _infoPanel.Show(slot.Item);
        }

        private void ClearSelection()
        {
            if (_selectedSlot != null)
                _selectedSlot.SetSelected(false);

            _selectedSlot = null;

            if (_infoPanel != null)
                _infoPanel.Hide();
        }

        // 같은 오브젝트에 붙은 메인 BoardSystem 비활성화
        private void DisableMergeBoardSystem()
        {
            BoardSystem[] boardSystems = GetComponents<BoardSystem>();

            for (int i = 0; i < boardSystems.Length; i++)
            {
                BoardSystem boardSystem = boardSystems[i];

                if (boardSystem == null)
                    continue;

                boardSystem.enabled = false;
            }
        }

        // @Slot Root 없으면 만들고 GridLayoutGroup 7열로 맞춤
        private void SetupSlotRoot()
        {
            Transform existingRoot = transform.Find(SlotRootObjectName);
            if (existingRoot != null)
            {
                _slotRoot = existingRoot;
            }
            else
            {
                GameObject slotRootObject = new("@Slot Root", typeof(RectTransform));
                slotRootObject.transform.SetParent(transform, false);
                _slotRoot = slotRootObject.transform;
            }

            RectTransform slotRootRect = _slotRoot as RectTransform;
            if (slotRootRect != null)
            {
                slotRootRect.anchorMin = Vector2.zero;
                slotRootRect.anchorMax = Vector2.one;
                slotRootRect.offsetMin = Vector2.zero;
                slotRootRect.offsetMax = Vector2.zero;
                slotRootRect.pivot = new Vector2(0.5f, 0.5f);
            }

            GridLayoutGroup grid = _slotRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = _slotRoot.gameObject.AddComponent<GridLayoutGroup>();

            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Width;
            grid.cellSize = new Vector2(SlotSize, SlotSize);
            grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
        }

        // 63칸 슬롯 런타임 생성 (배경 Image + 아이템 Image)
        private void GenerateSlots()
        {
            for (int i = _slotRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _slotRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            _slots.Clear();

            int slotCount = Width * Height;
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObject = new($"Slot_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                slotObject.transform.SetParent(_slotRoot, false);
                slotObject.layer = gameObject.layer;

                Image backgroundImage = slotObject.GetComponent<Image>();
                backgroundImage.color = new Color(1f, 1f, 1f, 0.25f);
                backgroundImage.raycastTarget = true;

                GameObject itemImageObject = new("Item Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                itemImageObject.transform.SetParent(slotObject.transform, false);
                itemImageObject.layer = gameObject.layer;

                RectTransform itemRect = itemImageObject.transform as RectTransform;
                itemRect.anchorMin = new Vector2(0.5f, 0.5f);
                itemRect.anchorMax = new Vector2(0.5f, 0.5f);
                itemRect.sizeDelta = new Vector2(ItemSize, ItemSize);
                itemRect.anchoredPosition = Vector2.zero;

                Image itemImage = itemImageObject.GetComponent<Image>();
                itemImage.color = Color.white;
                itemImage.preserveAspect = true;
                itemImage.raycastTarget = false;

                NyangQuariumItemSlot slot = slotObject.AddComponent<NyangQuariumItemSlot>();
                slot.Init(this, backgroundImage, itemImage);
                _slots.Add(slot);
            }
        }
    }
}
