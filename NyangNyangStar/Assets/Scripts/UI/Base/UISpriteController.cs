using Core.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace UI
{
    public class UISpriteController
    {
        private readonly Image _image;
        private Color _color;
        private bool _isColorchange;
        
        private AsyncOperationHandle<Sprite> _handle;
        private int _requestId;
        private bool _disposed;

        public UISpriteController(Image image)
            => _image = image;

        public void ChangeSprite(string key, bool nativeSize = false, System.Action onLoaded = null)
        {
            if (_disposed)
                return;

            if (_image == null)
            {
                DebugTool.Warning("Image가 null 입니다.", DebugType.UI);
                return;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                DebugTool.Warning("Sprite Key가 비어 있습니다.", DebugType.UI);
                return;
            }

            int currentRequestId = ++_requestId;

            GameManager.Addressable.LoadSprite(key,
                (loadedSprite, handle) =>
                {
                    if (_disposed || currentRequestId != _requestId)
                    {
                        if (handle.IsValid())
                            Addressables.Release(handle);

                        return;
                    }

                    ReleaseSprite();

                    _handle = handle;
                    _image.sprite = loadedSprite;
                    
                    if(_isColorchange)
                        _image.color = _color;
                    
                    if(nativeSize)
                        _image.SetNativeSize();

                    onLoaded?.Invoke();
                },
                failedKey =>
                {
                    if (_disposed || currentRequestId != _requestId)
                        return;

                    DebugTool.Warning($"{failedKey} : Sprite 로드 실패", DebugType.UI);
                    onLoaded?.Invoke();
                });
        }

        public void ChangeColor(Color color)
        {
            _color = color;
            _isColorchange = true;

            if (_image != null)
                _image.color = color;
        }

        public void ResetColor()
        {
            _color = Color.white;
            _isColorchange = false;

            if (_image != null)
                _image.color = Color.white;
        }

        public void ClearSprite()
        {
            if (_disposed)
                return;
            
            ++_requestId;
            
            if(_image != null)
                _image.sprite = null;
            
            ReleaseSprite();
        }

        public void ReleaseSprite()
        {
            if (!_handle.IsValid())
                return;

            Addressables.Release(_handle);
            _handle = default;

            // DebugTool.Log("기존 Sprite Release 완료", DebugType.Addressable);
        }

        public void Dispose()
        {
            _disposed = true;
            ++_requestId;

            ReleaseSprite();
        }
    }
}