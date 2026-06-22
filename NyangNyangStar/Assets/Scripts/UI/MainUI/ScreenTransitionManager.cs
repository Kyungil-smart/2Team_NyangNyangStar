using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Transition
{
    public class ScreenTransitionManager : MonoBehaviour
    {
        public static ScreenTransitionManager Instance { get; private set; }

        [Header("Transition")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _coverRect;

        [Header("Option")]
        [SerializeField] private float _screenHeight = 1920f;
        [SerializeField] private float _coverDuration = 0.35f;
        [SerializeField] private float _revealDuration = 0.2f;
        [SerializeField] private float _revealDelay = 0.15f;
        [SerializeField] private int _sortingOrder = 9999;

        private Tween _tween;
        private bool _isCovered;
        private bool _isTransitioning;

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

            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = _sortingOrder;
            }

            HideImmediately();
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
    }
}
