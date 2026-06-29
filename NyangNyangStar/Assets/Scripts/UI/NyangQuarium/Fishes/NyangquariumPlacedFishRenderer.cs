using System;
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
        [SerializeField] private bool _isSelectionEnabled = true;

        private readonly List<NyangquariumFishController> _spawnedFishes = new();
        private readonly List<NyangquariumPlacedFishData> _placedFishData = new();
        private NyangquariumFishController _selectedFish;

        public IReadOnlyList<NyangquariumFishController> SpawnedFishes => _spawnedFishes;
        public IReadOnlyList<NyangquariumPlacedFishData> PlacedFishData => _placedFishData;
        public NyangquariumFishController SelectedFish => _selectedFish;
        public bool HasSelectedFish => _selectedFish != null;
        public event Action<NyangquariumFishController> SelectedFishChanged;

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

        public NyangquariumFishController ConfirmPlacedFish(string spriteKey, float scale = 1f)
        {
            return ConfirmPlacedFish(0, spriteKey, scale);
        }

        public NyangquariumFishController ConfirmPlacedFish(int fishId, string spriteKey, float scale = 1f)
        {
            NyangquariumPlacedFishData placedFish = new(fishId, spriteKey, scale);
            return AddPlacedFish(placedFish);
        }

        public NyangquariumFishController ConfirmPlacedFish(int fishId, NyangQuariumFishSO fishSO, float scale = 1f)
        {
            if (!TryGetSpriteKey(fishId, fishSO, out string spriteKey))
            {
                DebugTool.Warning(
                    $"[냥쿠아리움 배치 물고기 표시] FishId로 SpriteKey를 찾지 못했습니다. FishId:{fishId}",
                    DebugType.UI,
                    this);
                return null;
            }

            return ConfirmPlacedFish(fishId, spriteKey, scale);
        }

        public NyangquariumFishController AddPlacedFish(NyangquariumPlacedFishData placedFish)
        {
            NyangquariumFishController fish = CreatePlacedFishObject(placedFish);

            if (fish == null)
                return null;

            RegisterFish(fish);
            _placedFishData.Add(CopyData(placedFish));
            return fish;
        }

        private NyangquariumFishController CreatePlacedFishObject(NyangquariumPlacedFishData placedFish, bool useSavedPosition = false)
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
                placedFish.FishId);

            return fish;
        }

        public NyangquariumPlacedFishData CreateDataFromFish(NyangquariumFishController fish)
        {
            return NyangquariumPlacedFishData.FromController(fish);
        }

        private static bool TryGetSpriteKey(int fishId, NyangQuariumFishSO fishSO, out string spriteKey)
        {
            spriteKey = string.Empty;

            if (fishId <= 0 || fishSO == null || fishSO.FishData == null)
                return false;

            foreach (NyangQuariumFishData fishData in fishSO.FishData)
            {
                if (fishData == null || fishData.FishId != fishId)
                    continue;

                spriteKey = fishData.FishKey;
                return !string.IsNullOrWhiteSpace(spriteKey);
            }

            return false;
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

        public void SetSelectionEnabled(bool isSelectionEnabled)
        {
            _isSelectionEnabled = isSelectionEnabled;

            if (!_isSelectionEnabled)
                ClearSelection();

            for (int i = 0; i < _spawnedFishes.Count; i++)
            {
                NyangquariumFishController fish = _spawnedFishes[i];

                if (fish != null)
                    fish.SetSelectable(_isSelectionEnabled);
            }
        }

        public void SelectFish(NyangquariumFishController fish)
        {
            if (!_isSelectionEnabled)
                return;

            if (fish != null && !_spawnedFishes.Contains(fish))
                return;

            if (_selectedFish == fish)
                return;

            if (_selectedFish != null)
                _selectedFish.SetSelected(false);

            _selectedFish = fish;

            if (_selectedFish != null)
                _selectedFish.SetSelected(true);

            SelectedFishChanged?.Invoke(_selectedFish);
        }

        public void ClearSelection()
        {
            if (_selectedFish != null)
                _selectedFish.SetSelected(false);

            _selectedFish = null;
            SelectedFishChanged?.Invoke(null);
        }

        public bool DeleteSelectedFish()
        {
            return DeleteFish(_selectedFish);
        }

        public bool DeleteFish(NyangquariumFishController fish)
        {
            int index = _spawnedFishes.IndexOf(fish);

            if (index < 0)
                return false;

            return DeleteFishAt(index);
        }

        public bool TryGetSelectedFishData(out NyangquariumPlacedFishData data)
        {
            data = null;

            int index = _spawnedFishes.IndexOf(_selectedFish);

            if (index < 0 || index >= _placedFishData.Count)
                return false;

            data = CopyData(_placedFishData[index]);
            return data != null;
        }

        public void Clear()
        {
            ClearSelection();

            for (int i = 0; i < _spawnedFishes.Count; i++)
            {
                NyangquariumFishController fish = _spawnedFishes[i];

                if (fish != null)
                {
                    UnregisterFish(fish);
                    Destroy(fish.gameObject);
                }
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

        private void RegisterFish(NyangquariumFishController fish)
        {
            if (fish == null)
                return;

            fish.Clicked += HandleFishClicked;
            fish.SetSelectable(_isSelectionEnabled);
            _spawnedFishes.Add(fish);
        }

        private void UnregisterFish(NyangquariumFishController fish)
        {
            if (fish == null)
                return;

            fish.Clicked -= HandleFishClicked;
            fish.SetSelectable(false);
            fish.SetSelected(false);
        }

        private void HandleFishClicked(NyangquariumFishController fish)
        {
            SelectFish(fish);
        }

        private bool DeleteFishAt(int index)
        {
            if (index < 0 || index >= _spawnedFishes.Count)
                return false;

            NyangquariumFishController fish = _spawnedFishes[index];

            if (_selectedFish == fish)
                ClearSelection();

            if (fish != null)
            {
                UnregisterFish(fish);
                Destroy(fish.gameObject);
            }

            _spawnedFishes.RemoveAt(index);

            if (index < _placedFishData.Count)
                _placedFishData.RemoveAt(index);

            return true;
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
                source.FishId,
                source.SpriteKey,
                source.Scale);
        }
    }
}
