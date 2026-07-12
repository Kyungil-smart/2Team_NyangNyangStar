using UnityEngine;

/// <summary>
/// 수조 전체 영역을 기준으로 물고기 랜덤 위치와 드래그 좌표를 계산합니다.
/// 자연 요소 설치 가능 영역은 수조 하단부터 기준 버튼 직전까지 자동 설정합니다.
/// </summary>
public sealed class NyangQuariumFishPlacementArea : MonoBehaviour
{
    [Header("전체 드래그 및 물고기 배치 기준 영역")]
    [SerializeField]
    private RectTransform _areaRect;

    [Header("자연 요소 설치 가능 영역")]
    [SerializeField]
    private RectTransform _naturePlacementArea;

    [Header("배치 영역 상단 기준 버튼")]
    [Tooltip("물고기 배치 패널을 여는 버튼입니다.")]
    [SerializeField]
    private RectTransform _fishInventoryButton;

    [Tooltip("수조 변경 버튼입니다.")]
    [SerializeField]
    private RectTransform _changeButton;

    [Header("버튼과 배치 영역 사이 간격")]
    [Tooltip("자연 요소 설치 가능 영역과 버튼 사이의 간격입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _buttonPadding = 30f;

    public RectTransform AreaRect => _areaRect;

    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        RefreshNaturePlacementArea();
    }

    private void OnEnable()
    {
        RefreshNaturePlacementArea();
    }

    /// <summary>
    /// 해상도나 부모 RectTransform 크기가 바뀌면 영역을 다시 계산합니다.
    /// Device Simulator 기종 변경에도 대응합니다.
    /// </summary>
    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;

        RefreshNaturePlacementArea();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _buttonPadding = Mathf.Max(0f, _buttonPadding);

        InitializeReferences();
    }
#endif

    /// <summary>
    /// 필수 RectTransform 참조를 초기화합니다.
    /// </summary>
    private void InitializeReferences()
    {
        if (_areaRect == null)
        {
            _areaRect = transform as RectTransform;
        }

        if (_areaRect == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementArea] " +
                "전체 배치 기준 RectTransform을 찾을 수 없습니다.",
                this);
        }

        if (_naturePlacementArea == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementArea] " +
                "자연 요소 설치 가능 영역이 연결되지 않았습니다.",
                this);
        }

        if (_fishInventoryButton == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementArea] " +
                "물고기 배치 패널 버튼이 연결되지 않았습니다.",
                this);
        }

        if (_changeButton == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementArea] " +
                "수조 변경 버튼이 연결되지 않았습니다.",
                this);
        }
    }

    /// <summary>
    /// 자연 요소 설치 가능 영역을 수조 바닥부터
    /// 두 기준 버튼 중 아래에서 먼저 만나는 버튼 직전까지 설정합니다.
    /// </summary>
    private void RefreshNaturePlacementArea()
    {
        if (_areaRect == null ||
            _naturePlacementArea == null ||
            _fishInventoryButton == null ||
            _changeButton == null)
        {
            return;
        }

        float fishButtonBottomY =
            GetBottomPositionInArea(_fishInventoryButton);

        float changeButtonBottomY =
            GetBottomPositionInArea(_changeButton);

        // 화면 아래쪽에서 위로 올라갈 때 먼저 만나는 버튼을 기준으로 사용합니다.
        float lowestButtonBottomY =
            Mathf.Min(fishButtonBottomY, changeButtonBottomY);

        Rect areaRect = _areaRect.rect;

        float bottomY = areaRect.yMin;
        float topY = lowestButtonBottomY - _buttonPadding;

        topY = Mathf.Clamp(
            topY,
            areaRect.yMin,
            areaRect.yMax);

        ApplyNaturePlacementArea(bottomY, topY);
    }

    /// <summary>
    /// 버튼 하단 위치를 전체 배치 영역의 로컬 Y 좌표로 반환합니다.
    /// </summary>
    private float GetBottomPositionInArea(
        RectTransform target)
    {
        Vector3 bottomCenterLocal = new Vector3(
            target.rect.center.x,
            target.rect.yMin,
            0f);

        Vector3 bottomCenterWorld =
            target.TransformPoint(bottomCenterLocal);

        Vector3 bottomCenterInArea =
            _areaRect.InverseTransformPoint(bottomCenterWorld);

        return bottomCenterInArea.y;
    }

    /// <summary>
    /// 계산한 하단과 상단 위치를 NaturePlacementArea에 적용합니다.
    /// </summary>
    private void ApplyNaturePlacementArea(
        float bottomY,
        float topY)
    {
        float localBottom =
            bottomY - _areaRect.rect.yMin;

        float localTop =
            topY - _areaRect.rect.yMin;

        _naturePlacementArea.anchorMin =
            new Vector2(0f, 0f);

        _naturePlacementArea.anchorMax =
            new Vector2(1f, 0f);

        _naturePlacementArea.pivot =
            new Vector2(0.5f, 0f);

        _naturePlacementArea.offsetMin =
            new Vector2(0f, localBottom);

        _naturePlacementArea.offsetMax =
            new Vector2(0f, localTop);
    }

    /// <summary>
    /// 자연 요소 설치 가능 영역 중앙 위치를
    /// 전체 배치 영역의 로컬 좌표로 반환합니다.
    /// </summary>
    public Vector2 GetCenterLocalPosition()
    {
        if (_areaRect == null ||
            _naturePlacementArea == null)
        {
            return Vector2.zero;
        }

        Vector3 worldCenter =
            _naturePlacementArea.TransformPoint(
                _naturePlacementArea.rect.center);

        Vector3 localCenter =
            _areaRect.InverseTransformPoint(worldCenter);

        return localCenter;
    }

    /// <summary>
    /// 물고기가 전체 영역 내부에 들어오는 랜덤 위치를 반환합니다.
    /// </summary>
    public Vector2 GetRandomLocalPosition(
        RectTransform target)
    {
        if (_areaRect == null || target == null)
            return Vector2.zero;

        Rect areaRect = _areaRect.rect;
        Rect targetRect = target.rect;

        float width =
            targetRect.width *
            Mathf.Abs(target.localScale.x);

        float height =
            targetRect.height *
            Mathf.Abs(target.localScale.y);

        float minX =
            areaRect.xMin +
            width * target.pivot.x;

        float maxX =
            areaRect.xMax -
            width * (1f - target.pivot.x);

        float minY =
            areaRect.yMin +
            height * target.pivot.y;

        float maxY =
            areaRect.yMax -
            height * (1f - target.pivot.y);

        float randomX = minX <= maxX
            ? Random.Range(minX, maxX)
            : areaRect.center.x;

        float randomY = minY <= maxY
            ? Random.Range(minY, maxY)
            : areaRect.center.y;

        return new Vector2(randomX, randomY);
    }

    /// <summary>
    /// 화면 좌표를 전체 배치 영역의 로컬 좌표로 변환합니다.
    /// 설치 불가능 영역까지 드래그할 수 있도록 좌표를 제한하지 않습니다.
    /// </summary>
    public bool TryGetLocalPosition(
        Vector2 screenPosition,
        Camera eventCamera,
        out Vector2 localPosition)
    {
        localPosition = Vector2.zero;

        if (_areaRect == null)
            return false;

        return RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                _areaRect,
                screenPosition,
                eventCamera,
                out localPosition);
    }

    /// <summary>
    /// 자연 요소의 네 모서리가 설치 가능 영역 안에
    /// 모두 포함되는지 확인합니다.
    /// </summary>
    public bool IsFullyInsideNaturePlacementArea(
        RectTransform target)
    {
        if (_naturePlacementArea == null ||
            target == null)
        {
            return false;
        }

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        Rect placementRect =
            _naturePlacementArea.rect;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 localCorner =
                _naturePlacementArea
                    .InverseTransformPoint(worldCorners[i]);

            if (!placementRect.Contains(localCorner))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 자연 요소 설치 가능 영역을
    /// 전체 배치 영역 기준의 Rect로 반환합니다.
    /// </summary>
    public Rect GetNaturePlacementRect()
    {
        if (_areaRect == null ||
            _naturePlacementArea == null)
        {
            return Rect.zero;
        }

        Vector3[] worldCorners = new Vector3[4];
        _naturePlacementArea.GetWorldCorners(worldCorners);

        Vector3 bottomLeft =
            _areaRect.InverseTransformPoint(worldCorners[0]);

        Vector3 topRight =
            _areaRect.InverseTransformPoint(worldCorners[2]);

        return Rect.MinMaxRect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x,
            topRight.y);
    }
}