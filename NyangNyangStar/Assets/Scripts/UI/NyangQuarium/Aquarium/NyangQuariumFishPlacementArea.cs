using UnityEngine;

/// <summary>
/// 수조 전체 RectTransform을 기준으로 물고기와 자연 요소의 배치 영역을 계산합니다.
/// 자연 요소는 인스펙터에서 지정한 Offset 안에 전체 이미지가 들어와야 배치할 수 있습니다.
/// </summary>
public sealed class NyangQuariumFishPlacementArea : MonoBehaviour
{
    [Header("기준 영역")]
    [SerializeField]
    private RectTransform _areaRect;

    [Header("자연 요소 배치 가능 영역 Offset")]
    [Tooltip("기준 영역의 왼쪽에서 제외할 거리입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _leftOffset;

    [Tooltip("기준 영역의 오른쪽에서 제외할 거리입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _rightOffset;

    [Tooltip("기준 영역의 위쪽에서 제외할 거리입니다. 1080x1920 기준 기본값은 1255입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _topOffset = 1255f;

    [Tooltip("기준 영역의 아래쪽에서 제외할 거리입니다.")]
    [Min(0f)]
    [SerializeField]
    private float _bottomOffset;

    public RectTransform AreaRect => _areaRect;

    private void Awake()
    {
        if (_areaRect == null)
            _areaRect = transform as RectTransform;
    }

    /// <summary>
    /// 자연 요소 배치 가능 영역의 중앙 위치를 반환합니다.
    /// </summary>
    public Vector2 GetCenterLocalPosition()
    {
        return GetNaturePlacementRect().center;
    }

    public Vector2 GetRandomLocalPosition(RectTransform target)
    {
        if (_areaRect == null || target == null)
            return Vector2.zero;

        Rect areaRect = _areaRect.rect;
        Rect targetRect = target.rect;

        float width = targetRect.width * Mathf.Abs(target.localScale.x);
        float height = targetRect.height * Mathf.Abs(target.localScale.y);

        float minX = areaRect.xMin + width * target.pivot.x;
        float maxX = areaRect.xMax - width * (1f - target.pivot.x);
        float minY = areaRect.yMin + height * target.pivot.y;
        float maxY = areaRect.yMax - height * (1f - target.pivot.y);

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
    /// 위치를 강제로 보정하지 않으므로 배치 영역 밖까지 드래그할 수 있습니다.
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
    /// 자연 요소 이미지의 네 모서리가 배치 가능 영역 안에 모두 포함되는지 확인합니다.
    /// 한쪽이라도 영역 밖으로 벗어나면 false를 반환합니다.
    /// </summary>
    public bool IsFullyInsideNaturePlacementArea(RectTransform target)
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

            if (localCorner.x < placementRect.xMin ||
                localCorner.x > placementRect.xMax ||
                localCorner.y < placementRect.yMin ||
                localCorner.y > placementRect.yMax)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 기준 영역 Rect에서 인스펙터 Offset을 제외한 자연 요소 배치 가능 Rect를 반환합니다.
    /// </summary>
    public Rect GetNaturePlacementRect()
    {
        if (_areaRect == null)
            return Rect.zero;

        Rect sourceRect = _areaRect.rect;

        float xMin = sourceRect.xMin + _leftOffset;
        float xMax = sourceRect.xMax - _rightOffset;
        float yMin = sourceRect.yMin + _bottomOffset;
        float yMax = sourceRect.yMax - _topOffset;

        if (xMin > xMax)
        {
            float centerX = sourceRect.center.x;
            xMin = centerX;
            xMax = centerX;
        }

        if (yMin > yMax)
        {
            float centerY = sourceRect.center.y;
            yMin = centerY;
            yMax = centerY;
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}
