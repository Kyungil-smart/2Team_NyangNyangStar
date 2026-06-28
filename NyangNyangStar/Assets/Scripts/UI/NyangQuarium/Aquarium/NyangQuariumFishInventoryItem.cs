using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FishLayoutGroup 또는 NatureLayoutGroup 아래의 각 선택 버튼에 붙입니다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class NyangQuariumFishInventoryItem : MonoBehaviour
{
    [Header("아이템 정보")]
    [SerializeField] private int _itemId;
    [SerializeField] private NyangQuariumPlacementCategory _category;

    [Tooltip("실제 물고기 또는 자연 요소 Sprite가 표시되는 Image")]
    [SerializeField] private Image _itemImage;

    [Header("배치 연결")]
    [SerializeField] private NyangQuariumFishPlacementController _placementController;

    private Button _button;

    public int ItemId => _itemId;
    public NyangQuariumPlacementCategory Category => _category;
    public Sprite ItemSprite => _itemImage != null ? _itemImage.sprite : null;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (_itemImage == null)
            _itemImage = FindItemImage();

        BindButton();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(SelectThisItem);
    }

    public void Initialize(
        int itemId,
        NyangQuariumPlacementCategory category,
        Sprite itemSprite,
        NyangQuariumFishPlacementController placementController)
    {
        _itemId = itemId;
        _category = category;
        _placementController = placementController;

        if (_button == null)
            _button = GetComponent<Button>();

        if (_itemImage == null)
            _itemImage = FindItemImage();

        if (_itemImage != null)
        {
            _itemImage.sprite = itemSprite;
            _itemImage.preserveAspect = true;
        }

        BindButton();
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
            if (image != null &&
                image.gameObject != gameObject &&
                image.sprite != null)
            {
                return image;
            }
        }

        return GetComponent<Image>();
    }
}
