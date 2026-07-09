using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumBackgroundFishController : MonoBehaviour
    {
        private static readonly string[] DefaultAutoBlockedAreaNames =
        {
            "LogoImage",
            "BackButton",
            "BoardButton",
            "CollectionButton",
            "LayoutButton",
            "AquariumLevel"
        };

        [SerializeField] private RectTransform _root;
        [SerializeField, Min(0)] private int _spawnCountPerType = 2;
        [SerializeField] private float _padding = 80f;
        [SerializeField] private Vector2 _speedRange = new(35f, 90f);
        [SerializeField] private Vector2 _scaleRange = new(0.75f, 1.1f);
        [SerializeField] private float _targetReachDistance = 10f;
        [SerializeField] private float _maxTiltAngle = 30f;
        [SerializeField] private float _rotationLerpSpeed = 5f;

        [Header("Movement Blocked Areas")]
        [Tooltip("물고기가 지나가지 않아야 하는 UI 영역입니다. 비워두면 로고와 메인 버튼을 자동으로 찾습니다.")]
        [SerializeField] private RectTransform[] _movementBlockedAreas;

        [Tooltip("로고, 뒤로가기, 메인 버튼, 수조 레벨 UI를 이름으로 자동 차단합니다.")]
        [SerializeField] private bool _autoCollectMainUiBlockedAreas = true;

        [Tooltip("자동으로 차단할 UI 오브젝트 이름입니다.")]
        [SerializeField] private string[] _autoBlockedAreaNames = DefaultAutoBlockedAreaNames;

        [Tooltip("차단 영역 주변에 추가할 여유 거리입니다.")]
        [SerializeField] private float _blockedAreaPadding = 40f;

        [Header("Addressables")]
        [SerializeField] private string[] _freshFishKeys =
        {
            "Fish_Fresh_Guppy",
            "Fish_Fresh_Angelfish",
            "Fish_Fresh_NeonTetra",
            "Fish_Fresh_ZebraDanio",
            "Fish_Fresh_Corydoras",
            "Fish_Fresh_Discus",
            "Fish_Fresh_Betta"
        };

        [SerializeField] private string[] _saltFishKeys =
        {
            "Fish_Salt_YellowTang",
            "Fish_Salt_GreenChromis",
            "Fish_Salt_FireGoby",
            "Fish_Salt_PerculaClownfish",
            "Fish_Salt_BanggaiCardinalfish",
            "Fish_Salt_RockBlenny",
            "Fish_Salt_BlueTang"
        };

        [SerializeField] private string[] _brackishFishKeys =
        {
            "Fish_Brackish_FigurePuffer",
            "Fish_Brackish_ArcherFish",
            "Fish_Brackish_Scat",
            "Fish_Brackish_Monodactylus",
            "Fish_Brackish_SpottedGreenPuffer",
            "Fish_Brackish_BumblebeeGoby",
            "Fish_Brackish_IndianGlassFish"
        };

        private readonly List<NyangquariumFishController> _spawnedFishes = new();
        private Coroutine _initializeCoroutine;

        private void Awake()
        {
            if (_root == null)
                _root = transform as RectTransform;
        }

        private void OnEnable()
        {
            if (_initializeCoroutine != null)
                StopCoroutine(_initializeCoroutine);

            _initializeCoroutine = StartCoroutine(InitializeNextFrame());
        }

        private IEnumerator InitializeNextFrame()
        {
            yield return null;

            _initializeCoroutine = null;
            RebuildFishes();
        }

        private void OnDisable()
        {
            if (_initializeCoroutine != null)
            {
                StopCoroutine(_initializeCoroutine);
                _initializeCoroutine = null;
            }

            ClearFishes();
        }

        private void OnDestroy()
        {
            ClearFishes();
        }

        private void RebuildFishes()
        {
            ClearFishes();

            if (_root == null)
                return;

            RectTransform[] blockedAreas = ResolveMovementBlockedAreas();

            SpawnRandomFishes(_freshFishKeys, blockedAreas);
            SpawnRandomFishes(_saltFishKeys, blockedAreas);
            SpawnRandomFishes(_brackishFishKeys, blockedAreas);
        }

        private void SpawnRandomFishes(string[] spriteKeys, RectTransform[] blockedAreas)
        {
            if (_spawnCountPerType <= 0 || spriteKeys == null || spriteKeys.Length == 0)
                return;

            List<string> candidates = new(spriteKeys);
            int spawnCount = Mathf.Min(_spawnCountPerType, candidates.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                int index = Random.Range(0, candidates.Count);
                string spriteKey = candidates[index];
                candidates.RemoveAt(index);

                CreateFish(spriteKey, blockedAreas);
            }
        }

        private void CreateFish(string spriteKey, RectTransform[] blockedAreas)
        {
            float speed = Random.Range(_speedRange.x, _speedRange.y);
            float scale = Random.Range(_scaleRange.x, _scaleRange.y);

            NyangquariumFishController fish = NyangquariumFishController.SpawnMovingFish(
                _root,
                spriteKey,
                scale: scale,
                speed: speed,
                padding: _padding,
                maxTiltAngle: _maxTiltAngle,
                rotationLerpSpeed: _rotationLerpSpeed,
                targetReachDistance: _targetReachDistance);

            if (fish != null)
            {
                fish.SetMovementBlockedAreas(blockedAreas, _blockedAreaPadding);
                _spawnedFishes.Add(fish);
            }
        }

        private RectTransform[] ResolveMovementBlockedAreas()
        {
            List<RectTransform> result = new();

            AddBlockedAreas(result, _movementBlockedAreas);

            if (_autoCollectMainUiBlockedAreas)
                AddAutoBlockedAreas(result);

            return result.ToArray();
        }

        private void AddAutoBlockedAreas(List<RectTransform> result)
        {
            if (result == null)
                return;

            Transform searchRoot = _root != null && _root.parent != null
                ? _root.parent
                : transform.root;

            if (searchRoot == null)
                return;

            string[] names = _autoBlockedAreaNames != null && _autoBlockedAreaNames.Length > 0
                ? _autoBlockedAreaNames
                : DefaultAutoBlockedAreaNames;

            RectTransform[] rectTransforms = searchRoot.GetComponentsInChildren<RectTransform>(true);

            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];

                if (rectTransform == null ||
                    !rectTransform.gameObject.activeInHierarchy ||
                    !ContainsName(names, rectTransform.name))
                {
                    continue;
                }

                AddBlockedArea(result, rectTransform);
            }
        }

        private static void AddBlockedAreas(List<RectTransform> result, RectTransform[] areas)
        {
            if (result == null || areas == null)
                return;

            for (int i = 0; i < areas.Length; i++)
                AddBlockedArea(result, areas[i]);
        }

        private static void AddBlockedArea(List<RectTransform> result, RectTransform area)
        {
            if (result == null || area == null || result.Contains(area))
                return;

            result.Add(area);
        }

        private static bool ContainsName(string[] names, string targetName)
        {
            if (names == null || string.IsNullOrWhiteSpace(targetName))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], targetName))
                    return true;
            }

            return false;
        }

        private void ClearFishes()
        {
            for (int i = 0; i < _spawnedFishes.Count; i++)
            {
                NyangquariumFishController fish = _spawnedFishes[i];

                if (fish == null)
                    continue;

                Destroy(fish.gameObject);
            }

            _spawnedFishes.Clear();
        }
    }
}
