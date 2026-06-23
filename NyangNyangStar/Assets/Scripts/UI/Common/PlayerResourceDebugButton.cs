using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class PlayerResourceDebugButton : MonoBehaviour
    {
        private const float ReferenceHeight = 1920f;

        [SerializeField] private Button _button;
        [SerializeField] private PlayerResourceType _resourceType = PlayerResourceType.Energy;
        [SerializeField] private int _amount = 100;
        [SerializeField] private bool _applyTallScreenLayoutOffset;
        [SerializeField] private float _tallScreenYOffsetRatio = 0.45f;
        [SerializeField] private float _maxTallScreenYOffset = 240f;

        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private bool _hasBaseAnchoredPosition;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _rectTransform = GetComponent<RectTransform>();
            CacheBaseAnchoredPosition();
            ApplyTallScreenLayoutOffset();
        }

        private void OnEnable()
        {
            if (_button == null)
                return;

            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);
            ApplyTallScreenLayoutOffset();
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        private void Update()
        {
            ApplyTallScreenLayoutOffset();
        }

        public async void HandleClicked()
        {
            int safeAmount = Mathf.Max(0, _amount);

            if (safeAmount <= 0)
                return;

            bool added = await PlayerResourceManager.Instance.AddAsync(_resourceType, safeAmount);

            if (!added)
                DebugTool.Warning($"[PlayerResourceDebugButton] 자원 추가 실패: Type={_resourceType}, Amount={safeAmount}", DebugType.UI, this);
        }

        private void CacheBaseAnchoredPosition()
        {
            if (_rectTransform == null || _hasBaseAnchoredPosition)
                return;

            _baseAnchoredPosition = _rectTransform.anchoredPosition;
            _hasBaseAnchoredPosition = true;
        }

        private void ApplyTallScreenLayoutOffset()
        {
            if (!_applyTallScreenLayoutOffset || _rectTransform == null)
                return;

            CacheBaseAnchoredPosition();

            float extraHeight = Mathf.Max(0f, GetCanvasHeight() - ReferenceHeight);
            float yOffset = Mathf.Min(extraHeight * _tallScreenYOffsetRatio, _maxTallScreenYOffset);
            _rectTransform.anchoredPosition = new Vector2(_baseAnchoredPosition.x, _baseAnchoredPosition.y - yOffset);
        }

        private float GetCanvasHeight()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null || canvas.scaleFactor <= 0f)
                return Screen.height;

            return Screen.height / canvas.scaleFactor;
        }
    }
}
