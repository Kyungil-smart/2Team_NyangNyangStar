using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.MergeBoard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    public sealed class NyangQuariumBoardItem
    {
        public static readonly NyangQuariumBoardItem Empty = new(ItemData.Empty);

        public ItemData ItemData { get; }
        public int Id => ItemData?.ItemID ?? 0;
        public string Name => ItemData?.ItemName ?? string.Empty;
        public int Level => ItemData?.ItemLevel ?? 0;
        public bool HasItem => ItemData != null && ItemData.HasItem;

        public NyangQuariumBoardItem(ItemData itemData)
        {
            ItemData = itemData?.Clone() ?? ItemData.Empty;
        }
    }

    public sealed class NyangQuariumMergeBoardBootstrap : MonoBehaviour
    {
        private const string RootName = "NyangQuariumMergeBoard";
        private const string GeneratorName = "Item Generator";
        private const string BoardName = "Item Board";
        private const string InfoName = "ItemInfo";
        private static NyangQuariumMergeBoardBootstrap _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureRunner();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRunner();
        }

        private static void EnsureRunner()
        {
            if (_instance != null)
                return;

            GameObject runner = new("@NyangQuariumMergeBoardBootstrap");
            DontDestroyOnLoad(runner);
            _instance = runner.AddComponent<NyangQuariumMergeBoardBootstrap>();
        }

        private void OnEnable()
        {
            StartCoroutine(WatchMergeBoard());
        }

        private IEnumerator WatchMergeBoard()
        {
            while (true)
            {
                BootstrapCurrentScene();
                yield return null;
            }
        }

        private static void BootstrapCurrentScene()
        {
            GameObject rootObject = FindActiveGameObject(RootName);
            if (rootObject == null)
                return;

            GameObject generatorObject = FindChildGameObject(rootObject.transform, GeneratorName);
            GameObject boardObject = FindChildGameObject(rootObject.transform, BoardName);
            GameObject infoObject = FindChildGameObject(rootObject.transform, InfoName);

            if (generatorObject == null || boardObject == null || infoObject == null)
                return;

            NyangQuariumItemInfoPanel infoPanel = infoObject.GetComponent<NyangQuariumItemInfoPanel>();
            if (infoPanel == null)
                infoPanel = infoObject.AddComponent<NyangQuariumItemInfoPanel>();

            NyangQuariumItemBoard board = boardObject.GetComponent<NyangQuariumItemBoard>();
            if (board == null)
                board = boardObject.AddComponent<NyangQuariumItemBoard>();

            board.Init(infoPanel);

            NyangQuariumItemGenerator generator = generatorObject.GetComponent<NyangQuariumItemGenerator>();
            if (generator == null)
                generator = generatorObject.AddComponent<NyangQuariumItemGenerator>();

            generator.Init(board);
        }

        private static GameObject FindChildGameObject(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.gameObject.activeInHierarchy && child.name == objectName)
                    return child.gameObject;
            }

            return null;
        }

        private static GameObject FindActiveGameObject(string objectName)
        {
            GameObject[] objects = FindObjectsOfType<GameObject>();

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject target = objects[i];
                if (target != null && target.name == objectName)
                    return target;
            }

            return null;
        }
    }

    public sealed class NyangQuariumItemGenerator : MonoBehaviour
    {
        private NyangQuariumItemBoard _board;
        private Button _button;
        private int _fallbackItemId = 1;
        private readonly string[] _fallbackItemNames =
        {
            "Random Item A",
            "Random Item B",
            "Random Item C",
            "Random Item D",
            "Random Item E"
        };

        public void Init(NyangQuariumItemBoard board)
        {
            _board = board;
            BindButton();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(GenerateItem);
        }

        private void BindButton()
        {
            Button button = GetComponent<Button>();
            if (button == null)
                button = GetComponentInChildren<Button>(true);

            if (_button == button)
                return;

            if (_button != null)
                _button.onClick.RemoveListener(GenerateItem);

            _button = button;

            if (_button != null)
                _button.onClick.AddListener(GenerateItem);
            else
                DebugTool.Warning("[NyangQuariumItemGenerator] Button 컴포넌트를 찾지 못했습니다.", DebugType.UI, this);
        }

        public void GenerateItem()
        {
            if (_board == null)
                return;

            NyangQuariumBoardItem item = CreateRandomItem();
            _board.TryAddItem(item);
        }

        private NyangQuariumBoardItem CreateRandomItem()
        {
            NyangQuariumFishSO fishSO = null;
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumFishSO(out NyangQuariumFishSO loadedFishSO))
                fishSO = loadedFishSO;

            if (fishSO != null && fishSO.FishData != null && fishSO.FishData.Count > 0)
            {
                NyangQuariumFishData fishData = fishSO.FishData[Random.Range(0, fishSO.FishData.Count)];
                if (fishData != null)
                {
                    ItemData itemData = new(
                        fishData.FishId,
                        fishData.FishName,
                        Mathf.Max(1, fishData.Level),
                        ItemType.Common,
                        fishData.FishKey);

                    return new NyangQuariumBoardItem(itemData);
                }
            }

            int fallbackIndex = Random.Range(0, _fallbackItemNames.Length);
            int itemId = _fallbackItemId++;
            ItemData fallbackItem = new(
                itemId,
                _fallbackItemNames[fallbackIndex],
                fallbackIndex + 1,
                ItemType.Common);

            DebugTool.Warning("[NyangQuariumItemGenerator] NyangQuariumFishSO가 준비되지 않아 임시 랜덤 아이템을 생성했습니다.", DebugType.UI, this);
            return new NyangQuariumBoardItem(fallbackItem);
        }
    }

    public sealed class NyangQuariumItemBoard : MonoBehaviour
    {
        private const int Width = 7;
        private const int Height = 9;
        private const int SlotSize = 135;
        private const int SlotSpacing = 9;
        private const int ItemSize = 85;

        private readonly List<NyangQuariumItemSlot> _slots = new();
        private NyangQuariumItemInfoPanel _infoPanel;
        private NyangQuariumItemSlot _selectedSlot;
        private Transform _slotRoot;
        private bool _initialized;

        public void Init(NyangQuariumItemInfoPanel infoPanel)
        {
            _infoPanel = infoPanel;

            if (_initialized)
                return;

            DisableMergeBoardSystem();
            SetupSlotRoot();
            GenerateSlots();
            ClearSelection();
            _initialized = true;
        }

        public bool TryAddItem(NyangQuariumBoardItem item)
        {
            if (item == null || !item.HasItem)
                return false;

            for (int i = 0; i < _slots.Count; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];
                if (slot == null || slot.HasItem)
                    continue;

                slot.SetItem(item);
                return true;
            }

            DebugTool.Warning("[NyangQuariumItemBoard] 빈 슬롯이 없습니다.", DebugType.UI, this);
            return false;
        }

        public void SelectSlot(NyangQuariumItemSlot slot)
        {
            if (slot == null || !slot.HasItem)
            {
                ClearSelection();
                return;
            }

            if (_selectedSlot != null)
                _selectedSlot.SetSelected(false);

            _selectedSlot = slot;
            _selectedSlot.SetSelected(true);

            if (_infoPanel != null)
                _infoPanel.Show(slot.Item);
        }

        private void ClearSelection()
        {
            if (_selectedSlot != null)
                _selectedSlot.SetSelected(false);

            _selectedSlot = null;

            if (_infoPanel != null)
                _infoPanel.Hide();
        }

        private void DisableMergeBoardSystem()
        {
            BoardSystem mergeBoardSystem = GetComponent<BoardSystem>();
            if (mergeBoardSystem != null)
                mergeBoardSystem.enabled = false;
        }

        private void SetupSlotRoot()
        {
            Transform existingRoot = transform.Find("@Slot Root");
            if (existingRoot != null)
            {
                _slotRoot = existingRoot;
            }
            else
            {
                GameObject slotRootObject = new("@Slot Root", typeof(RectTransform));
                slotRootObject.transform.SetParent(transform, false);
                _slotRoot = slotRootObject.transform;
            }

            RectTransform slotRootRect = _slotRoot as RectTransform;
            if (slotRootRect != null)
            {
                slotRootRect.anchorMin = Vector2.zero;
                slotRootRect.anchorMax = Vector2.one;
                slotRootRect.offsetMin = Vector2.zero;
                slotRootRect.offsetMax = Vector2.zero;
                slotRootRect.pivot = new Vector2(0.5f, 0.5f);
            }

            GridLayoutGroup grid = _slotRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = _slotRoot.gameObject.AddComponent<GridLayoutGroup>();

            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Width;
            grid.cellSize = new Vector2(SlotSize, SlotSize);
            grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
        }

        private void GenerateSlots()
        {
            for (int i = _slotRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _slotRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            _slots.Clear();

            int slotCount = Width * Height;
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObject = new($"Slot_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                slotObject.transform.SetParent(_slotRoot, false);
                slotObject.layer = gameObject.layer;

                Image backgroundImage = slotObject.GetComponent<Image>();
                backgroundImage.color = new Color(1f, 1f, 1f, 0.25f);
                backgroundImage.raycastTarget = true;

                GameObject itemImageObject = new("Item Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                itemImageObject.transform.SetParent(slotObject.transform, false);
                itemImageObject.layer = gameObject.layer;

                RectTransform itemRect = itemImageObject.transform as RectTransform;
                itemRect.anchorMin = new Vector2(0.5f, 0.5f);
                itemRect.anchorMax = new Vector2(0.5f, 0.5f);
                itemRect.sizeDelta = new Vector2(ItemSize, ItemSize);
                itemRect.anchoredPosition = Vector2.zero;

                Image itemImage = itemImageObject.GetComponent<Image>();
                itemImage.color = Color.white;
                itemImage.preserveAspect = true;
                itemImage.raycastTarget = false;

                NyangQuariumItemSlot slot = slotObject.AddComponent<NyangQuariumItemSlot>();
                slot.Init(this, backgroundImage, itemImage);
                _slots.Add(slot);
            }
        }
    }

    public sealed class NyangQuariumItemSlot : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color BaseColor = new(1f, 1f, 1f, 0.25f);
        private static readonly Color SelectedColor = new(1f, 0.9f, 0.6f, 0.65f);

        private NyangQuariumItemBoard _board;
        private Image _backgroundImage;
        private Image _itemImage;
        private UI.UISpriteController _spriteController;

        public NyangQuariumBoardItem Item { get; private set; } = NyangQuariumBoardItem.Empty;
        public bool HasItem => Item != null && Item.HasItem;

        public void Init(NyangQuariumItemBoard board, Image backgroundImage, Image itemImage)
        {
            _board = board;
            _backgroundImage = backgroundImage;
            _itemImage = itemImage;

            if (_itemImage != null)
                _spriteController = new UI.UISpriteController(_itemImage);

            SetItem(NyangQuariumBoardItem.Empty);
            SetSelected(false);
        }

        public void SetItem(NyangQuariumBoardItem item)
        {
            Item = item ?? NyangQuariumBoardItem.Empty;

            if (_itemImage == null)
                return;

            _spriteController?.ClearSprite();

            _itemImage.sprite = null;
            _itemImage.enabled = HasItem;
            _itemImage.gameObject.SetActive(HasItem);
            _itemImage.color = Color.white;

            if (!HasItem)
                return;

            ItemData itemData = Item.ItemData;

            if (itemData.ItemSprite != null)
            {
                _itemImage.sprite = itemData.ItemSprite;
                return;
            }

            if (!string.IsNullOrWhiteSpace(itemData.AddressableKey))
            {
                _spriteController ??= new UI.UISpriteController(_itemImage);
                _spriteController.ChangeSprite(itemData.AddressableKey);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_backgroundImage == null)
                return;

            _backgroundImage.color = selected ? SelectedColor : BaseColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_board != null)
                _board.SelectSlot(this);
        }

        private void OnDestroy()
        {
            _spriteController?.Dispose();
            _spriteController = null;
        }
    }

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
