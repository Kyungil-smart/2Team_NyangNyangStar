using System;
using Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class PlayerResourceDisplay : MonoBehaviour
    {
        [SerializeField] private ResourcesSO _resourcesSO;
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _jewelText;
        [SerializeField] private TMP_Text _energyText;
        [SerializeField] private bool _autoFindTexts = true;
        [SerializeField] private bool _refreshOnEnable = true;
        [SerializeField] private bool _showLabels;

        private void Awake()
        {
            if (_autoFindTexts)
                ResolveTextsFrom(transform);

            BindResourceSO();
        }

        private void OnEnable()
        {
            PlayerResourceManager.Instance.ResourcesChanged -= RefreshDisplay;
            PlayerResourceManager.Instance.ResourcesChanged += RefreshDisplay;

            BindResourceSO();
            RefreshDisplay();

            if (_refreshOnEnable)
                _ = PlayerResourceManager.Instance.RefreshAsync();
        }

        private void OnDisable()
        {
            PlayerResourceManager.Instance.ResourcesChanged -= RefreshDisplay;
        }

        public void ResolveTextsFrom(Transform root)
        {
            if (root == null)
                return;

            _coinText ??= FindText(root, "CoinText");
            _jewelText ??= FindText(root, "JewelText", "GemText");
            _energyText ??= FindText(root, "EnergyText");

            RefreshDisplay();
        }

        public void SetTexts(TMP_Text coinText, TMP_Text jewelText, TMP_Text energyText, bool showLabels = false)
        {
            _coinText = coinText;
            _jewelText = jewelText;
            _energyText = energyText;
            _showLabels = showLabels;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            PlayerResourceManager resourceManager = PlayerResourceManager.Instance;

            SetText(_coinText, "Coin", resourceManager.Coin);
            SetText(_jewelText, "Gem", resourceManager.Jewel);
            SetText(_energyText, "Energy", resourceManager.Energy);
        }

        public static PlayerResourceDisplay CreateGeneratedHud(Transform parent)
        {
            if (parent == null)
                return null;

            GameObject hud = new GameObject(
                "ResourceHud",
                typeof(RectTransform),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(PlayerResourceDisplay));

            hud.layer = parent.gameObject.layer;
            hud.transform.SetParent(parent, false);

            RectTransform rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -64f);
            rect.sizeDelta = new Vector2(620f, 64f);

            Image background = hud.GetComponent<Image>();
            background.color = new Color(1f, 0.96f, 0.88f, 0.9f);
            background.raycastTarget = false;

            HorizontalLayoutGroup layout = hud.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            PlayerResourceDisplay display = hud.GetComponent<PlayerResourceDisplay>();
            TMP_Text coinText = CreateText(hud.transform, "CoinText", "Coin 0", parent.gameObject.layer);
            TMP_Text jewelText = CreateText(hud.transform, "GemText", "Gem 0", parent.gameObject.layer);
            TMP_Text energyText = CreateText(hud.transform, "EnergyText", "Energy 0", parent.gameObject.layer);

            display._autoFindTexts = false;
            display._showLabels = true;
            display.SetTexts(coinText, jewelText, energyText, true);
            return display;
        }

        private void BindResourceSO()
        {
            if (_resourcesSO != null)
                PlayerResourceManager.Instance.Bind(_resourcesSO);
        }

        private void SetText(TMP_Text text, string label, int amount)
        {
            if (text == null)
                return;

            text.text = _showLabels ? $"{label} {amount}" : amount.ToString();
        }

        private static TMP_Text FindText(Transform root, params string[] names)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                foreach (string name in names)
                {
                    if (string.Equals(text.name, name, StringComparison.OrdinalIgnoreCase))
                        return text;
                }
            }

            return null;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, int layer)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.layer = layer;
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(184f, 52f);

            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 184f;
            layoutElement.preferredHeight = 52f;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = 28f;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }
    }
}
