using UI;
using Util;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiAddressableImage : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private string _spriteKey;
        [SerializeField] private bool _loadOnEnable = true;
        [SerializeField] private bool _nativeSize;
        [SerializeField] private bool _useTintColor;
        [SerializeField] private Color _tintColor = Color.white;

        private UISpriteController _controller;
        private string _loadedKey;

        private void Awake()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            if (_image != null && _controller == null)
                _controller = new UISpriteController(_image);
        }

        private void OnEnable()
        {
            if (_loadOnEnable)
                Load();
        }

        public void SetKey(string spriteKey, bool loadImmediately = true)
        {
            _spriteKey = spriteKey;

            if (loadImmediately)
                Load();
        }

        public void SetTint(Color color)
        {
            _tintColor = color;
            _useTintColor = true;

            if (_controller != null)
                _controller.ChangeColor(color);
            else if (_image != null)
                _image.color = color;
        }

        public void Load()
        {
            if (_image == null)
            {
                DebugTool.Warning("[FindMoongchiAddressableImage] Image가 연결되지 않았습니다.", DebugType.FindMoongchi, this);
                return;
            }

            if (string.IsNullOrWhiteSpace(_spriteKey))
            {
                DebugTool.Warning($"[FindMoongchiAddressableImage] Sprite Key가 비어 있습니다. Object={name}", DebugType.FindMoongchi, this);
                return;
            }

            if (_controller == null)
                _controller = new UISpriteController(_image);

            if (_useTintColor)
                _controller.ChangeColor(_tintColor);

            _loadedKey = _spriteKey;
            KeyContainer.Sprites.Add(_spriteKey);
            _controller.ChangeSprite(_spriteKey, _nativeSize);
        }

        public void Clear()
        {
            _loadedKey = null;
            _controller?.ClearSprite();
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
            _controller = null;
        }
    }
}
