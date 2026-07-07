using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumItemInfoPanel -> 선택한 아이템 Lv / 이름 표시
    public sealed class NyangQuariumItemInfoPanel : MonoBehaviour
    {
        private TMP_Text _levelText;
        private TMP_Text _itemText;
        private Button _sellButton;
        private Func<Task<bool>> _sellHandler;
        private bool _isSelling;

        private void Awake()
        {
            BindTexts();
            BindSellButton();
            Hide();
        }

        private void OnDestroy()
        {
            if (_sellButton != null)
                _sellButton.onClick.RemoveListener(OnSellButtonClicked);
        }

        public void Init(Func<Task<bool>> sellHandler)
        {
            _sellHandler = sellHandler;
            BindSellButton();
        }

        public void Show(NyangQuariumBoardItem item)
        {
            BindTexts();
            BindSellButton();
            _isSelling = false;

            if (item == null || !item.HasItem)
            {
                Hide();
                return;
            }

            if (_levelText != null)
                _levelText.text = $"Lv. {item.Level}";

            if (_itemText != null)
                _itemText.text = item.Name;

            if (_sellButton != null)
                _sellButton.interactable = true;
        }

        public void Hide()
        {
            BindTexts();
            BindSellButton();
            _isSelling = false;

            if (_levelText != null)
                _levelText.text = string.Empty;

            if (_itemText != null)
                _itemText.text = string.Empty;

            if (_sellButton != null)
                _sellButton.interactable = false;
        }

        // ItemInfo 자식 TMP 찾기
        private void BindTexts()
        {
            if (_levelText == null)
                _levelText = FindText("Item Level Text", "LevelText");

            if (_itemText == null)
                _itemText = FindText("Item Name Text", "ItemText");
        }

        private void BindSellButton()
        {
            if (_sellButton != null)
                return;

            _sellButton = FindButton("Item Sell Button");

            if (_sellButton != null)
            {
                _sellButton.onClick.RemoveListener(OnSellButtonClicked);
                _sellButton.onClick.AddListener(OnSellButtonClicked);
            }
        }

        private TMP_Text FindText(params string[] childNames)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                for (int nameIndex = 0; nameIndex < childNames.Length; nameIndex++)
                {
                    if (text.name == childNames[nameIndex])
                        return text;
                }
            }

            return null;
        }

        private Button FindButton(string childName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && button.name == childName)
                    return button;
            }

            return null;
        }

        private async void OnSellButtonClicked()
        {
            if (_isSelling || _sellHandler == null)
                return;

            _isSelling = true;

            if (_sellButton != null)
                _sellButton.interactable = false;

            bool result = false;

            try
            {
                result = await _sellHandler.Invoke();
            }
            finally
            {
                _isSelling = false;
            }

            if (!result && _sellButton != null)
                _sellButton.interactable = true;
        }
    }
}
