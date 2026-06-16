using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiTileView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TMP_Text _indexText;

        [Header("색상")]
        [SerializeField] private Color _hiddenColor = new(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color _revealedColor = new(1f, 1f, 1f, 1f);

        public int TileIndex { get; private set; }
        public bool IsRevealed { get; private set; }

        private void Awake()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
        }

        public void Init(int tileIndex)
        {
            TileIndex = tileIndex;

            if (_indexText != null)
                _indexText.text = (tileIndex + 1).ToString();
        }

        public void SetRevealed(bool isRevealed)
        {
            IsRevealed = isRevealed;

            if (_backgroundImage != null)
                _backgroundImage.color = isRevealed ? _revealedColor : _hiddenColor;
        }
    }
}
