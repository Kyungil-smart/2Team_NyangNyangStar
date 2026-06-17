using System.Collections;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiTileView : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("타일 앞면 이미지입니다. 공개되면 이 이미지가 꺼져 뒤 배경/목표물이 보입니다.")]
        [SerializeField] private Image _coverImage;
        [SerializeField] private TMP_Text _indexText;

        [Header("Addressable")]
        [SerializeField] private string _tileSpriteKey = FindMoongchiSpriteKeys.Tile;
        [SerializeField] private bool _loadTileSpriteOnAwake = true;

        [Header("색상")]
        [SerializeField] private Color _hiddenColor = Color.white;

        [Header("Reveal Animation")]
        [SerializeField] private bool _useRevealAnimation = true;
        [SerializeField] private float _revealDuration = 0.22f;
        [SerializeField] private AnimationCurve _revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private UISpriteController _spriteController;
        private RectTransform _rectTransform;
        private Coroutine _revealCoroutine;

        public int TileIndex { get; private set; }
        public bool IsRevealed { get; private set; }

        private void Awake()
        {
            _rectTransform = transform as RectTransform;

            if (_coverImage == null)
                _coverImage = GetComponent<Image>();

            if (_coverImage != null)
                _spriteController = new UISpriteController(_coverImage);

            if (_loadTileSpriteOnAwake)
                LoadTileSprite();
        }

        public void Init(int tileIndex)
        {
            TileIndex = tileIndex;

            if (_indexText != null)
                _indexText.text = (tileIndex + 1).ToString();
        }

        public void SetRevealed(bool isRevealed)
        {
            SetRevealed(isRevealed, false);
        }

        public void SetRevealed(bool isRevealed, bool animate)
        {
            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }

            if (IsRevealed == isRevealed && !animate)
            {
                ApplyRevealedState(isRevealed);
                return;
            }

            bool wasRevealed = IsRevealed;
            IsRevealed = isRevealed;

            if (animate && _useRevealAnimation && !wasRevealed && isRevealed && gameObject.activeInHierarchy)
            {
                _revealCoroutine = StartCoroutine(RevealAnimationCoroutine());
                return;
            }

            ApplyRevealedState(isRevealed);
        }

        public void ResetHidden()
        {
            SetRevealed(false, false);
        }

        private IEnumerator RevealAnimationCoroutine()
        {
            if (_coverImage == null || _rectTransform == null)
            {
                ApplyRevealedState(true);
                yield break;
            }

            _coverImage.enabled = true;
            _coverImage.color = _hiddenColor;
            _rectTransform.localEulerAngles = Vector3.zero;

            float halfDuration = Mathf.Max(0.01f, _revealDuration * 0.5f);
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float curved = _revealCurve != null ? _revealCurve.Evaluate(t) : t;
                float y = Mathf.Lerp(0f, 90f, curved);
                _rectTransform.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }

            _coverImage.enabled = false;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float curved = _revealCurve != null ? _revealCurve.Evaluate(t) : t;
                float y = Mathf.Lerp(90f, 0f, curved);
                _rectTransform.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }

            _rectTransform.localEulerAngles = Vector3.zero;
            _revealCoroutine = null;
        }

        private void ApplyRevealedState(bool isRevealed)
        {
            if (_rectTransform != null)
                _rectTransform.localEulerAngles = Vector3.zero;

            if (_coverImage == null)
                return;

            _coverImage.enabled = !isRevealed;

            if (!isRevealed)
                _coverImage.color = _hiddenColor;
        }

        public void LoadTileSprite()
        {
            if (_spriteController == null || string.IsNullOrWhiteSpace(_tileSpriteKey))
                return;

            _spriteController.ChangeColor(_hiddenColor);
            _spriteController.ChangeSprite(_tileSpriteKey);
        }

        private void OnDestroy()
        {
            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }

            _spriteController?.Dispose();
            _spriteController = null;
        }
    }
}
