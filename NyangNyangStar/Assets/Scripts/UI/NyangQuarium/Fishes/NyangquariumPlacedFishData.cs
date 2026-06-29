using System;
using UnityEngine;

namespace UI.NyangQuarium
{
    [Serializable]
    public sealed class NyangquariumPlacedFishData
    {
        [SerializeField] private int _fishId;
        [SerializeField] private string _spriteKey;
        [SerializeField] private float _anchoredPositionX;
        [SerializeField] private float _anchoredPositionY;
        [SerializeField] private float _scale = 1f;

        public NyangquariumPlacedFishData()
        {
        }

        public NyangquariumPlacedFishData(string spriteKey, float scale = 1f)
        {
            SpriteKey = spriteKey;
            Scale = scale;
        }

        public NyangquariumPlacedFishData(int fishId, string spriteKey, float scale = 1f)
        {
            FishId = fishId;
            SpriteKey = spriteKey;
            Scale = scale;
        }

        public NyangquariumPlacedFishData(string spriteKey, Vector2 anchoredPosition, float scale = 1f)
        {
            SpriteKey = spriteKey;
            AnchoredPosition = anchoredPosition;
            Scale = scale;
        }

        public NyangquariumPlacedFishData(int fishId, string spriteKey, Vector2 anchoredPosition, float scale = 1f)
        {
            FishId = fishId;
            SpriteKey = spriteKey;
            AnchoredPosition = anchoredPosition;
            Scale = scale;
        }

        public int FishId
        {
            get => _fishId;
            set => _fishId = Mathf.Max(0, value);
        }

        public string SpriteKey
        {
            get => _spriteKey;
            set => _spriteKey = value;
        }

        public float AnchoredPositionX
        {
            get => _anchoredPositionX;
            set => _anchoredPositionX = value;
        }

        public float AnchoredPositionY
        {
            get => _anchoredPositionY;
            set => _anchoredPositionY = value;
        }

        public float Scale
        {
            get => _scale <= 0f ? 1f : _scale;
            set => _scale = Mathf.Max(0.001f, value);
        }

        public Vector2 AnchoredPosition
        {
            get => new(_anchoredPositionX, _anchoredPositionY);
            set
            {
                _anchoredPositionX = value.x;
                _anchoredPositionY = value.y;
            }
        }

        public static NyangquariumPlacedFishData FromController(NyangquariumFishController fish, bool includeCurrentPosition = false)
        {
            if (fish == null)
                return null;

            return includeCurrentPosition
                ? new NyangquariumPlacedFishData(
                    fish.FishId,
                    fish.SpriteKey,
                    fish.AnchoredPosition,
                    fish.VisualScale)
                : new NyangquariumPlacedFishData(
                    fish.FishId,
                    fish.SpriteKey,
                    fish.VisualScale);
        }
    }
}
