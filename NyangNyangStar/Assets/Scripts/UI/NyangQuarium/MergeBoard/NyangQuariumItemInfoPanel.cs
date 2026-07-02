using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
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

        private void Awake()
        {
            BindTexts();
            Hide();
        }

        public void Show(NyangQuariumBoardItem item)
        {
            BindTexts();

            if (item == null || !item.HasItem)
            {
                Hide();
                return;
            }

            if (_levelText != null)
                _levelText.text = $"Lv. {item.Level}";

            if (_itemText != null)
                _itemText.text = item.Name;
        }

        public void Hide()
        {
            BindTexts();

            if (_levelText != null)
                _levelText.text = string.Empty;

            if (_itemText != null)
                _itemText.text = string.Empty;
        }

        // LevelText, ItemText 자식 TMP 찾기
        private void BindTexts()
        {
            if (_levelText == null)
                _levelText = FindText("LevelText");

            if (_itemText == null)
                _itemText = FindText("ItemText");
        }

        private TMP_Text FindText(string childName)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == childName)
                    return texts[i];
            }

            return null;
        }
    }
}
