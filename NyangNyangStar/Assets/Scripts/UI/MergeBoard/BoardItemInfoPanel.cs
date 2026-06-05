using Data.ScriptableObjects.MergeBoard;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class BoardItemInfoPanel : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private TMP_Text _itemNameText;
        [SerializeField] private TMP_Text _itemLevelText;

        [Header("버튼")]
        [SerializeField] private Button _sellButton;

        private BoardSystem _boardSystem;
        private Func<Task<bool>> _sellHandler;
        private ItemData _currentItemData = ItemData.Empty;
        private bool _isSelling;

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
            Show(itemData, null);
        }

        public void Show(ItemData itemData, Func<Task<bool>> sellHandler)
        {
            _currentItemData = itemData?.Clone() ?? ItemData.Empty;
            _sellHandler = sellHandler;
            _isSelling = false;

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
            _sellHandler = null;
            _isSelling = false;

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
            if (_isSelling)
                return;

            if (!_currentItemData.HasItem)
                return;

            _isSelling = true;

            if (_sellButton != null)
                _sellButton.interactable = false;

            bool result = false;

            try
            {
                if (_sellHandler != null)
                {
                    result = await _sellHandler.Invoke();
                }
                else if (_boardSystem != null)
                {
                    result = await _boardSystem.SellSelectedItemAsync();
                }
                else
                {
                    DebugTool.Warning("판매 처리 대상이 연결되지 않았습니다.", DebugType.Board, this);
                }
            }
            finally
            {
                _isSelling = false;
            }

            if (!result && _sellButton != null && gameObject.activeSelf)
                _sellButton.interactable = true;
        }
    }
}
