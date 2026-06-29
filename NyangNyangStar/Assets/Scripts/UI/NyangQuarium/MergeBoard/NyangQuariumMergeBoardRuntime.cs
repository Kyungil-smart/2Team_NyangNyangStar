using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
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
        private const string GeneratorName = "Item Generator";
        private const string BoardName = "Item Board";
        private const string InfoName = "ItemInfo";
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

            NyangQuariumMergeBoardNavigation navigation = rootObject.GetComponent<NyangQuariumMergeBoardNavigation>();
            if (navigation == null)
                navigation = rootObject.AddComponent<NyangQuariumMergeBoardNavigation>();

            navigation.Init();
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
                if (child != null && child.gameObject.activeInHierarchy && child.name == objectName)
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

    // NyangQuariumItemGenerator -> 보드 밖 Item Generator 버튼, 클릭 시 랜덤 아이템 생성
    // TODO : 생성기 테이블(NyangQuariumGeneratorSO) 연동 전 임시 구현
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

        // 빈 슬롯에 랜덤 아이템 1개 추가
        public void GenerateItem()
        {
            if (_board == null)
                return;

            NyangQuariumBoardItem item = CreateRandomItem();
            if (_board.TryAddItem(item))
                UnlockCollectionItem(item);
        }

        private async void UnlockCollectionItem(NyangQuariumBoardItem item)
        {
            if (!IsKnownFishItem(item))
                return;

            if (!await WaitForFirestoreReadyAsync())
            {
                DebugTool.Warning("[NyangQuariumItemGenerator] Firestore가 준비되지 않아 도감 해금 저장을 생략합니다.", DebugType.Data, this);
                return;
            }

            FireStoreManager fireStoreManager = FireStoreManager.Instance;

            if (fireStoreManager == null ||
                !fireStoreManager.IsInitialized ||
                !fireStoreManager.TryGetStore(out NyangQuariumFirestoreSO nyangQuariumSO) ||
                nyangQuariumSO == null)
            {
                DebugTool.Warning("[NyangQuariumItemGenerator] Firestore가 준비되지 않아 도감 해금 저장을 생략합니다.", DebugType.Data, this);
                return;
            }

            try
            {
                await nyangQuariumSO.UnlockFishAsync(item.Id);
            }
            catch (System.Exception e)
            {
                DebugTool.Warning($"[NyangQuariumItemGenerator] 도감 해금 저장 실패: {e.Message}", DebugType.Data, this);
            }
        }

        private static async Task<bool> WaitForFirestoreReadyAsync(int timeoutMs = 5000)
        {
            int elapsedMs = 0;
            const int intervalMs = 100;

            while (elapsedMs < timeoutMs)
            {
                if (FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized)
                    return true;

                await Task.Delay(intervalMs);
                elapsedMs += intervalMs;
            }

            return FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized;
        }

        private bool IsKnownFishItem(NyangQuariumBoardItem item)
        {
            if (item == null || !item.HasItem || item.Id <= 0)
                return false;

            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader == null ||
                !sheetLoader.TryGetNyangQuariumFishSO(out NyangQuariumFishSO fishSO) ||
                fishSO == null ||
                fishSO.FishData == null)
            {
                return false;
            }

            for (int i = 0; i < fishSO.FishData.Count; i++)
            {
                NyangQuariumFishData fishData = fishSO.FishData[i];

                if (fishData != null && fishData.FishId == item.Id)
                    return true;
            }

            return false;
        }

        // 물고기 시트에서 랜덤 뽑기, 없으면 fallback 이름으로 임시 아이템
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

                    if (NyangQuariumFishSpriteCache.TryGetSprite(fishData.FishKey, out Sprite sprite))
                        itemData.SetSprite(sprite);

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

    // NyangQuariumItemBoard -> 7x9 슬롯 그리드, 아이템 배치·선택 처리
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
            BoardSystem mergeBoardSystem = GetComponent<BoardSystem>();
            if (mergeBoardSystem != null)
                mergeBoardSystem.enabled = false;
        }

        // @Slot Root 없으면 만들고 GridLayoutGroup 7열로 맞춤
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
