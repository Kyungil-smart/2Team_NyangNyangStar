using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class ShopSlotView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TMP_Text _itemNameText;
        [SerializeField] private TMP_Text _remainCountText;
        [SerializeField] private TMP_Text _costAmountText;
        [Tooltip("가격 옆 이벤트 코인/재화 아이콘입니다. 비워두면 자식 중 CoinIcon을 자동 검색합니다.")]
        [SerializeField] private Image _costIconImage;
        [SerializeField] private GameObject _soldOutOverlay;
        [SerializeField] private Button _button;

        private UISpriteController _itemIconController;
        private UISpriteController _costIconController;
        private FindMoongchiShopViewData _data;
        private System.Action<FindMoongchiShopViewData> _onClicked;

        private void Awake()
        {
            if (_itemIcon != null)
                _itemIconController = new UISpriteController(_itemIcon);

            if (_costIconImage == null)
                _costIconImage = FindChildImage(transform, "CoinIcon");

            if (_costIconImage != null)
                _costIconController = new UISpriteController(_costIconImage);
        }

        public void SetData(FindMoongchiShopViewData data, System.Action<FindMoongchiShopViewData> onClicked)
        {
            _data = data;
            _onClicked = onClicked;

            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            SetText(_itemNameText, BuildName(data));
            SetText(_remainCountText, BuildLimitText(data));
            SetText(_costAmountText, data.CostAmount.ToString());
            SetIcon(data);
            SetCostIcon(data);

            if (_soldOutOverlay != null)
                _soldOutOverlay.SetActive(data.IsSoldOut);

            if (_button != null)
            {
                _button.interactable = true;
                _button.onClick.RemoveListener(HandleClicked);
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            if (_data == null)
                return;

            DebugTool.Log($"[ShopSlotView] 슬롯 클릭: ShopItemId={_data.ShopItemId}, SoldOut={_data.IsSoldOut}, Cost={_data.CostAmount}", DebugType.FindMoongchi, this);

            _onClicked?.Invoke(_data);
        }

        private static string BuildName(FindMoongchiShopViewData data)
        {
            string baseName = string.IsNullOrEmpty(data.ProductName)
                ? $"{data.ProductType} {data.ProductId}"
                : data.ProductName;

            return data.Quantity > 1 ? $"{baseName} x{data.Quantity}" : baseName;
        }

        private static string BuildLimitText(FindMoongchiShopViewData data)
        {
            if (data == null)
                return string.Empty;

            if (!data.HasLimit)
                return "∞";

            int remainingLimit = Mathf.Clamp(data.RemainingLimit, 0, data.LimitCount);
            return $"{remainingLimit}/{data.LimitCount}";
        }

        private void SetIcon(FindMoongchiShopViewData data)
        {
            if (_itemIcon == null)
                return;

            if (!string.IsNullOrWhiteSpace(data.IconKey))
            {
                _itemIcon.enabled = true;

                if (_itemIconController == null)
                    _itemIconController = new UISpriteController(_itemIcon);

                _itemIconController.ChangeSprite(data.IconKey);
                return;
            }

            _itemIcon.sprite = data.Icon;
            _itemIcon.enabled = data.Icon != null;
        }

        private void SetCostIcon(FindMoongchiShopViewData data)
        {
            if (_costIconImage == null)
                _costIconImage = FindChildImage(transform, "CoinIcon");

            if (_costIconImage == null)
                return;

            // 상품 아이콘은 ProductType/ProductID를 따르지만, 구매 비용 아이콘은 항상 이벤트 코인입니다.
            string key = !string.IsNullOrWhiteSpace(data?.CostIconKey)
                ? data.CostIconKey
                : FindMoongchiSpriteKeys.EventCoinIcon;

            if (string.IsNullOrWhiteSpace(key))
            {
                _costIconImage.enabled = false;
                return;
            }

            _costIconImage.enabled = true;
            _costIconImage.sprite = null;
            _costIconImage.color = Color.white;
            _costIconImage.preserveAspect = true;

            if (_costIconController == null)
                _costIconController = new UISpriteController(_costIconImage);

            _costIconController.ChangeSprite(key);
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == childName && child.TryGetComponent(out Image image))
                    return image;

                Image nested = FindChildImage(child, childName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);

            _itemIconController?.Dispose();
            _costIconController?.Dispose();
        }
    }
}
