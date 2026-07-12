using System;
using System.Collections;
using System.Collections.Generic;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.KeyContainerSO;
using Services.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;


// 터치 위치에 스크래치 이펙트를 표시
// 스프라이트는 Addressables(ST_Effect_Normal / ST_Effect_Critical)에서 한 번 로드해 캐시
// UI Image는 오브젝트 풀링으로 재사용

public class ScratchEffectPool : MonoBehaviour
{
    // Addressables 스프라이트 키
    private const string EffectNormalKey = "ST_Effect_Normal";
    private const string EffectCriticalKey = "ST_Effect_Critical";

    [Header("Layout")]
    [SerializeField] private RectTransform effectRoot;   // 이펙트가 생성될 부모 RectTransform
    [SerializeField] private RectTransform touchArea;    // 터치 입력을 받을 영역

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 10;           // 시작 시 미리 생성할 풀 개수
    [SerializeField] private Vector2 effectSize = new(100f, 100f); // 이펙트 Image 기본 크기
    [SerializeField] private float effectDisplaySeconds = 0.8f;  // 이펙트 표시 후 풀 반환까지 대기 시간

    private readonly Queue<PooledScratchEffect> _pool = new(); // 비활성화된 이펙트 대기열

    private Sprite _normalSprite;
    private Sprite _criticalSprite;
    private AsyncOperationHandle<Sprite> _normalHandle;
    private AsyncOperationHandle<Sprite> _criticalHandle;
    private int _spriteLoadPending;  // 아직 로드되지 않은 스프라이트 수
    private bool _spritesReady;      // 일반 이펙트 스프라이트 로드 완료 여부

    // 터치 영역 안에서 클릭이 발생했을 때 호출 (screenPosition 전달)
    // 모바일에서는 한 손가락 입력만 공격으로 인정해 다중 터치 연타를 막는다.
    private int _activeTouchFingerId = -1;
    private int _lastScratchInputFrame = -1;

    public event Action<Vector2> OnScratchClicked;


    // 초기화: Addressables 키 등록, 스프라이트 로드, 풀 생성
    private void Start()
    {
        GameManager.Init();
        EnsureLocalDataAccess();
        RegisterEffectSpriteKeys();
        LoadEffectSprites();
        CreatePool();
    }


    // Addressables 핸들 해제
    private void OnDestroy()
    {
        ReleaseEffectSprites();
    }


    // 입력 상태 초기화
    private void OnEnable()
    {
        ResetTouchInputState();
    }


    // 터치 영역 내 클릭 시 스크래치 입력 이벤트 발생
    // 모바일에서는 첫 터치 손가락이 끝날 때까지 다른 손가락 입력을 무시
    private void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
            return;
        }

        // 에디터 / PC 폴백 : 터치가 없을 때만 마우스 입력 처리
        ResetTouchInputState();
        HandleMouseInput();
    }


    // 첫 접촉만 스크래치 입력 이벤트로 전달
    private void HandleTouchInput()
    {
        int touchCount = Input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (_activeTouchFingerId >= 0 && touch.fingerId != _activeTouchFingerId)
                continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (touch.fingerId == _activeTouchFingerId)
                    ResetTouchInputState();

                continue;
            }

            if (touch.phase != TouchPhase.Began)
                continue;

            if (!IsInsideTouchArea(touch.position))
                continue;

            if (TryRaiseScratchInput(touch.position))
                _activeTouchFingerId = touch.fingerId;

            break;
        }
    }


    private void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(0) || !IsInsideTouchArea(Input.mousePosition))
            return;

        TryRaiseScratchInput(Input.mousePosition);
    }


    private bool TryRaiseScratchInput(Vector2 screenPosition)
    {
        if (_lastScratchInputFrame == Time.frameCount)
            return false;

        _lastScratchInputFrame = Time.frameCount;
        OnScratchClicked?.Invoke(screenPosition);
        return true;
    }


    private void ResetTouchInputState()
    {
        _activeTouchFingerId = -1;
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

        // 스크린 좌표를 effectRoot 기준 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectRoot,
            screenPosition,
            null,
            out Vector2 localPosition);

        image.sprite = sprite;
        image.color = Color.white;
        // image.SetNativeSize();
        itemRect.sizeDelta = effectSize;

        itemRect.anchoredPosition = localPosition;
        itemRect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-20f, 20f));
        itemRect.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);
        itemRect.gameObject.SetActive(true);

        // 이전 반환 코루틴이 남아 있으면 중단
        if (item.ReturnCoroutine != null)
            StopCoroutine(item.ReturnCoroutine);

        item.ReturnCoroutine = StartCoroutine(ReturnEffectAfterDelay(item));
    }


    // 일정 시간 후 이펙트를 풀에 반환
    private IEnumerator ReturnEffectAfterDelay(PooledScratchEffect item)
    {
        yield return new WaitForSeconds(effectDisplaySeconds);
        ReturnEffect(item);
    }


    // 이펙트를 비활성화하고 풀에 되돌림
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


    // 화면 좌표가 터치 영역 안에 있는지 확인
    private bool IsInsideTouchArea(Vector2 screenPosition)
    {
        if (touchArea == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(touchArea, screenPosition, null);
    }


    // 초기 풀 오브젝트를 미리 생성
    private void CreatePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            _pool.Enqueue(CreatePooledItem());
    }


    // 풀에서 이펙트를 꺼내고, 없으면 새로 생성
    private PooledScratchEffect GetEffect()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        return CreatePooledItem();
    }


    // 풀에 넣을 UI Image 이펙트 오브젝트 생성
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
        image.raycastTarget = false; // 터치 입력을 가로막지 않음
        image.maskable = true;

        go.SetActive(false);

        return new PooledScratchEffect(rect, image);
    }


    // 일반 / 크리티컬 이펙트 스프라이트를 Addressables에서 로드
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


    // 단일 스프라이트를 Addressables로 로드하고 핸들을 보관
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


    // 모든 스프라이트 로드가 끝났는지 확인하고 사용 가능 상태로 전환
    private void OnOneSpriteLoaded()
    {
        _spriteLoadPending--;
        if (_spriteLoadPending > 0)
            return;

        // 일반 이펙트만 있어도 스폰 가능
        _spritesReady = _normalSprite != null;
    }


    // Addressables 키 컨테이너에 이펙트 스프라이트 키 등록
    private static void RegisterEffectSpriteKeys()
    {
        RegisterSpriteKeyIfMissing(EffectNormalKey, AddressableGroupType.Scratching);
        RegisterSpriteKeyIfMissing(EffectCriticalKey, AddressableGroupType.Scratching);
    }


    // 키가 없을 때만 KeyContainer에 스프라이트 키 추가
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


    // 에디터 등에서 LocalDataAccess가 없을 때 런타임 인스턴스 보장
    private static void EnsureLocalDataAccess()
    {
        if (LocalDataAccess.Instance != null)
            return;

        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
    }


    // 로드된 스프라이트와 Addressables 핸들 해제
    private void ReleaseEffectSprites()
    {
        ReleaseHandle(ref _normalHandle);
        ReleaseHandle(ref _criticalHandle);
        _normalSprite = null;
        _criticalSprite = null;
        _spritesReady = false;
    }


    // 유효한 Addressables 핸들만 Release 후 초기화
    // OnDestroy는 GameManager보다 늦게/먼저 파괴될 수 있어 Addressables를 직접 호출함
    private static void ReleaseHandle(ref AsyncOperationHandle<Sprite> handle)
    {
        if (!handle.IsValid())
            return;

        Addressables.Release(handle);
        handle = default;
    }


    // 풀링 대상 이펙트의 RectTransform, Image, 반환 코루틴 참조
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
