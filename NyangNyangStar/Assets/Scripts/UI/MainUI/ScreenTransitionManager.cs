using System;
using System.Collections.Generic;
using Core.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;
using Util;

namespace UI.Transition
{
    public class ScreenTransitionManager : MonoBehaviour
    {
        private const string DefaultBubbleSpritesAddress = "Bubbles";
        private const string DefaultBubbleSfxAddress = "bubble_sfx";
        private const string LegacyBubbleSpritesAddress = "Assets/Test/Bubbles.png";
        private const string LegacyBubbleSfxAddress = "Assets/Test/bubble_transition_sfx_no_pop.wav";

        public static ScreenTransitionManager Instance { get; private set; }

        [Header("Transition")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _coverRect;
        [SerializeField] private Image _coverImage;

        [Header("Option")]
        [SerializeField] private float _screenHeight = 1920f;
        [SerializeField] private float _coverDuration = 0.35f;
        [SerializeField] private float _revealDuration = 0.2f;
        [SerializeField] private float _revealDelay = 0.15f;
        [SerializeField] private int _sortingOrder = 9999;

        [Header("Bubble Transition")]
        [SerializeField] private bool _useBubbleTransition = true;
        [SerializeField] private string _bubbleSpritesAddress = DefaultBubbleSpritesAddress;
        [SerializeField] private string _bubbleSpriteNamePrefix = "Bubbles_";
        [SerializeField] private int _bubbleSpriteFallbackCount = 20;
        [SerializeField] private string _bubbleSfxAddress = DefaultBubbleSfxAddress;
        [SerializeField] private float _bubbleCoverDuration = 0.65f;
        [SerializeField] private float _bubbleRevealDuration = 0.55f;
        [SerializeField] private float _bubbleFloatDistance = 220f;
        [SerializeField, Range(0.1f, 1.5f)] private float _bubbleVisualScale = 1f;
        [SerializeField, Range(5, 12)] private int _bubbleCurtainColumns = 8;
        [SerializeField, Range(1f, 2f)] private float _bubbleCurtainOverlap = 1.45f;
        [SerializeField, Range(0f, 1f)] private float _bubbleSfxVolume = 0.8f;

        private Tween _tween;
        private Sequence _bubbleSequence;
        private bool _isCovered;
        private bool _isTransitioning;
        private bool _isBubbleTransitionCovered;
        private Sprite _defaultCoverSprite;
        private Color _defaultCoverColor = Color.white;
        private AsyncOperationHandle<Sprite> _coverSpriteHandle;
        private int _coverSpriteRequestId;
        private bool _hasDefaultCoverSprite;
        private RectTransform _bubbleRoot;
        private CanvasGroup _bubbleCanvasGroup;
        private readonly List<Image> _bubbleImages = new();
        private readonly List<Vector2> _bubbleCoveredPositions = new();
        private readonly List<Sprite> _bubbleSprites = new();
        private readonly List<Action<bool>> _bubbleSpriteReadyCallbacks = new();
        private readonly List<AsyncOperationHandle<IList<IResourceLocation>>> _bubbleSpriteSubLocationHandles = new();
        private readonly List<AsyncOperationHandle<Sprite>> _bubbleSpriteSubHandles = new();
        private AsyncOperationHandle<IList<IResourceLocation>> _bubbleSpriteLocationsHandle;
        private AsyncOperationHandle<IList<Sprite>> _bubbleSpritesHandle;
        private AsyncOperationHandle<IList<IResourceLocation>> _bubbleSfxLocationsHandle;
        private AsyncOperationHandle<AudioClip> _bubbleSfxHandle;
        private AudioSource _bubbleAudioSource;
        private AudioClip _bubbleSfxClip;
        private bool _isLoadingBubbleSprites;
        private bool _bubbleSpritesReady;
        private bool _isLoadingBubbleSfx;

        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_canvas == null)
                _canvas = GetComponentInChildren<Canvas>(true);

            if (_canvasGroup == null)
                _canvasGroup = GetComponentInChildren<CanvasGroup>(true);

            if (_coverRect == null)
                _coverRect = GetComponentInChildren<RectTransform>(true);

            if (_coverImage == null && _coverRect != null)
                _coverImage = _coverRect.GetComponent<Image>();

            if (_coverImage == null)
                _coverImage = GetComponentInChildren<Image>(true);

            if (_coverRect == null && _coverImage != null)
                _coverRect = _coverImage.rectTransform;

            if (_coverImage != null)
            {
                _defaultCoverSprite = _coverImage.sprite;
                _defaultCoverColor = _coverImage.color;
                _hasDefaultCoverSprite = true;
            }

            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = _sortingOrder;
            }

            HideImmediately();
        }

        public void Cover(string coverSpriteKey, Action onComplete = null)
        {
            ChangeCoverSprite(coverSpriteKey, () => Cover(onComplete));
        }

        public void Cover(Action onComplete = null)
        {
            PlaySlideCover(onComplete);
        }

        private void PlaySlideCover(Action onComplete = null)
        {
            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();
            _bubbleSequence?.Kill();
            ClearBubbleImages();
            SetCoverImageAlpha(_defaultCoverColor.a);

            SetBlock(true);
            _isTransitioning = true;
            _isCovered = false;
            _isBubbleTransitionCovered = false;

            _coverRect.anchoredPosition = new Vector2(0f, _screenHeight);

            _tween = _coverRect
                .DOAnchorPos(Vector2.zero, _coverDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _isCovered = true;
                    _isTransitioning = false;
                    onComplete?.Invoke();
                });
        }

        public void RestoreDefaultCoverSprite()
        {
            _coverSpriteRequestId++;
            ReleaseCoverSprite();

            if (_coverImage != null && _hasDefaultCoverSprite)
            {
                _coverImage.sprite = _defaultCoverSprite;
                SetCoverImageAlpha(_defaultCoverColor.a);
            }
        }

        public void Reveal(Action onComplete = null)
        {
            if (_isBubbleTransitionCovered)
            {
                RevealWithBubbles(onComplete);
                return;
            }

            PlaySlideReveal(onComplete);
        }

        private void PlaySlideReveal(Action onComplete = null)
        {
            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            if (!_isCovered)
            {
                HideImmediately();
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();
            _bubbleSequence?.Kill();
            ClearBubbleImages();
            SetCoverImageAlpha(_defaultCoverColor.a);

            SetBlock(true);
            _isTransitioning = true;
            _coverRect.anchoredPosition = Vector2.zero;

            _tween = _coverRect
                .DOAnchorPos(new Vector2(0f, _screenHeight), _revealDuration)
                .SetEase(Ease.OutSine)
                .SetDelay(_revealDelay)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    HideImmediately();
                    onComplete?.Invoke();
                });
        }

        public void ShowImmediately()
        {
            _tween?.Kill();
            _bubbleSequence?.Kill();
            ClearBubbleImages();

            if (_coverRect != null)
                _coverRect.anchoredPosition = Vector2.zero;

            _isCovered = true;
            _isTransitioning = false;
            _isBubbleTransitionCovered = false;
            SetCoverImageAlpha(_defaultCoverColor.a);
            SetBlock(true);
        }

        public void HideImmediately()
        {
            _tween?.Kill();
            _bubbleSequence?.Kill();
            ClearBubbleImages();

            if (_coverRect != null)
                _coverRect.anchoredPosition = new Vector2(0f, _screenHeight);

            _isCovered = false;
            _isTransitioning = false;
            _isBubbleTransitionCovered = false;
            SetCoverImageAlpha(_defaultCoverColor.a);
            SetBlock(false);
        }

        private void SetBlock(bool isActive)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = isActive ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isActive;
            _canvasGroup.interactable = isActive;
        }

        private void ChangeCoverSprite(string coverSpriteKey, Action onResolved = null)
        {
            if (_coverImage == null || string.IsNullOrWhiteSpace(coverSpriteKey))
            {
                onResolved?.Invoke();
                return;
            }

            KeyContainer.Sprites.Add(coverSpriteKey);

            int requestId = ++_coverSpriteRequestId;

            GameManager.Addressable.LoadSprite(
                coverSpriteKey,
                (sprite, handle) =>
                {
                    if (requestId != _coverSpriteRequestId || _coverImage == null)
                    {
                        if (handle.IsValid())
                            Addressables.Release(handle);

                        return;
                    }

                    ReleaseCoverSprite();
                    _coverSpriteHandle = handle;
                    _coverImage.type = Image.Type.Simple;
                    _coverImage.preserveAspect = false;
                    _coverImage.sprite = sprite;
                    onResolved?.Invoke();
                },
                failedKey =>
                {
                    if (requestId != _coverSpriteRequestId)
                        return;

                    onResolved?.Invoke();
                });
        }

        public void CoverWithBubbles(Action onComplete = null)
        {
            if (!_useBubbleTransition || string.IsNullOrWhiteSpace(GetBubbleSpritesAddress()))
            {
                CompleteTransparentCover(onComplete);
                return;
            }

            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            SetBlock(true);
            _isTransitioning = true;
            _coverRect.anchoredPosition = Vector2.zero;
            SetCoverImageAlpha(0f);

            PreloadBubbleSfx();
            EnsureBubbleSprites(loaded =>
            {
                if (this == null)
                    return;

                if (!loaded)
                {
                    CompleteTransparentCover(onComplete);
                    return;
                }

                PlayBubbleCover(onComplete);
            });
        }

        private void CompleteTransparentCover(Action onComplete = null)
        {
            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();
            _bubbleSequence?.Kill();
            ClearBubbleImages();

            SetBlock(true);
            _isCovered = true;
            _isTransitioning = false;
            _isBubbleTransitionCovered = true;
            _coverRect.anchoredPosition = Vector2.zero;
            SetCoverImageAlpha(0f);
            onComplete?.Invoke();
        }

        private void RevealWithBubbles(Action onComplete = null)
        {
            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();
            _bubbleSequence?.Kill();

            SetBlock(true);
            _isTransitioning = true;
            _coverRect.anchoredPosition = Vector2.zero;
            SetCoverImageAlpha(0f);

            EnsureBubbleRoot();
            _bubbleRoot.gameObject.SetActive(true);
            _bubbleCanvasGroup.alpha = 1f;

            Vector2 size = GetTransitionSize();
            if (_bubbleImages.Count == 0)
                CreateBubbleCurtain(size, false);

            _bubbleSequence = DOTween.Sequence().SetUpdate(true);
            AddBubbleRevealTweens(_bubbleSequence, size, _bubbleRevealDuration);
            _bubbleSequence.Insert(_bubbleRevealDuration * 0.45f,
                DOTween.To(() => _bubbleCanvasGroup.alpha, value => _bubbleCanvasGroup.alpha = value,
                    0f, _bubbleRevealDuration * 0.55f)
                .SetEase(Ease.OutSine));
            _bubbleSequence.OnComplete(() =>
            {
                HideImmediately();
                onComplete?.Invoke();
            });
        }

        private void PlayBubbleCover(Action onComplete = null)
        {
            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();
            _bubbleSequence?.Kill();

            SetBlock(true);
            _isTransitioning = true;
            _isCovered = false;
            _isBubbleTransitionCovered = false;
            _coverRect.anchoredPosition = Vector2.zero;
            SetCoverImageAlpha(0f);

            EnsureBubbleRoot();
            ClearBubbleImages();
            _bubbleRoot.gameObject.SetActive(true);
            _bubbleCanvasGroup.alpha = 1f;

            PlayBubbleSfx();

            Vector2 size = GetTransitionSize();
            _bubbleSequence = DOTween.Sequence().SetUpdate(true);
            CreateBubbleCurtain(size, true);
            AddBubbleCoverTweens(_bubbleSequence, size, _bubbleCoverDuration);
            _bubbleSequence.OnComplete(() =>
            {
                SetCoverImageAlpha(0f);
                _isCovered = true;
                _isTransitioning = false;
                _isBubbleTransitionCovered = true;
                onComplete?.Invoke();
            });
        }

        private void EnsureBubbleSprites(Action<bool> onComplete)
        {
            if (_bubbleSpritesReady)
            {
                onComplete?.Invoke(_bubbleSprites.Count > 0);
                return;
            }

            _bubbleSpriteReadyCallbacks.Add(onComplete);

            if (_isLoadingBubbleSprites)
                return;

            _isLoadingBubbleSprites = true;
            _bubbleSprites.Clear();
            string spritesAddress = GetBubbleSpritesAddress();

            _bubbleSpriteLocationsHandle = Addressables.LoadResourceLocationsAsync(spritesAddress, typeof(Sprite));
            _bubbleSpriteLocationsHandle.Completed += locationsHandle =>
            {
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded ||
                    locationsHandle.Result == null ||
                    locationsHandle.Result.Count == 0)
                {
                    LoadBubbleSpritesBySubAddress(spritesAddress);
                    return;
                }

                _bubbleSpritesHandle = Addressables.LoadAssetsAsync<Sprite>(
                    locationsHandle.Result,
                    sprite =>
                    {
                        if (sprite != null && !_bubbleSprites.Contains(sprite))
                            _bubbleSprites.Add(sprite);
                    });

                _bubbleSpritesHandle.Completed += spritesHandle =>
                {
                    if (spritesHandle.Status == AsyncOperationStatus.Succeeded && spritesHandle.Result != null)
                    {
                        foreach (Sprite sprite in spritesHandle.Result)
                        {
                            if (sprite != null && !_bubbleSprites.Contains(sprite))
                                _bubbleSprites.Add(sprite);
                        }
                    }

                    LoadBubbleSpritesBySubAddress(spritesAddress);
                };
            };
        }

        private void LoadBubbleSpritesBySubAddress(string spritesAddress)
        {
            int fallbackCount = Mathf.Max(0, _bubbleSpriteFallbackCount);
            if (fallbackCount == 0 || string.IsNullOrWhiteSpace(spritesAddress))
            {
                CompleteBubbleSpriteLoad();
                return;
            }

            int pendingCount = fallbackCount;

            void CompleteOne()
            {
                pendingCount--;
                if (pendingCount > 0)
                    return;

                CompleteBubbleSpriteLoad();
            }

            for (int i = 0; i < fallbackCount; i++)
            {
                string spriteAddress = $"{spritesAddress}[{_bubbleSpriteNamePrefix}{i}]";
                AsyncOperationHandle<IList<IResourceLocation>> locationHandle =
                    Addressables.LoadResourceLocationsAsync(spriteAddress, typeof(Sprite));
                _bubbleSpriteSubLocationHandles.Add(locationHandle);

                locationHandle.Completed += completedLocationHandle =>
                {
                    if (completedLocationHandle.Status != AsyncOperationStatus.Succeeded ||
                        completedLocationHandle.Result == null ||
                        completedLocationHandle.Result.Count == 0)
                    {
                        CompleteOne();
                        return;
                    }

                    AsyncOperationHandle<Sprite> spriteHandle =
                        Addressables.LoadAssetAsync<Sprite>(completedLocationHandle.Result[0]);
                    _bubbleSpriteSubHandles.Add(spriteHandle);

                    spriteHandle.Completed += completedSpriteHandle =>
                    {
                        if (completedSpriteHandle.Status == AsyncOperationStatus.Succeeded &&
                            completedSpriteHandle.Result != null &&
                            !_bubbleSprites.Contains(completedSpriteHandle.Result))
                        {
                            _bubbleSprites.Add(completedSpriteHandle.Result);
                        }

                        CompleteOne();
                    };
                };
            }
        }

        private void CompleteBubbleSpriteLoad()
        {
            _bubbleSprites.Sort(CompareBubbleSprites);
            _bubbleSpritesReady = _bubbleSprites.Count > 0;
            _isLoadingBubbleSprites = false;
            FlushBubbleSpriteCallbacks(_bubbleSpritesReady);
        }

        private void FlushBubbleSpriteCallbacks(bool loaded)
        {
            for (int i = 0; i < _bubbleSpriteReadyCallbacks.Count; i++)
                _bubbleSpriteReadyCallbacks[i]?.Invoke(loaded);

            _bubbleSpriteReadyCallbacks.Clear();
        }

        private void EnsureBubbleRoot()
        {
            if (_bubbleRoot != null || _coverRect == null)
                return;

            GameObject rootObject = new("Bubble Transition Effects", typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(_coverRect, false);

            _bubbleRoot = rootObject.transform as RectTransform;
            _bubbleRoot.anchorMin = Vector2.zero;
            _bubbleRoot.anchorMax = Vector2.one;
            _bubbleRoot.offsetMin = Vector2.zero;
            _bubbleRoot.offsetMax = Vector2.zero;
            _bubbleRoot.pivot = new Vector2(0.5f, 0.5f);

            _bubbleCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            _bubbleCanvasGroup.alpha = 0f;
            _bubbleCanvasGroup.blocksRaycasts = false;
            _bubbleCanvasGroup.interactable = false;
            rootObject.SetActive(false);
        }

        private void CreateBubbleCurtain(Vector2 size, bool startBelow)
        {
            List<Sprite> floatingSprites = GetFloatingSprites();
            int spriteCount = floatingSprites.Count;
            if (spriteCount == 0)
                return;

            if (_bubbleImages.Count == 0)
                _bubbleCoveredPositions.Clear();

            int columns = Mathf.Clamp(_bubbleCurtainColumns, 5, 12);
            float stepX = columns <= 1 ? size.x : size.x / (columns - 1);
            float visualScale = Mathf.Clamp(_bubbleVisualScale, 0.55f, 1.2f);
            float targetDiameter = Mathf.Clamp(stepX * _bubbleCurtainOverlap * visualScale, 72f, 180f);
            float stepY = targetDiameter * 0.7f;
            int rows = Mathf.CeilToInt(size.y / stepY) + 3;

            int bubbleCount = columns * rows;
            for (int i = 0; i < bubbleCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                Sprite sprite = floatingSprites[i % spriteCount];
                Image image = CreateBubbleImage(sprite, $"Bubble Curtain {i + 1}");
                RectTransform rectTransform = image.rectTransform;

                float scale = GetBubbleCurtainScale(sprite, targetDiameter) * UnityEngine.Random.Range(0.9f, 1.18f);
                rectTransform.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);

                float x = -size.x * 0.5f + column * stepX + UnityEngine.Random.Range(-stepX * 0.16f, stepX * 0.16f);
                float targetY = -size.y * 0.5f - stepY + row * stepY +
                                UnityEngine.Random.Range(-stepY * 0.16f, stepY * 0.16f);
                Vector2 coveredPosition = new(x, targetY);
                Vector2 startPosition = startBelow
                    ? new Vector2(x + UnityEngine.Random.Range(-24f, 24f),
                        targetY - size.y - targetDiameter - UnityEngine.Random.Range(60f, 180f))
                    : coveredPosition;

                rectTransform.anchoredPosition = startPosition;
                rectTransform.SetAsLastSibling();
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-12f, 12f));
                _bubbleCoveredPositions.Add(coveredPosition);
            }
        }

        private void AddBubbleCoverTweens(Sequence sequence, Vector2 size, float duration)
        {
            if (sequence == null)
                return;

            duration = Mathf.Max(0.05f, duration);
            float halfHeight = size.y * 0.5f;
            for (int i = 0; i < _bubbleImages.Count; i++)
            {
                RectTransform rectTransform = _bubbleImages[i] != null ? _bubbleImages[i].rectTransform : null;
                if (rectTransform == null)
                    continue;

                Vector2 coveredPosition = i < _bubbleCoveredPositions.Count
                    ? _bubbleCoveredPositions[i]
                    : rectTransform.anchoredPosition;

                float rowRate = Mathf.InverseLerp(-halfHeight, halfHeight, coveredPosition.y);
                float delay = Mathf.Lerp(0f, duration * 0.34f, rowRate) + UnityEngine.Random.Range(0f, duration * 0.05f);
                float moveDuration = Mathf.Max(0.05f, duration - delay);
                sequence.Insert(delay, rectTransform
                    .DOAnchorPos(coveredPosition, moveDuration)
                    .SetEase(Ease.OutSine));
            }
        }

        private void AddBubbleRevealTweens(Sequence sequence, Vector2 size, float duration)
        {
            if (sequence == null)
                return;

            duration = Mathf.Max(0.05f, duration);
            float halfHeight = size.y * 0.5f;
            for (int i = 0; i < _bubbleImages.Count; i++)
            {
                RectTransform rectTransform = _bubbleImages[i] != null ? _bubbleImages[i].rectTransform : null;
                if (rectTransform == null)
                    continue;

                Vector2 startPosition = rectTransform.anchoredPosition;
                float rowRate = Mathf.InverseLerp(-halfHeight, halfHeight, startPosition.y);
                float delay = Mathf.Lerp(0f, duration * 0.22f, rowRate) + UnityEngine.Random.Range(0f, duration * 0.04f);
                float endY = startPosition.y + size.y + _bubbleFloatDistance + UnityEngine.Random.Range(40f, 180f);
                float moveDuration = Mathf.Max(0.05f, duration - delay);
                sequence.Insert(delay, rectTransform
                    .DOAnchorPos(new Vector2(startPosition.x + UnityEngine.Random.Range(-40f, 40f), endY), moveDuration)
                    .SetEase(Ease.InSine));
            }
        }

        private float GetBubbleCurtainScale(Sprite sprite, float targetDiameter)
        {
            float maxSize = Mathf.Max(sprite.rect.width, sprite.rect.height);
            return targetDiameter / Mathf.Max(1f, maxSize);
        }

        private Image CreateBubbleImage(Sprite sprite, string objectName)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(_bubbleRoot, false);

            RectTransform rectTransform = imageObject.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            _bubbleImages.Add(image);
            return image;
        }

        private List<Sprite> GetFloatingSprites()
        {
            List<Sprite> result = new();

            foreach (Sprite sprite in _bubbleSprites)
            {
                bool isFoam = sprite.rect.width >= sprite.rect.height * 2.2f;
                bool isColumn = sprite.rect.height >= sprite.rect.width * 2.2f;
                bool isTooLarge = Mathf.Max(sprite.rect.width, sprite.rect.height) >= 260f;

                if (!isFoam && !isColumn && !isTooLarge)
                    result.Add(sprite);
            }

            return result;
        }

        private Vector2 GetTransitionSize()
        {
            if (_coverRect != null && _coverRect.rect.width > 1f && _coverRect.rect.height > 1f)
                return _coverRect.rect.size;

            if (_canvas != null && _canvas.transform is RectTransform canvasRect &&
                canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return canvasRect.rect.size;

            return new Vector2(1080f, _screenHeight);
        }

        private void ClearBubbleImages()
        {
            for (int i = _bubbleImages.Count - 1; i >= 0; i--)
            {
                Image image = _bubbleImages[i];
                if (image != null)
                    Destroy(image.gameObject);
            }

            _bubbleImages.Clear();
            _bubbleCoveredPositions.Clear();

            if (_bubbleRoot != null)
            {
                _bubbleRoot.gameObject.SetActive(false);
                if (_bubbleCanvasGroup != null)
                    _bubbleCanvasGroup.alpha = 0f;
            }
        }

        private string GetBubbleSpritesAddress()
            => NormalizeBubbleAddress(_bubbleSpritesAddress, DefaultBubbleSpritesAddress, LegacyBubbleSpritesAddress);

        private string GetBubbleSfxAddress()
            => NormalizeBubbleAddress(_bubbleSfxAddress, DefaultBubbleSfxAddress, LegacyBubbleSfxAddress);

        private static string NormalizeBubbleAddress(string address, string defaultAddress, string legacyAddress)
        {
            if (string.IsNullOrWhiteSpace(address))
                return string.Empty;

            string trimmedAddress = address.Trim();
            return string.Equals(trimmedAddress, legacyAddress, StringComparison.OrdinalIgnoreCase)
                ? defaultAddress
                : trimmedAddress;
        }

        private static int CompareBubbleSprites(Sprite left, Sprite right)
        {
            int leftOrder = GetBubbleSpriteOrder(left);
            int rightOrder = GetBubbleSpriteOrder(right);
            int orderCompare = leftOrder.CompareTo(rightOrder);
            if (orderCompare != 0)
                return orderCompare;

            return string.Compare(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty,
                StringComparison.Ordinal);
        }

        private static int GetBubbleSpriteOrder(Sprite sprite)
        {
            string spriteName = sprite != null ? sprite.name : string.Empty;
            int separatorIndex = spriteName.LastIndexOf('_');
            if (separatorIndex >= 0 &&
                separatorIndex < spriteName.Length - 1 &&
                int.TryParse(spriteName.Substring(separatorIndex + 1), out int order))
            {
                return order;
            }

            return int.MaxValue;
        }

        private void SetCoverImageAlpha(float alpha)
        {
            if (_coverImage == null)
                return;

            Color color = _coverImage.color;
            color.a = alpha;
            _coverImage.color = color;
        }

        private void PreloadBubbleSfx()
        {
            string sfxAddress = GetBubbleSfxAddress();
            if (string.IsNullOrWhiteSpace(sfxAddress) || _isLoadingBubbleSfx || _bubbleSfxClip != null)
                return;

            _isLoadingBubbleSfx = true;
            _bubbleSfxLocationsHandle = Addressables.LoadResourceLocationsAsync(sfxAddress, typeof(AudioClip));
            _bubbleSfxLocationsHandle.Completed += locationsHandle =>
            {
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded ||
                    locationsHandle.Result == null ||
                    locationsHandle.Result.Count == 0)
                {
                    _isLoadingBubbleSfx = false;
                    return;
                }

                _bubbleSfxHandle = Addressables.LoadAssetAsync<AudioClip>(locationsHandle.Result[0]);
                _bubbleSfxHandle.Completed += clipHandle =>
                {
                    _isLoadingBubbleSfx = false;

                    if (clipHandle.Status == AsyncOperationStatus.Succeeded)
                        _bubbleSfxClip = clipHandle.Result;
                };
            };
        }

        private void PlayBubbleSfx()
        {
            if (string.IsNullOrWhiteSpace(GetBubbleSfxAddress()))
                return;

            if (_bubbleSfxClip == null)
            {
                PreloadBubbleSfx();
                return;
            }

            EnsureBubbleAudioSource();
            if (_bubbleAudioSource != null)
                _bubbleAudioSource.PlayOneShot(_bubbleSfxClip, _bubbleSfxVolume);
        }

        private void EnsureBubbleAudioSource()
        {
            if (_bubbleAudioSource != null)
                return;

            _bubbleAudioSource = gameObject.AddComponent<AudioSource>();
            _bubbleAudioSource.playOnAwake = false;
            _bubbleAudioSource.loop = false;
        }

        private void ReleaseCoverSprite()
        {
            if (!_coverSpriteHandle.IsValid())
                return;

            Addressables.Release(_coverSpriteHandle);
            _coverSpriteHandle = default;
        }

        private void OnDestroy()
        {
            _coverSpriteRequestId++;
            _bubbleSequence?.Kill();
            ReleaseCoverSprite();

            if (_bubbleSpritesHandle.IsValid())
                Addressables.Release(_bubbleSpritesHandle);

            if (_bubbleSpriteLocationsHandle.IsValid())
                Addressables.Release(_bubbleSpriteLocationsHandle);

            for (int i = 0; i < _bubbleSpriteSubLocationHandles.Count; i++)
            {
                if (_bubbleSpriteSubLocationHandles[i].IsValid())
                    Addressables.Release(_bubbleSpriteSubLocationHandles[i]);
            }

            for (int i = 0; i < _bubbleSpriteSubHandles.Count; i++)
            {
                if (_bubbleSpriteSubHandles[i].IsValid())
                    Addressables.Release(_bubbleSpriteSubHandles[i]);
            }

            if (_bubbleSfxLocationsHandle.IsValid())
                Addressables.Release(_bubbleSfxLocationsHandle);

            if (_bubbleSfxHandle.IsValid())
                Addressables.Release(_bubbleSfxHandle);

            if (Instance == this)
                Instance = null;
        }
    }
}
