using System;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 배치가 확정된 자연 요소의 선택 및 삭제용 데이터를 관리합니다.
/// </summary>
public sealed class NyangQuariumPlacedNature :
    MonoBehaviour,
    IPointerClickHandler
{
    private NyangquariumFishController _visualController;
    private Image _image;

    public int ItemId { get; private set; }
    public string SpriteKey { get; private set; }
    public RectTransform RectTransform { get; private set; }

    public static event Action<NyangQuariumPlacedNature> Clicked;

    private void Awake()
    {
        ResolveComponents();
    }

    /// <summary>
    /// 배치가 완료된 자연 요소의 식별 데이터를 설정합니다.
    /// </summary>
    public void Initialize(
        int itemId,
        string spriteKey,
        NyangquariumFishController visualController)
    {
        ItemId = itemId;
        SpriteKey = spriteKey;

        _visualController = visualController != null
            ? visualController
            : GetComponent<NyangquariumFishController>();

        ResolveComponents();

        // 자연 요소는 유영하지 않습니다.
        _visualController?.SetMovementEnabled(false);

        // 물고기 선택 이벤트는 사용하지 않습니다.
        _visualController?.SetSelectable(false);
        _visualController?.SetSelected(false);

        /*
         * SetSelectable(false) 호출 시
         * NyangquariumFishController가 Image.raycastTarget을 false로 변경합니다.
         *
         * 자연 요소 삭제 클릭은 이 컴포넌트가 직접 받아야 하므로
         * Raycast를 다시 활성화합니다.
         */
        if (_image != null)
            _image.raycastTarget = true;

        DebugTool.Log($"[NyangQuariumPlacedNature] 자연 요소 등록 - " +
            $"ItemId:{ItemId}, SpriteKey:{SpriteKey}, " +
            $"Raycast:{_image != null && _image.raycastTarget}",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 삭제 모드 선택 표시를 변경합니다.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        _visualController?.SetSelected(isSelected);

        /*
         * SetSelected는 현재는 Raycast를 변경하지 않지만,
         * 자연 요소 클릭 상태를 확실하게 유지하기 위해 다시 활성화합니다.
         */
        if (_image != null)
            _image.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        DebugTool.Log($"[NyangQuariumPlacedNature] 자연 요소 클릭 - " +
            $"ItemId:{ItemId}, SpriteKey:{SpriteKey}",
            DebugType.UI,
            this);

        Clicked?.Invoke(this);
    }

    private void ResolveComponents()
    {
        RectTransform = transform as RectTransform;

        if (_visualController == null)
        {
            _visualController =
                GetComponent<NyangquariumFishController>();
        }

        if (_image == null)
            _image = GetComponent<Image>();
    }
}