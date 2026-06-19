using System;
using Core.Managers;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class PlayerResourceDisplay : MonoBehaviour
    {
        [SerializeField] private ResourcesSO _resourcesSO;

        [Header("Energy")]
        [SerializeField] private TMP_Text _energyText;
        [SerializeField] private Image _energyIcon;
        [SerializeField] private string _energyIconKey = "Main_Icon_Energy";

        [Header("Coin")]
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private Image _coinIcon;
        [SerializeField] private string _coinIconKey = "Main_Icon_Coin";

        [Header("Jewel")]
        [SerializeField] private TMP_Text _jewelText;
        [SerializeField] private Image _jewelIcon;
        [SerializeField] private string _jewelIconKey = "Main_Icon_Jewel";

        [SerializeField] private bool _autoFindReferences = true;
        [SerializeField] private bool _refreshOnEnable = true;
        [SerializeField] private bool _loadIconsOnEnable = true;
        [SerializeField] private bool _showLabels;

        private UISpriteController _energyIconController;
        private UISpriteController _coinIconController;
        private UISpriteController _jewelIconController;

        private void Awake()
        {
            if (_autoFindReferences)
                ResolveReferencesFrom(transform);

            BindResourceSO();
            LoadIcons();
        }

        private void OnEnable()
        {
            PlayerResourceManager.Instance.ResourcesChanged -= RefreshDisplay;
            PlayerResourceManager.Instance.ResourcesChanged += RefreshDisplay;

            BindResourceSO();
            LoadIcons();
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
            ResolveReferencesFrom(root);
        }

        public void ResolveReferencesFrom(Transform root)
        {
            if (root == null)
                return;

            ResolveSlot(root, "EnergyBox", ref _energyText, ref _energyIcon, "EnergyText");
            ResolveSlot(root, "CoinBox", ref _coinText, ref _coinIcon, "CoinText");
            ResolveSlot(root, "JewelBox", ref _jewelText, ref _jewelIcon, "JewelText", "GemText");
            ResolveSlot(root, "GemBox", ref _jewelText, ref _jewelIcon, "JewelText", "GemText");

            _energyText ??= FindText(root, "EnergyText");
            _coinText ??= FindText(root, "CoinText");
            _jewelText ??= FindText(root, "JewelText", "GemText");

            LoadIcons();
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

        public void SetReferences(
            TMP_Text energyText,
            Image energyIcon,
            TMP_Text coinText,
            Image coinIcon,
            TMP_Text jewelText,
            Image jewelIcon,
            bool showLabels = false)
        {
            _energyText = energyText;
            _energyIcon = energyIcon;
            _coinText = coinText;
            _coinIcon = coinIcon;
            _jewelText = jewelText;
            _jewelIcon = jewelIcon;
            _showLabels = showLabels;
            LoadIcons();
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            PlayerResourceManager resourceManager = PlayerResourceManager.Instance;

            SetText(_energyText, "Energy", resourceManager.Energy);
            SetText(_coinText, "Coin", resourceManager.Coin);
            SetText(_jewelText, "Jewel", resourceManager.Jewel);
        }

        private void BindResourceSO()
        {
            if (_resourcesSO != null)
                PlayerResourceManager.Instance.Bind(_resourcesSO);
        }

        private void LoadIcons()
        {
            if (!_loadIconsOnEnable)
                return;

            LoadIcon(ref _energyIconController, _energyIcon, _energyIconKey);
            LoadIcon(ref _coinIconController, _coinIcon, _coinIconKey);
            LoadIcon(ref _jewelIconController, _jewelIcon, _jewelIconKey);
        }

        private static void LoadIcon(ref UISpriteController controller, Image image, string key)
        {
            if (image == null || string.IsNullOrWhiteSpace(key))
                return;

            controller ??= new UISpriteController(image);
            controller.ChangeSprite(key);
        }

        private void SetText(TMP_Text text, string label, int amount)
        {
            if (text == null)
                return;

            text.text = _showLabels ? $"{label} {amount}" : amount.ToString();
        }

        private static void ResolveSlot(
            Transform root,
            string boxName,
            ref TMP_Text text,
            ref Image icon,
            params string[] fallbackTextNames)
        {
            Transform box = FindChild(root, boxName);

            if (box == null)
                return;

            text ??= FindText(box, BuildTextNames(fallbackTextNames));
            icon ??= FindImage(box, "Icon", "ResourceIcon");
        }

        private static string[] BuildTextNames(string[] fallbackTextNames)
        {
            if (fallbackTextNames == null || fallbackTextNames.Length == 0)
                return new[] { "ResourceText" };

            string[] names = new string[fallbackTextNames.Length + 1];
            names[0] = "ResourceText";

            for (int i = 0; i < fallbackTextNames.Length; i++)
                names[i + 1] = fallbackTextNames[i];

            return names;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child != null && string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
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

        private static Image FindImage(Transform root, params string[] names)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image == null)
                    continue;

                foreach (string name in names)
                {
                    if (string.Equals(image.name, name, StringComparison.OrdinalIgnoreCase))
                        return image;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            _energyIconController?.Dispose();
            _coinIconController?.Dispose();
            _jewelIconController?.Dispose();
        }
    }
}
