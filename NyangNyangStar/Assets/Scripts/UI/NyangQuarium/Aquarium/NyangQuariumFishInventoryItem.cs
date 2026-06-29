using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 관상어 배치 패널의 아이템 버튼 하나를 관리합니다.
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

    [Header("배치 연결")]
    [SerializeField] private NyangQuariumFishPlacementController _placementController;

    private Button _button;

    public int ItemId => _itemId;
    public Sprite ItemSprite => _itemImage != null ? _itemImage.sprite : null;

    private void Awake()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(SelectThisItem);
    }

    public void Initialize(
        int itemId,
        int count,
        Sprite itemSprite,
        NyangQuariumFishPlacementController placementController)
    {
        _itemId = itemId;
        _category = NyangQuariumPlacementCategory.Fish;
        _placementController = placementController;

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
            _countText.gameObject.SetActive(count > 1);
        }

        if (_button != null)
            _button.interactable = true;

        BindButton();
    }

    /// <summary>
    /// 표시용 빈 슬롯으로 초기화합니다.
    /// 슬롯 배경은 유지하고 아이템 이미지와 수량만 숨깁니다.
    /// </summary>
    public void InitializeEmpty()
    {
        _itemId = 0;
        _category = NyangQuariumPlacementCategory.Fish;
        _placementController = null;

        ResolveReferences();

        if (_itemImage != null)
        {
            _itemImage.sprite = null;
            _itemImage.enabled = false;
        }

        if (_countText != null)
        {
            _countText.text = string.Empty;
            _countText.gameObject.SetActive(false);
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(SelectThisItem);
            _button.interactable = false;
        }
    }

    private void SelectThisItem()
    {
        if (_placementController == null)
        {
            Debug.LogWarning(
                $"[NyangQuariumFishInventoryItem] {name}에 PlacementController가 연결되지 않았습니다.",
                this);
            return;
        }

        if (_itemImage == null || _itemImage.sprite == null)
        {
            Debug.LogWarning(
                $"[NyangQuariumFishInventoryItem] {name}에 Item Sprite가 없습니다.",
                this);
            return;
        }

        _placementController.SelectItem(
            _itemId,
            _itemImage.sprite,
            _category);
    }

    private void ResolveReferences()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_itemImage == null)
            _itemImage = FindItemImage();

        if (_countText == null)
            _countText = FindCountText();
    }

    private void BindButton()
    {
        if (_button == null)
            return;

        _button.onClick.RemoveListener(SelectThisItem);
        _button.onClick.AddListener(SelectThisItem);
    }

    private Image FindItemImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image != null && image.gameObject != gameObject)
                return image;
        }

        return GetComponent<Image>();
    }

    private TMP_Text FindCountText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text != null && text.name.Contains("Count"))
                return text;
        }

        return texts.Length > 0 ? texts[0] : null;
    }
}
