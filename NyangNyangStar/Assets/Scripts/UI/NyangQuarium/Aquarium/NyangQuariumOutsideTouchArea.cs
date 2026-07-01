using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 수조의 빈 영역에 들어온 UI 클릭을 외부로 전달합니다.
/// Image의 Raycast Target이 활성화된 UI 오브젝트에 추가합니다.
/// </summary>
public sealed class NyangQuariumOutsideTouchArea :
    MonoBehaviour,
    IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        Clicked?.Invoke();
    }
}
