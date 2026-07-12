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

        [Header("Movement Blocked Areas")]
        [Tooltip("물고기가 목적지와 이동 경로에서 항상 피해야 하는 UI 영역입니다.")]
        [SerializeField] private RectTransform[] _movementBlockedAreas;

        [Tooltip("물고기 크기 외에 금지 영역에 추가할 여유 거리입니다.")]
        [SerializeField] private float _blockedAreaPadding = 10f;

        [Tooltip("안전한 목적지를 찾기 위한 최대 재시도 횟수입니다.")]
        [SerializeField] private int _targetPickAttempts = 50;

        private AsyncOperationHandle<Sprite> _spriteHandle;
        private Vector2 _target;
        private float _baseScale = 1f;
        private int _spriteLoadVersion;
        private bool _hasTarget;
        private bool _isSelected;

        public int FishId { get; private set; }
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
            float targetReachDistance = 10f,
            int fishId = 0)
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
                !anchoredPosition.HasValue,
                fishId);

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
            bool randomizePosition,
            int fishId = 0)
        {
            ResolveComponents();
            PrepareImage();

            FishId = Mathf.Max(0, fishId);
            SetMovementOptions(speed, padding, maxTiltAngle, rotationLerpSpeed, targetReachDistance);
            SetSwimArea(swimArea);
            SetVisualScale(scale);

            if (randomizePosition && _rectTransform != null)
                _rectTransform.anchoredPosition = GetRandomUnblockedPosition();

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

        /// <summary>
        /// 물고기가 항상 피해야 하는 UI 금지 영역을 설정합니다.
        /// UI 활성화 여부와 관계없이 RectTransform 좌표를 사용합니다.
        /// </summary>
        public void SetMovementBlockedAreas(
            RectTransform[] blockedAreas,
            float blockedAreaPadding)
        {
            _movementBlockedAreas = blockedAreas;
            _blockedAreaPadding = Mathf.Max(0f, blockedAreaPadding);

            if (_rectTransform != null &&
                IsPositionBlocked(_rectTransform.anchoredPosition))
            {
                _rectTransform.anchoredPosition = GetRandomUnblockedPosition();
                _hasTarget = false;
            }

            if (_isMovementEnabled)
                PickNewTarget();
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
            ResolveComponents();

            Vector2 currentPosition =
                _rectTransform != null
                    ? _rectTransform.anchoredPosition
                    : Vector2.zero;

            int attempts = Mathf.Max(1, _targetPickAttempts);

            for (int i = 0; i < attempts; i++)
            {
                Vector2 candidate = GetRandomPosition();

                if (TryGetMovementBlockingRect(
                        currentPosition,
                        candidate,
                        out _))
                {
                    continue;
                }

                SetTarget(candidate, currentPosition, true);
                return;
            }

            SetTarget(
                GetRandomUnblockedPosition(),
                currentPosition,
                true);

            DebugTool.Warning(
                "[냥쿠아리움 물고기 컨트롤러] " +
                "UI 금지 구역을 피하는 이동 경로를 찾지 못했습니다.",
                DebugType.UI,
                this);
        }

        private void Move(float deltaTime)
        {
            if (!_hasTarget)
                PickNewTarget();

            Vector2 currentPosition = _rectTransform.anchoredPosition;

            if (TryGetPositionBlockingRect(
                    currentPosition,
                    out Rect currentBlockedRect))
            {
                Vector2 escapedPosition =
                    GetEscapePositionFromBlockedRect(
                        currentPosition,
                        currentBlockedRect);

                _rectTransform.anchoredPosition = escapedPosition;
                SetTargetAwayFromBlockedRect(escapedPosition, currentBlockedRect);
                return;
            }

            Vector2 toTarget = _target - currentPosition;

            if (toTarget.sqrMagnitude <= _targetReachDistance * _targetReachDistance)
            {
                PickNewTarget();
                toTarget = _target - currentPosition;
            }

            if (toTarget.sqrMagnitude <= 0.001f)
                return;

            Vector2 direction = toTarget.normalized;
            Vector2 nextPosition = Vector2.MoveTowards(
                currentPosition,
                _target,
                _speed * deltaTime);

            if (TryGetMovementBlockingRect(
                    currentPosition,
                    nextPosition,
                    out Rect blockingRect))
            {
                SetTargetAwayFromBlockedRect(currentPosition, blockingRect);
                return;
            }

            _rectTransform.anchoredPosition = nextPosition;
            ApplyFishDirection(direction, false);
        }

        private void SetTarget(Vector2 target, Vector2 currentPosition, bool immediateDirection)
        {
            _target = target;
            _hasTarget = true;

            if (_rectTransform != null)
            {
                ApplyFishDirection(
                    _target - currentPosition,
                    immediateDirection);
            }
        }

        private void SetTargetAwayFromBlockedRect(Vector2 currentPosition, Rect blockedRect)
        {
            Vector2 blockedCenter = blockedRect.center;
            Vector2 awayDirection = currentPosition - blockedCenter;

            if (awayDirection.sqrMagnitude <= 0.001f)
            {
                awayDirection = _target - currentPosition;

                if (awayDirection.sqrMagnitude <= 0.001f)
                    awayDirection = Random.insideUnitCircle;
            }

            if (awayDirection.sqrMagnitude <= 0.001f)
                awayDirection = Vector2.left;

            float escapeDistance = Mathf.Max(_padding * 1.5f, 160f);
            Vector2 target = ClampPositionToMovementRect(
                currentPosition + awayDirection.normalized * escapeDistance);

            if (IsPositionBlocked(target))
                target = GetRandomUnblockedPosition();

            SetTarget(target, currentPosition, true);
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

        private Vector2 ClampPositionToMovementRect(Vector2 position)
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

            return new Vector2(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY));
        }

        private Vector2 GetRandomUnblockedPosition()
        {
            int attempts = Mathf.Max(1, _targetPickAttempts);

            for (int i = 0; i < attempts; i++)
            {
                Vector2 candidate = GetRandomPosition();

                if (!IsPositionBlocked(candidate))
                    return candidate;
            }

            return GetRandomPosition();
        }

        private bool IsPositionBlocked(Vector2 position)
        {
            return TryGetPositionBlockingRect(position, out _);
        }

        private bool TryGetPositionBlockingRect(Vector2 position, out Rect blockingRect)
        {
            blockingRect = default;

            if (_movementBlockedAreas == null ||
                _movementBlockedAreas.Length == 0)
            {
                return false;
            }

            foreach (RectTransform blockedArea in _movementBlockedAreas)
            {
                if (blockedArea == null)
                    continue;

                if (!TryGetBlockedRectInSwimArea(
                        blockedArea,
                        out Rect blockedRect))
                {
                    continue;
                }

                if (blockedRect.Contains(position))
                {
                    blockingRect = blockedRect;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetMovementBlockingRect(Vector2 start, Vector2 end, out Rect blockingRect)
        {
            blockingRect = default;

            if (_movementBlockedAreas == null ||
                _movementBlockedAreas.Length == 0)
            {
                return false;
            }

            foreach (RectTransform blockedArea in _movementBlockedAreas)
            {
                if (blockedArea == null)
                    continue;

                if (!TryGetBlockedRectInSwimArea(
                        blockedArea,
                        out Rect blockedRect))
                {
                    continue;
                }

                if (blockedRect.Contains(end) ||
                    DoesSegmentIntersectRect(
                        start,
                        end,
                        blockedRect))
                {
                    blockingRect = blockedRect;
                    return true;
                }
            }

            return false;
        }

        private Vector2 GetEscapePositionFromBlockedRect(Vector2 position, Rect blockedRect)
        {
            float clearance = Mathf.Max(_targetReachDistance * 2f, 24f);

            float leftDistance = Mathf.Abs(position.x - blockedRect.xMin);
            float rightDistance = Mathf.Abs(blockedRect.xMax - position.x);
            float bottomDistance = Mathf.Abs(position.y - blockedRect.yMin);
            float topDistance = Mathf.Abs(blockedRect.yMax - position.y);

            float minDistance = leftDistance;
            Vector2 escapePosition = new(blockedRect.xMin - clearance, position.y);

            if (rightDistance < minDistance)
            {
                minDistance = rightDistance;
                escapePosition = new Vector2(blockedRect.xMax + clearance, position.y);
            }

            if (bottomDistance < minDistance)
            {
                minDistance = bottomDistance;
                escapePosition = new Vector2(position.x, blockedRect.yMin - clearance);
            }

            if (topDistance < minDistance)
                escapePosition = new Vector2(position.x, blockedRect.yMax + clearance);

            escapePosition = ClampPositionToMovementRect(escapePosition);

            if (IsPositionBlocked(escapePosition))
                return GetRandomUnblockedPosition();

            return escapePosition;
        }

        private bool TryGetBlockedRectInSwimArea(
            RectTransform blockedArea,
            out Rect blockedRect)
        {
            blockedRect = default;

            RectTransform swimArea =
                _swimArea != null
                    ? _swimArea
                    : _rectTransform != null
                        ? _rectTransform.parent as RectTransform
                        : null;

            if (swimArea == null)
                return false;

            Vector3[] worldCorners = new Vector3[4];
            blockedArea.GetWorldCorners(worldCorners);

            Vector2 min = new(
                float.PositiveInfinity,
                float.PositiveInfinity);
            Vector2 max = new(
                float.NegativeInfinity,
                float.NegativeInfinity);

            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 localPoint =
                    swimArea.InverseTransformPoint(
                        worldCorners[i]);

                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Vector2 fishHalfSize =
                GetFishHalfSizeInSwimArea();
            float extraPadding =
                Mathf.Max(0f, _blockedAreaPadding);

            blockedRect = Rect.MinMaxRect(
                min.x - fishHalfSize.x - extraPadding,
                min.y - fishHalfSize.y - extraPadding,
                max.x + fishHalfSize.x + extraPadding,
                max.y + fishHalfSize.y + extraPadding);

            return true;
        }

        private Vector2 GetFishHalfSizeInSwimArea()
        {
            if (_rectTransform == null)
                return Vector2.zero;

            RectTransform swimArea =
                _swimArea != null
                    ? _swimArea
                    : _rectTransform.parent as RectTransform;

            if (swimArea == null)
                return _rectTransform.rect.size * 0.5f;

            Vector3[] worldCorners = new Vector3[4];
            _rectTransform.GetWorldCorners(worldCorners);

            Vector2 min = new(
                float.PositiveInfinity,
                float.PositiveInfinity);
            Vector2 max = new(
                float.NegativeInfinity,
                float.NegativeInfinity);

            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 localPoint =
                    swimArea.InverseTransformPoint(
                        worldCorners[i]);

                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            return (max - min) * 0.5f;
        }

        private static bool DoesSegmentIntersectRect(
            Vector2 start,
            Vector2 end,
            Rect rect)
        {
            if (rect.Contains(start) ||
                rect.Contains(end))
            {
                return true;
            }

            Vector2 bottomLeft =
                new(rect.xMin, rect.yMin);
            Vector2 bottomRight =
                new(rect.xMax, rect.yMin);
            Vector2 topRight =
                new(rect.xMax, rect.yMax);
            Vector2 topLeft =
                new(rect.xMin, rect.yMax);

            return DoSegmentsIntersect(
                       start,
                       end,
                       bottomLeft,
                       bottomRight) ||
                   DoSegmentsIntersect(
                       start,
                       end,
                       bottomRight,
                       topRight) ||
                   DoSegmentsIntersect(
                       start,
                       end,
                       topRight,
                       topLeft) ||
                   DoSegmentsIntersect(
                       start,
                       end,
                       topLeft,
                       bottomLeft);
        }

        private static bool DoSegmentsIntersect(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d)
        {
            float abC = Cross(b - a, c - a);
            float abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c);
            float cdB = Cross(d - c, b - c);

            return abC * abD <= 0f &&
                   cdA * cdB <= 0f;
        }

        private static float Cross(
            Vector2 lhs,
            Vector2 rhs)
        {
            return lhs.x * rhs.y -
                   lhs.y * rhs.x;
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
