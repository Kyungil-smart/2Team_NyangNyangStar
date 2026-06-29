using System;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace UI.NyangQuarium.Quest
{
    // NyangQuariumQuestPopUp — 조건·보상 아이콘 로드 partial
    public sealed partial class NyangQuariumQuestPopUp
    {
        // Coin -> 코인 아이콘, 숫자 -> ItemDatabase 아이템 스프라이트
        private void ApplyConditionIcon(string condition)
        {
            if (_conditionIcon == null)
                return;

            if (NyangQuariumQuestConditionUtil.IsCoinCondition(condition))
            {
                _conditionIconBindId = -1;

                LoadQuestSprite(
                    CoinIconKey,
                    (sprite, _) =>
                    {
                        if (_conditionIcon != null)
                        {
                            _conditionIcon.enabled = true;
                            _conditionIcon.sprite = sprite;
                        }
                    },
                    _ => ClearIcon(_conditionIcon));
                return;
            }

            if (int.TryParse(condition, out int itemId))
            {
                ApplyConditionItemIcon(itemId);
                return;
            }

            _conditionIconBindId = -1;
            ClearIcon(_conditionIcon);
        }

        // 조건 아이콘용 — 비동기 로드 완료 시 bindId 일치 여부로 stale 결과 무시
        private void ApplyConditionItemIcon(int itemId)
        {
            _conditionIconBindId = itemId;
            ApplyItemIconInternal(itemId, _conditionIcon, itemId);
        }

        // 보상 아이콘용
        private void ApplyItemIcon(int itemId, Image target)
        {
            ApplyItemIconInternal(itemId, target, -1);
        }

        private void ApplyItemIconInternal(int itemId, Image target, int conditionBindId)
        {
            if (target == null)
                return;

            if (!TryGetItemData(itemId, out ItemData itemData))
            {
                if (conditionBindId >= 0 && conditionBindId == _conditionIconBindId)
                    ClearIcon(target);
                return;
            }

            if (string.IsNullOrEmpty(itemData.AddressableKey))
            {
                if (itemData.ItemSprite != null)
                {
                    target.enabled = true;
                    target.sprite = itemData.ItemSprite;
                    return;
                }

                if (conditionBindId >= 0 && conditionBindId == _conditionIconBindId)
                    ClearIcon(target);
                return;
            }

            string addressableKey = itemData.AddressableKey;

            LoadQuestSprite(
                addressableKey,
                (sprite, _) =>
                {
                    if (target == null)
                        return;

                    if (conditionBindId >= 0 && conditionBindId != _conditionIconBindId)
                        return;

                    target.enabled = true;
                    target.sprite = sprite;
                },
                _ =>
                {
                    if (target == null)
                        return;

                    if (conditionBindId >= 0 && conditionBindId != _conditionIconBindId)
                        return;

                    ClearIcon(target);
                });
        }

        // 경험치 보상 아이콘
        private void ApplyExpRewardIcon()
        {
            if (_rewardIconPrimary == null)
                return;

            LoadQuestSprite(
                ExpRewardIconKey,
                (sprite, _) =>
                {
                    if (_rewardIconPrimary == null)
                        return;

                    _rewardIconPrimary.enabled = true;
                    _rewardIconPrimary.sprite = sprite;
                },
                _ => ClearIcon(_rewardIconPrimary));
        }

        // Addressable 스프라이트 로드 (핸들은 _loadedIconHandles에 보관)
        private void LoadQuestSprite(
            string key,
            Action<Sprite, AsyncOperationHandle<Sprite>> onLoaded,
            Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                onFailed?.Invoke(key);
                return;
            }

            GameManager.Addressable.LoadSprite(
                key,
                (sprite, handle) =>
                {
                    _loadedIconHandles.Add(handle);
                    onLoaded?.Invoke(sprite, handle);
                },
                failedKey =>
                {
                    DebugTool.Warning($"{failedKey} : QuestPopUp Sprite load failed", DebugType.UI);
                    onFailed?.Invoke(failedKey);
                });
        }

        // ItemDatabase 또는 로컬 저장 데이터에서 아이템 조회
        private bool TryGetItemData(int itemId, out ItemData itemData)
        {
            itemData = null;

            if (itemId <= 0)
                return false;

            if (_itemDatabase != null && _itemDatabase.TryGetItemById(itemId, out itemData))
                return true;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out itemData))
                return true;

            return false;
        }

        private static void ClearIcon(Image target)
        {
            if (target == null)
                return;

            target.sprite = null;
            target.enabled = false;
        }
    }
}
