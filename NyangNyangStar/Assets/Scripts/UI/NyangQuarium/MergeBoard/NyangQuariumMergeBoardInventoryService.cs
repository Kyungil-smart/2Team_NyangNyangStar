using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.NyangQuarium.MergeBoard
{
    // 수조 API — 머지보드 보유 관상어 조회/제거
    
    public static class NyangQuariumMergeBoardInventoryService
    {
        // 수조 인벤토리에 한 번에 최대 6마리까지 호출
        public const int AquariumInventoryMaxDisplayCount = 6;

        private static NyangQuariumItemBoard _board;

        // 냥쿠 머지보드 화면이 열려 있을 때만 true
        public static bool IsReady => _board != null;

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

        // FishId 기준 전체 보유 개수 (머지보드 슬롯 전체 합산)
        public static int GetOwnedFishCount(int fishId)
        {
            if (_board == null || fishId <= 0)
                return 0;

            return _board.GetOwnedFishCount(fishId);
        }

        // 수조 인벤토리 채울 때 호출
        // results는 호출 전 비워지고, 왼쪽 슬롯부터 maxCount개까지 복사됨
        // aquariumType : FishType.Freshwater(담수) / FishType.Saltwater(해수)
        // None이면 관상어만 전부 (자연요소 Environments 제외)
        public static void CopyOwnedFishEntries(
            List<NyangQuariumMergeBoardFishEntry> results,
            FishType aquariumType = FishType.None,
            int maxCount = AquariumInventoryMaxDisplayCount)
        {
            results?.Clear();

            if (_board == null || results == null)
                return;

            _board.CopyOwnedFishEntries(results, aquariumType, maxCount);
        }

        // 수조에 배치 확정했을 때 — fishId 같은 슬롯을 왼쪽부터 count만큼 제거
        public static bool TryConsumeFish(int fishId, int count = 1)
        {
            if (_board == null || fishId <= 0 || count <= 0)
                return false;

            if (!_board.TryConsumeFish(fishId, count))
                return false;

            NotifyInventoryChanged();
            return true;
        }

        // CopyOwnedFishEntries로 받은 SlotIndex 그대로 넘기면 됨
        public static bool TryConsumeFishAtSlot(int slotIndex)
        {
            if (_board == null)
                return false;

            if (!_board.TryClearSlot(slotIndex))
                return false;

            NotifyInventoryChanged();
            return true;
        }

        // entry.SlotIndex로 1마리 제거 — 드래그앤드롭 배치 확정 시 이쪽 쓰면 편함
        public static bool TryConsumeFishEntry(NyangQuariumMergeBoardFishEntry entry)
        {
            return TryConsumeFishAtSlot(entry.SlotIndex);
        }

        // 담수 수조: Freshwater + BrackishWater
        // 해수 수조: Saltwater + BrackishWater
        public static bool CanPlaceFish(FishType fishType, FishType aquariumType)
        {
            if (fishType == FishType.None || fishType == FishType.Environments)
                return false;

            if (aquariumType == FishType.None)
                return fishType == FishType.Freshwater
                    || fishType == FishType.Saltwater
                    || fishType == FishType.BrackishWater;

            if (fishType == FishType.BrackishWater)
                return aquariumType == FishType.Freshwater || aquariumType == FishType.Saltwater;

            return fishType == aquariumType;
        }
    }

    // CopyOwnedFishEntries 결과 1칸 — 수조 인벤토리 슬롯 1개에 대응
    public readonly struct NyangQuariumMergeBoardFishEntry
    {
        public int SlotIndex { get; }   // TryConsumeFishAtSlot / TryConsumeFishEntry에 그대로 사용
        public int FishId { get; }
        public int Level { get; }
        public string FishKey { get; }   // NyangquariumPlacedFishRenderer 배치용
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
}
