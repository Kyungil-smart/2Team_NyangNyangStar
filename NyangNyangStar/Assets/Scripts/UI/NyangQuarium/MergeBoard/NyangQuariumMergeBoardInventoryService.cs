using Core.Managers;
using Data.LibrarySystem;
using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.NyangQuarium.MergeBoard
{
    // 수조 인벤토리와 냥쿠아리움 머지보드 데이터를 연결하는 서비스
    public static class NyangQuariumMergeBoardInventoryService
    {
        public const int AquariumInventoryMaxDisplayCount = 6;

        private const int BoardSlotCount = 63;
        private const int FirestoreWaitIntervalMs = 100;

        private static NyangQuariumItemBoard _board;
        private static NyangQuariumMergeBoardSlotsSO _boardStore;

        private static readonly Dictionary<int, ItemData> _cachedBoardData = new();
        private static readonly Dictionary<string, Sprite> _loadedSpritesByKey = new();

        private static bool _isRefreshing;

        // 배치 패널은 Firestore 캐시 기준으로만 목록을 생성합니다.
        public static bool IsReady => _cachedBoardData.Count > 0;

        // 아이템 추가 / 제거됐을 때 — 수조 인벤토리 UI 갱신용
        public static event Action InventoryChanged;

        // NyangQuariumItemBoard.Init()에서 자동 등록
        internal static void RegisterBoard(NyangQuariumItemBoard board)
        {
            _board = board;
        }

        internal static void UnregisterBoard(NyangQuariumItemBoard board)
        {
            if (ReferenceEquals(_board, board))
                _board = null;
        }

        internal static void NotifyInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }

        // 배치 패널을 열 때 호출할 Firestore 최신화 함수
        public static async Task<bool> RefreshFromFirestoreAsync(int timeoutMs = 5000)
        {
            if (_isRefreshing)
                return true;

            _isRefreshing = true;

            try
            {
                if (!await ResolveBoardStoreAsync(timeoutMs))
                    return false;

                Dictionary<int, ItemData> loadedBoard = await _boardStore.LoadBoardAsync();
                CacheBoardData(loadedBoard);

                await EnsureCachedSpritesAsync();

                DebugTool.Log(
                    "[NyangQuariumMergeBoardInventoryService] 냥쿠아리움 머지보드 Firestore 캐시 갱신 완료",
                    DebugType.Board);

                NotifyInventoryChanged();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[NyangQuariumMergeBoardInventoryService] 냥쿠아리움 머지보드 Firestore 로드 실패: {exception.Message}");

                return false;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        // FishId 기준 전체 보유 개수
        public static int GetOwnedFishCount(int fishId)
        {
            if (fishId <= 0)
                return 0;

            int count = 0;

            foreach (ItemData itemData in _cachedBoardData.Values)
            {
                if (itemData == null || !itemData.HasItem)
                    continue;

                if (itemData.ItemID == fishId)
                    count++;
            }

            return count;
        }

        // 수조 인벤토리 채울 때 호출
        public static void CopyOwnedFishEntries(
            List<NyangQuariumMergeBoardFishEntry> results,
            FishType aquariumType = FishType.None,
            int maxCount = AquariumInventoryMaxDisplayCount)
        {
            results?.Clear();

            if (results == null || maxCount <= 0)
                return;

            CopyCachedFishEntries(results, aquariumType, maxCount);
        }

        // 수조에 배치 확정했을 때 — fishId 같은 슬롯을 왼쪽부터 count만큼 제거
        public static bool TryConsumeFish(int fishId, int count = 1)
        {
            if (fishId <= 0 || count <= 0)
                return false;

            int remaining = count;

            for (int slotIndex = 0; slotIndex < BoardSlotCount && remaining > 0; slotIndex++)
            {
                ItemData itemData = GetCachedItemBySlotIndex(slotIndex);

                if (itemData == null || !itemData.HasItem || itemData.ItemID != fishId)
                    continue;

                if (!TryClearCachedSlot(slotIndex))
                    continue;

                TryClearRuntimeBoardSlot(slotIndex);

                remaining--;
            }

            if (remaining > 0)
                return false;

            NotifyInventoryChanged();
            return true;
        }

        // CopyOwnedFishEntries로 받은 SlotIndex 그대로 넘기면 됨
        public static bool TryConsumeFishAtSlot(int slotIndex)
        {
            if (!TryClearCachedSlot(slotIndex))
                return false;

            TryClearRuntimeBoardSlot(slotIndex);

            NotifyInventoryChanged();
            return true;
        }

        // entry.SlotIndex로 1마리 제거
        public static bool TryConsumeFishEntry(NyangQuariumMergeBoardFishEntry entry)
        {
            return TryConsumeFishAtSlot(entry.SlotIndex);
        }

        // ItemId 기준 자연요소 보유 개수
        public static int GetOwnedNatureCount(int itemId)
        {
            if (itemId <= 0)
                return 0;

            int count = 0;

            foreach (ItemData itemData in _cachedBoardData.Values)
            {
                if (itemData == null || !itemData.HasItem || itemData.ItemID != itemId)
                    continue;

                if (!TryResolveFishData(itemData.ItemID, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                count++;
            }

            return count;
        }

        // 좌측 자연요소 인벤 채울 때
        public static void CopyOwnedNatureEntries(
            List<NyangQuariumMergeBoardNatureEntry> results,
            int maxCount = AquariumInventoryMaxDisplayCount)
        {
            results?.Clear();

            if (results == null || maxCount <= 0)
                return;

            CopyCachedNatureEntries(results, maxCount);
        }

        // 자연요소 배치 확정 — itemId 같은 슬롯을 왼쪽부터 count만큼 제거
        public static bool TryConsumeNature(int itemId, int count = 1)
        {
            if (itemId <= 0 || count <= 0)
                return false;

            int remaining = count;

            for (int slotIndex = 0; slotIndex < BoardSlotCount && remaining > 0; slotIndex++)
            {
                ItemData itemData = GetCachedItemBySlotIndex(slotIndex);

                if (itemData == null || !itemData.HasItem || itemData.ItemID != itemId)
                    continue;

                if (!TryResolveFishData(itemData.ItemID, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                if (!TryClearCachedSlot(slotIndex))
                    continue;

                TryClearRuntimeBoardSlot(slotIndex);

                remaining--;
            }

            if (remaining > 0)
                return false;

            NotifyInventoryChanged();
            return true;
        }

        public static bool TryConsumeNatureAtSlot(int slotIndex)
        {
            ItemData itemData = GetCachedItemBySlotIndex(slotIndex);

            if (itemData == null || !itemData.HasItem)
                return false;

            if (!TryResolveFishData(itemData.ItemID, out NyangQuariumFishData fishData))
                return false;

            if (fishData.FishType != FishType.Environments)
                return false;

            if (!TryClearCachedSlot(slotIndex))
                return false;

            TryClearRuntimeBoardSlot(slotIndex);

            NotifyInventoryChanged();
            return true;
        }

        public static bool TryConsumeNatureEntry(NyangQuariumMergeBoardNatureEntry entry)
        {
            return TryConsumeNatureAtSlot(entry.SlotIndex);
        }

        // 담수 수조: Freshwater + BrackishWater
        // 해수 수조: Saltwater + BrackishWater
        public static bool CanPlaceFish(FishType fishType, FishType aquariumType)
        {
            if (fishType == FishType.None || fishType == FishType.Environments)
                return false;

            if (aquariumType == FishType.None)
            {
                return fishType == FishType.Freshwater
                    || fishType == FishType.Saltwater
                    || fishType == FishType.BrackishWater;
            }

            if (fishType == FishType.BrackishWater)
                return aquariumType == FishType.Freshwater || aquariumType == FishType.Saltwater;

            return fishType == aquariumType;
        }

        private static void CopyCachedFishEntries(
            List<NyangQuariumMergeBoardFishEntry> results,
            FishType aquariumType,
            int maxCount)
        {
            for (int slotIndex = 0; slotIndex < BoardSlotCount && results.Count < maxCount; slotIndex++)
            {
                ItemData itemData = GetCachedItemBySlotIndex(slotIndex);

                if (itemData == null || !itemData.HasItem)
                    continue;

                if (!TryResolveFishData(itemData.ItemID, out NyangQuariumFishData fishData))
                    continue;

                if (!CanPlaceFish(fishData.FishType, aquariumType))
                    continue;

                results.Add(new NyangQuariumMergeBoardFishEntry(
                    slotIndex,
                    itemData.ItemID,
                    itemData.ItemLevel,
                    fishData.FishKey,
                    itemData.ItemName,
                    fishData.FishType,
                    itemData.ItemSprite));
            }
        }

        private static void CopyCachedNatureEntries(
            List<NyangQuariumMergeBoardNatureEntry> results,
            int maxCount)
        {
            for (int slotIndex = 0; slotIndex < BoardSlotCount && results.Count < maxCount; slotIndex++)
            {
                ItemData itemData = GetCachedItemBySlotIndex(slotIndex);

                if (itemData == null || !itemData.HasItem)
                    continue;

                if (!TryResolveFishData(itemData.ItemID, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                results.Add(new NyangQuariumMergeBoardNatureEntry(
                    slotIndex,
                    itemData.ItemID,
                    itemData.ItemLevel,
                    fishData.FishKey,
                    itemData.ItemName,
                    itemData.ItemSprite));
            }
        }

        /// <summary>
        /// 현재 냥쿠아리움 머지보드 UI가 살아 있으면 같은 슬롯을 즉시 비웁니다.
        /// Firestore 저장 반영과 별개로, 게임 재시작 전에도 보드 화면을 최신 상태로 맞추기 위한 처리입니다.
        /// </summary>
        private static void TryClearRuntimeBoardSlot(int slotIndex)
        {
            if (_board == null)
                return;

            if (_board.TryClearSlot(slotIndex))
            {
                DebugTool.Log(
                    $"[NyangQuariumMergeBoardInventoryService] 런타임 머지보드 슬롯 즉시 제거 완료 SlotIndex:{slotIndex}",
                    DebugType.Board);

                return;
            }

            DebugTool.Warning(
                $"[NyangQuariumMergeBoardInventoryService] 런타임 머지보드 슬롯 즉시 제거 실패 SlotIndex:{slotIndex}",
                DebugType.Board);
        }

        private static bool TryClearCachedSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= BoardSlotCount)
                return false;

            int slotNumber = ToSlotNumber(slotIndex);

            if (!_cachedBoardData.TryGetValue(slotNumber, out ItemData itemData))
                return false;

            if (itemData == null || !itemData.HasItem)
                return false;

            _cachedBoardData[slotNumber] = ItemData.Empty;
            _ = SaveCachedSlotSafeAsync(slotNumber, ItemData.Empty);

            DebugTool.Log(
                $"[NyangQuariumMergeBoardInventoryService] 캐시 슬롯 제거 완료 Slot:{slotNumber}",
                DebugType.Board);

            return true;
        }

        private static ItemData GetCachedItemBySlotIndex(int slotIndex)
        {
            int slotNumber = ToSlotNumber(slotIndex);

            if (_cachedBoardData.TryGetValue(slotNumber, out ItemData itemData))
                return itemData ?? ItemData.Empty;

            return ItemData.Empty;
        }

        private static void CacheBoardData(Dictionary<int, ItemData> boardData)
        {
            _cachedBoardData.Clear();

            for (int slotNumber = 1; slotNumber <= BoardSlotCount; slotNumber++)
            {
                ItemData itemData = ItemData.Empty;

                if (boardData != null && boardData.TryGetValue(slotNumber, out ItemData loadedData))
                    itemData = loadedData ?? ItemData.Empty;

                _cachedBoardData[slotNumber] = itemData;
            }
        }



        /// <summary>
        /// Firestore에서 불러온 슬롯 데이터에 Sprite가 없으면
        /// FishKey / AddressableKey 기준으로 Sprite를 직접 로드합니다.
        /// </summary>
        private static async Task EnsureCachedSpritesAsync()
        {
            List<Task> loadTasks = new();
            HashSet<string> requestedKeys = new();

            foreach (ItemData itemData in _cachedBoardData.Values)
            {
                if (itemData == null || !itemData.HasItem)
                    continue;

                string spriteKey = GetSpriteKey(itemData);

                if (string.IsNullOrWhiteSpace(spriteKey))
                    continue;

                if (_loadedSpritesByKey.TryGetValue(spriteKey, out Sprite cachedSprite) &&
                    cachedSprite != null)
                {
                    itemData.SetSprite(cachedSprite);
                    continue;
                }

                if (!requestedKeys.Add(spriteKey))
                    continue;

                loadTasks.Add(LoadAndCacheSpriteAsync(spriteKey));
            }

            if (loadTasks.Count <= 0)
                return;

            await Task.WhenAll(loadTasks);

            DebugTool.Log(
                $"[NyangQuariumMergeBoardInventoryService] 캐시 Sprite 로드 완료 - Count:{loadTasks.Count}",
                DebugType.Addressable);
        }

        private static async Task LoadAndCacheSpriteAsync(string spriteKey)
        {
            TaskCompletionSource<bool> completionSource = new();

            GameManager.Addressable.LoadSprite(
                spriteKey,
                (sprite, handle) =>
                {
                    if (sprite != null)
                    {
                        _loadedSpritesByKey[spriteKey] = sprite;
                        ApplySpriteToCachedItems(spriteKey, sprite);
                    }

                    completionSource.TrySetResult(true);
                },
                failedKey =>
                {
                    DebugTool.Warning(
                        $"[NyangQuariumMergeBoardInventoryService] Sprite 로드 실패 Key:{failedKey}",
                        DebugType.Addressable);

                    completionSource.TrySetResult(false);
                });

            await completionSource.Task;
        }

        private static void ApplySpriteToCachedItems(string spriteKey, Sprite sprite)
        {
            if (sprite == null)
                return;

            foreach (ItemData itemData in _cachedBoardData.Values)
            {
                if (itemData == null || !itemData.HasItem)
                    continue;

                string itemSpriteKey = GetSpriteKey(itemData);

                if (itemSpriteKey != spriteKey)
                    continue;

                itemData.SetSprite(sprite);
            }
        }

        private static string GetSpriteKey(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
                return string.Empty;

            // 배치 패널에 표시할 Sprite는 Firestore의 냥쿠아리움 머지보드 슬롯에 저장된 AddressableKey를 우선 사용합니다.
            if (!string.IsNullOrWhiteSpace(itemData.AddressableKey))
                return itemData.AddressableKey;

            // 이전 저장 데이터처럼 AddressableKey가 비어 있는 경우만 배치용 FishKey를 예외 fallback으로 사용합니다.
            if (TryResolveFishData(itemData.ItemID, out NyangQuariumFishData fishData) &&
                fishData != null &&
                !string.IsNullOrWhiteSpace(fishData.FishKey))
            {
                return fishData.FishKey;
            }

            return string.Empty;
        }

        private static async Task<bool> ResolveBoardStoreAsync(int timeoutMs)
        {
            if (_boardStore != null && _boardStore.IsReady)
                return true;

            int elapsedMs = 0;

            while (elapsedMs <= timeoutMs)
            {
                FireStoreManager manager = FireStoreManager.Instance;

                if (manager != null && manager.HasDatabaseContext)
                {
                    if (_boardStore == null)
                        _boardStore = ScriptableObject.CreateInstance<NyangQuariumMergeBoardSlotsSO>();

                    if (!_boardStore.IsReady)
                        manager.TryBindStore(_boardStore);

                    if (_boardStore.IsReady)
                        return true;
                }

                await Task.Delay(FirestoreWaitIntervalMs);
                elapsedMs += FirestoreWaitIntervalMs;
            }

            DebugTool.Warning(
                "[NyangQuariumMergeBoardInventoryService] Firestore가 준비되지 않아 냥쿠아리움 머지보드 캐시 갱신을 생략합니다.",
                DebugType.Board);

            return false;
        }

        private static async Task SaveCachedSlotSafeAsync(int slotNumber, ItemData itemData)
        {
            if (!await ResolveBoardStoreAsync(5000))
                return;

            try
            {
                await _boardStore.SaveSlotAsync(slotNumber, itemData ?? ItemData.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[NyangQuariumMergeBoardInventoryService] {slotNumber}번 캐시 슬롯 저장 실패: {exception.Message}");
            }
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

        private static ItemData CreateRuntimeItem(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
                return ItemData.Empty;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryCreateMergeBoardRuntimeItem(itemData, out ItemData runtimeData))
            {
                return runtimeData;
            }

            return itemData.Clone();
        }

        private static int ToSlotNumber(int slotIndex)
        {
            return slotIndex + 1;
        }
    }

    // CopyOwnedFishEntries 결과 1칸 — 수조 인벤토리 슬롯 1개에 대응
    public readonly struct NyangQuariumMergeBoardFishEntry
    {
        public int SlotIndex { get; }
        public int FishId { get; }
        public int Level { get; }
        public string FishKey { get; }
        public string FishName { get; }
        public FishType FishType { get; }
        public Sprite Sprite { get; }

        public NyangQuariumMergeBoardFishEntry(
            int slotIndex,
            int fishId,
            int level,
            string fishKey,
            string fishName,
            FishType fishType,
            Sprite sprite)
        {
            SlotIndex = slotIndex;
            FishId = fishId;
            Level = level;
            FishKey = fishKey ?? string.Empty;
            FishName = fishName ?? string.Empty;
            FishType = fishType;
            Sprite = sprite;
        }
    }

    // CopyOwnedNatureEntries 결과 1칸 — 좌측 자연요소 인벤 슬롯 1개
    public readonly struct NyangQuariumMergeBoardNatureEntry
    {
        public int SlotIndex { get; }
        public int ItemId { get; }
        public int Level { get; }
        public string ItemKey { get; }
        public string ItemName { get; }
        public Sprite Sprite { get; }

        public NyangQuariumMergeBoardNatureEntry(
            int slotIndex,
            int itemId,
            int level,
            string itemKey,
            string itemName,
            Sprite sprite)
        {
            SlotIndex = slotIndex;
            ItemId = itemId;
            Level = level;
            ItemKey = itemKey ?? string.Empty;
            ItemName = itemName ?? string.Empty;
            Sprite = sprite;
        }
    }
}