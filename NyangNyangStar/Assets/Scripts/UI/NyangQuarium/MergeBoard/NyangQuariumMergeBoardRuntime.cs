using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumBoardItem -> 보드 슬롯에 올라가는 아이템 래퍼
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

    // NyangQuariumMergeBoardBootstrap -> 씬에 NyangQuariumMergeBoard 프리팹이 뜨면 런타임 컴포넌트 자동 부착
    public sealed class NyangQuariumMergeBoardBootstrap : MonoBehaviour
    {
        private const string RootName = "NyangQuariumMergeBoard";
        private const string GeneratorName = "Item Generator Button";
        private const string BoardName = "Item Board";
        private const string InfoName = "ItemInfo";
        private const string RewardRootName = "Reward Root";
        private const string QuestBoardPanelName = "QuestBoardPanel";
        private static NyangQuariumMergeBoardBootstrap _instance;

        // 게임 시작, 씬 로드 시 부트스트랩 러너 등록
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

        // DontDestroyOnLoad 러너가 없으면 하나 만들어 둠
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

        // 매 프레임 씬에 머지보드가 있는지 감시해서 Init 해줌
        private IEnumerator WatchMergeBoard()
        {
            while (true)
            {
                BootstrapCurrentScene();
                yield return null;
            }
        }

        // 프리팹 자식 이름 기준으로 Board / Generator / Info / Navigation 연결
        private static void BootstrapCurrentScene()
        {
            GameObject rootObject = FindActiveGameObject(RootName);
            if (rootObject == null)
                return;

            GameObject generatorObject = FindChildGameObject(rootObject.transform, GeneratorName);
            GameObject boardObject = FindChildGameObject(rootObject.transform, BoardName);
            GameObject infoObject = FindChildGameObject(rootObject.transform, InfoName);
            GameObject rewardRootObject = FindChildGameObject(rootObject.transform, RewardRootName);

            if (generatorObject == null || boardObject == null || infoObject == null)
                return;

            NyangQuariumItemInfoPanel infoPanel = infoObject.GetComponent<NyangQuariumItemInfoPanel>();
            if (infoPanel == null)
                infoPanel = infoObject.AddComponent<NyangQuariumItemInfoPanel>();

            NyangQuariumItemBoard board = boardObject.GetComponent<NyangQuariumItemBoard>();
            if (board == null)
                board = boardObject.AddComponent<NyangQuariumItemBoard>();

            board.Init(infoPanel);

            NyangQuariumRewardQueue rewardQueue = null;
            if (rewardRootObject != null)
            {
                rewardQueue = rewardRootObject.GetComponent<NyangQuariumRewardQueue>();
                if (rewardQueue == null)
                    rewardQueue = rewardRootObject.AddComponent<NyangQuariumRewardQueue>();

                rewardQueue.Init(board);
            }

            NyangQuariumItemGenerator generator = generatorObject.GetComponent<NyangQuariumItemGenerator>();
            if (generator == null)
                generator = generatorObject.AddComponent<NyangQuariumItemGenerator>();

            generator.Init(board, rewardQueue);

            NyangQuariumMergeBoardNavigation navigation = rootObject.GetComponent<NyangQuariumMergeBoardNavigation>();
            if (navigation == null)
                navigation = rootObject.AddComponent<NyangQuariumMergeBoardNavigation>();

            navigation.Init();

            GameObject questBoardPanelObject = FindChildGameObject(rootObject.transform, QuestBoardPanelName);
            if (questBoardPanelObject != null)
            {
                NyangQuariumMergeQuestBoardUI questBoardUI =
                    questBoardPanelObject.GetComponent<NyangQuariumMergeQuestBoardUI>();

                if (questBoardUI == null)
                    questBoardUI = questBoardPanelObject.AddComponent<NyangQuariumMergeQuestBoardUI>();

                questBoardUI.Init();
            }
        }

        // 활성화된 자식 오브젝트만 이름으로 찾음
        private static GameObject FindChildGameObject(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == objectName)
                    return child.gameObject;
            }

            return null;
        }

        // 씬 전체에서 이름으로 루트 오브젝트 검색
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

    // NyangQuariumItemGenerator -> Item Generator 버튼, 클릭 시 랜덤 또는 InputItemID 지정 아이템 생성
    public sealed class NyangQuariumItemGenerator : MonoBehaviour
    {
        private const string InputItemIdFieldName = "InputItemID";
        private const int GeneratorItemIdOffset = 200000;

        private NyangQuariumItemBoard _board;
        private NyangQuariumRewardQueue _rewardQueue;
        private Button _button;
        private TMP_InputField _itemIdInputField;
        private int _fallbackItemId = 1;
        private readonly string[] _fallbackItemNames =
        {
            "Random Item A",
            "Random Item B",
            "Random Item C",
            "Random Item D",
            "Random Item E"
        };

        public void Init(NyangQuariumItemBoard board, NyangQuariumRewardQueue rewardQueue)
        {
            _board = board;
            _rewardQueue = rewardQueue;
            BindButton();
            BindInputField();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(GenerateItem);
        }

        // 자기 자신 또는 자식에서 Button 찾아서 GenerateItem 연결
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

        private void BindInputField()
        {
            if (_itemIdInputField != null)
                return;

            Transform root = transform;
            while (root.parent != null)
                root = root.parent;

            Transform inputTransform = FindChildTransform(root, InputItemIdFieldName);
            if (inputTransform == null)
            {
                DebugTool.Warning(
                    "[NyangQuariumItemGenerator] InputItemID를 찾지 못했습니다.",
                    DebugType.UI,
                    this);
                return;
            }

            _itemIdInputField = inputTransform.GetComponent<TMP_InputField>();
            if (_itemIdInputField == null)
                DebugTool.Warning(
                    "[NyangQuariumItemGenerator] InputItemID에 TMP_InputField가 없습니다.",
                    DebugType.UI,
                    this);
        }

        public void GenerateItem()
        {
            if (_board == null)
                return;

            BindInputField();

            NyangQuariumBoardItem item = null;
            bool usedInputItemId = false;

            if (TryReadInputItemId(out int itemId, out bool hasInput))
            {
                if (!TryCreateItemById(itemId, out item))
                {
                    DebugTool.Warning(
                        $"[NyangQuariumItemGenerator] {itemId} ID에 해당하는 아이템 데이터를 찾지 못했습니다.",
                        DebugType.UI,
                        this);
                    return;
                }

                usedInputItemId = true;
            }
            else if (hasInput)
                return;
            else
                item = CreateRandomItem();

            if (item == null || !item.HasItem)
                return;

            bool added = false;

            if (_rewardQueue != null)
            {
                _rewardQueue.EnqueueItem(item);
                added = true;
            }
            else
                added = _board.TryAddItem(item);

            if (usedInputItemId && added)
                ClearInputItemIdField();
        }

        private void ClearInputItemIdField()
        {
            if (_itemIdInputField == null)
                return;

            _itemIdInputField.text = string.Empty;
            _itemIdInputField.ReleaseSelection();
        }

        private bool TryReadInputItemId(out int itemId, out bool hasInput)
        {
            itemId = 0;
            hasInput = false;

            if (_itemIdInputField == null)
                return false;

            string input = _itemIdInputField.text?.Trim();
            if (string.IsNullOrEmpty(input))
                return false;

            hasInput = true;

            if (!int.TryParse(input, out itemId) || itemId <= 0)
            {
                DebugTool.Warning(
                    $"[NyangQuariumItemGenerator] 아이템 ID 입력값이 올바르지 않습니다. 입력값: {input}",
                    DebugType.UI,
                    this);
                return false;
            }

            return true;
        }

        private bool TryCreateItemById(int itemId, out NyangQuariumBoardItem item)
        {
            item = null;

            if (itemId <= 0)
                return false;

            TryResolveFishSO(out NyangQuariumFishSO fishSO);
            TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO);

            if (fishSO?.FishData != null)
            {
                for (int i = 0; i < fishSO.FishData.Count; i++)
                {
                    NyangQuariumFishData fishData = fishSO.FishData[i];
                    if (fishData == null || fishData.FishId != itemId)
                        continue;

                    item = CreateFishItem(fishData);
                    return item != null && item.HasItem;
                }
            }

            if (generatorSO != null)
            {
                if (itemId >= GeneratorItemIdOffset &&
                    generatorSO.TryGetById(itemId - GeneratorItemIdOffset, out NyangQuariumGeneratorData offsetGeneratorData))
                {
                    item = CreateGeneratorItem(offsetGeneratorData);
                    return item != null && item.HasItem;
                }

                if (generatorSO.TryGetById(itemId, out NyangQuariumGeneratorData generatorData))
                {
                    item = CreateGeneratorItem(generatorData);
                    return item != null && item.HasItem;
                }
            }

            return false;
        }

        private NyangQuariumBoardItem CreateRandomItem()
        {
            TryResolveFishSO(out NyangQuariumFishSO fishSO);
            TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO);

            List<NyangQuariumBoardItem> candidates = new();

            if (fishSO?.FishData != null && fishSO.FishData.Count > 0)
            {
                for (int i = 0; i < fishSO.FishData.Count; i++)
                {
                    NyangQuariumBoardItem candidate = CreateFishItem(fishSO.FishData[i]);
                    if (candidate != null && candidate.HasItem)
                        candidates.Add(candidate);
                }
            }

            if (generatorSO?.Generators != null && generatorSO.Generators.Count > 0)
            {
                for (int i = 0; i < generatorSO.Generators.Count; i++)
                {
                    NyangQuariumBoardItem candidate = CreateGeneratorItem(generatorSO.Generators[i]);
                    if (candidate != null && candidate.HasItem)
                        candidates.Add(candidate);
                }
            }

            if (candidates.Count > 0)
                return candidates[Random.Range(0, candidates.Count)];

            int fallbackIndex = Random.Range(0, _fallbackItemNames.Length);
            int fallbackId = _fallbackItemId++;
            ItemData fallbackItem = new(
                fallbackId,
                _fallbackItemNames[fallbackIndex],
                fallbackIndex + 1,
                ItemType.Common);

            DebugTool.Warning(
                "[NyangQuariumItemGenerator] NyangQuariumFishSO가 준비되지 않아 임시 랜덤 아이템을 생성했습니다.",
                DebugType.UI,
                this);
            return new NyangQuariumBoardItem(fallbackItem);
        }

        private static NyangQuariumBoardItem CreateFishItem(NyangQuariumFishData fishData)
        {
            if (fishData == null || fishData.FishId <= 0)
                return null;

            ItemData itemData = new(
                fishData.FishId,
                fishData.FishName,
                Mathf.Max(1, fishData.Level),
                ItemType.Common,
                fishData.FishKey);

            if (NyangQuariumFishSpriteCache.TryGetSprite(fishData.FishKey, out Sprite sprite))
                itemData.SetSprite(sprite);

            return new NyangQuariumBoardItem(itemData);
        }

        private static NyangQuariumBoardItem CreateGeneratorItem(NyangQuariumGeneratorData generatorData)
        {
            if (generatorData == null || generatorData.GeneratorId <= 0)
                return null;

            ItemData itemData = new(
                GeneratorItemIdOffset + generatorData.GeneratorId,
                generatorData.GeneratorName,
                Mathf.Max(1, generatorData.Level),
                ItemType.Common);

            return new NyangQuariumBoardItem(itemData);
        }

        private static bool TryResolveFishSO(out NyangQuariumFishSO fishSO)
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumFishSO(out fishSO))
                return fishSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            fishSO = quariumLoader != null ? quariumLoader.FishSO : null;
            return fishSO != null;
        }

        private static bool TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO)
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumGeneratorSO(out generatorSO))
                return generatorSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            generatorSO = quariumLoader != null ? quariumLoader.GeneratorSO : null;
            return generatorSO != null;
        }

        private static Transform FindChildTransform(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == objectName)
                    return child;
            }

            return null;
        }
    }

    // NyangQuariumItemBoard -> 7x9 슬롯 그리드, 아이템 배치·선택 처리
    public sealed class NyangQuariumRewardQueue : MonoBehaviour
    {
        private const int PreviewCount = 3;

        private readonly Queue<NyangQuariumBoardItem> _rewardQueue = new();
        private readonly List<NyangQuariumRewardQueueSlot> _slotViews = new();

        private NyangQuariumItemBoard _board;
        private TMP_Text _countText;
        private bool _initialized;
        private bool _isMoving;

        public void Init(NyangQuariumItemBoard board)
        {
            _board = board;

            if (!_initialized)
            {
                BindViews();
                _initialized = true;
            }

            RefreshView();
        }

        public void EnqueueItem(NyangQuariumBoardItem item)
        {
            if (item == null || !item.HasItem)
                return;

            _rewardQueue.Enqueue(new NyangQuariumBoardItem(item.ItemData));
            RefreshView();
        }

        public void TryMoveTopItemToBoard()
        {
            if (_isMoving || _rewardQueue.Count <= 0)
                return;

            if (_board == null)
            {
                DebugTool.Warning("[NyangQuariumRewardQueue] Board is not ready.", DebugType.UI, this);
                return;
            }

            _isMoving = true;

            try
            {
                NyangQuariumBoardItem item = _rewardQueue.Peek();
                if (!_board.TryAddItem(item))
                    return;

                _rewardQueue.Dequeue();
                RefreshView();
            }
            finally
            {
                _isMoving = false;
            }
        }

        private void BindViews()
        {
            _slotViews.Clear();

            AddSlotView("ItemSlot3");
            AddSlotView("ItemSlot2");
            AddSlotView("ItemSlot1");

            if (_countText == null)
                _countText = FindCountText();
        }

        private void AddSlotView(string slotName)
        {
            Transform slotTransform = FindChild(slotName);
            if (slotTransform == null)
                return;

            NyangQuariumRewardQueueSlot slotView = slotTransform.GetComponent<NyangQuariumRewardQueueSlot>();
            if (slotView == null)
                slotView = slotTransform.gameObject.AddComponent<NyangQuariumRewardQueueSlot>();

            slotView.Init(this, _slotViews.Count == 0);
            _slotViews.Add(slotView);
        }

        private Transform FindChild(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }

        private TMP_Text FindCountText()
        {
            Transform countRoot = FindChild("ItemCount");
            if (countRoot != null)
                return countRoot.GetComponentInChildren<TMP_Text>(true);

            TMP_Text directText = GetComponentInChildren<TMP_Text>(true);
            if (directText != null)
                return directText;

            return null;
        }

        private void RefreshView()
        {
            List<NyangQuariumBoardItem> previewItems = new(_rewardQueue);

            for (int i = 0; i < _slotViews.Count; i++)
            {
                if (_slotViews[i] == null)
                    continue;

                if (i < previewItems.Count && i < PreviewCount)
                    _slotViews[i].SetItem(previewItems[i]);
                else
                    _slotViews[i].SetItem(NyangQuariumBoardItem.Empty);
            }

            if (_countText == null)
                return;

            int count = _rewardQueue.Count;
            _countText.text = count.ToString();
            _countText.transform.parent?.gameObject.SetActive(count > 0);
            _countText.gameObject.SetActive(count > 0);
        }
    }

    public sealed class NyangQuariumRewardQueueSlot : MonoBehaviour, IPointerClickHandler
    {
        private NyangQuariumRewardQueue _rewardQueue;
        private Image _itemImage;
        private Button _button;
        private UI.UISpriteController _spriteController;
        private Sprite _emptySprite;
        private Color _emptyColor;
        private bool _isTopSlot;
        private bool _hasItem;

        public void Init(NyangQuariumRewardQueue rewardQueue, bool isTopSlot)
        {
            _rewardQueue = rewardQueue;
            _isTopSlot = isTopSlot;
            BindImage();
            BindButton();
            SetItem(NyangQuariumBoardItem.Empty);
        }

        public void SetItem(NyangQuariumBoardItem item)
        {
            BindImage();

            _hasItem = item != null && item.HasItem;

            if (_itemImage == null)
                return;

            _spriteController?.ClearSprite();

            if (!_hasItem)
            {
                _itemImage.sprite = null;
                _itemImage.color = _emptyColor;
                _itemImage.enabled = false;
                _itemImage.gameObject.SetActive(false);
                _itemImage.raycastTarget = _button != null;
                return;
            }

            ItemData itemData = item.ItemData;
            _itemImage.gameObject.SetActive(true);
            _itemImage.color = Color.white;
            _itemImage.enabled = true;
            _itemImage.raycastTarget = _button != null;

            if (itemData.ItemSprite != null)
            {
                _itemImage.sprite = itemData.ItemSprite;
                return;
            }

            _itemImage.sprite = null;

            if (!string.IsNullOrWhiteSpace(itemData.AddressableKey))
            {
                _spriteController ??= new UI.UISpriteController(_itemImage);
                _spriteController.ChangeSprite(itemData.AddressableKey);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null)
                return;

            HandleClick();
        }

        private void HandleClick()
        {
            if (!_isTopSlot || !_hasItem || _rewardQueue == null)
                return;

            _rewardQueue.TryMoveTopItemToBoard();
        }

        private void BindImage()
        {
            if (_itemImage != null)
                return;

            _itemImage = FindButtonImage();

            if (_itemImage == null)
                _itemImage = FindChildImage();

            if (_itemImage == null)
                return;

            _emptySprite = _itemImage.sprite;
            _emptyColor = _itemImage.color;
        }

        private Image FindButtonImage()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                Image image = button.GetComponent<Image>();
                if (image != null)
                    return image;
            }

            return null;
        }

        private Image FindChildImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.gameObject == gameObject)
                    continue;

                return image;
            }

            return null;
        }

        private void BindButton()
        {
            Button button = null;

            if (_itemImage != null)
                button = _itemImage.GetComponent<Button>();

            if (button == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0)
                    button = buttons[0];
            }

            if (_button == button)
                return;

            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);

            _button = button;

            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);

            _spriteController?.Dispose();
            _spriteController = null;
        }
    }

    [DefaultExecutionOrder(-50)]
    public sealed class NyangQuariumItemBoard : MonoBehaviour
    {
        private const int Width = 7;
        private const int Height = 9;
        private const int SlotSize = 135;
        private const int SlotSpacing = 9;
        private const int ItemSize = 85;
        private const string SlotRootObjectName = "@Slot Root";

        private readonly List<NyangQuariumItemSlot> _slots = new();
        private NyangQuariumItemInfoPanel _infoPanel;
        private NyangQuariumItemSlot _selectedSlot;
        private Transform _slotRoot;
        private bool _initialized;

        private void Awake()
        {
            // BoardSystem.Start()보다 먼저 비활성화해서 메인 보드 @Slot Root 오염을 방지합니다.
            DisableMergeBoardSystem();
        }

        public void Init(NyangQuariumItemInfoPanel infoPanel)
        {
            _infoPanel = infoPanel;

            if (_initialized)
                return;

            // 메인 머지보드 BoardSystem 끄고 냥쿠 전용 슬롯으로 대체
            DisableMergeBoardSystem();
            SetupSlotRoot();
            GenerateSlots();
            ClearSelection();
            NyangQuariumMergeBoardInventoryService.RegisterBoard(this);
            _initialized = true;
        }

        private void OnDestroy()
        {
            NyangQuariumMergeBoardInventoryService.UnregisterBoard(this);
        }

        // InventoryService.GetOwnedFishCount — 슬롯 전체에서 fishId 개수 세기
        public int GetOwnedFishCount(int fishId)
        {
            if (fishId <= 0)
                return 0;

            int count = 0;

            for (int i = 0; i < _slots.Count; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot != null && slot.HasItem && slot.Item.Id == fishId)
                    count++;
            }

            return count;
        }

        // InventoryService.CopyOwnedFishEntries — 왼쪽 슬롯부터 maxCount개, FishSO에 없는 아이템은 스킵
        public void CopyOwnedFishEntries(
            List<NyangQuariumMergeBoardFishEntry> results,
            FishType aquariumType,
            int maxCount)
        {
            if (results == null)
                return;

            results.Clear();

            if (maxCount <= 0)
                return;

            for (int i = 0; i < _slots.Count && results.Count < maxCount; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem)
                    continue;

                NyangQuariumBoardItem item = slot.Item;

                if (!TryResolveFishData(item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (!NyangQuariumMergeBoardInventoryService.CanPlaceFish(
                        fishData.FishType,
                        aquariumType))
                {
                    continue;
                }

                results.Add(new NyangQuariumMergeBoardFishEntry(
                    i,
                    item.Id,
                    item.Level,
                    fishData.FishKey,
                    item.Name,
                    fishData.FishType,
                    item.ItemData?.ItemSprite));
            }
        }

        public int GetOwnedNatureCount(int itemId)
        {
            if (itemId <= 0)
                return 0;

            int count = 0;

            for (int i = 0; i < _slots.Count; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem || slot.Item.Id != itemId)
                    continue;

                if (!TryResolveFishData(slot.Item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                count++;
            }

            return count;
        }

        public void CopyOwnedNatureEntries(
            List<NyangQuariumMergeBoardNatureEntry> results,
            int maxCount)
        {
            if (results == null)
                return;

            results.Clear();

            if (maxCount <= 0)
                return;

            for (int i = 0; i < _slots.Count && results.Count < maxCount; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem)
                    continue;

                NyangQuariumBoardItem item = slot.Item;

                if (!TryResolveFishData(item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                results.Add(new NyangQuariumMergeBoardNatureEntry(
                    i,
                    item.Id,
                    item.Level,
                    fishData.FishKey,
                    item.Name,
                    item.ItemData?.ItemSprite));
            }
        }

        public bool TryConsumeNature(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0)
                return false;

            int remaining = count;

            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem || slot.Item.Id != itemId)
                    continue;

                if (!TryResolveFishData(slot.Item.Id, out NyangQuariumFishData fishData))
                    continue;

                if (fishData.FishType != FishType.Environments)
                    continue;

                if (!TryClearSlot(i))
                    continue;

                remaining--;
            }

            if (remaining > 0)
                return false;

            return true;
        }

        public bool TryConsumeNatureAtSlot(int slotIndex)
        {
            if (!TryGetNatureSlotData(slotIndex, out _))
                return false;

            return TryClearSlot(slotIndex);
        }

        private bool TryGetNatureSlotData(int slotIndex, out NyangQuariumFishData fishData)
        {
            fishData = null;

            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;

            NyangQuariumItemSlot slot = _slots[slotIndex];

            if (slot == null || !slot.HasItem)
                return false;

            if (!TryResolveFishData(slot.Item.Id, out fishData))
                return false;

            return fishData.FishType == FishType.Environments;
        }

        // InventoryService.TryConsumeFish — fishId 같은 슬롯을 왼쪽부터 count만큼 비움
        public bool TryConsumeFish(int fishId, int count)
        {
            if (fishId <= 0 || count <= 0)
                return false;

            int remaining = count;

            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                NyangQuariumItemSlot slot = _slots[i];

                if (slot == null || !slot.HasItem || slot.Item.Id != fishId)
                    continue;

                if (!TryClearSlot(i))
                    continue;

                remaining--;
            }

            if (remaining > 0)
                return false;

            return true;
        }

        // 슬롯 1칸 비우기 — 수조 배치 후 머지보드에서 사라지는 처리
        public bool TryClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;

            NyangQuariumItemSlot slot = _slots[slotIndex];

            if (slot == null || !slot.HasItem)
                return false;

            if (_selectedSlot == slot)
                ClearSelection();

            slot.SetItem(NyangQuariumBoardItem.Empty);
            return true;
        }

        private static bool TryResolveFishData(int fishId, out NyangQuariumFishData fishData)
        {
            fishData = null;

            SheetLoader sheetLoader = UnityEngine.Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader == null ||
                !sheetLoader.TryGetNyangQuariumFishSO(out NyangQuariumFishSO fishSO) ||
                fishSO.FishData == null)
            {
                return false;
            }

            for (int i = 0; i < fishSO.FishData.Count; i++)
            {
                NyangQuariumFishData data = fishSO.FishData[i];

                if (data == null || data.FishId != fishId)
                    continue;

                fishData = data;
                return true;
            }

            return false;
        }

        // 왼쪽부터 빈 슬롯 찾아서 아이템 넣기
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
                NyangQuariumMergeBoardInventoryService.NotifyInventoryChanged();
                return true;
            }

            DebugTool.Warning("[NyangQuariumItemBoard] 빈 슬롯이 없습니다.", DebugType.UI, this);
            return false;
        }

        // 슬롯 클릭 시 선택 표시 + 하단 Info 패널 갱신
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

        // 같은 오브젝트에 붙은 메인 BoardSystem 비활성화
        private void DisableMergeBoardSystem()
        {
            BoardSystem[] boardSystems = GetComponents<BoardSystem>();

            for (int i = 0; i < boardSystems.Length; i++)
            {
                BoardSystem boardSystem = boardSystems[i];

                if (boardSystem == null)
                    continue;

                boardSystem.enabled = false;
            }
        }

        // @Slot Root 없으면 만들고 GridLayoutGroup 7열로 맞춤
        private void SetupSlotRoot()
        {
            Transform existingRoot = transform.Find(SlotRootObjectName);
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

        // 63칸 슬롯 런타임 생성 (배경 Image + 아이템 Image)
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

    // NyangQuariumItemSlot -> 슬롯 1칸, 클릭하면 보드에 선택 전달
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

        // AddressableKey로 로드
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
