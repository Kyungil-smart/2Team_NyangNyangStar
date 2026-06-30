using System;
using UnityEngine;

[Serializable]
public sealed class NyangQuariumPlacedNatureData
{
    [SerializeField] private int _itemId;
    [SerializeField] private string _spriteKey;
    [SerializeField] private float _anchoredPositionX;
    [SerializeField] private float _anchoredPositionY;
    [SerializeField] private float _scaleX = 1f;
    [SerializeField] private float _scaleY = 1f;

    public int ItemId => _itemId;
    public string SpriteKey => _spriteKey;

    public Vector2 AnchoredPosition =>
        new(_anchoredPositionX, _anchoredPositionY);

    public Vector2 Scale =>
        new(_scaleX, _scaleY);

    // FirestoreMapper가 데이터를 복원할 때 사용합니다.
    public NyangQuariumPlacedNatureData()
    {
    }

    public NyangQuariumPlacedNatureData(
        int itemId,
        string spriteKey,
        Vector2 anchoredPosition,
        Vector2 scale)
    {
        _itemId = itemId;
        _spriteKey = spriteKey;
        _anchoredPositionX = anchoredPosition.x;
        _anchoredPositionY = anchoredPosition.y;
        _scaleX = Mathf.Approximately(scale.x, 0f) ? 1f : scale.x;
        _scaleY = Mathf.Approximately(scale.y, 0f) ? 1f : scale.y;
    }
}
