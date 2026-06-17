using System;
using UnityEngine;

[Serializable]
public class NyangNyangSnapToolData
{
    [Header("아이템 ID")]
    [SerializeField] private int _itemID;

    [Header("아이템 유형")]
    [SerializeField] private NyangNyangSnapToolType _itemToolType;

    [Header("아이템 범위")]
    [SerializeField] private int _itemRange;

    public int ItemID => _itemID;
    public NyangNyangSnapToolType ItemToolType => _itemToolType;
    public int ItemRange => _itemRange;

    public NyangNyangSnapToolData(int itemID, NyangNyangSnapToolType itemToolType, int itemRange)
    {
        _itemID = itemID;
        _itemToolType = itemToolType;
        _itemRange = itemRange;
    }
}

public enum NyangNyangSnapToolType
{
    None,
    Toy,
    Food,
    Snack
}