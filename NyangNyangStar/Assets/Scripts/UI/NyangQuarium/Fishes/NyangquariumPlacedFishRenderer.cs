using System.Collections.Generic;
using UnityEngine;
using Util;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumPlacedFishRenderer : MonoBehaviour
    {
        [SerializeField] private RectTransform _swimArea;

        [Header("Movement Defaults")]
        [SerializeField] private float _padding = 80f;
        [SerializeField] private float _speed = 60f;
        [SerializeField] private float _targetReachDistance = 10f;
        [SerializeField] private float _maxTiltAngle = 30f;
        [SerializeField] private float _rotationLerpSpeed = 5f;

        private readonly List<NyangquariumFishController> _spawnedFishes = new();
        private readonly List<NyangquariumPlacedFishData> _placedFishData = new();

        public IReadOnlyList<NyangquariumFishController> SpawnedFishes => _spawnedFishes;
        public IReadOnlyList<NyangquariumPlacedFishData> PlacedFishData => _placedFishData;

        private void Awake()
        {
            ResolveSwimArea();
        }

        private void OnDestroy()
        {
            Clear();
        }

        public List<NyangquariumFishController> ShowPlacedFishes(IEnumerable<NyangquariumPlacedFishData> placedFishes)
        {
            Clear();

            if (placedFishes == null)
                return new List<NyangquariumFishController>(_spawnedFishes);

            foreach (NyangquariumPlacedFishData placedFish in placedFishes)
                AddPlacedFish(placedFish);

            return new List<NyangquariumFishController>(_spawnedFishes);
        }

        public NyangquariumFishController ConfirmPlacedFish(string spriteKey, float scale = 1f, string placedId = null)
        {
            NyangquariumPlacedFishData placedFish = new(spriteKey, scale, placedId);
            return AddPlacedFish(placedFish);
        }

        public NyangquariumFishController AddPlacedFish(NyangquariumPlacedFishData placedFish)
        {
            NyangquariumFishController fish = SpawnPlacedFish(placedFish);

            if (fish == null)
                return null;

            _placedFishData.Add(CopyData(placedFish));
            return fish;
        }

        public NyangquariumFishController SpawnPlacedFish(NyangquariumPlacedFishData placedFish, bool useSavedPosition = false)
        {
            if (placedFish == null)
                return null;

            if (string.IsNullOrWhiteSpace(placedFish.SpriteKey))
            {
                DebugTool.Warning("[냥쿠아리움 배치 물고기 표시] 물고기 스프라이트 키가 비어 있습니다.", DebugType.UI, this);
                return null;
            }

            RectTransform swimArea = ResolveSwimArea();

            if (swimArea == null)
            {
                DebugTool.Warning("[냥쿠아리움 배치 물고기 표시] 물고기가 헤엄칠 유영 영역이 연결되지 않았습니다.", DebugType.UI, this);
                return null;
            }

            NyangquariumFishController fish = NyangquariumFishController.SpawnMovingFish(
                swimArea,
                placedFish.SpriteKey,
                useSavedPosition ? placedFish.AnchoredPosition : null,
                placedFish.Scale,
                _speed,
                _padding,
                _maxTiltAngle,
                _rotationLerpSpeed,
                _targetReachDistance,
                placedFish.PlacedId);

            if (fish != null)
                _spawnedFishes.Add(fish);

            return fish;
        }

        public NyangquariumPlacedFishData CreateDataFromFish(NyangquariumFishController fish)
        {
            return NyangquariumPlacedFishData.FromController(fish);
        }

        public List<NyangquariumPlacedFishData> GetCurrentPlacedFishData(bool includeCurrentPosition = false)
        {
            if (!includeCurrentPosition)
                return CopyDataList(_placedFishData);

            List<NyangquariumPlacedFishData> result = new();

            for (int i = 0; i < _spawnedFishes.Count; i++)
            {
                NyangquariumFishController fish = _spawnedFishes[i];

                if (fish == null)
                    continue;

                NyangquariumPlacedFishData data = fish.ToPlacedFishData(includeCurrentPosition);

                if (data != null)
                    result.Add(data);
            }

            return result;
        }

        public void Clear()
        {
            for (int i = 0; i < _spawnedFishes.Count; i++)
            {
                NyangquariumFishController fish = _spawnedFishes[i];

                if (fish != null)
                    Destroy(fish.gameObject);
            }

            _spawnedFishes.Clear();
            _placedFishData.Clear();
        }

        private RectTransform ResolveSwimArea()
        {
            if (_swimArea == null)
                _swimArea = transform as RectTransform;

            return _swimArea;
        }

        private static List<NyangquariumPlacedFishData> CopyDataList(List<NyangquariumPlacedFishData> source)
        {
            List<NyangquariumPlacedFishData> result = new();

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                NyangquariumPlacedFishData copy = CopyData(source[i]);

                if (copy != null)
                    result.Add(copy);
            }

            return result;
        }

        private static NyangquariumPlacedFishData CopyData(NyangquariumPlacedFishData source)
        {
            if (source == null)
                return null;

            return new NyangquariumPlacedFishData(
                source.SpriteKey,
                source.Scale,
                source.PlacedId);
        }
    }
}
