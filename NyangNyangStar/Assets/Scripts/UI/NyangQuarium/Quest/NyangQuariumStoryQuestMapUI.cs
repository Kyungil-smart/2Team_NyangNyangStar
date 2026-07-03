using System;
using System.Collections;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.KeyContainerSO;
using Data.ScriptableObjects.MergeBoard;
using Data.ScriptableObjects.NyangQuariumSO;
using Services.Enums;
using UI;
using UI.MergeBoard;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium.Quest
{
    // MainUI 맵 위 FirstQuest / SeconQuest / ThirdQuest 버튼을 Story 퀘스트와 연결합니다.
    [RequireComponent(typeof(NyangQuariumQuestPopupOpener))]
    public sealed class NyangQuariumStoryQuestMapUI : MonoBehaviour
    {
        private const string QuestItemObjectName = "QuestItem";
        private const string NormalMarkerSpriteKey = "NQ_Marker_normal";
        private const string CompleteMarkerSpriteKey = "NQ_Marker_clear";
        private const string CoinIconKey = "Main_Icon_Coin";
        private static readonly string[] SlotObjectNames = { "FirstQuest", "SeconQuest", "ThirdQuest" };
        private static readonly string[] TankTutorialObjectNames = { "Tank_Tutorial_01", "Tank_Tutorial_02" };
        private static readonly string[] TankTutorialSpriteKeys = { "NQ_Object_Tank_T01", "NQ_Object_Tank_T02" };
        private static readonly int[] DefaultStoryMapQuestIds = { 43001, 43002, 43003 };

        [Header("Story Map Quest IDs")]
        [SerializeField]
        private int[] _storyMapQuestIds = { 43001, 43002, 43003 };

        private readonly StoryQuestSlotBinding[] _slotBindings = new StoryQuestSlotBinding[3];
        private readonly TankTutorialBinding[] _tankTutorialBindings = new TankTutorialBinding[TankTutorialObjectNames.Length];
        private bool _initialized;

        private static NyangQuariumStoryQuestMapUI _instance;

        private sealed class StoryQuestSlotBinding
        {
            public int SlotIndex;
            public Button Button;
            public GameObject Root;
            public UISpriteController MarkerSprite;
            public UISpriteController QuestItemSprite;
            public Image QuestItemImage;
            public NyangQuariumQuestData Quest;
            public bool IsCompleteReady;
            public string CurrentMarkerSpriteKey;
            public int CurrentQuestItemBindId = -1;
        }

        private sealed class TankTutorialBinding
        {
            public GameObject Root;
            public Image Image;
            public UISpriteController Sprite;
            public string SpriteKey;
            public bool SpriteRequested;
            public bool SpriteLoaded;
        }

        private void Awake()
        {
            _instance = this;
            InitializeSlots();
            InitializeTankTutorials();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            RefreshStoryQuestMap();
        }

        private void Start()
        {
            EnsureSubscribed();
            StartCoroutine(PrepareStoryQuestCoroutine());
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            UnsubscribeEvents();

            for (int i = 0; i < _slotBindings.Length; i++)
            {
                StoryQuestSlotBinding binding = _slotBindings[i];
                if (binding?.Button == null)
                    continue;

                binding.Button.onClick.RemoveAllListeners();
                binding.MarkerSprite?.Dispose();
                binding.QuestItemSprite?.Dispose();
            }

            for (int i = 0; i < _tankTutorialBindings.Length; i++)
                _tankTutorialBindings[i]?.Sprite?.Dispose();
        }

        public static int[] GetStoryMapQuestIds()
        {
            if (_instance != null &&
                _instance._storyMapQuestIds != null &&
                _instance._storyMapQuestIds.Length > 0)
            {
                return _instance._storyMapQuestIds;
            }

            return DefaultStoryMapQuestIds;
        }

        public static void RequestMapRefresh()
        {
            if (_instance != null)
            {
                _instance.RefreshStoryQuestMap(force: true);
                return;
            }

            FindFirstObjectByType<NyangQuariumStoryQuestMapUI>()
                ?.RefreshStoryQuestMap(force: true);
        }

        public static void ShowTankTutorial(int tierIndex)
        {
            if (_instance != null)
            {
                _instance.SetTankTutorialVisible(tierIndex, visible: true);
                return;
            }

            FindFirstObjectByType<NyangQuariumStoryQuestMapUI>()
                ?.SetTankTutorialVisible(tierIndex, visible: true);
        }

        public static void HideTankTutorial(int tierIndex)
        {
            if (_instance != null)
            {
                _instance.SetTankTutorialVisible(tierIndex, visible: false);
                return;
            }

            FindFirstObjectByType<NyangQuariumStoryQuestMapUI>()
                ?.SetTankTutorialVisible(tierIndex, visible: false);
        }

        public static bool CanCompleteStoryQuest(NyangQuariumQuestData quest)
        {
            if (quest == null)
                return false;

            if (NyangQuariumQuestManager.Instance != null)
                return NyangQuariumQuestManager.Instance.CanCompleteQuest(quest);

            if (!CanCompleteCondition(quest.QuestCondition1, quest.ConditionAmount1))
                return false;

            return !quest.HasCondition2 ||
                   CanCompleteCondition(quest.QuestCondition2, quest.ConditionAmount2);
        }

        private void EnsureSubscribed()
        {
            if (MainUI.Instance != null)
            {
                MainUI.Instance.MergeBoardVisibilityChanged -= RefreshStoryQuestMap;
                MainUI.Instance.MergeBoardVisibilityChanged += RefreshStoryQuestMap;
            }

            if (NyangQuariumQuestManager.Instance != null)
            {
                NyangQuariumQuestManager.Instance.StoryQuestProgressChanged -= RefreshStoryQuestMap;
                NyangQuariumQuestManager.Instance.StoryQuestProgressChanged += RefreshStoryQuestMap;
            }
        }

        private void UnsubscribeEvents()
        {
            if (MainUI.Instance != null)
                MainUI.Instance.MergeBoardVisibilityChanged -= RefreshStoryQuestMap;

            if (NyangQuariumQuestManager.Instance != null)
                NyangQuariumQuestManager.Instance.StoryQuestProgressChanged -= RefreshStoryQuestMap;
        }

        private void InitializeSlots()
        {
            EnsureMarkerSpriteKeys();
            EnsureTankTutorialSpriteKeys();

            Transform[] transforms = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < _slotBindings.Length; i++)
            {
                Transform root = FindNamedTransform(transforms, SlotObjectNames[i]);
                if (root == null)
                    continue;

                Button button = root.GetComponent<Button>();
                if (button == null)
                    continue;

                StoryQuestSlotBinding binding = new StoryQuestSlotBinding
                {
                    SlotIndex = i,
                    Button = button,
                    Root = root.gameObject
                };

                Image markerImage = button.targetGraphic as Image ?? button.GetComponent<Image>();
                if (markerImage != null)
                    binding.MarkerSprite = new UISpriteController(markerImage);

                Transform questItemTransform = root.Find(QuestItemObjectName);
                if (questItemTransform != null &&
                    questItemTransform.TryGetComponent(out Image questItemImage))
                {
                    binding.QuestItemImage = questItemImage;
                    binding.QuestItemSprite = new UISpriteController(questItemImage);
                }
                else
                {
                    DebugTool.Warning(
                        $"[NyangQuariumStoryQuestMapUI] Slot:{i} {QuestItemObjectName} Image를 찾지 못했습니다.",
                        DebugType.UI,
                        this);
                }

                int slotIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnStoryQuestClicked(slotIndex));

                _slotBindings[i] = binding;

                if (binding.Root != null)
                    binding.Root.SetActive(false);
            }
        }

        private void InitializeTankTutorials()
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < TankTutorialObjectNames.Length; i++)
            {
                Transform root = FindNamedTransform(transforms, TankTutorialObjectNames[i]);
                if (root == null)
                    continue;

                Image image = root.GetComponent<Image>();
                TankTutorialBinding binding = new TankTutorialBinding
                {
                    Root = root.gameObject,
                    Image = image,
                    SpriteKey = i < TankTutorialSpriteKeys.Length ? TankTutorialSpriteKeys[i] : null
                };

                if (image != null)
                {
                    image.sprite = null;
                    image.enabled = false;
                    binding.Sprite = new UISpriteController(image);
                }

                _tankTutorialBindings[i] = binding;

                if (binding.Root != null)
                    binding.Root.SetActive(false);
            }
        }

        private void SetTankTutorialVisible(int tierIndex, bool visible)
        {
            if (tierIndex < 0 || tierIndex >= _tankTutorialBindings.Length)
                return;

            if (visible && tierIndex == 1)
                SetTankTutorialVisible(tierIndex: 0, visible: false);

            TankTutorialBinding binding = _tankTutorialBindings[tierIndex];
            if (binding?.Root == null)
            {
                InitializeTankTutorials();
                binding = _tankTutorialBindings[tierIndex];
            }

            if (binding?.Root == null)
                return;

            if (!visible)
            {
                binding.Root.SetActive(false);
                return;
            }

            if (binding.SpriteLoaded && binding.Image != null && binding.Image.sprite != null)
            {
                binding.Image.enabled = true;
                binding.Root.SetActive(true);
                return;
            }

            binding.Root.SetActive(false);

            if (binding.Sprite != null &&
                !binding.SpriteRequested &&
                !string.IsNullOrWhiteSpace(binding.SpriteKey))
            {
                binding.SpriteRequested = true;
                binding.Sprite.ChangeSprite(
                    binding.SpriteKey,
                    onLoaded: () => OnTankTutorialSpriteLoaded(tierIndex));
            }
        }

        private void OnTankTutorialSpriteLoaded(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= _tankTutorialBindings.Length)
                return;

            TankTutorialBinding binding = _tankTutorialBindings[tierIndex];
            if (binding?.Root == null || binding.Image?.sprite == null)
                return;

            binding.SpriteLoaded = true;

            if (tierIndex == 1)
                SetTankTutorialVisible(tierIndex: 0, visible: false);

            binding.Image.enabled = true;
            binding.Root.SetActive(true);
        }

        private IEnumerator PrepareStoryQuestCoroutine()
        {
            while (!NyangQuariumQuestSOLocator.TryResolveQuestSO(out NyangQuariumQuestSO questSO) ||
                   !IsStoryMapQuestDataReady(questSO))
            {
                yield return null;
            }

            BindStoryMapQuests();
            NyangQuariumQuestManager.Instance?.EnsureStoryQuestInitialized();
            EnsureSubscribed();
            _initialized = true;
            RefreshStoryQuestMap();

            DebugTool.Log("[NyangQuariumStoryQuestMapUI] Story 퀘스트 맵 준비 완료", DebugType.UI, this);
        }

        private void RefreshStoryQuestMap() => RefreshStoryQuestMap(force: false);

        private void RefreshStoryQuestMap(bool force)
        {
            if (!_initialized && !force)
                return;

            int activeSlotIndex = ResolveActiveStoryQuestSlotIndex();

            for (int i = 0; i < _slotBindings.Length; i++)
            {
                StoryQuestSlotBinding binding = _slotBindings[i];
                if (binding == null)
                    continue;

                bool isVisible = activeSlotIndex >= 0 &&
                                 binding.Quest != null &&
                                 binding.SlotIndex == activeSlotIndex;

                if (binding.Root != null)
                    binding.Root.SetActive(isVisible);

                if (!isVisible)
                {
                    binding.IsCompleteReady = false;
                    binding.CurrentMarkerSpriteKey = null;
                    continue;
                }

                RefreshSlotMarker(binding);
                RefreshQuestItemIcon(binding);
            }

            NyangQuariumQuestPopupOpener.RefreshOpenPopup();
        }

        private int ResolveActiveStoryQuestSlotIndex()
        {
            if (NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetActiveStoryMapSlotIndex(out int slotIndex))
            {
                return slotIndex;
            }

            return -1;
        }

        private void BindStoryMapQuests()
        {
            int[] questIds = GetStoryMapQuestIds();

            for (int i = 0; i < _slotBindings.Length; i++)
            {
                StoryQuestSlotBinding binding = _slotBindings[i];
                if (binding == null)
                    continue;

                binding.Quest = null;

                if (i >= questIds.Length || questIds[i] <= 0)
                    continue;

                if (TryResolveQuestById(questIds[i], out NyangQuariumQuestData quest))
                {
                    binding.Quest = quest;
                    continue;
                }

                DebugTool.Warning(
                    $"[NyangQuariumStoryQuestMapUI] Story 맵 퀘스트를 찾지 못했습니다. Slot:{i}, QuestId:{questIds[i]}",
                    DebugType.UI,
                    this);
            }
        }

        private static bool TryResolveQuestById(int questId, out NyangQuariumQuestData quest)
        {
            quest = null;

            if (questId <= 0)
                return false;

            if (NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetQuest(questId, out quest))
            {
                return quest != null;
            }

            return NyangQuariumQuestSOLocator.TryResolveQuestSO(out NyangQuariumQuestSO questSO) &&
                   questSO.TryGetQuest(questId, out quest);
        }

        private void RefreshSlotMarker(StoryQuestSlotBinding binding)
        {
            if (binding?.Quest == null || binding.MarkerSprite == null)
                return;

            bool canComplete = CanCompleteStoryQuest(binding.Quest);
            string spriteKey = canComplete ? CompleteMarkerSpriteKey : NormalMarkerSpriteKey;

            if (canComplete == binding.IsCompleteReady &&
                binding.CurrentMarkerSpriteKey == spriteKey)
            {
                return;
            }

            binding.IsCompleteReady = canComplete;
            binding.CurrentMarkerSpriteKey = spriteKey;
            binding.MarkerSprite.ChangeSprite(spriteKey, nativeSize: true);
        }

        private void RefreshQuestItemIcon(StoryQuestSlotBinding binding)
        {
            if (binding?.Quest == null || binding.QuestItemImage == null)
                return;

            if (!TryResolveConditionItemIcon(
                    binding.Quest,
                    out int itemBindId,
                    out string spriteKey,
                    out Sprite embeddedSprite))
            {
                binding.CurrentQuestItemBindId = -1;
                binding.QuestItemSprite?.ClearSprite();
                binding.QuestItemImage.sprite = null;
                binding.QuestItemImage.enabled = false;
                return;
            }

            if (binding.CurrentQuestItemBindId == itemBindId)
                return;

            binding.CurrentQuestItemBindId = itemBindId;
            binding.QuestItemImage.enabled = true;

            if (!string.IsNullOrWhiteSpace(spriteKey))
            {
                RegisterSpriteKeyIfMissing(
                    spriteKey,
                    spriteKey == CoinIconKey ? AddressableGroupType.Main : AddressableGroupType.Items);
                binding.QuestItemSprite?.ChangeSprite(spriteKey);
                return;
            }

            binding.QuestItemSprite?.ClearSprite();
            binding.QuestItemImage.sprite = embeddedSprite;
        }

        private static bool TryResolveConditionItemIcon(
            NyangQuariumQuestData quest,
            out int itemBindId,
            out string spriteKey,
            out Sprite embeddedSprite)
        {
            itemBindId = -1;
            spriteKey = null;
            embeddedSprite = null;

            if (quest == null)
                return false;

            if (NyangQuariumQuestConditionUtil.IsCoinCondition(quest.QuestCondition1))
            {
                itemBindId = 0;
                spriteKey = CoinIconKey;
                return true;
            }

            if (!TryResolveStoryMapItemId(quest, out int itemId) ||
                !TryGetItemData(itemId, out ItemData itemData))
            {
                return false;
            }

            itemBindId = itemId;

            if (!string.IsNullOrWhiteSpace(itemData.AddressableKey))
            {
                spriteKey = itemData.AddressableKey;
                return true;
            }

            if (itemData.ItemSprite == null)
                return false;

            embeddedSprite = itemData.ItemSprite;
            return true;
        }

        private static bool TryResolveStoryMapItemId(NyangQuariumQuestData quest, out int itemId)
        {
            itemId = 0;

            if (quest == null)
                return false;

            if (int.TryParse(quest.QuestCondition1, out itemId) && itemId > 0)
                return true;

            if (quest.QuestCondition1 != null &&
                quest.QuestCondition1.Equals("story", StringComparison.OrdinalIgnoreCase) &&
                quest.ConditionAmount1 > 0 &&
                TryGetItemData(quest.ConditionAmount1, out _))
            {
                itemId = quest.ConditionAmount1;
                return true;
            }

            return false;
        }

        private static bool TryGetItemData(int itemId, out ItemData itemData)
        {
            itemData = null;

            if (itemId <= 0)
                return false;

            return LocalDataAccess.Instance?.Game != null &&
                   LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out itemData);
        }

        private void OnStoryQuestClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotBindings.Length)
                return;

            StoryQuestSlotBinding binding = _slotBindings[slotIndex];
            if (binding?.Quest == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumStoryQuestMapUI] Story 퀘스트 데이터가 없습니다. Slot:{slotIndex}",
                    DebugType.UI,
                    this);
                return;
            }

            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            NyangQuariumQuestPopupOpener.Open(binding.Quest);
        }

        private static bool CanCompleteCondition(string condition, int amount)
        {
            if (string.IsNullOrWhiteSpace(condition) || amount <= 0)
                return true;

            if (NyangQuariumQuestConditionUtil.IsCoinCondition(condition))
            {
                return PlayerResourceManager.Instance != null &&
                       PlayerResourceManager.Instance.HasEnough(PlayerResourceType.Coin, amount);
            }

            if (!int.TryParse(condition, out int itemId))
                return false;

            return MergeBoardItemService.Instance != null &&
                   MergeBoardItemService.Instance.GetOwnedItemCount(itemId) >= amount;
        }

        private static void EnsureMarkerSpriteKeys()
        {
            RegisterSpriteKeyIfMissing(NormalMarkerSpriteKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(CompleteMarkerSpriteKey, AddressableGroupType.Nyangquarium);
        }

        private static void EnsureTankTutorialSpriteKeys()
        {
            for (int i = 0; i < TankTutorialSpriteKeys.Length; i++)
            {
                RegisterSpriteKeyIfMissing(
                    TankTutorialSpriteKeys[i],
                    AddressableGroupType.Nyangquarium);
            }
        }

        private static void RegisterSpriteKeyIfMissing(string key, AddressableGroupType groupType)
        {
            if (KeyContainer.Sprites.Contains(key))
                return;

            KeyContainer.Register(new KeyData
            {
                Key = key,
                FileName = key,
                Usage = "NyangQuariumStoryQuestMapUI",
                GroupType = groupType,
                LabelType = LabelType.Sprite,
                BuildType = BuildType.Local
            });
        }

        private static Transform FindNamedTransform(Transform[] transforms, string objectName)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name == objectName)
                    return candidate;
            }

            return null;
        }

        private static bool IsStoryMapQuestDataReady(NyangQuariumQuestSO questSO)
        {
            if (questSO == null)
                return false;

            int[] questIds = GetStoryMapQuestIds();

            for (int i = 0; i < questIds.Length; i++)
            {
                if (questIds[i] > 0 && questSO.TryGetQuest(questIds[i], out _))
                    return true;
            }

            return questSO.GetQuestsByType(NyangQuariumQuestType.Story).Count > 0;
        }
    }
}
