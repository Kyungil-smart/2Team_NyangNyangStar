using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class NyangQuariumFishPlacementDragHandler :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform _rectTransform;
    private NyangQuariumFishPlacementArea _placementArea;
    private bool _canDrag;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    public void Initialize(NyangQuariumFishPlacementArea placementArea)
    {
        _placementArea = placementArea;
        _canDrag = true;
    }

    public void SetDraggable(bool canDrag)
    {
        _canDrag = canDrag;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_canDrag)
            MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canDrag)
            MoveToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canDrag)
            MoveToPointer(eventData);
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (_rectTransform == null || _placementArea == null)
            return;

        if (_placementArea.TryGetClampedLocalPosition(
                eventData.position,
                eventData.pressEventCamera,
                _rectTransform,
                out Vector2 localPosition))
        {
            _rectTransform.anchoredPosition = localPosition;
        }
    }
}
