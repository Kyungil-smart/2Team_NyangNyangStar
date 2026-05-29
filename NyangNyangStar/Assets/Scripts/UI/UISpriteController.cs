using Core.Managers;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace UI
{
    public class UISpriteController
    {
        private readonly Image _image;

        private AsyncOperationHandle<Sprite> _handle;
        private int _requestId;
        private bool _disposed;

        public UISpriteController(Image image)
            => _image = image;

        public void ChangeSprite(string key)
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
                        GameManager.Addressable.Release(handle);
                        return;
                    }

                    ReleaseSprite();

                    _handle = handle;
                    _image.sprite = loadedSprite;
                },
                failedKey =>
                {
                    if (_disposed || currentRequestId != _requestId)
                        return;

                    DebugTool.Warning($"{failedKey} : Sprite 로드 실패", DebugType.UI);
                });
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

            GameManager.Addressable.Release(_handle);
            _handle = default;

            DebugTool.Log("기존 Sprite Release 완료", DebugType.Addressable);
        }

        public void Dispose()
        {
            _disposed = true;
            ++_requestId;

            ReleaseSprite();
        }
    }
}