using System;
using UnityEngine;

namespace UI.NyangQuarium
{
    [Serializable]
    public sealed class NyangquariumPlacedFishData
    {
        [SerializeField] private string _placedId;
        [SerializeField] private string _spriteKey;
        [SerializeField] private float _anchoredPositionX;
        [SerializeField] private float _anchoredPositionY;
        [SerializeField] private float _scale = 1f;

        public NyangquariumPlacedFishData()
        {
        }

        public NyangquariumPlacedFishData(string spriteKey, float scale = 1f, string placedId = null)
        {
            PlacedId = placedId;
            SpriteKey = spriteKey;
            Scale = scale;
        }

        public NyangquariumPlacedFishData(string spriteKey, Vector2 anchoredPosition, float scale = 1f, string placedId = null)
        {
            PlacedId = placedId;
            SpriteKey = spriteKey;
            AnchoredPosition = anchoredPosition;
            Scale = scale;
        }

        public string PlacedId
        {
            get => _placedId;
            set => _placedId = value;
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
                    fish.SpriteKey,
                    fish.AnchoredPosition,
                    fish.VisualScale,
                    fish.PlacedId)
                : new NyangquariumPlacedFishData(
                    fish.SpriteKey,
                    fish.VisualScale,
                    fish.PlacedId);
        }
    }
}
