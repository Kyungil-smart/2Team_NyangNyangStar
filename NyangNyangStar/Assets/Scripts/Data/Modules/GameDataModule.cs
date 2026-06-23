using Data.ScriptableObjects.KeyContainerSO;
using Data.ScriptableObjects.MergeBoard;
using Data.ScriptableObjects.MoongchiSO;
using Services.Enums;
using System;
using System.Collections.Generic;

namespace Data.Modules
{
    public class GameDataModule
    {
        private Dictionary<int, KeyContainerSo> _keyContainerDict = new();
        private ItemDatabaseSo _mergeBoardItemDatabase;
        private NyangNyangSnapBackgroundSO _nyangNyangSnapBackgroundSO;
        private NyangNyangSnapPoseSO _nyangNyangSnapPoseSO;
        private NyangNyangSnapToolSO _nyangNyangSnapToolSO;
        private MoongchiShopSO _findMoongchiShopSO;
        private MoongchiMissionSO _findMoongchiMissionSO;
        private MoongchiProfileSO _findMoongchiProfileSO;

        public bool IsReady { get; private set; }

        private event Action _onReady;

        public event Action OnReady
        {
            add
            {
                _onReady += value;
                if (IsReady) value?.Invoke();
            }
            remove { _onReady -= value; }
        }

        public void MarkNotReady()
        {
            IsReady = false;
        }

        public void RegisterKeyContainers(Dictionary<int, KeyContainerSo> dict)
        {
            _keyContainerDict = dict ?? new Dictionary<int, KeyContainerSo>();
            DebugTool.Log($"[GameDataModule] KeyContainer 등록 ({_keyContainerDict.Count}개)", DebugType.Data);
        }

        public void RegisterMergeBoardItemDatabase(ItemDatabaseSo itemDatabase)
        {
            _mergeBoardItemDatabase = itemDatabase;

            int dataCount = itemDatabase != null ? itemDatabase.DataCount : 0;
            DebugTool.Log($"[GameDataModule] MergeBoard ItemDatabase 등록 ({dataCount}개)", DebugType.Data);
        }

        public void RegisterNyangNyangSnapData(
            NyangNyangSnapBackgroundSO backgroundSO,
            NyangNyangSnapPoseSO poseSO,
            NyangNyangSnapToolSO toolSO)
        {
            _nyangNyangSnapBackgroundSO = backgroundSO;
            _nyangNyangSnapPoseSO = poseSO;
            _nyangNyangSnapToolSO = toolSO;

            DebugTool.Log("[GameDataModule] 냥냥스냅 데이터 SO 등록 완료", DebugType.Data);
        }

        public void RegisterFindMoongchiData(
            MoongchiShopSO shopSO,
            MoongchiMissionSO missionSO,
            MoongchiProfileSO profileSO)
        {
            _findMoongchiShopSO = shopSO;
            _findMoongchiMissionSO = missionSO;
            _findMoongchiProfileSO = profileSO;

            int shopCount = shopSO != null ? shopSO.ShopItems.Count : 0;
            int missionCount = missionSO != null ? missionSO.Missions.Count : 0;
            int profileCount = profileSO != null ? profileSO.Profiles.Count : 0;

            DebugTool.Log(
                $"[GameDataModule] 뭉치를 찾아라 데이터 SO 등록 완료 / Shop={shopCount}, Mission={missionCount}, Profile={profileCount}",
                DebugType.Data);
        }

        public bool TryGetFindMoongchiShopSO(out MoongchiShopSO shopSO)
        {
            shopSO = _findMoongchiShopSO;
            return shopSO != null;
        }

        public bool TryGetFindMoongchiMissionSO(out MoongchiMissionSO missionSO)
        {
            missionSO = _findMoongchiMissionSO;
            return missionSO != null;
        }

        public bool TryGetFindMoongchiProfileSO(out MoongchiProfileSO profileSO)
        {
            profileSO = _findMoongchiProfileSO;
            return profileSO != null;
        }

        public bool TryGetNyangNyangSnapBackgroundSO(out NyangNyangSnapBackgroundSO backgroundSO)
        {
            backgroundSO = _nyangNyangSnapBackgroundSO;
            return backgroundSO != null;
        }

        public bool TryGetNyangNyangSnapPoseSO(out NyangNyangSnapPoseSO poseSO)
        {
            poseSO = _nyangNyangSnapPoseSO;
            return poseSO != null;
        }

        public bool TryGetNyangNyangSnapToolSO(out NyangNyangSnapToolSO toolSO)
        {
            toolSO = _nyangNyangSnapToolSO;
            return toolSO != null;
        }

        public bool TryGetMergeBoardItemById(int itemID, out ItemData itemData)
        {
            itemData = null;

            if (!CheckReady(nameof(TryGetMergeBoardItemById), itemID))
                return false;

            if (_mergeBoardItemDatabase == null)
            {
                DebugTool.Warning("[GameDataModule] MergeBoard ItemDatabase가 등록되지 않았습니다.", DebugType.Data);
                return false;
            }

            return _mergeBoardItemDatabase.TryGetItemById(itemID, out itemData);
        }

        public bool TryGetMergeBoardItemById(int itemID, int count, out ItemData itemData)
        {
            return TryGetMergeBoardItemById(itemID, out itemData);
        }

        public bool TryGetRandomMergeBoardItem(ItemType itemType, out ItemData itemData)
        {
            itemData = null;

            if (!CheckReady(nameof(TryGetRandomMergeBoardItem), 0))
                return false;

            if (_mergeBoardItemDatabase == null)
            {
                DebugTool.Warning("[GameDataModule] MergeBoard ItemDatabase가 등록되지 않았습니다.", DebugType.Data);
                return false;
            }

            return _mergeBoardItemDatabase.TryGetRandomItem(itemType, out itemData);
        }

        public bool TryCreateMergeBoardRuntimeItem(ItemData sourceData, out ItemData itemData)
        {
            itemData = null;

            if (!CheckReady(nameof(TryCreateMergeBoardRuntimeItem), sourceData?.ItemID ?? 0))
                return false;

            if (_mergeBoardItemDatabase == null)
            {
                DebugTool.Warning("[GameDataModule] MergeBoard ItemDatabase가 등록되지 않았습니다.", DebugType.Data);
                return false;
            }

            itemData = _mergeBoardItemDatabase.CreateRuntimeItem(sourceData);
            return itemData != null && itemData.HasItem;
        }

        public void MarkReady()
        {
            if (IsReady)
            {
                DebugTool.Warning("[GameDataModule] 이미 Ready 상태에서 MarkReady() 재호출", DebugType.Data);
                return;
            }

            IsReady = true;
            DebugTool.Log("[GameDataModule] 모든 데이터 준비 완료 (IsReady = true)", DebugType.Data);
            _onReady?.Invoke();
        }

        private bool CheckReady(string methodName, int id)
        {
            if (IsReady) return true;

            DebugTool.Warning(
                $"[GameDataModule] 미준비 상태에서 {methodName}({id}) 호출",
                DebugType.Data);
            return false;
        }

        public void Clear()
        {
            IsReady = false;
            _onReady = null;
            _mergeBoardItemDatabase = null;
            _nyangNyangSnapBackgroundSO = null;
            _nyangNyangSnapPoseSO = null;
            _nyangNyangSnapToolSO = null;
            _findMoongchiShopSO = null;
            _findMoongchiMissionSO = null;
            _findMoongchiProfileSO = null;
            _keyContainerDict.Clear();

            DebugTool.Log("[GameDataModule] 데이터 초기화 완료", DebugType.Data);
        }

        public void ClearEvent()
        {
            _onReady = null;
            _mergeBoardItemDatabase = null;
            _nyangNyangSnapBackgroundSO = null;
            _nyangNyangSnapPoseSO = null;
            _nyangNyangSnapToolSO = null;
            _findMoongchiShopSO = null;
            _findMoongchiMissionSO = null;
            _findMoongchiProfileSO = null;
        }
    }
}
