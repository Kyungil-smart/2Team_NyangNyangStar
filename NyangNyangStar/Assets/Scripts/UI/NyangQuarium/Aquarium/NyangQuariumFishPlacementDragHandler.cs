using System;
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
    private Action<bool> _placementValidityChanged;
    private bool _canDrag;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    public void Initialize(
        NyangQuariumFishPlacementArea placementArea,
        Action<bool> placementValidityChanged)
    {
        _placementArea = placementArea;
        _placementValidityChanged = placementValidityChanged;
        _canDrag = true;

        NotifyPlacementValidity();
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

        if (!_placementArea.TryGetLocalPosition(
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPosition))
        {
            return;
        }

        _rectTransform.anchoredPosition = localPosition;
        NotifyPlacementValidity();
    }

    private void NotifyPlacementValidity()
    {
        if (_placementArea == null || _rectTransform == null)
            return;

        bool isValid =
            _placementArea.IsFullyInsideNaturePlacementArea(
                _rectTransform);

        _placementValidityChanged?.Invoke(isValid);
    }
}
