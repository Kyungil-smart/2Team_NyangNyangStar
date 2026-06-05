using DG.Tweening;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapResultUISprite : UIBase
{
    [Header("결과 이미지")]
    [Tooltip("최고 점수 사진을 표시할 Image 이름")]
    [SerializeField] private string _resultPhotoImageName = "SnapImage";

    [Header("별점 이미지")]
    [Tooltip("별점 Fill Image 이름 앞부분")]
    [SerializeField] private string _starFillImagePrefix = "StarFillImage";

    [Tooltip("별점 Fill Image 5개")]
    [SerializeField] private Image[] _starFillImages = new Image[5];

    private UISpriteController[] _spriteController;

    private Image _resultPhotoImage;
    private Image _poseGaugeFillImage;
    private Image _compositionGaugeFillImage;
    private Image _reactionGaugeFillImage;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapResultImages));

        _spriteController = new UISpriteController[(int)NyangNyangSnapResultImages.Count];

        for (int i = 0; i < (int)NyangNyangSnapResultImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        AutoAssignRuntimeImages();
        SetSprites();
    }

    private void SetSprites()
    {
        // 고정 UI Sprite만 여기서 관리
        // 실제 Addressables Sprite Key가 정해지면 "" 부분에 키를 넣으면 됨.

        // SetSprite(NyangNyangSnapResultImages.ResultHeaderPanel, "");
        // SetSprite(NyangNyangSnapResultImages.ImagePanel, "");
        // SetSprite(NyangNyangSnapResultImages.GaugePanel, "");
        // SetSprite(NyangNyangSnapResultImages.PoseGaugeBackImage, "");
        // SetSprite(NyangNyangSnapResultImages.CompositionGaugeBackImage, "");
        // SetSprite(NyangNyangSnapResultImages.ReactionGaugeBackImage, "");
        // SetSprite(NyangNyangSnapResultImages.ButtonPanel, "");
        // SetSprite(NyangNyangSnapResultImages.RetryButton, "");
        // SetSprite(NyangNyangSnapResultImages.SnsButton, "");

        // 주의:
        // SnapImage, PoseGaugeFillImage, CompositionGaugeFillImage, ReactionGaugeFillImage는
        // 런타임 결과 표시용이라 고정 Sprite를 넣지 않는다.
    }

    private void SetSprite(NyangNyangSnapResultImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void ResetRuntimeImages()
    {
        AutoAssignRuntimeImages();

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
        AutoAssignRuntimeImages();

        if (_resultPhotoImage == null)
        {
            DebugTool.Warning($"[NyangNyangSnapResultUISprite] 결과 사진 Image가 없습니다. 이름: {_resultPhotoImageName}", DebugType.UI, this);
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
        _resultPhotoImage.transform.SetAsLastSibling();

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
        AutoAssignRuntimeImages();

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
        AutoAssignStarImages();

        Sequence starSequence = DOTween.Sequence();

        float starValue = Mathf.Clamp(totalScore, 0, 100) / 20f;

        for (int i = 0; i < _starFillImages.Length; i++)
        {
            Image starImage = _starFillImages[i];

            if (starImage == null)
                continue;

            float targetFillAmount = Mathf.Clamp01(starValue - i);

            starImage.fillAmount = 0f;

            DebugTool.Log(
                $"[NyangNyangSnapResultUISprite] 별점 계산 / " +
                $"TotalScore: {totalScore}, " +
                $"StarIndex: {i + 1}, " +
                $"TargetFill: {targetFillAmount}",
                DebugType.UI,
                this
            );

            starSequence.Append(
                starImage.DOFillAmount(targetFillAmount, starFillDuration)
                    .SetEase(Ease.OutQuad)
            );
        }

        return starSequence;
    }

    private Tween CreateGaugeTween(Image gaugeImage, float targetValue, float duration)
    {
        AutoAssignRuntimeImages();

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
        AutoAssignStarImages();

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

    private void AutoAssignRuntimeImages()
    {
        if (_resultPhotoImage == null)
        {
            _resultPhotoImage = FindImageByNameInCanvas(_resultPhotoImageName);

            if (_resultPhotoImage != null)
            {
                DebugTool.Log(
                    $"[NyangNyangSnapResultUISprite] 결과 사진 Image 자동 연결 완료: {_resultPhotoImage.name}",
                    DebugType.UI,
                    this
                );
            }
            else
            {
                DebugTool.Warning(
                    $"[NyangNyangSnapResultUISprite] 결과 사진 Image 자동 연결 실패. 이름: {_resultPhotoImageName}",
                    DebugType.UI,
                    this
                );
            }
        }

        if (_poseGaugeFillImage == null)
            _poseGaugeFillImage = GetImage((int)NyangNyangSnapResultImages.PoseGaugeFillImage);

        if (_compositionGaugeFillImage == null)
            _compositionGaugeFillImage = GetImage((int)NyangNyangSnapResultImages.CompositionGaugeFillImage);

        if (_reactionGaugeFillImage == null)
            _reactionGaugeFillImage = GetImage((int)NyangNyangSnapResultImages.ReactionGaugeFillImage);
    }

    private void AutoAssignStarImages()
    {
        if (_starFillImages == null || _starFillImages.Length != 5)
            _starFillImages = new Image[5];

        for (int i = 0; i < _starFillImages.Length; i++)
        {
            if (_starFillImages[i] != null) continue;

            string imageName = $"{_starFillImagePrefix}{i + 1}";
            _starFillImages[i] = FindImageByNameInCanvas(imageName);

            if (_starFillImages[i] == null)
            {
                DebugTool.Warning($"[NyangNyangSnapResultUISprite] 별점 Image를 찾지 못했습니다. 이름: {imageName}", DebugType.UI, this);
                continue;
            }

            _starFillImages[i].type = Image.Type.Filled;
            _starFillImages[i].fillMethod = Image.FillMethod.Horizontal;
            _starFillImages[i].fillOrigin = 0;
            _starFillImages[i].fillAmount = 0f;

            DebugTool.Log($"[NyangNyangSnapResultUISprite] 별점 Image 자동 연결 완료: {imageName}", DebugType.UI, this);
        }
    }

    private Image FindImageByNameInCanvas(string objectName)
    {
        Transform target = FindTransformByNameInCanvas(objectName);

        if (target == null)
            return null;

        return target.GetComponent<Image>();
    }

    private Transform FindTransformByNameInCanvas(string objectName)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform searchRoot = canvas != null ? canvas.transform : transform;

        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == objectName)
                return child;
        }

        return null;
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
    ResultHeaderPanel,
    ImagePanel,
    GaugePanel,
    PoseGaugeBackImage,
    PoseGaugeFillImage,
    CompositionGaugeBackImage,
    CompositionGaugeFillImage,
    ReactionGaugeBackImage,
    ReactionGaugeFillImage,
    ButtonPanel,
    RetryButton,
    SnsButton,

    Count
}