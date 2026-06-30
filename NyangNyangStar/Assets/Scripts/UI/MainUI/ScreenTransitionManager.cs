using System;
using Core.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;

namespace UI.Transition
{
    public class ScreenTransitionManager : MonoBehaviour
    {
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

        private Tween _tween;
        private bool _isCovered;
        private bool _isTransitioning;
        private Sprite _defaultCoverSprite;
        private AsyncOperationHandle<Sprite> _coverSpriteHandle;
        private int _coverSpriteRequestId;
        private bool _hasDefaultCoverSprite;

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
            if (_coverRect == null)
            {
                _isTransitioning = false;
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();

            SetBlock(true);
            _isTransitioning = true;
            _isCovered = false;

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
                _coverImage.sprite = _defaultCoverSprite;
        }

        public void Reveal(Action onComplete = null)
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

            if (_coverRect != null)
                _coverRect.anchoredPosition = Vector2.zero;

            _isCovered = true;
            _isTransitioning = false;
            SetBlock(true);
        }

        public void HideImmediately()
        {
            _tween?.Kill();

            if (_coverRect != null)
                _coverRect.anchoredPosition = new Vector2(0f, _screenHeight);

            _isCovered = false;
            _isTransitioning = false;
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
            ReleaseCoverSprite();

            if (Instance == this)
                Instance = null;
        }
    }
}
