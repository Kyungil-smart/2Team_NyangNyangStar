using Services.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.MergeBoard;
using Core.Managers;
using UnityEngine;

public class ScratchingTimeReward : MonoBehaviour
{
    [Serializable]
    private class StageReward
    {
        [SerializeField] private int _stage;
        [SerializeField] private List<ItemReward> _rewards = new();

        public int Stage => _stage;
        public IReadOnlyList<ItemReward> Rewards => _rewards;
    }

    [Serializable]
    private class ItemReward
    {
        [SerializeField] private int _itemID;
        [SerializeField] private int _count = 1;

        public int ItemID => _itemID;
        public int Count => Math.Max(_count, 1);
    }

    // 주간 스테이지 보상
    [Header("주간 스테이지")] [SerializeField] private List<StageReward> _weeklyRewards = new();

    public void GiveReward(int stage, StageType stageType)
    {
        if (stageType != StageType.Weekly)
            return;


        StageReward stageReward = _weeklyRewards.Find(x => x.Stage == stage);
        if (stageReward == null)
        {
            DebugTool.Warning($"{stage} 단계 주간 보상 데이터가 없습니다.", DebugType.ScratchingTime, this);
            return;
        }

        if (MergeBoardItemService.Instance == null)
        {
            DebugTool.Warning("MergeBoardItemService가 없어 스크래칭 타임 보상을 지급할 수 없습니다.", DebugType.ScratchingTime, this);
            return;
        }


        foreach (ItemReward reward in stageReward.Rewards)
        {
            if (reward.ItemID <= 0)
            {
                continue;
            }

            MergeBoardItemService.Instance.AddItemById(reward.ItemID, reward.Count);
        }

        DebugTool.Log($"{stage}단계 주간 보상 지급 완료", DebugType.ScratchingTime, this);
    }
}