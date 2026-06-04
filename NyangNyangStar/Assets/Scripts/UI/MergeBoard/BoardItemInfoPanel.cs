using Data.ScriptableObjects.MergeBoard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class BoardItemInfoPanel : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private TMP_Text _itemLevelText;
        [SerializeField] private TMP_Text _itemNameText;

        [Header("버튼")]
        [SerializeField] private Button _sellButton;

        private BoardSystem _boardSystem;
        private ItemData _currentItemData = ItemData.Empty;

        private void Awake()
        {
            if (_sellButton != null)
                _sellButton.onClick.AddListener(OnSellButtonClicked);

            Hide();
        }

        private void OnDestroy()
        {
            if (_sellButton != null)
                _sellButton.onClick.RemoveListener(OnSellButtonClicked);
        }

        public void Init(BoardSystem boardSystem)
        {
            _boardSystem = boardSystem;
        }

        public void Show(ItemData itemData)
        {
            _currentItemData = itemData?.Clone() ?? ItemData.Empty;

            if (!_currentItemData.HasItem)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            if (_itemNameText != null)
                _itemNameText.text = _currentItemData.ItemName;

            if (_itemLevelText != null)
                _itemLevelText.text = $"Lv. {_currentItemData.ItemLevel}";

            if (_sellButton != null)
                _sellButton.interactable = true;
        }

        public void Hide()
        {
            _currentItemData = ItemData.Empty;

            if (_itemNameText != null)
                _itemNameText.text = string.Empty;

            if (_itemLevelText != null)
                _itemLevelText.text = string.Empty;

            if (_sellButton != null)
                _sellButton.interactable = false;

            gameObject.SetActive(false);
        }

        private async void OnSellButtonClicked()
        {
            if (_boardSystem == null)
            {
                DebugTool.Warning("BoardSystem이 연결되지 않아 판매할 수 없습니다.", DebugType.Board, this);
                return;
            }

            if (_sellButton != null)
                _sellButton.interactable = false;

            bool result = await _boardSystem.SellSelectedItemAsync();

            if (!result && _sellButton != null)
                _sellButton.interactable = true;
        }
    }
}
