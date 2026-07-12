using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumPlacedFishRenderer : MonoBehaviour
    {
        [SerializeField] private RectTransform _swimArea;

        [Header("Placement Spawn Area")]
        [Tooltip("관상어 배치 순간에만 유영 영역 왼쪽에서 제외할 너비입니다.")]
        [SerializeField] private float _placementLeftInset = 0f;

        [Tooltip("관상어 배치 순간에만 유영 영역 오른쪽에서 제외할 너비입니다.")]
        [SerializeField] private float _placementRightInset = 0f;

        [Tooltip("관상어 배치 순간에만 유영 영역 위쪽에서 제외할 높이입니다.")]
        [SerializeField] private float _placementTopInset = 0f;

        [Tooltip("관상어 배치 순간에만 유영 영역 아래쪽에서 제외할 높이입니다.")]
        [SerializeField] private float _placementBottomInset = 665f;

        [Header("Movement Blocked Areas")]
        [Tooltip("물고기가 항상 피해야 하는 버튼 또는 UI RectTransform 목록입니다.")]
        [SerializeField] private RectTransform[] _movementBlockedAreas;

        [Tooltip("물고기 크기 외에 금지 영역에 추가할 여유 거리입니다.")]
        [SerializeField] private float _blockedAreaPadding = 10f;

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
            Vector2 spawnPosition = GetRandomPlacementSpawnPosition();

            return AddPlacedFish(
                placedFish,
                spawnPosition);
        }

        public NyangquariumFishController AddPlacedFish(NyangquariumPlacedFishData placedFish)
        {
            return AddPlacedFish(
                placedFish,
                null);
        }

        private NyangquariumFishController AddPlacedFish(
            NyangquariumPlacedFishData placedFish,
            Vector2? spawnPosition)
        {
            NyangquariumFishController fish =
                CreatePlacedFishObject(
                    placedFish,
                    spawnPosition);

            if (fish == null)
                return null;

            RegisterFish(fish);
            _placedFishData.Add(CopyData(placedFish));
            return fish;
        }

        private NyangquariumFishController CreatePlacedFishObject(
            NyangquariumPlacedFishData placedFish,
            Vector2? spawnPosition = null)
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
                spawnPosition,
                placedFish.Scale,
                _speed,
                _padding,
                _maxTiltAngle,
                _rotationLerpSpeed,
                _targetReachDistance,
                placedFish.FishId);

            if (fish != null)
            {
                fish.SetMovementBlockedAreas(
                    _movementBlockedAreas,
                    _blockedAreaPadding);
            }

            return fish;
        }

        /// <summary>
        /// 관상어를 배치하는 순간에만 인벤토리 위쪽 영역에서
        /// 랜덤 생성 위치를 계산합니다.
        /// 생성 이후 이동 목표는 FishArea 전체 범위를 그대로 사용합니다.
        /// </summary>
        private Vector2 GetRandomPlacementSpawnPosition()
        {
            RectTransform swimArea = ResolveSwimArea();

            if (swimArea == null)
                return Vector2.zero;

            Rect areaRect = swimArea.rect;
            float horizontalPadding =
                Mathf.Min(_padding, areaRect.width * 0.45f);
            float verticalPadding =
                Mathf.Min(_padding, areaRect.height * 0.45f);

            float minX =
                areaRect.xMin +
                Mathf.Max(0f, _placementLeftInset) +
                horizontalPadding;
            float maxX =
                areaRect.xMax -
                Mathf.Max(0f, _placementRightInset) -
                horizontalPadding;

            float minY =
                areaRect.yMin +
                Mathf.Max(0f, _placementBottomInset) +
                verticalPadding;
            float maxY =
                areaRect.yMax -
                Mathf.Max(0f, _placementTopInset) -
                verticalPadding;

            if (minX > maxX)
            {
                DebugTool.Warning(
                    "[냥쿠아리움 배치 물고기 표시] " +
                    "배치 영역의 Left/Right Inset 합계가 유영 영역보다 큽니다. " +
                    "가로 범위를 유영 영역 전체로 보정합니다.",
                    DebugType.UI,
                    this);

                minX = areaRect.xMin + horizontalPadding;
                maxX = areaRect.xMax - horizontalPadding;
            }

            if (minY > maxY)
            {
                DebugTool.Warning(
                    "[냥쿠아리움 배치 물고기 표시] " +
                    "배치 영역의 Top/Bottom Inset 합계가 유영 영역보다 큽니다. " +
                    "세로 범위를 유영 영역 전체로 보정합니다.",
                    DebugType.UI,
                    this);

                minY = areaRect.yMin + verticalPadding;
                maxY = areaRect.yMax - verticalPadding;
            }

            Vector2 spawnPosition = new(
                UnityEngine.Random.Range(minX, maxX),
                UnityEngine.Random.Range(minY, maxY));

            DebugTool.Log(
                "[냥쿠아리움 배치 물고기 표시] " +
                $"관상어 배치 위치: {spawnPosition}, " +
                $"Insets(L:{_placementLeftInset}, " +
                $"R:{_placementRightInset}, " +
                $"T:{_placementTopInset}, " +
                $"B:{_placementBottomInset})",
                DebugType.UI,
                this);

            return spawnPosition;
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
