using System;
using UnityEngine;
using Util;

public class NyangNyangSnapPhotoFrameCapture : MonoBehaviour
{
    [Header("Capture")]
    [Tooltip("사진을 찍을 전용 카메라")]
    [SerializeField] private Camera _photoCamera;

    [Tooltip("카메라 화면을 저장할 RenderTexture")]
    [SerializeField] private RenderTexture _renderTexture;

    private Sprite _lastCapturedSprite;

    public Sprite LastCapturedSprite => _lastCapturedSprite;

    public void CapturePhoto(Action<Sprite> onCaptured)
    {
        if (_photoCamera == null)
        {
            DebugTool.Warning("[NyangNyangSnapPhotoFrameCapture] PhotoCamera가 없습니다.", DebugType.UI, this);
            onCaptured?.Invoke(null);
            return;
        }

        if (_renderTexture == null)
        {
            DebugTool.Warning("[NyangNyangSnapPhotoFrameCapture] RenderTexture가 없습니다.", DebugType.UI, this);
            onCaptured?.Invoke(null);
            return;
        }

        // 1. 카메라가 RenderTexture에 렌더링하도록 설정
        _photoCamera.targetTexture = _renderTexture;

        // 2. 카메라 화면을 RenderTexture에 그림
        _photoCamera.Render();

        // 3. RenderTexture 내용을 Texture2D로 복사
        Texture2D texture = new Texture2D(
            _renderTexture.width,
            _renderTexture.height,
            TextureFormat.RGBA32,
            false
        );

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _renderTexture;

        texture.ReadPixels(
            new Rect(0, 0, _renderTexture.width, _renderTexture.height),
            0,
            0
        );

        texture.Apply();

        RenderTexture.active = previous;

        // 4. 이전 캡처 Sprite 정리
        ClearLastCapturedSprite();

        // 5. Texture2D를 Sprite로 변환
        _lastCapturedSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        DebugTool.Log(
            $"[NyangNyangSnapPhotoFrameCapture] 사진 캡처 성공 - Size: {texture.width}x{texture.height}",
            DebugType.UI,
            this
        );

        // 6. 찍은 사진 전달
        onCaptured?.Invoke(_lastCapturedSprite);
    }

    private void ClearLastCapturedSprite()
    {
        if (_lastCapturedSprite == null) return;

        Texture2D texture = _lastCapturedSprite.texture;

        Destroy(_lastCapturedSprite);

        if (texture != null)
            Destroy(texture);

        _lastCapturedSprite = null;
    }

    private void OnDestroy()
    {
        ClearLastCapturedSprite();
    }
}