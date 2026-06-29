using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;
using Random = UnityEngine.Random;


namespace UI.NyangQuarium
{
    /// <summary>
    /// Controls one fish instance.
    /// Main background fish and placed aquarium fish can share this component,
    /// while each owner decides which fish key to pass and where the fish can swim.
    /// </summary>
    public sealed class NyangquariumFishController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _image;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private RectTransform _swimArea;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = new(1f, 0.92f, 0.35f, 1f);

        [Header("Movement")]
        [SerializeField] private bool _spriteFacesRight;
        [SerializeField] private float _speed = 60f;
        [SerializeField] private float _padding = 80f;
        [SerializeField] private float _targetReachDistance = 10f;
        [SerializeField] private float _maxTiltAngle = 30f;
        [SerializeField] private float _rotationLerpSpeed = 5f;
        [SerializeField] private bool _isMovementEnabled;
        [SerializeField] private bool _isSelectable;

        private AsyncOperationHandle<Sprite> _spriteHandle;
        private Vector2 _target;
        private float _baseScale = 1f;
        private int _spriteLoadVersion;
        private bool _hasTarget;
        private bool _isSelected;

        public string SpriteKey { get; private set; }
        public float VisualScale => _baseScale;
        public bool IsSelectable => _isSelectable;
        public bool IsSelected => _isSelected;
        public event Action<NyangquariumFishController> Clicked;
        public RectTransform RectTransform
        {
            get
            {
                ResolveComponents();
                return _rectTransform;
            }
        }
        public Vector2 AnchoredPosition
        {
            get
            {
                ResolveComponents();
                return _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero;
            }
        }

        public static NyangquariumFishController SpawnMovingFish(
            RectTransform swimArea,
            string spriteKey,
            Vector2? anchoredPosition = null,
            float scale = 1f,
            float speed = 60f,
            float padding = 80f,
            float maxTiltAngle = 30f,
            float rotationLerpSpeed = 5f,
            float targetReachDistance = 10f)
        {
            if (swimArea == null)
            {
                DebugTool.Warning("[냥쿠아리움 물고기 컨트롤러] 물고기를 생성할 유영 영역이 없습니다.", DebugType.UI);
                return null;
            }

            GameObject fishObject = CreateRuntimeFishObject(swimArea);

            if (fishObject == null)
                return null;

            fishObject.name = string.IsNullOrWhiteSpace(spriteKey) ? "Fish" : spriteKey;
            fishObject.SetActive(true);

            RectTransform fishRectTransform = fishObject.transform as RectTransform;

            if (fishRectTransform == null)
            {
                Destroy(fishObject);
                return null;
            }

            fishRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            fishRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            fishRectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (anchoredPosition.HasValue)
                fishRectTransform.anchoredPosition = anchoredPosition.Value;

            NyangquariumFishController fishController = fishObject.GetComponent<NyangquariumFishController>();

            if (fishController == null)
                fishController = fishObject.AddComponent<NyangquariumFishController>();

            fishController.Initialize(
                spriteKey,
                swimArea,
                speed,
                padding,
                maxTiltAngle,
                rotationLerpSpeed,
                targetReachDistance,
                scale,
                !anchoredPosition.HasValue);

            return fishController;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isSelectable)
                return;

            Clicked?.Invoke(this);
        }

        private void Awake()
        {
            ResolveComponents();
            PrepareImage();
            CacheBaseScale();
        }

        private void OnEnable()
        {
            ResolveComponents();

            if (_swimArea == null)
                _swimArea = _rectTransform != null ? _rectTransform.parent as RectTransform : null;

            if (_isMovementEnabled && !_hasTarget)
                PickNewTarget();
        }

        private void Update()
        {
            if (!_isMovementEnabled || _rectTransform == null)
                return;

            Move(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _spriteLoadVersion++;
            ReleaseSprite();
        }

        public void Initialize(string spriteKey, RectTransform swimArea)
        {
            Initialize(
                spriteKey,
                swimArea,
                _speed,
                _padding,
                _maxTiltAngle,
                _rotationLerpSpeed,
                _targetReachDistance,
                _baseScale,
                false);
        }

        public void Initialize(
            string spriteKey,
            RectTransform swimArea,
            float speed,
            float padding,
            float maxTiltAngle,
            float rotationLerpSpeed,
            float targetReachDistance,
            float scale,
            bool randomizePosition)
        {
            ResolveComponents();
            PrepareImage();

            SetMovementOptions(speed, padding, maxTiltAngle, rotationLerpSpeed, targetReachDistance);
            SetSwimArea(swimArea);
            SetVisualScale(scale);

            if (randomizePosition && _rectTransform != null)
                _rectTransform.anchoredPosition = GetRandomPosition();

            SetFish(spriteKey);
            SetMovementEnabled(true);
            PickNewTarget();
        }

        public void SetFish(string spriteKey)
        {
            ResolveComponents();
            PrepareImage();

            SpriteKey = spriteKey;
            _spriteLoadVersion++;
            int loadVersion = _spriteLoadVersion;

            ReleaseSprite();

            if (_image != null)
                _image.sprite = null;

            if (string.IsNullOrWhiteSpace(spriteKey))
                return;

            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);
            _spriteHandle = handle;

            handle.Completed += completedHandle =>
            {
                if (loadVersion != _spriteLoadVersion || _image == null)
                    return;

                if (completedHandle.Status == AsyncOperationStatus.Succeeded && completedHandle.Result != null)
                {
                    _image.sprite = completedHandle.Result;
                    return;
                }

                DebugTool.Warning($"{spriteKey} : 냥쿠아리움 물고기 스프라이트 로드 실패", DebugType.Addressable, this);
            };
        }

        public void SetSwimArea(RectTransform swimArea)
        {
            _swimArea = swimArea;

            if (_swimArea == null && _rectTransform != null)
                _swimArea = _rectTransform.parent as RectTransform;
        }

        public void SetMovementEnabled(bool isMovementEnabled)
        {
            _isMovementEnabled = isMovementEnabled;

            if (_isMovementEnabled && !_hasTarget)
                PickNewTarget();
        }

        public void SetSelectable(bool isSelectable)
        {
            _isSelectable = isSelectable;
            UpdateImageState();
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            UpdateImageState();
        }

        public void SetMovementOptions(
            float speed,
            float padding,
            float maxTiltAngle,
            float rotationLerpSpeed,
            float targetReachDistance)
        {
            _speed = Mathf.Max(0f, speed);
            _padding = Mathf.Max(0f, padding);
            _maxTiltAngle = Mathf.Max(0f, maxTiltAngle);
            _rotationLerpSpeed = Mathf.Max(0f, rotationLerpSpeed);
            _targetReachDistance = Mathf.Max(0.01f, targetReachDistance);
        }

        public void SetVisualScale(float scale)
        {
            _baseScale = Mathf.Max(0.001f, Mathf.Abs(scale));

            if (_rectTransform == null)
                return;

            float horizontalSign = _rectTransform.localScale.x < 0f ? -1f : 1f;
            _rectTransform.localScale = new Vector3(horizontalSign * _baseScale, _baseScale, 1f);
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition, bool pickNewTarget = true)
        {
            if (_rectTransform == null)
                return;

            _rectTransform.anchoredPosition = anchoredPosition;

            if (pickNewTarget)
                PickNewTarget();
        }

        public NyangquariumPlacedFishData ToPlacedFishData(bool includeCurrentPosition = false)
        {
            return NyangquariumPlacedFishData.FromController(this, includeCurrentPosition);
        }

        public void PickNewTarget()
        {
            _target = GetRandomPosition();
            _hasTarget = true;

            if (_rectTransform != null)
                ApplyFishDirection(_target - _rectTransform.anchoredPosition, true);
        }

        private void Move(float deltaTime)
        {
            if (!_hasTarget)
                PickNewTarget();

            Vector2 currentPosition = _rectTransform.anchoredPosition;
            Vector2 toTarget = _target - currentPosition;

            if (toTarget.sqrMagnitude <= _targetReachDistance * _targetReachDistance)
            {
                PickNewTarget();
                toTarget = _target - currentPosition;
            }

            if (toTarget.sqrMagnitude <= 0.001f)
                return;

            Vector2 direction = toTarget.normalized;
            _rectTransform.anchoredPosition = Vector2.MoveTowards(
                currentPosition,
                _target,
                _speed * deltaTime);

            ApplyFishDirection(direction, false);
        }

        private void ApplyFishDirection(Vector2 direction, bool immediate)
        {
            if (_rectTransform == null || direction.sqrMagnitude <= 0.001f)
                return;

            float horizontalSign = GetHorizontalSign(direction);
            _rectTransform.localScale = new Vector3(horizontalSign * _baseScale, _baseScale, 1f);

            float verticalAngle = Mathf.Atan2(direction.y, Mathf.Max(Mathf.Abs(direction.x), 0.001f)) * Mathf.Rad2Deg;
            verticalAngle = Mathf.Clamp(verticalAngle, -_maxTiltAngle, _maxTiltAngle);

            bool isFacingRight = IsFacingRight(horizontalSign);
            float targetAngle = isFacingRight ? verticalAngle : -verticalAngle;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            _rectTransform.localRotation = immediate
                ? targetRotation
                : Quaternion.Lerp(
                    _rectTransform.localRotation,
                    targetRotation,
                    Mathf.Clamp01(_rotationLerpSpeed * Time.deltaTime));
        }

        private float GetHorizontalSign(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > 0.001f)
                return GetScaleSignForFacing(direction.x > 0f);

            if (_rectTransform != null && Mathf.Abs(_rectTransform.localScale.x) > 0.001f)
                return _rectTransform.localScale.x < 0f ? -1f : 1f;

            return GetScaleSignForFacing(_spriteFacesRight);
        }

        private float GetScaleSignForFacing(bool faceRight)
        {
            if (_spriteFacesRight)
                return faceRight ? 1f : -1f;

            return faceRight ? -1f : 1f;
        }

        private bool IsFacingRight(float horizontalSign)
        {
            if (_spriteFacesRight)
                return horizontalSign > 0f;

            return horizontalSign < 0f;
        }

        private Vector2 GetRandomPosition()
        {
            Rect rect = GetMovementRect();
            float horizontalPadding = Mathf.Min(_padding, rect.width * 0.45f);
            float verticalPadding = Mathf.Min(_padding, rect.height * 0.45f);

            float minX = rect.xMin + horizontalPadding;
            float maxX = rect.xMax - horizontalPadding;
            float minY = rect.yMin + verticalPadding;
            float maxY = rect.yMax - verticalPadding;

            if (minX > maxX)
            {
                minX = rect.xMin;
                maxX = rect.xMax;
            }

            if (minY > maxY)
            {
                minY = rect.yMin;
                maxY = rect.yMax;
            }

            return new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
        }

        private Rect GetMovementRect()
        {
            RectTransform movementRoot = _swimArea != null
                ? _swimArea
                : _rectTransform != null ? _rectTransform.parent as RectTransform : null;

            if (movementRoot != null && movementRoot.rect.width > 1f && movementRoot.rect.height > 1f)
                return movementRoot.rect;

            return new Rect(-540f, -960f, 1080f, 1920f);
        }

        private void ResolveComponents()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_image == null)
                _image = GetComponent<Image>();
        }

        private void PrepareImage()
        {
            if (_image == null)
                return;

            UpdateImageState();
        }

        private void CacheBaseScale()
        {
            if (_rectTransform == null)
                return;

            _baseScale = Mathf.Max(Mathf.Abs(_rectTransform.localScale.y), 0.001f);
        }

        private void ReleaseSprite()
        {
            if (!_spriteHandle.IsValid())
                return;

            Addressables.Release(_spriteHandle);
            _spriteHandle = default;
        }

        private void UpdateImageState()
        {
            if (_image == null)
                return;

            _image.raycastTarget = _isSelectable;
            _image.preserveAspect = true;
            _image.color = _isSelected ? _selectedColor : _normalColor;
        }

        private static GameObject CreateRuntimeFishObject(RectTransform swimArea)
        {
            GameObject fishObject = new("Fish", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(NyangquariumFishController));
            fishObject.transform.SetParent(swimArea, false);
            fishObject.layer = swimArea.gameObject.layer;

            RectTransform fishRectTransform = fishObject.transform as RectTransform;

            if (fishRectTransform != null)
                fishRectTransform.sizeDelta = new Vector2(100f, 100f);

            Image image = fishObject.GetComponent<Image>();

            if (image != null)
            {
                image.raycastTarget = false;
                image.preserveAspect = true;
            }

            return fishObject;
        }
    }
}
