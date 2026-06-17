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
        [SerializeField] private GameObject _foundMark;

        [Header("Color")]
        [SerializeField] private Color _hiddenColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color _foundColor = Color.white;

        private UISpriteController _iconController;
        private string _currentIconKey;

        private void Awake()
        {
            if (_iconImage == null)
                _iconImage = GetComponent<Image>();

            if (_iconImage != null)
                _iconController = new UISpriteController(_iconImage);
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

            if (_foundMark != null)
                _foundMark.SetActive(data.IsFound);
        }

        private void SetIcon(FindMoongchiTargetHintViewData data, Color color)
        {
            if (_iconImage == null)
                return;

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
            _iconImage.color = color;
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

            if (_foundMark != null)
                _foundMark.SetActive(false);
        }

        private void OnDestroy()
        {
            _iconController?.Dispose();
            _iconController = null;
        }
    }
}
