using UnityEngine;

/// <summary>
/// 수조 전체 RectTransform을 기준으로 물고기와 자연 요소의 배치 영역을 계산합니다.
/// 자연 요소는 화면 높이에 대한 비율로 배치 가능 영역을 계산하며,
/// 대상 이미지 전체가 영역 안에 들어와야 배치할 수 있습니다.
/// </summary>
public sealed class NyangQuariumFishPlacementArea : MonoBehaviour
{
    [Header("기준 영역")]
    [SerializeField]
    private RectTransform _areaRect;

    [Header("자연 요소 배치 가능 영역")]
    [Tooltip("화면 아래쪽부터 자연 요소를 배치할 수 있는 높이 비율입니다.")]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float _naturePlacementHeightRatio = 0.235f;

    [Tooltip("기준 영역의 왼쪽에서 제외할 거리입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _leftOffset;

    [Tooltip("기준 영역의 오른쪽에서 제외할 거리입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _rightOffset;

    [Tooltip("기준 영역의 아래쪽에서 제외할 거리입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _bottomOffset;

    public RectTransform AreaRect => _areaRect;

    private void Awake()
    {
        InitializeAreaRect();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _naturePlacementHeightRatio =
            Mathf.Clamp01(_naturePlacementHeightRatio);

        _leftOffset = Mathf.Max(0f, _leftOffset);
        _rightOffset = Mathf.Max(0f, _rightOffset);
        _bottomOffset = Mathf.Max(0f, _bottomOffset);

        InitializeAreaRect();
    }
#endif

    /// <summary>
    /// 기준 영역이 비어 있으면 현재 오브젝트의 RectTransform을 사용합니다.
    /// </summary>
    private void InitializeAreaRect()
    {
        if (_areaRect != null)
            return;

        _areaRect = transform as RectTransform;

        if (_areaRect == null)
        {
            Debug.LogWarning(
                "[NyangQuariumFishPlacementArea] " +
                "배치 기준 RectTransform을 찾을 수 없습니다.",
                this);
        }
    }

    /// <summary>
    /// 자연 요소 배치 가능 영역의 중앙 위치를 반환합니다.
    /// </summary>
    public Vector2 GetCenterLocalPosition()
    {
        return GetNaturePlacementRect().center;
    }

    /// <summary>
    /// 물고기 대상이 기준 영역 내부에 완전히 들어오는 랜덤 위치를 반환합니다.
    /// </summary>
    public Vector2 GetRandomLocalPosition(RectTransform target)
    {
        if (_areaRect == null || target == null)
            return Vector2.zero;

        Rect areaRect = _areaRect.rect;
        Rect targetRect = target.rect;

        float width =
            targetRect.width * Mathf.Abs(target.localScale.x);

        float height =
            targetRect.height * Mathf.Abs(target.localScale.y);

        float minX =
            areaRect.xMin + width * target.pivot.x;

        float maxX =
            areaRect.xMax - width * (1f - target.pivot.x);

        float minY =
            areaRect.yMin + height * target.pivot.y;

        float maxY =
            areaRect.yMax - height * (1f - target.pivot.y);

        float randomX = minX <= maxX
            ? Random.Range(minX, maxX)
            : areaRect.center.x;

        float randomY = minY <= maxY
            ? Random.Range(minY, maxY)
            : areaRect.center.y;

        return new Vector2(randomX, randomY);
    }

    /// <summary>
    /// 화면 좌표를 기준 영역의 로컬 좌표로 변환합니다.
    /// 배치 영역 밖까지 드래그할 수 있도록 좌표를 강제로 제한하지 않습니다.
    /// </summary>
    public bool TryGetLocalPosition(
        Vector2 screenPosition,
        Camera eventCamera,
        out Vector2 localPosition)
    {
        localPosition = Vector2.zero;

        if (_areaRect == null)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _areaRect,
            screenPosition,
            eventCamera,
            out localPosition);
    }

    /// <summary>
    /// 자연 요소 이미지의 네 모서리가 배치 가능 영역 내부에
    /// 모두 포함되는지 확인합니다.
    /// </summary>
    public bool IsFullyInsideNaturePlacementArea(
        RectTransform target)
    {
        if (_areaRect == null || target == null)
            return false;

        Rect placementRect = GetNaturePlacementRect();

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 localCorner =
                _areaRect.InverseTransformPoint(worldCorners[i]);

            bool isOutside =
                localCorner.x < placementRect.xMin ||
                localCorner.x > placementRect.xMax ||
                localCorner.y < placementRect.yMin ||
                localCorner.y > placementRect.yMax;

            if (isOutside)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 화면 높이 비율과 좌우·하단 여백을 적용한
    /// 자연 요소 배치 가능 영역을 반환합니다.
    /// </summary>
    public Rect GetNaturePlacementRect()
    {
        if (_areaRect == null)
            return Rect.zero;

        Rect sourceRect = _areaRect.rect;

        float xMin =
            sourceRect.xMin + _leftOffset;

        float xMax =
            sourceRect.xMax - _rightOffset;

        float yMin =
            sourceRect.yMin + _bottomOffset;

        float placementHeight =
            sourceRect.height * _naturePlacementHeightRatio;

        float yMax =
            sourceRect.yMin + placementHeight;

        ValidateHorizontalRange(
            sourceRect,
            ref xMin,
            ref xMax);

        ValidateVerticalRange(
            sourceRect,
            ref yMin,
            ref yMax);

        return Rect.MinMaxRect(
            xMin,
            yMin,
            xMax,
            yMax);
    }

    /// <summary>
    /// 좌우 Offset이 기준 영역보다 큰 경우 중앙으로 보정합니다.
    /// </summary>
    private static void ValidateHorizontalRange(
        Rect sourceRect,
        ref float xMin,
        ref float xMax)
    {
        if (xMin <= xMax)
            return;

        xMin = sourceRect.center.x;
        xMax = sourceRect.center.x;
    }

    /// <summary>
    /// 하단 Offset이 배치 가능 높이를 초과한 경우 중앙으로 보정합니다.
    /// </summary>
    private static void ValidateVerticalRange(
        Rect sourceRect,
        ref float yMin,
        ref float yMax)
    {
        yMin = Mathf.Clamp(
            yMin,
            sourceRect.yMin,
            sourceRect.yMax);

        yMax = Mathf.Clamp(
            yMax,
            sourceRect.yMin,
            sourceRect.yMax);

        if (yMin <= yMax)
            return;

        float correctedY = Mathf.Clamp(
            yMin,
            sourceRect.yMin,
            sourceRect.yMax);

        yMin = correctedY;
        yMax = correctedY;
    }
}