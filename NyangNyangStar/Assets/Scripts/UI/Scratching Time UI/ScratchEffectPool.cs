using System;
using System.Collections;
using System.Collections.Generic;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.KeyContainerSO;
using Services.Enums;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;


// 터치 위치에 스크래치 이펙트를 표시
// 스프라이트는 Addressables(ST_Effect_Nromal / ST_Effect_Critical)에서 한 번 로드해 캐시
// UI Image 자체는 풀링 아 어렵네 아 

public class ScratchEffectPool : MonoBehaviour
{
    private const string EffectNormalKey = "ST_Effect_Nromal";
    private const string EffectCriticalKey = "ST_Effect_Critical";

    [Header("Layout")]
    [SerializeField] private RectTransform effectRoot;
    [SerializeField] private RectTransform touchArea;

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private Vector2 effectSize = new(100f, 100f);
    [SerializeField] private float effectDisplaySeconds = 0.8f;

    private readonly Queue<PooledScratchEffect> _pool = new();

    private Sprite _normalSprite;
    private Sprite _criticalSprite;
    private AsyncOperationHandle<Sprite> _normalHandle;
    private AsyncOperationHandle<Sprite> _criticalHandle;
    private int _spriteLoadPending;
    private bool _spritesReady;

    public event Action<Vector2> OnScratchClicked;

    private void Start()
    {
        GameManager.Init();
        EnsureLocalDataAccess();
        RegisterEffectSpriteKeys();
        LoadEffectSprites();
        CreatePool();
    }

    private void OnDestroy()
    {
        ReleaseEffectSprites();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || !IsInsideTouchArea(Input.mousePosition))
            return;

        OnScratchClicked?.Invoke(Input.mousePosition);
    }

    
    // 터치 위치에 일반 / 크리티컬 스크래치 스프라이트 이펙트를 표시
    public void SpawnEffect(Vector2 screenPosition, bool isCritical)
    {
        if (!_spritesReady)
            return;

        Sprite sprite = isCritical && _criticalSprite != null ? _criticalSprite : _normalSprite;
        if (sprite == null)
            return;

        PooledScratchEffect item = GetEffect();
        RectTransform itemRect = item.RectTransform;
        Image image = item.Image;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectRoot,
            screenPosition,
            null,
            out Vector2 localPosition);

        image.sprite = sprite;
        image.color = Color.white;
        image.SetNativeSize();

        itemRect.anchoredPosition = localPosition;
        itemRect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-20f, 20f));
        itemRect.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);
        itemRect.gameObject.SetActive(true);

        if (item.ReturnCoroutine != null)
            StopCoroutine(item.ReturnCoroutine);

        item.ReturnCoroutine = StartCoroutine(ReturnEffectAfterDelay(item));
    }

    private IEnumerator ReturnEffectAfterDelay(PooledScratchEffect item)
    {
        yield return new WaitForSeconds(effectDisplaySeconds);
        ReturnEffect(item);
    }

    private void ReturnEffect(PooledScratchEffect item)
    {
        if (item.ReturnCoroutine != null)
        {
            StopCoroutine(item.ReturnCoroutine);
            item.ReturnCoroutine = null;
        }

        item.Image.sprite = null;
        item.RectTransform.gameObject.SetActive(false);
        _pool.Enqueue(item);
    }

    private bool IsInsideTouchArea(Vector2 screenPosition)
    {
        if (touchArea == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(touchArea, screenPosition, null);
    }

    private void CreatePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            _pool.Enqueue(CreatePooledItem());
    }

    private PooledScratchEffect GetEffect()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        return CreatePooledItem();
    }

    private PooledScratchEffect CreatePooledItem()
    {
        GameObject go = new GameObject(
            "ScratchEffect",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        go.transform.SetParent(effectRoot, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = effectSize;

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.maskable = true;

        go.SetActive(false);

        return new PooledScratchEffect(rect, image);
    }

    private void LoadEffectSprites()
    {
        _spriteLoadPending = 2;
        _spritesReady = false;

        LoadEffectSprite(EffectNormalKey, sprite =>
        {
            _normalSprite = sprite;
            OnOneSpriteLoaded();
        });

        LoadEffectSprite(EffectCriticalKey, sprite =>
        {
            _criticalSprite = sprite;
            OnOneSpriteLoaded();
        });
    }

    private void LoadEffectSprite(string key, Action<Sprite> onLoaded)
    {
        GameManager.Addressable.LoadSprite(
            key,
            (loadedSprite, handle) =>
            {
                if (loadedSprite == null)
                {
                    DebugTool.Warning($"{key} : 스크래치 이펙트 Sprite가 null 입니다.", DebugType.ScratchingTime);
                    OnOneSpriteLoaded();
                    return;
                }

                if (key == EffectNormalKey)
                {
                    ReleaseHandle(ref _normalHandle);
                    _normalHandle = handle;
                }
                else if (key == EffectCriticalKey)
                {
                    ReleaseHandle(ref _criticalHandle);
                    _criticalHandle = handle;
                }

                onLoaded?.Invoke(loadedSprite);
            },
            failedKey =>
            {
                DebugTool.Warning($"{failedKey} : 스크래치 이펙트 Sprite 로드 실패", DebugType.ScratchingTime);
                OnOneSpriteLoaded();
            });
    }

    private void OnOneSpriteLoaded()
    {
        _spriteLoadPending--;
        if (_spriteLoadPending > 0)
            return;

        _spritesReady = _normalSprite != null;
    }

    private static void RegisterEffectSpriteKeys()
    {
        RegisterSpriteKeyIfMissing(EffectNormalKey, AddressableGroupType.Scratching);
        RegisterSpriteKeyIfMissing(EffectCriticalKey, AddressableGroupType.Scratching);
    }

    private static void RegisterSpriteKeyIfMissing(string key, AddressableGroupType groupType)
    {
        if (KeyContainer.Sprites.Contains(key))
            return;

        KeyContainer.Register(new KeyData
        {
            Key = key,
            FileName = key,
            Usage = "ScratchEffectPool",
            GroupType = groupType,
            LabelType = LabelType.Sprite,
            BuildType = BuildType.Local
        });
    }

    private static void EnsureLocalDataAccess()
    {
        if (LocalDataAccess.Instance != null)
            return;

        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
    }

    private void ReleaseEffectSprites()
    {
        ReleaseHandle(ref _normalHandle);
        ReleaseHandle(ref _criticalHandle);
        _normalSprite = null;
        _criticalSprite = null;
        _spritesReady = false;
    }

    private static void ReleaseHandle(ref AsyncOperationHandle<Sprite> handle)
    {
        if (!handle.IsValid())
            return;

        GameManager.Addressable.Release(handle);
        handle = default;
    }

    private sealed class PooledScratchEffect
    {
        public PooledScratchEffect(RectTransform rectTransform, Image image)
        {
            RectTransform = rectTransform;
            Image = image;
        }

        public RectTransform RectTransform { get; }
        public Image Image { get; }
        public Coroutine ReturnCoroutine { get; set; }
    }
}
