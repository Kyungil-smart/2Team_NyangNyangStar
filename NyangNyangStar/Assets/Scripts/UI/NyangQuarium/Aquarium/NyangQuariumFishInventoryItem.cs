using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 냥쿠아리움 통합 인벤토리의 아이템 슬롯 하나를 관리합니다.
/// 관상어와 자연 요소를 모두 지원합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class NyangQuariumFishInventoryItem : MonoBehaviour
{
    [Header("아이템 정보")]
    [SerializeField] private int _itemId;

    [SerializeField]
    private NyangQuariumPlacementCategory _category =
        NyangQuariumPlacementCategory.Fish;

    [SerializeField] private Image _itemImage;
    [SerializeField] private TMP_Text _countText;

    [Header("이름 표시")]
    [Tooltip("아이템 이름을 표시하는 TextMeshPro Text")]
    [SerializeField] private TMP_Text _fishText;

    [Tooltip("아이템 타입별 색상을 적용할 이름 배경 Image")]
    [SerializeField] private Image _fishTextImage;

    [Header("선택 강조")]
    [Tooltip("선택 상태의 색상을 적용할 슬롯 루트 Image")]
    [SerializeField] private Image _slotBackgroundImage;

    [SerializeField] private Color _normalSlotColor = Color.white;

    [SerializeField]
    private Color _selectedSlotColor =
        new Color(0.70f, 1f, 0.80f, 1f);

    [Header("배치 연결")]
    [SerializeField]
    private NyangQuariumFishPlacementController _placementController;

    private Button _button;
    private Action<NyangQuariumFishInventoryItem> _selectedCallback;

    // 선택 시 FishTextImage에 적용할 타입별 배경색입니다.
    private Color _nameBackgroundColor = Color.white;

    public int ItemId => _itemId;

    public NyangQuariumPlacementCategory Category => _category;

    public Sprite ItemSprite =>
        _itemImage != null
            ? _itemImage.sprite
            : null;

    private void Awake()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(
                SelectThisItem);
        }

        _selectedCallback = null;
    }

    /// <summary>
    /// 관상어 또는 자연 요소 슬롯을 초기화합니다.
    /// </summary>
    public void Initialize(
        int itemId,
        int count,
        Sprite itemSprite,
        string displayName,
        Color nameBackgroundColor,
        NyangQuariumPlacementCategory category,
        NyangQuariumFishPlacementController placementController,
        Action<NyangQuariumFishInventoryItem> selectedCallback)
    {
        _itemId = itemId;
        _category = category;
        _placementController = placementController;
        _selectedCallback = selectedCallback;
        _nameBackgroundColor = nameBackgroundColor;

        ResolveReferences();

        if (_itemImage != null)
        {
            _itemImage.sprite = itemSprite;
            _itemImage.preserveAspect = true;
            _itemImage.enabled = itemSprite != null;
        }

        if (_countText != null)
        {
            _countText.text = $"x{count}";
            _countText.gameObject.SetActive(count >= 1);
        }

        if (_fishText != null)
        {
            _fishText.text =
                string.IsNullOrWhiteSpace(displayName)
                    ? string.Empty
                    : displayName;

            // FishText 글자색은 인스펙터 설정을 그대로 사용합니다.
        }

        if (_fishTextImage != null)
        {
            _fishTextImage.color = _nameBackgroundColor;

            // 이름 패널은 선택 여부와 관계없이 항상 표시합니다.
            _fishTextImage.gameObject.SetActive(true);
        }

        if (_button != null)
            _button.interactable = itemSprite != null;

        SetSelected(false);
        BindButton();
    }

    /// <summary>
    /// 슬롯의 선택 강조 상태를 변경합니다.
    /// 이름 패널은 항상 표시하고 슬롯 배경색으로만 선택을 구분합니다.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (_slotBackgroundImage != null)
        {
            _slotBackgroundImage.color =
                isSelected
                    ? _selectedSlotColor
                    : _normalSlotColor;
        }

        if (_fishTextImage != null)
        {
            _fishTextImage.color = _nameBackgroundColor;
            _fishTextImage.gameObject.SetActive(true);
        }
    }

    private void SelectThisItem()
    {
        if (_placementController == null)
        {
            DebugTool.Warning(
                $"[NyangQuariumFishInventoryItem] " +
                $"{name}에 PlacementController가 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return;
        }

        if (_itemImage == null ||
            _itemImage.sprite == null)
        {
            DebugTool.Warning(
                $"[NyangQuariumFishInventoryItem] " +
                $"{name}에 Item Sprite가 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        // 리스트 UI에 현재 슬롯이 선택됐음을 알립니다.
        _selectedCallback?.Invoke(this);

        _placementController.SelectItem(
            _itemId,
            _itemImage.sprite,
            _category);

        DebugTool.Log(
            $"[NyangQuariumFishInventoryItem] " +
            $"아이템 선택 - " +
            $"ItemId:{_itemId}, Category:{_category}",
            DebugType.UI,
            this);
    }

    private void ResolveReferences()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_slotBackgroundImage == null)
            _slotBackgroundImage = GetComponent<Image>();

        if (_itemImage == null)
            _itemImage = FindImageByName("FishImage");

        if (_fishTextImage == null)
            _fishTextImage = FindImageByName("FishTextImage");

        if (_countText == null)
            _countText = FindTextByName("CountText");

        if (_fishText == null)
            _fishText = FindTextByName("FishText");
    }

    private void BindButton()
    {
        if (_button == null)
            return;

        _button.onClick.RemoveListener(
            SelectThisItem);

        _button.onClick.AddListener(
            SelectThisItem);
    }

    private Image FindImageByName(string targetName)
    {
        Image[] images =
            GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image != null &&
                image.gameObject.name == targetName)
            {
                return image;
            }
        }

        return null;
    }

    private TMP_Text FindTextByName(string targetName)
    {
        TMP_Text[] texts =
            GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text != null &&
                text.gameObject.name == targetName)
            {
                return text;
            }
        }

        return null;
    }
}