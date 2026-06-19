using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public class FindMoongchiTargetHintView : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private Image _iconImage;

        [Header("Optional")]
        [SerializeField] private TMP_Text _nameText;

        [Header("Found Mark")]
        [SerializeField] private bool _showFoundMark = true;
        [SerializeField] private GameObject _foundMark;
        [SerializeField] private Image _foundMarkImage;
        [SerializeField] private string _foundMarkSpriteKey = FindMoongchiSpriteKeys.IconCheck;
        [SerializeField] private bool _fitFoundMarkToIcon = true;
        [SerializeField] private Vector2 _foundMarkSize = new(42f, 42f);
        [SerializeField] private float _foundMarkScale = 0.45f;
        [SerializeField] private Vector2 _foundMarkOffset = Vector2.zero;
        [SerializeField] private bool _createFoundMarkIfMissing = true;

        [Header("Color")]
        [SerializeField] private Color _hiddenColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color _foundColor = Color.white;

        private UISpriteController _iconController;
        private UISpriteController _foundMarkController;
        private string _currentIconKey;
        private string _currentFoundMarkKey;

        private void Awake()
        {
            if (_iconImage == null)
                _iconImage = GetComponent<Image>();

            if (_iconImage != null)
            {
                _iconImage.preserveAspect = true;
                _iconController = new UISpriteController(_iconImage);
            }

            ResolveFoundMark();
            SetFoundMark(false);
        }

        public void SetData(FindMoongchiTargetHintViewData data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            Color targetColor = data.IsFound ? _foundColor : _hiddenColor;
            SetIcon(data, targetColor);

            if (_nameText != null)
                _nameText.text = data.TargetName;

            SetFoundMark(data.IsFound);
        }

        private void ResolveFoundMark()
        {
            if (_foundMarkImage == null && _foundMark != null)
                _foundMarkImage = _foundMark.GetComponent<Image>();

            if (_foundMark == null && _foundMarkImage != null)
                _foundMark = _foundMarkImage.gameObject;

            if (_foundMark == null && _createFoundMarkIfMissing)
            {
                GameObject go = new GameObject("FoundMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();

                _foundMark = go;
                _foundMarkImage = go.GetComponent<Image>();
            }

            if (_foundMarkImage == null)
                return;

            _foundMarkImage.raycastTarget = false;
            _foundMarkImage.preserveAspect = true;

            RectTransform rectTransform = _foundMarkImage.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ApplyFoundMarkRect(rectTransform);
            }

            _foundMarkController ??= new UISpriteController(_foundMarkImage);
        }


        private void ApplyFoundMarkRect(RectTransform foundMarkRect)
        {
            if (foundMarkRect == null)
                return;

            foundMarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            foundMarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            foundMarkRect.pivot = new Vector2(0.5f, 0.5f);
            foundMarkRect.anchoredPosition = _foundMarkOffset;

            Vector2 size = _foundMarkSize;

            if (_fitFoundMarkToIcon && _iconImage != null)
            {
                RectTransform iconRect = _iconImage.transform as RectTransform;

                if (iconRect != null)
                {
                    Vector2 iconSize = iconRect.rect.size;

                    if (iconSize.x > 0f && iconSize.y > 0f)
                        size = iconSize * Mathf.Max(0f, _foundMarkScale);
                }
            }

            foundMarkRect.sizeDelta = size;
            foundMarkRect.SetAsLastSibling();
        }

        private void SetIcon(FindMoongchiTargetHintViewData data, Color color)
        {
            if (_iconImage == null)
                return;

            _iconImage.preserveAspect = true;

            if (!string.IsNullOrWhiteSpace(data.IconKey))
            {
                if (_iconController == null)
                    _iconController = new UISpriteController(_iconImage);

                _iconController.ChangeColor(color);

                if (_currentIconKey != data.IconKey || _iconImage.sprite == null)
                {
                    _currentIconKey = data.IconKey;
                    _iconController.ChangeSprite(data.IconKey);
                }

                return;
            }

            _currentIconKey = null;
            _iconImage.sprite = data.Icon;
            _iconImage.preserveAspect = true;
            _iconImage.color = color;
        }

        private void SetFoundMark(bool isFound)
        {
            ResolveFoundMark();

            bool shouldShow = _showFoundMark && isFound;

            if (_foundMark != null)
                _foundMark.SetActive(shouldShow);

            if (!shouldShow || _foundMarkImage == null || string.IsNullOrWhiteSpace(_foundMarkSpriteKey))
                return;

            _foundMarkImage.preserveAspect = true;

            RectTransform rectTransform = _foundMarkImage.transform as RectTransform;
            if (rectTransform != null)
            {
                ApplyFoundMarkRect(rectTransform);
            }

            _foundMarkController ??= new UISpriteController(_foundMarkImage);

            if (_currentFoundMarkKey == _foundMarkSpriteKey && _foundMarkImage.sprite != null)
                return;

            _currentFoundMarkKey = _foundMarkSpriteKey;
            _foundMarkController.ChangeSprite(_foundMarkSpriteKey);
        }

        public void Clear()
        {
            _currentIconKey = null;

            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.color = _hiddenColor;
            }

            if (_nameText != null)
                _nameText.text = string.Empty;

            SetFoundMark(false);
        }

        private void OnDestroy()
        {
            _iconController?.Dispose();
            _iconController = null;

            _foundMarkController?.Dispose();
            _foundMarkController = null;
        }
    }
}
