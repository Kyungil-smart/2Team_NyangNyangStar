using System;
using UnityEngine;


namespace Data.ScriptableObjects.HideAndSeekSO
{
    // 뭉치를 찾아라 이벤트에서 재화 / 보상
    // 이벤트 코인, 에너지
    public enum HideAndSeekCurrencyType
    {
        None,
        EVENT_COIN,
        ENERGY
    }

    // 이벤트 상점 상품 종류
    // productID 가 어떤 데이터랑 연결되는지 구분
    public enum HideAndSeekProductType
    {
        None,
        CURRENCY,
        ITEM,
        PROFILE
    }

    // 미션 주차 구분, 초기화
    public enum HideAndSeekMissionType
    {
        None,
        DAILY,
        WEEKLY,
        WEEKLY_1ST,
        WEEKLY_2ND
    }
}