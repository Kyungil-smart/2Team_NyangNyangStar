using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumBackgroundFishController : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private GameObject _fishPrefab;
        [SerializeField, Min(0)] private int _spawnCountPerType = 2;
        [SerializeField] private float _padding = 80f;
        [SerializeField] private Vector2 _speedRange = new(35f, 90f);
        [SerializeField] private Vector2 _scaleRange = new(0.75f, 1.1f);
        [SerializeField] private float _targetReachDistance = 10f;
        [SerializeField] private float _maxTiltAngle = 30f;
        [SerializeField] private float _rotationLerpSpeed = 5f;

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

            SpawnRandomFishes(_freshFishKeys);
            SpawnRandomFishes(_saltFishKeys);
            SpawnRandomFishes(_brackishFishKeys);
        }

        private void SpawnRandomFishes(string[] spriteKeys)
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

                CreateFish(spriteKey);
            }
        }

        private void CreateFish(string spriteKey)
        {
            float speed = Random.Range(_speedRange.x, _speedRange.y);
            float scale = Random.Range(_scaleRange.x, _scaleRange.y);

            NyangquariumFishController fish = NyangquariumFishController.SpawnMovingFish(
                _fishPrefab,
                _root,
                spriteKey,
                scale: scale,
                speed: speed,
                padding: _padding,
                maxTiltAngle: _maxTiltAngle,
                rotationLerpSpeed: _rotationLerpSpeed,
                targetReachDistance: _targetReachDistance);

            if (fish != null)
                _spawnedFishes.Add(fish);
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
