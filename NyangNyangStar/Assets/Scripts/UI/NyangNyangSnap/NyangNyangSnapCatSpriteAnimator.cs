using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapCatSpriteAnimator : MonoBehaviour
{
    private const int OptimalCutFrameIndex = 3;

    [Header("출력 대상")]
    [Tooltip("고양이 Image")]
    [SerializeField] private Image _catImage;

    [Header("Addresable 키")]
    [Tooltip("Idle")]
    [SerializeField] private string _idleKey = "Snap_Ani_Idle";

    [Header("Addresable 키")]
    [Tooltip("Run")]
    [SerializeField] private string _runKey = "Snap_Ani_Run";

    [Header("Addresable 키")]
    [Tooltip("Hit")]
    [SerializeField] private string _hitKey = "Snap_Ani_Hit";

    [Header("Addresable 키")]
    [Tooltip("Catch")]
    [SerializeField] private string _catchKey = "Snap_Ani_Catch";

    [Header("Addresable 키")]
    [Tooltip("Bite")]
    [SerializeField] private string _biteKey = "Snap_Ani_Bite";

    [Header("프레임 시간")]
    [Tooltip("Sprite 한 장의 출력 시간")]
    [SerializeField] private float _baseFrameDuration = 0.3f;

    [Tooltip("아이템 레벨당 추가되는 출력 시간")]
    [SerializeField] private float _levelFrameDurationIncrease = 0.3f;

    private readonly Dictionary<CatAnimationType, Sprite[]> _sprite = new();
    private readonly Dictionary<CatAnimationType, AsyncOperationHandle<IList<Sprite>>> _loadHandles = new();

    private Coroutine _playCoroutine;
    private CatAnimationType _currentAnimationType = CatAnimationType.None;

    private int _currentFrameIndex = -1;
    private int _currentFrameCount;

    private float _currentFrameElapsedTime;
    private float _currentFrameDuration;

    private float _animationElapsedTime;
    private float _animationDuration;

    public CatAnimationType CurrentAnimationType => _currentAnimationType;
    public bool IsPlaying => _playCoroutine != null;

    public int CurrentFrameIndex => _currentFrameIndex;

    public float CurrentFrameDuration => _currentFrameDuration;

    public float AnimationElapsedTime => _animationElapsedTime;
    public float AnimationDuration => _animationDuration;


    public bool IsPoseAnimation =>
        IsPoseAnimationType(_currentAnimationType);

    public bool IsOptimalCut =>
        IsPoseAnimation &&
        _currentFrameIndex == OptimalCutFrameIndex;

    public event Action<CatAnimationType> OnAnimationStarted;
    public event Action<CatAnimationType> OnAnimationCompleted;

    public event Action<CatAnimationType, int, int> OnFrameChanged;

    private void Awake()
    {
        if (_catImage == null)
        {
            _catImage = GetComponent<Image>();
        }
    }

    private void Start()
    {
        PlayIdle();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }

    public void PlayLoop(CatAnimationType animationType)
    {
        Play(animationType, 1, true);
    }

    public void PlayOnce(
        CatAnimationType animationType,
        int itemLevel
    )
    {
        Play(animationType, itemLevel, false);
    }

    public void Play(
        CatAnimationType animationType,
        int itemLevel,
        bool loop
    )
    {
        if (animationType == CatAnimationType.None)
        {
            DebugTool.Warnning(
                "애니메이션 타입 없음",
                DebugType.Game,
                this
            );

            return;
        }

        if (_catImage == null)
        {
            _catImage = GetComponent<Image>();
        }

        if (_catImage == null)
        {
            DebugTool.Warnning(
                "[NyangNyangSnapCatSpriteAnimator] 고양이 Image가 없습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        Stop();

        _playCoroutine = StartCoroutine(
            PlayAnimationCoroutine(
                animationType,
                itemLevel,
                loop
            )
        );
    }

    public void Stop()
    {
        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
        }

        ClearPlaybackState();
    }

    public void PlayIdle()
    {
        PlayLoop(CatAnimationType.Idle);
    }

    private IEnumerator PlayAnimationCoroutine(
        CatAnimationType animationType,
        int itemLevel,
        bool loop
    )
    {
        Sprite[] sprites = null;

        yield return LoadSpritesCoroutine(
            animationType,
            loadedSprites => sprites = loadedSprites
        );

        if (sprites == null || sprites.Length == 0)
        {
            ClearPlaybackState();
            yield break;
        }

        _currentAnimationType = animationType;

        float frameDuration =
            CalculateFrameDuration(itemLevel);

        _currentFrameCount = sprites.Length;
        _currentFrameDuration = frameDuration;

        _animationElapsedTime = 0f;
        _animationDuration =
            frameDuration * sprites.Length;

        OnAnimationStarted?.Invoke(animationType);

        DebugTool.Log(
            $"[NyangNyangSnapCatSpriteAnimator] 애니메이션 시작 " +
            $"/ Type:{animationType}, " +
            $"Frames:{sprites.Length}, " +
            $"ItemLevel:{Mathf.Max(1, itemLevel)}, " +
            $"FrameDuration:{frameDuration:F2}, " +
            $"TotalDuration:{_animationDuration:F2}",
            DebugType.UI,
            this
        );

        while (true)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (_catImage == null)
                {
                    ClearPlaybackState();
                    yield break;
                }

                SetCurrentFrame(
                    animationType,
                    sprites[i],
                    i,
                    sprites.Length
                );

                while (_currentFrameElapsedTime < frameDuration)
                {
                    float deltaTime = Time.deltaTime;

                    _currentFrameElapsedTime += deltaTime;
                    _animationElapsedTime += deltaTime;

                    yield return null;
                }

                _currentFrameElapsedTime = frameDuration;
            }

            if (!loop)
                break;

            _animationElapsedTime = 0f;
        }

        CompleteAnimation(animationType);
    }

    private void SetCurrentFrame(
        CatAnimationType animationType,
        Sprite sprite,
        int frameIndex,
        int frameCount
    )
    {
        _currentFrameIndex = frameIndex;
        _currentFrameCount = frameCount;
        _currentFrameElapsedTime = 0f;

        _catImage.sprite = sprite;

        OnFrameChanged?.Invoke(
            animationType,
            frameIndex,
            frameCount
        );

        if (IsOptimalCut)
        {
            DebugTool.Log(
                $"[NyangNyangSnapCatSpriteAnimator] 최적 컷 진입 " +
                $"/ Type:{animationType}, " +
                $"Frame:{frameIndex + 1}/{frameCount}",
                DebugType.UI,
                this
            );
        }
    }

    private IEnumerator LoadSpritesCoroutine(
        CatAnimationType animationType,
        Action<Sprite[]> onCompleted
    )
    {
        if (_sprite.TryGetValue(
            animationType,
            out Sprite[] sprites
        ))
        {
            onCompleted?.Invoke(sprites);
            yield break;
        }

        string addressableKey =
            GetAddressableKey(animationType);

        if (string.IsNullOrEmpty(addressableKey))
        {
            DebugTool.Warnning(
                "키가 없습니다.",
                DebugType.UI,
                this
            );

            onCompleted?.Invoke(null);
            yield break;
        }

        AsyncOperationHandle<IList<Sprite>> handle =
            Addressables.LoadAssetAsync<IList<Sprite>>(
                addressableKey
            );

        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded ||
            handle.Result == null ||
            handle.Result.Count == 0)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            onCompleted?.Invoke(null);
            yield break;
        }

        Sprite[] sortedSprites =
            new Sprite[handle.Result.Count];

        for (int i = 0; i < handle.Result.Count; i++)
        {
            sortedSprites[i] = handle.Result[i];
        }

        Array.Sort(
            sortedSprites,
            CompareSpriteFrameIndex
        );

        _loadHandles[animationType] = handle;
        _sprite[animationType] = sortedSprites;

        DebugTool.Log(
            $"[NyangNyangSnapCatSpriteAnimator] Sprite Sheet 로드 완료 " +
            $"/ Type:{animationType}, " +
            $"Key:{addressableKey}, " +
            $"Count:{sortedSprites.Length}",
            DebugType.UI,
            this
        );

        onCompleted?.Invoke(sortedSprites);
    }

    private float CalculateFrameDuration(int itemLevel)
    {
        int safeItemLevel = Mathf.Max(1, itemLevel);

        float frameDuration =
            _baseFrameDuration +
            (safeItemLevel - 1) *
            _levelFrameDurationIncrease;

        return Mathf.Round(frameDuration * 100f) / 100f;
    }

    private string GetAddressableKey(
        CatAnimationType animationType
    )
    {
        return animationType switch
        {
            CatAnimationType.Idle => _idleKey,
            CatAnimationType.Run => _runKey,
            CatAnimationType.Hit => _hitKey,
            CatAnimationType.Catch => _catchKey,
            CatAnimationType.Bite => _biteKey,

            _ => string.Empty
        };
    }

    private static bool IsPoseAnimationType(
        CatAnimationType animationType
    )
    {
        return animationType == CatAnimationType.Hit ||
               animationType == CatAnimationType.Catch ||
               animationType == CatAnimationType.Bite;
    }

    private static int CompareSpriteFrameIndex(
        Sprite left,
        Sprite right
    )
    {
        int leftIndex = ExtractFrameIndex(
            left != null
                ? left.name
                : string.Empty
        );

        int rightIndex = ExtractFrameIndex(
            right != null
                ? right.name
                : string.Empty
        );

        return leftIndex.CompareTo(rightIndex);
    }

    private static int ExtractFrameIndex(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return int.MaxValue;

        Match match =
            Regex.Match(spriteName, @"(\d+)$");

        return match.Success &&
               int.TryParse(
                   match.Value,
                   out int index
               )
            ? index
            : int.MaxValue;
    }

    private void CompleteAnimation(
        CatAnimationType animationType
    )
    {
        ClearPlaybackState();
        OnAnimationCompleted?.Invoke(animationType);
    }

    private void ClearPlaybackState()
    {
        _playCoroutine = null;
        _currentAnimationType = CatAnimationType.None;

        _currentFrameIndex = -1;
        _currentFrameCount = 0;

        _currentFrameElapsedTime = 0f;
        _currentFrameDuration = 0f;

        _animationElapsedTime = 0f;
        _animationDuration = 0f;
    }

    private void ReleaseAll()
    {
        foreach (
            AsyncOperationHandle<IList<Sprite>> handle
            in _loadHandles.Values
        )
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _loadHandles.Clear();
        _sprite.Clear();
    }
    public void SetFacingDirection(bool faceRight)
    {
        if (_catImage == null)
            _catImage = GetComponent<Image>();

        if (_catImage == null)
            return;

        RectTransform imageRectTransform =
            _catImage.rectTransform;

        Vector3 localScale =
            imageRectTransform.localScale;

        float absoluteScaleX =
            Mathf.Abs(localScale.x);

        localScale.x =
            faceRight
                ? -absoluteScaleX
                : absoluteScaleX;

        imageRectTransform.localScale =
            localScale;
    }

    [ContextMenu("Test Run")]
    private void TestRun()
    {
        PlayLoop(CatAnimationType.Run);
    }

    [ContextMenu("Test Hit")]
    private void TestHit()
    {
        PlayOnce(CatAnimationType.Hit, 1);
    }

    [ContextMenu("Test Catch")]
    private void TestCatch()
    {
        PlayOnce(CatAnimationType.Catch, 1);
    }

    [ContextMenu("Test Bite")]
    private void TestBite()
    {
        PlayOnce(CatAnimationType.Bite, 1);
    }
}

public enum CatAnimationType
{
    None,
    Idle,
    Run,
    Hit,
    Catch,
    Bite
}