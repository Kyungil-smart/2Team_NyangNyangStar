using UnityEngine;

/// <summary>
/// NyangQuariumLayoutPanel 전체를 관상어 배치 가능 영역으로 사용합니다.
/// 자연 요소의 별도 바닥 판정은 이후 확장할 수 있습니다.
/// </summary>
public sealed class NyangQuariumFishPlacementArea : MonoBehaviour
{
    [SerializeField] private RectTransform _areaRect;

    public RectTransform AreaRect => _areaRect;

    private void Awake()
    {
        if (_areaRect == null)
            _areaRect = transform as RectTransform;
    }

    public Vector2 GetCenterLocalPosition()
    {
        return _areaRect != null ? _areaRect.rect.center : Vector2.zero;
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

    public bool TryGetClampedLocalPosition(
        Vector2 screenPosition,
        Camera eventCamera,
        RectTransform target,
        out Vector2 clampedPosition)
    {
        clampedPosition = Vector2.zero;

        if (_areaRect == null || target == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _areaRect,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        clampedPosition = ClampLocalPosition(localPoint, target);
        return true;
    }

    public Vector2 ClampLocalPosition(Vector2 localPosition, RectTransform target)
    {
        if (_areaRect == null || target == null)
            return localPosition;

        Rect areaRect = _areaRect.rect;
        Rect targetRect = target.rect;

        float width = targetRect.width * Mathf.Abs(target.localScale.x);
        float height = targetRect.height * Mathf.Abs(target.localScale.y);

        float minX = areaRect.xMin + width * target.pivot.x;
        float maxX = areaRect.xMax - width * (1f - target.pivot.x);
        float minY = areaRect.yMin + height * target.pivot.y;
        float maxY = areaRect.yMax - height * (1f - target.pivot.y);

        float x = minX <= maxX
            ? Mathf.Clamp(localPosition.x, minX, maxX)
            : areaRect.center.x;

        float y = minY <= maxY
            ? Mathf.Clamp(localPosition.y, minY, maxY)
            : areaRect.center.y;

        return new Vector2(x, y);
    }
}
