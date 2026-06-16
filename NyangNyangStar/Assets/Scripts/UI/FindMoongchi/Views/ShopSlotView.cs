using TMPro;
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
        [SerializeField] private GameObject _soldOutOverlay;
        [SerializeField] private Button _button;

        private FindMoongchiShopViewData _data;
        private System.Action<FindMoongchiShopViewData> _onClicked;

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
            SetSprite(data.Icon);

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
            if (!data.HasLimit)
                return "구매 제한 없음";

            return $"{data.PurchasedCount}/{data.LimitCount}";
        }

        private void SetSprite(Sprite sprite)
        {
            if (_itemIcon == null)
                return;

            _itemIcon.sprite = sprite;
            _itemIcon.enabled = sprite != null;
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
        }
    }
}
