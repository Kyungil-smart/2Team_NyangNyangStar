using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using Util;


public class NyangNyangSnapCatController : MonoBehaviour
{
    private const float LeaveMoveDuration = 2f;

    public enum CatInteractionState
    {
        Idle,
        Alert,
        React,
        Pose,
        Eating,
        Leave
    }

    [Header("고양이")]
    [Tooltip("이동시킬 고양이 RectTransform")]
    [SerializeField] private RectTransform _catRectTransform;

    [Tooltip("고양이가 랜덤 스폰되고 이동할 영역")]
    [SerializeField] private RectTransform _moveArea;

    [Header("랜덤 스폰")]
    [Tooltip("영역 가장자리에서 띄울 여백")]
    [SerializeField]
    private Vector2 _spawnPadding =
        new Vector2(100f, 100f);

    [Header("도구 이동")]
    [Tooltip("고양이가 도구 위치까지 이동하는 시간")]
    [Min(0f)]
    [SerializeField] private float _moveDuration = 2f;

    [Tooltip("고양이가 도구와 완전히 겹치지 않도록 적용할 위치 보정값")]
    [SerializeField] private Vector2 _toolOffset = Vector2.zero;

    [Header("애니메이션")]
    [Tooltip("고양이 Sprite 애니메이션 담당")]
    [SerializeField]
    private NyangNyangSnapCatSpriteAnimator _spriteAnimator;

    [Tooltip("ItemID에 맞는 포즈 데이터를 조회할 SO")]
    [SerializeField]
    private NyangNyangSnapPoseSO _poseSO;

    [Header("Alert 텍스트")]
    [Tooltip("고양이 머리 위에 ?,!")]
    [SerializeField] private TMP_Text _alertText;

    private Tween _moveTween;

    private int _currentItemID;
    private CatInteractionState _state =
        CatInteractionState.Idle;

    public bool IsMoving =>
        _moveTween != null &&
        _moveTween.IsActive();

    public bool IsEating =>
        _state == CatInteractionState.Eating;

    public event Action<int> OnDestinationReached;

    private void Awake()
    {
        AutoAssignReferences();
        HideAlertIcons();
    }

    private void OnEnable()
    {
        RegisterAnimationEvent();
    }

    private void OnDisable()
    {
        UnregisterAnimationEvent();

        // 오브젝트가 비활성화되는 중에는
        // Idle 코루틴을 새로 시작하지 않고 현재 동작만 정리합니다.
        StopInteraction(false, false);
    }

    /// <summary>
    /// 필요한 컴포넌트를 자동으로 연결한다.
    /// </summary>
    private void AutoAssignReferences()
    {
        if (_catRectTransform == null)
            _catRectTransform = GetComponent<RectTransform>();

        if (_moveArea == null &&
            _catRectTransform != null)
        {
            _moveArea =
                _catRectTransform.parent as RectTransform;
        }

        if (_spriteAnimator == null)
        {
            _spriteAnimator =
                GetComponent<NyangNyangSnapCatSpriteAnimator>();
        }
    }
    /// <summary>
    /// 도구 위치를 지정하거나 드래그하는 동안 ALERT 상태로 전환한다.
    /// 이동이 확정되기 전까지 물음표를 유지한다.
    /// </summary>
    public void BeginAlert()
    {
        StopInteraction(false, false);

        _state = CatInteractionState.Alert;

        PlayIdleAnimation();
        ShowAlertText("?");
    }

    /// <summary>
    /// ALERT 상태를 취소하고 텍스트를 숨긴다.
    /// </summary>
    public void EndAlert()
    {
        HideAlertIcons();

        if (_state == CatInteractionState.Alert)
            _state = CatInteractionState.Idle;
    }
    private void ShowAlertText(
    string message)
    {
        if (_alertText == null)
            return;

        _alertText.text = message;
        _alertText.gameObject.SetActive(true);
    }
    private void HideAlertIcons()
    {
        if (_alertText == null)
            return;

        _alertText.text = string.Empty;
        _alertText.gameObject.SetActive(false);
    }
    /// <summary>
    /// Sprite 애니메이션 완료 이벤트를 연결한다.
    /// </summary>
    private void RegisterAnimationEvent()
    {
        AutoAssignReferences();

        if (_spriteAnimator == null)
            return;

        _spriteAnimator.OnAnimationCompleted -=
            OnPoseAnimationCompleted;

        _spriteAnimator.OnAnimationCompleted +=
            OnPoseAnimationCompleted;
    }

    /// <summary>
    /// Sprite 애니메이션 완료 이벤트를 해제한다.
    /// </summary>
    private void UnregisterAnimationEvent()
    {
        if (_spriteAnimator == null)
            return;

        _spriteAnimator.OnAnimationCompleted -=
            OnPoseAnimationCompleted;
    }

    /// <summary>
    /// 고양이를 이동 영역 안의 랜덤한 위치에 배치한다.
    /// </summary>
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

        float halfWidth =
            catRect.width * 0.5f;

        float halfHeight =
            catRect.height * 0.5f;

        float minX =
            areaRect.xMin +
            halfWidth +
            _spawnPadding.x;

        float maxX =
            areaRect.xMax -
            halfWidth -
            _spawnPadding.x;

        float minY =
            areaRect.yMin +
            halfHeight +
            _spawnPadding.y;

        float maxY =
            areaRect.yMax -
            halfHeight -
            _spawnPadding.y;

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

        _catRectTransform.anchoredPosition =
            randomPosition;

        PlayIdleAnimation();

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 랜덤 스폰 완료 " +
            $"/ Position:{randomPosition}",
            DebugType.UI,
            this
        );
    }

    /// <summary>
    /// 고양이가 도구 효과 범위 안에 있으면 도구 위치로 이동한다.
    /// </summary>
    public bool TryMoveToTool(
        int itemID,
        RectTransform toolRectTransform,
        float itemRange)
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

        RectTransform catParent =
            _catRectTransform.parent as RectTransform;

        if (catParent == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] 고양이 부모 RectTransform이 없습니다.",
                DebugType.UI,
                this
            );

            return false;
        }

        Vector2 toolPosition =
            catParent.InverseTransformPoint(
                toolRectTransform.position
            );

        Vector2 catPosition =
            _catRectTransform.anchoredPosition;

        float distance =
            Vector2.Distance(
                catPosition,
                toolPosition
            );

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 도구 범위 검사 " +
            $"/ ItemID:{itemID}, " +
            $"Distance:{distance:F1}, " +
            $"Range:{itemRange:F1}",
            DebugType.UI,
            this
        );

        if (distance > itemRange)
        {
            DebugTool.Log(
                $"[NyangNyangSnapCatController] 고양이가 도구 범위 밖에 있습니다. " +
                $"ItemID:{itemID}",
                DebugType.UI,
                this
            );

            return false;
        }

        Vector2 targetPosition =
            toolPosition + _toolOffset;

        UpdateFacingDirection(toolPosition);

        StartInteraction(
            itemID,
            targetPosition
        );

        // 실제 이동이 확정된 순간 느낌표로 변경합니다.
        // 이동 중에는 느낌표를 유지하고 도착하면 숨깁니다.
        ShowAlertText("!");

        return true;
    }
    private void UpdateFacingDirection(
    Vector2 targetPosition)
    {
        if (_spriteAnimator == null ||
            _catRectTransform == null)
        {
            return;
        }

        bool faceRight =
            targetPosition.x >
            _catRectTransform.anchoredPosition.x;

        _spriteAnimator.SetFacingDirection(
            faceRight
        );
    }
    /// <summary>
    /// 도구 상호작용을 시작한다.
    /// </summary>
    private void StartInteraction(
        int itemID,
        Vector2 targetPosition)
    {
        StopInteraction(false, false);

        _currentItemID = itemID;
        _state = CatInteractionState.React;

        PlayMoveAnimation();
        StartMoveTween(targetPosition);

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 도구 상호작용 시작 " +
            $"/ ItemID:{itemID}, " +
            $"Target:{targetPosition}",
            DebugType.UI,
            this
        );
    }

    /// <summary>
    /// 도구 위치까지 이동하는 Tween을 시작한다.
    /// </summary>
    private void StartMoveTween(
        Vector2 targetPosition)
    {
        if (_catRectTransform == null)
            return;

        if (_moveDuration <= 0f)
        {
            _catRectTransform.anchoredPosition =
                targetPosition;

            OnMoveComplete();
            return;
        }

        _moveTween =
            _catRectTransform
                .DOAnchorPos(
                    targetPosition,
                    _moveDuration
                )
                .SetEase(Ease.OutSine)
                .OnComplete(OnMoveComplete);
    }

    /// <summary>
    /// 도구 위치 도착 후 포즈 애니메이션을 시작한다.
    /// </summary>
    private void OnMoveComplete()
    {
        _moveTween = null;
        HideAlertIcons();

        int arrivedItemID =
            _currentItemID;

        _state = CatInteractionState.Pose;

        PlayPoseAnimation(arrivedItemID);

        OnDestinationReached?.Invoke(
            arrivedItemID
        );

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 도구 위치 도착 " +
            $"/ ItemID:{arrivedItemID}",
            DebugType.UI,
            this
        );
    }

    /// <summary>
    /// 간식 도착 후 섭취 상태로 전환한다.
    /// 이미 같은 포즈가 재생 중이면 다시 시작하지 않는다.
    /// </summary>
    public void EnterEating(int itemID)
    {
        if (itemID <= 0)
            return;

        _currentItemID = itemID;
        _state = CatInteractionState.Eating;

        PlayEatingAnimation(itemID);

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 고양이 섭취 상태 시작 " +
            $"/ ItemID:{itemID}",
            DebugType.UI,
            this
        );
    }

    /// <summary>
    /// 현재 도구 상호작용을 중지한다.
    /// </summary>
    public void StopInteraction()
    {
        StopInteraction(true, true);
    }

    private void StopInteraction(
        bool notify,
        bool playIdle)
    {

        HideAlertIcons();

        if (_moveTween != null)
        {
            _moveTween.Kill();
            _moveTween = null;
        }

        _currentItemID = 0;
        _state = CatInteractionState.Idle;

        if (playIdle &&
            _spriteAnimator != null &&
            _spriteAnimator.isActiveAndEnabled &&
            _spriteAnimator.gameObject.activeInHierarchy)
        {
            _spriteAnimator.PlayIdle();
        }
    }

    /// <summary>
    /// 이동 중 Run 애니메이션을 반복 재생한다.
    /// </summary>
    private void PlayMoveAnimation()
    {
        if (_spriteAnimator == null)
            return;

        _spriteAnimator.PlayLoop(
            CatAnimationType.Run
        );
    }

    /// <summary>
    /// ItemID에 해당하는 포즈를 찾아 한 번 재생한다.
    /// </summary>
    private void PlayPoseAnimation(int itemID)
    {
        if (_spriteAnimator == null ||
            itemID <= 0)
        {
            return;
        }

        if (_poseSO == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] PoseSO가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );

            PlayIdleAnimation();
            return;
        }

        NyangNyangSnapPoseData poseData =
            _poseSO.GetRandomPoseByTool(itemID);

        if (poseData == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapCatController] ItemID에 맞는 포즈가 없습니다. " +
                $"ItemID:{itemID}",
                DebugType.UI,
                this
            );

            PlayIdleAnimation();
            return;
        }

        CatAnimationType animationType =
            ConvertAnimationType(
                poseData.PoseName
            );

        if (animationType ==
            CatAnimationType.None)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapCatController] 지원하지 않는 포즈입니다. " +
                $"PoseName:{poseData.PoseName}",
                DebugType.UI,
                this
            );

            PlayIdleAnimation();
            return;
        }

        int itemLevel =
            GetItemLevel(itemID);

        _spriteAnimator.PlayOnce(
            animationType,
            itemLevel
        );

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 포즈 애니메이션 실행 " +
            $"/ ItemID:{itemID}, " +
            $"ItemLevel:{itemLevel}, " +
            $"Pose:{poseData.PoseName}, " +
            $"Animation:{animationType}",
            DebugType.UI,
            this
        );
    }

    /// <summary>
    /// 간식 섭취 애니메이션을 재생한다.
    /// 이동 완료 시 이미 같은 포즈가 재생 중이면 재시작하지 않는다.
    /// </summary>
    private void PlayEatingAnimation(int itemID)
    {
        if (_spriteAnimator == null)
            return;

        NyangNyangSnapPoseData poseData =
            _poseSO != null
                ? _poseSO.GetRandomPoseByTool(itemID)
                : null;

        if (poseData == null)
            return;

        CatAnimationType animationType =
            ConvertAnimationType(
                poseData.PoseName
            );

        if (animationType ==
            CatAnimationType.None)
        {
            return;
        }

        if (_spriteAnimator.CurrentAnimationType ==
            animationType)
        {
            return;
        }

        int itemLevel =
            GetItemLevel(itemID);

        _spriteAnimator.PlayOnce(
            animationType,
            itemLevel
        );
    }

    /// <summary>
    /// 포즈 이름을 Sprite 애니메이션 타입으로 변환한다.
    /// </summary>
    private CatAnimationType ConvertAnimationType(
        string poseName)
    {
        return poseName switch
        {
            "때리기" => CatAnimationType.Hit,
            "잡기" => CatAnimationType.Catch,
            "물기" => CatAnimationType.Bite,
            _ => CatAnimationType.None
        };
    }

    /// <summary>
    /// ItemID를 이용해 머지 아이템 레벨을 조회한다.
    /// 조회에 실패하면 1레벨을 반환한다.
    /// </summary>
    private int GetItemLevel(int itemID)
    {
        if (LocalDataAccess.Instance == null ||
            LocalDataAccess.Instance.Game == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] LocalDataAccess가 준비되지 않았습니다.",
                DebugType.Data,
                this
            );

            return 1;
        }

        bool found =
            LocalDataAccess.Instance.Game
                .TryGetMergeBoardItemById(
                    itemID,
                    out ItemData itemData
                );

        if (!found || itemData == null)
        {
            DebugTool.Warning(
                $"[NyangNyangSnapCatController] 아이템 데이터를 찾을 수 없습니다. " +
                $"ItemID:{itemID}",
                DebugType.Data,
                this
            );

            return 1;
        }

        return Mathf.Max(
            1,
            itemData.ItemLevel
        );
    }

    /// <summary>
    /// 1회 포즈 애니메이션이 끝나면 LEAVE 상태로 전환한다.
    /// </summary>
    private void OnPoseAnimationCompleted(
        CatAnimationType animationType)
    {
        if (animationType != CatAnimationType.Hit &&
            animationType != CatAnimationType.Catch &&
            animationType != CatAnimationType.Bite)
        {
            return;
        }

        StartLeave();

        DebugTool.Log(
            $"[NyangNyangSnapCatController] 포즈 애니메이션 완료 - LEAVE 시작 " +
            $"/ Animation:{animationType}",
            DebugType.UI,
            this
        );
    }
    public void LookAtTarget(RectTransform targetRectTransform)
    {
        AutoAssignReferences();

        if (_catRectTransform == null ||
            targetRectTransform == null)
        {
            return;
        }

        RectTransform catParent =
            _catRectTransform.parent as RectTransform;

        if (catParent == null)
            return;

        Vector2 targetPosition =
            catParent.InverseTransformPoint(
                targetRectTransform.position
            );

        UpdateFacingDirection(
            targetPosition
        );
    }
    /// <summary>
    /// 화면 중앙으로 2초 동안 복귀한다.
    /// </summary>
    private void StartLeave()
    {
        AutoAssignReferences();

        if (_catRectTransform == null ||
            _moveArea == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapCatController] LEAVE 이동에 필요한 참조가 없습니다.",
                DebugType.UI,
                this
            );

            CompleteLeave();
            return;
        }

        if (_moveTween != null)
        {
            _moveTween.Kill();
            _moveTween = null;
        }

        _state = CatInteractionState.Leave;

        PlayMoveAnimation();

        Vector2 centerPosition =
            _moveArea.rect.center;

        _moveTween =
            _catRectTransform
                .DOAnchorPos(
                    centerPosition,
                    LeaveMoveDuration
                )
                .SetEase(Ease.OutSine)
                .OnComplete(CompleteLeave);
    }

    /// <summary>
    /// 화면 중앙 복귀 완료 후 Idle 상태로 전환한다.
    /// </summary>
    private void CompleteLeave()
    {
        _moveTween = null;
        _currentItemID = 0;
        _state = CatInteractionState.Idle;

        PlayIdleAnimation();

        DebugTool.Log(
            "[NyangNyangSnapCatController] LEAVE 완료 - 화면 중앙 복귀",
            DebugType.UI,
            this
        );
    }

    /// <summary>
    /// Idle 애니메이션을 반복 재생한다.
    /// </summary>
    private void PlayIdleAnimation()
    {
        if (_spriteAnimator == null)
            return;

        _spriteAnimator.PlayIdle();
    }
}