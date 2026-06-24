using System;

namespace Data.ScriptableObjects.MergeBoard
{
    // Firestore 문서 1개 = 슬롯/큐 항목 1개에 대응하는 영속화 전용 구조체.
    // 필드명과 형식은 기존 DB 구조를 그대로 따른다 (특히 ItemType은 문자열).
    // FirestoreMapper가 이 구조체를 문서 dictionary로 직렬화/역직렬화한다.

    [Serializable]
    public struct MergeSlotDoc
    {
        public int SlotNumber;
        public bool HasItem;
        public int ItemID;
        public string ItemName;
        public int ItemLevel;
        public string ItemType;
        public string AddressableKey;
    }

    [Serializable]
    public struct RewardQueueDoc
    {
        public int Order;
        public bool HasItem;
        public int ItemID;
        public string ItemName;
        public int ItemLevel;
        public string ItemType;
        public string AddressableKey;
    }

    [Serializable]
    public struct SpecialSlotDoc
    {
        public int SlotNumber;
        public bool HasItem;
        public int ItemID;
        public string ItemName;
        public int ItemLevel;
        public string ItemType;
        public int Count;
        public string AddressableKey;
    }
}
