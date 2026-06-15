using DG.Tweening;
using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapResultUISprite : UIBase
{
    private Image[] _starFillImages = new Image[5]; // 별점 표시용 Image 배열
    private Image _resultPhotoImage; // 최고 점수 사진을 표시할 Image
    private Image _poseGaugeFillImage;
    private Image _compositionGaugeFillImage;
    private Image _reactionGaugeFillImage;

    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapResultImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapResultImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapResultImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        InitImages();
        SetSprites();
    }

    private void InitImages()
    {
        _resultPhotoImage = GetImage((int)NyangNyangSnapResultImages.ResultPhotoImage);

        _poseGaugeFillImage = GetImage((int)NyangNyangSnapResultImages.PoseGaugeFillImage);
        _compositionGaugeFillImage = GetImage((int)NyangNyangSnapResultImages.CompositionGaugeFillImage);
        _reactionGaugeFillImage = GetImage((int)NyangNyangSnapResultImages.ReactionGaugeFillImage);

        _starFillImages[0] = GetImage((int)NyangNyangSnapResultImages.StarFillImage1);
        _starFillImages[1] = GetImage((int)NyangNyangSnapResultImages.StarFillImage2);
        _starFillImages[2] = GetImage((int)NyangNyangSnapResultImages.StarFillImage3);
        _starFillImages[3] = GetImage((int)NyangNyangSnapResultImages.StarFillImage4);
        _starFillImages[4] = GetImage((int)NyangNyangSnapResultImages.StarFillImage5);
    }

    private void SetSprites()
    {
        // 고정 UI Sprite만 여기서 관리
        // 실제 Addressables Sprite Key가 정해지면 "" 부분에 키를 넣으면 됨.

        SetSprite(NyangNyangSnapResultImages.Background, "NYS_Background");
        // SetSprite(NyangNyangSnapResultImages.PoseGaugeBackImage, "");
        // SetSprite(NyangNyangSnapResultImages.CompositionGaugeBackImage, "");
        // SetSprite(NyangNyangSnapResultImages.ReactionGaugeBackImage, "");
        //SetSprite(NyangNyangSnapResultImages.PoseGaugeFillImage, "");
        //SetSprite(NyangNyangSnapResultImages.CompositionGaugeFillImage, "");
        //SetSprite(NyangNyangSnapResultImages.ReactionGaugeFillImage, "");

        SetSprite(NyangNyangSnapResultImages.SelectPhotosButton, "Snap_Btn_Green");
        // SetSprite(NyangNyangSnapResultImages.RetryButton, "");
        // SetSprite(NyangNyangSnapResultImages.SnsButton, "");

        // 주의:
        // ResultPhotoImage, Star1~Star5는
        // 런타임 결과 표시용이라 고정 Sprite를 넣지 않는다.
    }

    private void SetSprite(NyangNyangSnapResultImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetStarSprites(int starCount)
    {
        string spriteKey = starCount >= 5 ? "Snap_Icon_PinkStar" : "Snap_Icon_Star";

        SetSprite(NyangNyangSnapResultImages.StarFillImage1, spriteKey);
        SetSprite(NyangNyangSnapResultImages.StarFillImage2, spriteKey);
        SetSprite(NyangNyangSnapResultImages.StarFillImage3, spriteKey);
        SetSprite(NyangNyangSnapResultImages.StarFillImage4, spriteKey);
        SetSprite(NyangNyangSnapResultImages.StarFillImage5, spriteKey);
    }

    public void ResetRuntimeImages()
    {
        SetGaugeImmediate(_poseGaugeFillImage, 0f);
        SetGaugeImmediate(_compositionGaugeFillImage, 0f);
        SetGaugeImmediate(_reactionGaugeFillImage, 0f);
        ResetStarImages();

        if (_resultPhotoImage != null)
        {
            _resultPhotoImage.gameObject.SetActive(true);
            _resultPhotoImage.enabled = true;
            _resultPhotoImage.type = Image.Type.Simple;
            _resultPhotoImage.fillAmount = 1f;

            Color color = _resultPhotoImage.color;
            color.a = 0f;
            _resultPhotoImage.color = color;
        }
    }

    public void SetResultPhoto(Sprite capturedSprite)
    {
        if (_resultPhotoImage == null)
        {
            DebugTool.Warning($"[NyangNyangSnapResultUISprite] 결과 사진 Image가 없습니다.", DebugType.UI, this);
            return;
        }

        if (capturedSprite == null)
        {
            DebugTool.Warning("[NyangNyangSnapResultUISprite] 표시할 캡처 Sprite가 없습니다.", DebugType.UI, this);
            return;
        }

        _resultPhotoImage.gameObject.SetActive(true);
        _resultPhotoImage.enabled = true;

        _resultPhotoImage.type = Image.Type.Simple;
        _resultPhotoImage.fillAmount = 1f;
        _resultPhotoImage.sprite = capturedSprite;
        _resultPhotoImage.color = Color.white;
        _resultPhotoImage.preserveAspect = true;

        Color color = _resultPhotoImage.color;
        color.a = 0f;
        _resultPhotoImage.color = color;

        DebugTool.Log(
            $"[NyangNyangSnapResultUISprite] 결과 이미지 적용 완료 / " +
            $"Target: {_resultPhotoImage.name}, " +
            $"Sprite: {capturedSprite.name}, " +
            $"Size: {capturedSprite.texture.width}x{capturedSprite.texture.height}",
            DebugType.UI,
            this
        );
    }

    public Tween CreatePhotoFadeTween(float targetAlpha, float duration)
    {
        if (_resultPhotoImage == null)
            return DOVirtual.DelayedCall(0f, () => { });

        return _resultPhotoImage.DOFade(targetAlpha, duration);
    }

    public Tween CreatePoseGaugeTween(float targetValue, float duration)
    {
        return CreateGaugeTween(_poseGaugeFillImage, targetValue, duration);
    }

    public Tween CreateCompositionGaugeTween(float targetValue, float duration)
    {
        return CreateGaugeTween(_compositionGaugeFillImage, targetValue, duration);
    }

    public Tween CreateReactionGaugeTween(float targetValue, float duration)
    {
        return CreateGaugeTween(_reactionGaugeFillImage, targetValue, duration);
    }

    public Sequence CreateStarFillSequence(int totalScore, float starFillDuration)
    {
        Sequence starSequence = DOTween.Sequence();

        int starCount = Mathf.Clamp(totalScore / 20 + 1, 1, 5);

        SetStarSprites(starCount);

        foreach (Image starImage in _starFillImages)
        {
            if (starImage == null) continue;

            starImage.fillAmount = 0f;
        }

        for (int i = 0; i < starCount; i++)
        {
            Image starImage = _starFillImages[i];

            if (starImage == null)
                continue;

            starSequence.Append(
                starImage.DOFillAmount(1f, starFillDuration)
                    .SetEase(Ease.OutQuad)
            );
        }

        return starSequence;
    }

    private Tween CreateGaugeTween(Image gaugeImage, float targetValue, float duration)
    {
        if (gaugeImage == null)
            return DOVirtual.DelayedCall(0f, () => { });

        gaugeImage.gameObject.SetActive(true);
        gaugeImage.enabled = true;
        gaugeImage.type = Image.Type.Filled;
        gaugeImage.fillMethod = Image.FillMethod.Horizontal;
        gaugeImage.fillOrigin = 0;
        gaugeImage.fillAmount = 0f;

        return gaugeImage.DOFillAmount(Mathf.Clamp01(targetValue), duration)
            .SetEase(Ease.OutQuad);
    }

    private void SetGaugeImmediate(Image gaugeImage, float value)
    {
        if (gaugeImage == null) return;

        gaugeImage.gameObject.SetActive(true);
        gaugeImage.enabled = true;
        gaugeImage.type = Image.Type.Filled;
        gaugeImage.fillMethod = Image.FillMethod.Horizontal;
        gaugeImage.fillOrigin = 0;
        gaugeImage.fillAmount = Mathf.Clamp01(value);
    }

    private void ResetStarImages()
    {
        foreach (Image starImage in _starFillImages)
        {
            if (starImage == null) continue;

            starImage.gameObject.SetActive(true);
            starImage.enabled = true;
            starImage.type = Image.Type.Filled;
            starImage.fillMethod = Image.FillMethod.Horizontal;
            starImage.fillOrigin = 0;
            starImage.fillAmount = 0f;
        }
    }

    private void OnDestroy()
    {
        if (_spriteController == null) return;

        foreach (UISpriteController controller in _spriteController)
        {
            controller?.ReleaseSprite();
        }
    }
}

public enum NyangNyangSnapResultImages
{
    Background,
    StarFillImage1,
    StarFillImage2,
    StarFillImage3,
    StarFillImage4, 
    StarFillImage5,
    ResultPhotoImage,
    PoseGaugeBackImage,
    PoseGaugeFillImage,
    CompositionGaugeBackImage,
    CompositionGaugeFillImage,
    ReactionGaugeBackImage,
    ReactionGaugeFillImage,
    SelectPhotosButton,
}