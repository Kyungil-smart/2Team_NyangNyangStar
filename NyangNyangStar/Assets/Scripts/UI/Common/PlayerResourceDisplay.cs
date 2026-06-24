using System;
using System.Collections.Generic;
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

        [Header("Safe Area")]
        [SerializeField] private bool _applyTopSafeAreaPadding = true;
        [SerializeField] private float _minimumTallMobileTopInsetPixels = 32f;
        [SerializeField] private float _topSafeAreaSpacingPixels = 8f;
        [SerializeField] private float _maximumTopSafeAreaOffsetPixels = 40f;

        private UISpriteController _energyIconController;
        private UISpriteController _coinIconController;
        private UISpriteController _jewelIconController;
        private RectTransform[] _topSafeAreaTargets = Array.Empty<RectTransform>();
        private Vector2[] _topSafeAreaBasePositions = Array.Empty<Vector2>();

        private void Awake()
        {
            if (_autoFindReferences)
                ResolveReferencesFrom(transform);
            else
                ResolveTopSafeAreaTargets(transform);

            BindResourceSO();
            LoadIcons();
            ApplyTopSafeAreaPadding();
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

            ApplyTopSafeAreaPadding();
        }

        private void OnDisable()
        {
            PlayerResourceManager.Instance.ResourcesChanged -= RefreshDisplay;
        }

        private void Update()
        {
            ApplyTopSafeAreaPadding();
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

            ResolveTopSafeAreaTargets(root);
            LoadIcons();
            RefreshDisplay();
            ApplyTopSafeAreaPadding();
        }

        public void SetTexts(TMP_Text coinText, TMP_Text jewelText, TMP_Text energyText, bool showLabels = false)
        {
            _coinText = coinText;
            _jewelText = jewelText;
            _energyText = energyText;
            _showLabels = showLabels;
            ResolveTopSafeAreaTargets(transform);
            RefreshDisplay();
            ApplyTopSafeAreaPadding();
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
            ResolveTopSafeAreaTargets(transform);
            LoadIcons();
            RefreshDisplay();
            ApplyTopSafeAreaPadding();
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

        private void ResolveTopSafeAreaTargets(Transform root)
        {
            if (!_applyTopSafeAreaPadding || root == null)
                return;

            ResetTopSafeAreaPadding();

            List<RectTransform> targets = new();
            RectTransform resourceGroup = FindTopAnchoredRect(root, "Resources");

            if (resourceGroup != null)
            {
                targets.Add(resourceGroup);
            }
            else
            {
                AddTopAnchoredTarget(targets, FindChild(root, "EnergyBox"));
                AddTopAnchoredTarget(targets, FindChild(root, "CoinBox"));
                AddTopAnchoredTarget(targets, FindChild(root, "JewelBox"));
                AddTopAnchoredTarget(targets, FindChild(root, "GemBox"));
                AddTopAnchoredTarget(targets, FindChild(root, "Energy"));
                AddTopAnchoredTarget(targets, FindChild(root, "Coin"));
                AddTopAnchoredTarget(targets, FindChild(root, "Jewel"));
                AddTopAnchoredTarget(targets, FindChild(root, "Gem"));
            }

            if (targets.Count == 0)
                AddTopAnchoredTarget(targets, transform);

            _topSafeAreaTargets = targets.ToArray();
            _topSafeAreaBasePositions = new Vector2[_topSafeAreaTargets.Length];

            for (int i = 0; i < _topSafeAreaTargets.Length; i++)
                _topSafeAreaBasePositions[i] = _topSafeAreaTargets[i].anchoredPosition;
        }

        private void ApplyTopSafeAreaPadding()
        {
            if (!_applyTopSafeAreaPadding || _topSafeAreaTargets == null || _topSafeAreaTargets.Length == 0)
                return;

            float topInsetPixels = GetTopInsetPixels();
            float spacingPixels = topInsetPixels > 0f ? _topSafeAreaSpacingPixels : 0f;
            float offsetPixels = Mathf.Min(topInsetPixels + spacingPixels, _maximumTopSafeAreaOffsetPixels);

            for (int i = 0; i < _topSafeAreaTargets.Length; i++)
            {
                RectTransform target = _topSafeAreaTargets[i];

                if (target == null || i >= _topSafeAreaBasePositions.Length)
                    continue;

                Vector2 basePosition = _topSafeAreaBasePositions[i];
                float scaleFactor = GetCanvasScaleFactor(target);
                float offset = offsetPixels / scaleFactor;
                float direction = basePosition.y <= 0f ? -1f : 1f;
                target.anchoredPosition = new Vector2(basePosition.x, basePosition.y + direction * offset);
            }
        }

        private void ResetTopSafeAreaPadding()
        {
            if (_topSafeAreaTargets == null || _topSafeAreaBasePositions == null)
                return;

            for (int i = 0; i < _topSafeAreaTargets.Length; i++)
            {
                if (_topSafeAreaTargets[i] == null || i >= _topSafeAreaBasePositions.Length)
                    continue;

                _topSafeAreaTargets[i].anchoredPosition = _topSafeAreaBasePositions[i];
            }
        }

        private float GetTopInsetPixels()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return 0f;

            Rect safeArea = Screen.safeArea;
            float topInset = Mathf.Max(0f, Screen.height - safeArea.yMax);

            if (Application.isMobilePlatform && IsTallScreen())
                topInset = Mathf.Max(topInset, _minimumTallMobileTopInsetPixels);

            return topInset;
        }

        private static bool IsTallScreen()
        {
            float shortSide = Mathf.Min(Screen.width, Screen.height);

            if (shortSide <= 0f)
                return false;

            float longSide = Mathf.Max(Screen.width, Screen.height);
            return longSide / shortSide > 1.9f;
        }

        private static float GetCanvasScaleFactor(RectTransform target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();

            if (canvas == null || canvas.scaleFactor <= 0f)
                return 1f;

            return canvas.scaleFactor;
        }

        private static RectTransform FindTopAnchoredRect(Transform root, string name)
        {
            Transform child = FindChild(root, name);

            if (child == null)
                return null;

            RectTransform rectTransform = child.GetComponent<RectTransform>();
            return IsTopAnchored(rectTransform) ? rectTransform : null;
        }

        private static void AddTopAnchoredTarget(List<RectTransform> targets, Transform target)
        {
            if (target == null)
                return;

            RectTransform rectTransform = target.GetComponent<RectTransform>();

            if (!IsTopAnchored(rectTransform) || targets.Contains(rectTransform))
                return;

            targets.Add(rectTransform);
        }

        private static bool IsTopAnchored(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return false;

            return rectTransform.anchorMin.y >= 0.95f && rectTransform.anchorMax.y >= 0.95f;
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
