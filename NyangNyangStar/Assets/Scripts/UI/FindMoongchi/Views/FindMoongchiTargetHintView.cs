using TMPro;
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

        public void SetData(FindMoongchiTargetHintViewData data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_iconImage != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.color = data.IsFound ? _foundColor : _hiddenColor;
            }

            if (_nameText != null)
                _nameText.text = data.TargetName;

            if (_foundMark != null)
                _foundMark.SetActive(data.IsFound);
        }

        public void Clear()
        {
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
    }
}