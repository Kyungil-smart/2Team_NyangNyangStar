using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium
{
    public sealed class NyangQuariumLevelBubbleUI : MonoBehaviour
    {
        private const string DefaultBubbleSpriteKey = "NQ_Icon_TankLevel";

        [Header("Addressables")]
        [SerializeField] private string _bubbleSpriteKey = DefaultBubbleSpriteKey;
        [SerializeField] private string _turtleSpriteKey;

        [Header("View")]
        [SerializeField] private Image _bubbleImage;
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _turtleImage;

        [Header("State")]
        [SerializeField] private int _level = 1;
        [SerializeField, Range(0f, 1f)] private float _expRatio;
        [SerializeField] private Color _fillColor = new(0.67f, 0.27f, 1f, 0.72f);
        [SerializeField] private string _levelFormat = "Lv.{0}";

        private global::UI.UISpriteController _bubbleSprite;
        private global::UI.UISpriteController _fillSprite;
        private global::UI.UISpriteController _turtleSprite;
        private string _boundBubbleSpriteKey;
        private string _boundTurtleSpriteKey;

        private void Awake()
        {
            EnsureView();
            RefreshView();
        }

        private void OnValidate()
        {
            _level = Mathf.Max(1, _level);
            _expRatio = Mathf.Clamp01(_expRatio);

            ConfigureBubbleImage(_bubbleImage);
            ConfigureFillImage(_fillImage);
            ConfigureLevelText(_levelText);
            ConfigureTurtleImage(_turtleImage);
            RefreshView();
        }

        private void OnEnable()
        {
            EnsureView();
            BindAddressableSprites();
            RefreshView();
        }

        private void OnDestroy()
        {
            DisposeSpriteControllers();
        }

        public void Initialize(string bubbleSpriteKey, string turtleSpriteKey, int level, float expRatio)
        {
            if (!string.IsNullOrWhiteSpace(bubbleSpriteKey))
                _bubbleSpriteKey = bubbleSpriteKey;

            _turtleSpriteKey = turtleSpriteKey ?? string.Empty;

            EnsureView();
            BindAddressableSprites();
            SetLevelProgress(level, expRatio);
        }

        public void SetLevelProgress(int level, float expRatio)
        {
            _level = Mathf.Max(1, level);
            _expRatio = Mathf.Clamp01(expRatio);
            RefreshView();
        }

        public void SetExperience(int level, int currentExp, int maxExp)
        {
            float ratio = maxExp <= 0 ? 0f : (float)Mathf.Clamp(currentExp, 0, maxExp) / maxExp;
            SetLevelProgress(level, ratio);
        }

        public void SetTurtleSpriteKey(string turtleSpriteKey)
        {
            _turtleSpriteKey = turtleSpriteKey ?? string.Empty;
            BindAddressableSprites();
            RefreshView();
        }

        private void EnsureView()
        {
            _bubbleImage ??= FindImage("BubbleBase");
            _fillImage ??= FindImage("BubbleFill");
            _levelText ??= GetComponentInChildren<TMP_Text>(true);
            _turtleImage ??= FindImage("TurtleImage");

            if (_bubbleImage == null)
                _bubbleImage = CreateImage("BubbleBase", transform, Vector2.zero, Vector2.one);

            if (_fillImage == null)
                _fillImage = CreateImage("BubbleFill", transform, Vector2.zero, Vector2.one);

            if (_levelText == null)
                _levelText = CreateLevelText(transform);

            if (_turtleImage == null)
                _turtleImage = CreateTurtleImage(transform);

            ConfigureBubbleImage(_bubbleImage);
            ConfigureFillImage(_fillImage);
            ConfigureLevelText(_levelText);
            ConfigureTurtleImage(_turtleImage);
        }

        private void BindAddressableSprites()
        {
            if (!string.Equals(_boundBubbleSpriteKey, _bubbleSpriteKey))
            {
                _bubbleSprite?.Dispose();
                _fillSprite?.Dispose();
                _bubbleSprite = null;
                _fillSprite = null;
                _boundBubbleSpriteKey = _bubbleSpriteKey;

                if (!string.IsNullOrWhiteSpace(_bubbleSpriteKey))
                {
                    KeyContainer.Sprites.Add(_bubbleSpriteKey);
                    _bubbleSprite = new global::UI.UISpriteController(_bubbleImage);
                    _fillSprite = new global::UI.UISpriteController(_fillImage);
                    _bubbleSprite.ChangeSprite(_bubbleSpriteKey);
                    _fillSprite.ChangeSprite(_bubbleSpriteKey);
                }
            }

            if (!string.Equals(_boundTurtleSpriteKey, _turtleSpriteKey))
            {
                _turtleSprite?.Dispose();
                _turtleSprite = null;
                _boundTurtleSpriteKey = _turtleSpriteKey;

                if (!string.IsNullOrWhiteSpace(_turtleSpriteKey))
                {
                    KeyContainer.Sprites.Add(_turtleSpriteKey);
                    _turtleSprite = new global::UI.UISpriteController(_turtleImage);
                    _turtleSprite.ChangeSprite(_turtleSpriteKey);
                }
            }
        }

        private void RefreshView()
        {
            if (_fillImage != null)
                _fillImage.fillAmount = _expRatio;

            if (_levelText != null)
                _levelText.text = string.Format(_levelFormat, _level);

            if (_turtleImage != null)
                _turtleImage.gameObject.SetActive(!string.IsNullOrWhiteSpace(_turtleSpriteKey));
        }

        private void DisposeSpriteControllers()
        {
            _bubbleSprite?.Dispose();
            _fillSprite?.Dispose();
            _turtleSprite?.Dispose();
            _bubbleSprite = null;
            _fillSprite = null;
            _turtleSprite = null;
        }

        private Image FindImage(string objectName)
        {
            Image[] images = GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image != null && image.name == objectName)
                    return image;
            }

            return null;
        }

        private static Image CreateImage(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rectTransform = imageObject.transform as RectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateLevelText(Transform parent)
        {
            GameObject textObject = new("LevelText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.transform as RectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(8f, 8f);
            rectTransform.offsetMax = new Vector2(-8f, -8f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            return textObject.GetComponent<TMP_Text>();
        }

        private static Image CreateTurtleImage(Transform parent)
        {
            GameObject imageObject = new("TurtleImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureBubbleImage(Image image)
        {
            if (image == null)
                return;

            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private void ConfigureFillImage(Image image)
        {
            if (image == null)
                return;

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Bottom;
            image.preserveAspect = true;
            image.color = _fillColor;
            image.raycastTarget = false;
        }

        private static void ConfigureLevelText(TMP_Text text)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 30f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private static void ConfigureTurtleImage(Image image)
        {
            if (image == null)
                return;

            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }
}
