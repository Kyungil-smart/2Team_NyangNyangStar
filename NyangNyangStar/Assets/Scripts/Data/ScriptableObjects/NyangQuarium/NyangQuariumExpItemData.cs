using System;
using UnityEngine;

[Serializable]
public class NyangQuariumExpItemData
{
    [Header("아이템 ID")]
    [SerializeField] private int _itemId;
    [Header("아이템 이름")]
    [SerializeField] private string _itemName;
    [Header("아이템 레벨")]
    [SerializeField] private int _itemLevel;
    [Header("경험치")]
    [SerializeField] private int _expValue;
    [Header("Addressable Key")]
    [SerializeField] private string _addressableKey;

    public int ItemId => _itemId;
    public string ItemName => _itemName;
    public int ItemLevel => _itemLevel;
    public int ExpValue => _expValue;
    public string AddressableKey => _addressableKey;

    public NyangQuariumExpItemData(
        int itemId,
        string itemName,
        int itemLevel,
        int expValue,
        string addressableKey)
    {
        _itemId = itemId;
        _itemName = itemName;
        _itemLevel = itemLevel;
        _expValue = expValue;
        _addressableKey = addressableKey;
    }
}
