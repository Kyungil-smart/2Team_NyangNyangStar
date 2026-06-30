using System;
using Core.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace UI.NyangQuarium.Quest
{
    // QuestPopUp Image 하나에 Addressable 스프라이트를 로드
    internal sealed class NyangQuariumQuestPopUpSpriteController
    {
        private readonly Image _image;
        private AsyncOperationHandle<Sprite> _handle;
        private int _requestId;
        private bool _disposed;

        public NyangQuariumQuestPopUpSpriteController(Image image)
            => _image = image;

        // Addressable 키로 스프라이트 교체 (이전 요청, 핸들은 무시/해제)
        public void ChangeSprite(string key, Action onResolved = null)
        {
            if (_disposed || _image == null || string.IsNullOrWhiteSpace(key))
                return;

            int currentRequestId = ++_requestId;

            GameManager.Addressable.LoadSprite(
                key,
                (sprite, handle) =>
                {
                    if (_disposed || currentRequestId != _requestId)
                    {
                        if (handle.IsValid())
                            Addressables.Release(handle);

                        return;
                    }

                    Release();

                    _handle = handle;
                    _image.sprite = sprite;
                    onResolved?.Invoke();
                },
                failedKey =>
                {
                    if (_disposed || currentRequestId != _requestId)
                        return;

                    DebugTool.Warning($"{failedKey} : QuestPopUp Sprite load failed", DebugType.UI);
                    onResolved?.Invoke();
                });
        }

        // 팝업 닫힘 등에서 호출 — 진행 중인 로드 취소 및 핸들 해제
        public void Dispose()
        {
            _disposed = true;
            ++_requestId;
            Release();
        }

        private void Release()
        {
            if (!_handle.IsValid())
                return;

            Addressables.Release(_handle);
            _handle = default;
        }
    }
}
