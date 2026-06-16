using DG.Tweening;
using System;
using UnityEngine;
using Util;

public class NyangNyangSnapCatController : MonoBehaviour
{
    [Header("고양이")]
    [Tooltip("이동시킬 고양이 RectTransform")]
    [SerializeField] private RectTransform _catRectTransform;

    [Tooltip("고양이가 랜덤 스폰되고 이동할 영역")]
    [SerializeField] private RectTransform _moveArea;

    [Header("랜덤 스폰")]
    [Tooltip("영역 가장자리에서 띄울 여백")]
    [SerializeField] private Vector2 _spawnPadding = new Vector2(100f, 100f);

    [Header("도구 이동")]
    [Tooltip("고양이가 도구 위치까지 이동하는 시간")]
    [Min(0f)]
    [SerializeField] private float _moveDuration = 0.5f;

    [Tooltip("고양이가 도구와 완전히 겹치지 않도록 적용할 위치 보정값")]
    [SerializeField] private Vector2 _toolOffset = Vector2.zero;

    [Header("애니메이션")]
    [Tooltip("추후 이동 및 포즈 애니메이션에 사용할 Animator")]
    [SerializeField] private Animator _animator;

    private Tween _moveTween;
    private int _currentItemID;

    public RectTransform CatRectTransform => _catRectTransform;
    public int CurrentItemID => _currentItemID;
    public bool IsMoving => _moveTween != null && _moveTween.IsActive();

    public event Action<int> OnDestinationReached;
    public event Action OnInteractionStopped;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnDisable()
    {
        StopInteraction();
    }

    private void AutoAssignReferences()
    {
        if (_catRectTransform == null)
            _catRectTransform = GetComponent<RectTransform>();

        if (_moveArea == null && _catRectTransform != null)
            _moveArea = _catRectTransform.parent as RectTransform;

        if (_animator == null)
            _animator = GetComponent<Animator>();
    }

    public void SetRandomPosition()
    {
        AutoAssignReferences();

        if (_catRectTransform == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 고양이 RectTransform이 없습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        if (_moveArea == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 고양이 이동 영역이 없습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        Rect areaRect = _moveArea.rect;
        Rect catRect = _catRectTransform.rect;

        float halfWidth = catRect.width * 0.5f;
        float halfHeight = catRect.height * 0.5f;

        float minX = areaRect.xMin + halfWidth + _spawnPadding.x;
        float maxX = areaRect.xMax - halfWidth - _spawnPadding.x;
        float minY = areaRect.yMin + halfHeight + _spawnPadding.y;
        float maxY = areaRect.yMax - halfHeight - _spawnPadding.y;

        if (minX > maxX || minY > maxY)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 랜덤 스폰 영역이 너무 작습니다.",
                DebugType.UI,
                this
            );

            return;
        }

        StopInteraction();

        Vector2 randomPosition = new Vector2(
            UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(minY, maxY)
        );

        _catRectTransform.anchoredPosition = randomPosition;

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 랜덤 스폰 완료 / Position:{randomPosition}",
            DebugType.UI,
            this
        );
    }

    public bool TryMoveToTool(int itemID, RectTransform toolRectTransform, float itemRange)
    {
        AutoAssignReferences();

        if (_catRectTransform == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 고양이 RectTransform이 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        if (toolRectTransform == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 배치된 도구 RectTransform이 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        RectTransform catParent = _catRectTransform.parent as RectTransform;

        if (catParent == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 고양이 부모 RectTransform이 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        Vector2 toolPosition = catParent.InverseTransformPoint(toolRectTransform.position);
        Vector2 catPosition = _catRectTransform.anchoredPosition;
        float distance = Vector2.Distance(catPosition, toolPosition);

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 도구 범위 검사 / ItemID:{itemID}, Distance:{distance:F1}, Range:{itemRange:F1}",
            DebugType.UI,
            this
        );

        if (distance > itemRange)
        {
            DebugTool.Log(
                $"[NyangNyangSnapCatController] 고양이가 도구 범위 밖에 있습니다. ItemID:{itemID}",
                DebugType.UI,
                this
            );

            return false;
        }

        Vector2 targetPosition = toolPosition + _toolOffset;

        StartInteraction(itemID, targetPosition);

        return true;
    }

    private void StartInteraction(int itemID, Vector2 targetPosition)
    {
        StopInteraction(false);

        _currentItemID = itemID;

        PlayMoveAnimation(itemID);
        StartMoveTween(targetPosition);

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 도구 상호작용 시작 / ItemID:{itemID}, Target:{targetPosition}",
            DebugType.UI,
            this
        );
    }

    private void StartMoveTween(Vector2 targetPosition)
    {
        if (_catRectTransform == null)
            return;

        if (_moveDuration <= 0f)
        {
            _catRectTransform.anchoredPosition = targetPosition;
            OnMoveComplete();

            return;
        }

        _moveTween = _catRectTransform
            .DOAnchorPos(targetPosition, _moveDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(OnMoveComplete);
    }

    private void OnMoveComplete()
    {
        _moveTween = null;

        int arrivedItemID = _currentItemID;

        StopMoveAnimation();
        PlayPoseAnimation(arrivedItemID);

        OnDestinationReached?.Invoke(arrivedItemID);

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 도구 위치 도착 / ItemID:{arrivedItemID}",
            DebugType.UI,
            this
        );
    }

    public void StopInteraction()
    {
        StopInteraction(true);
    }

    private void StopInteraction(bool notify)
    {
        bool hadInteraction = _moveTween != null || _currentItemID > 0;

        if (_moveTween != null)
        {
            _moveTween.Kill();
            _moveTween = null;
        }

        StopMoveAnimation();
        StopPoseAnimation();

        _currentItemID = 0;

        if (notify && hadInteraction)
            OnInteractionStopped?.Invoke();
    }

    private void PlayMoveAnimation(int itemID)
    {
        if (_animator == null)
            return;

        // 추후 이동 애니메이션 실행
    }

    private void StopMoveAnimation()
    {
        if (_animator == null)
            return;

        // 추후 이동 애니메이션 종료
    }

    private void PlayPoseAnimation(int itemID)
    {
        if (_animator == null || itemID <= 0)
            return;

        // 추후 ItemID에 맞는 포즈 애니메이션 실행
    }

    private void StopPoseAnimation()
    {
        if (_animator == null)
            return;

        // 추후 포즈 종료 후 Idle 처리
    }

}