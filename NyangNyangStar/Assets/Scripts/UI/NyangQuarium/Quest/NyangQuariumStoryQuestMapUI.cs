using System.Collections;
using System.Collections.Generic;
using Core.Managers;
using Data.ScriptableObjects.KeyContainerSO;
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
        private const string FirstQuestObjectName = "FirstQuest";
        private const string SecondQuestObjectName = "SeconQuest";
        private const string ThirdQuestObjectName = "ThirdQuest";
        private const string NormalMarkerSpriteKey = "NQ_Marker_normal";
        private const string CompleteMarkerSpriteKey = "NQ_Marker_clear";
        private static readonly int[] DefaultStoryMapQuestIds = { 43001, 43002, 43003 };

        [Header("Story Map Quest IDs")]
        [SerializeField] private int[] _storyMapQuestIds = { 43001, 43002, 43003 };

        [Header("Story Quest Buttons")]
        [SerializeField] private Button _firstQuestButton;
        [SerializeField] private GameObject _firstQuestRoot;
        [SerializeField] private Button _secondQuestButton;
        [SerializeField] private GameObject _secondQuestRoot;
        [SerializeField] private Button _thirdQuestButton;
        [SerializeField] private GameObject _thirdQuestRoot;

        private readonly StoryQuestSlotBinding[] _slotBindings = new StoryQuestSlotBinding[3];
        private bool _initialized;

        private static NyangQuariumStoryQuestMapUI _instance;

        private sealed class StoryQuestSlotBinding
        {
            public int SlotIndex;
            public Button Button;
            public GameObject Root;
            public Image MarkerImage;
            public UISpriteController MarkerSprite;
            public NyangQuariumQuestData Quest;
            public bool IsCompleteReady;
            public string CurrentMarkerSpriteKey;
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            RefreshStoryQuestMap();
        }

        private void Start()
        {
            ResolveReferences();
            BindButtons();
            BindMarkerSprites();
            SubscribeEvents();
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
            }
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

        private void SubscribeEvents()
        {
            EnsureSubscribed();
        }

        private void UnsubscribeEvents()
        {
            if (MainUI.Instance != null)
                MainUI.Instance.MergeBoardVisibilityChanged -= RefreshStoryQuestMap;

            if (NyangQuariumQuestManager.Instance != null)
                NyangQuariumQuestManager.Instance.StoryQuestProgressChanged -= RefreshStoryQuestMap;
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

            NyangQuariumStoryQuestMapUI mapUI =
                UnityEngine.Object.FindFirstObjectByType<NyangQuariumStoryQuestMapUI>();

            mapUI?.RefreshStoryQuestMap(force: true);
        }

        private void ResolveReferences()
        {
            ResolveSlot(0, FirstQuestObjectName, ref _firstQuestButton, ref _firstQuestRoot);
            ResolveSlot(1, SecondQuestObjectName, ref _secondQuestButton, ref _secondQuestRoot);
            ResolveSlot(2, ThirdQuestObjectName, ref _thirdQuestButton, ref _thirdQuestRoot);
        }

        private void ResolveSlot(
            int slotIndex,
            string objectName,
            ref Button button,
            ref GameObject root)
        {
            Transform target = FindTransform(objectName);

            if (target == null)
                return;

            if (root == null)
                root = target.gameObject;

            if (button == null)
                button = target.GetComponent<Button>();

            _slotBindings[slotIndex] = new StoryQuestSlotBinding
            {
                SlotIndex = slotIndex,
                Button = button,
                Root = root
            };
        }

        private void BindMarkerSprites()
        {
            EnsureMarkerSpriteKeys();

            for (int i = 0; i < _slotBindings.Length; i++)
            {
                StoryQuestSlotBinding binding = _slotBindings[i];

                if (binding?.Button == null)
                    continue;

                binding.MarkerImage = binding.Button.targetGraphic as Image;

                if (binding.MarkerImage == null)
                    binding.MarkerImage = binding.Button.GetComponent<Image>();

                if (binding.MarkerImage == null)
                    continue;

                binding.MarkerSprite = new UISpriteController(binding.MarkerImage);
            }
        }

        private void BindButtons()
        {
            for (int i = 0; i < _slotBindings.Length; i++)
            {
                StoryQuestSlotBinding binding = _slotBindings[i];

                if (binding?.Button == null)
                    continue;

                int slotIndex = binding.SlotIndex;
                binding.Button.onClick.RemoveAllListeners();
                binding.Button.onClick.AddListener(() => OnStoryQuestClicked(slotIndex));
            }
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

        private void RefreshStoryQuestMap()
        {
            RefreshStoryQuestMap(force: false);
        }

        private void RefreshStoryQuestMap(bool force)
        {
            if (!_initialized && !force)
                return;

            int activeSlotIndex = ResolveActiveStoryQuestSlotIndex();

            DebugTool.Log(
                $"[NyangQuariumStoryQuestMapUI] 맵 갱신. ActiveSlot:{activeSlotIndex}, ActiveQuestId:{ResolveActiveQuestIdForLog()}",
                DebugType.UI,
                this);

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

                RefreshSlotReadyState(binding, force: true);
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

            int[] questIds = GetStoryMapQuestIds();

            for (int i = 0; i < questIds.Length && i < _slotBindings.Length; i++)
            {
                int questId = questIds[i];

                if (questId <= 0)
                    continue;

                if (NyangQuariumQuestManager.Instance != null &&
                    NyangQuariumQuestManager.Instance.IsQuestCompleted(questId))
                {
                    continue;
                }

                return i;
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

                if (i >= questIds.Length)
                    continue;

                int questId = questIds[i];

                if (questId <= 0)
                    continue;

                if (TryResolveQuestById(questId, out NyangQuariumQuestData quest))
                {
                    binding.Quest = quest;
                    continue;
                }

                DebugTool.Warning(
                    $"[NyangQuariumStoryQuestMapUI] Story 맵 퀘스트를 찾지 못했습니다. Slot:{i}, QuestId:{questId}",
                    DebugType.UI,
                    this);
            }
        }

        private int ResolveActiveQuestIdForLog()
        {
            if (NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetActiveQuest(out NyangQuariumQuestData quest))
            {
                return quest.ID;
            }

            return 0;
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

            if (!NyangQuariumQuestSOLocator.TryResolveQuestSO(out NyangQuariumQuestSO questSO))
                return false;

            return questSO.TryGetQuest(questId, out quest);
        }

        private void RefreshSlotReadyState(StoryQuestSlotBinding binding, bool force = false)
        {
            if (binding?.Quest == null || binding.MarkerSprite == null)
                return;

            bool canComplete = CanCompleteStoryQuest(binding.Quest);

            if (!force &&
                canComplete == binding.IsCompleteReady &&
                !string.IsNullOrWhiteSpace(binding.CurrentMarkerSpriteKey))
            {
                return;
            }

            binding.IsCompleteReady = canComplete;
            string spriteKey = canComplete ? CompleteMarkerSpriteKey : NormalMarkerSpriteKey;

            if (binding.CurrentMarkerSpriteKey == spriteKey)
                return;

            binding.CurrentMarkerSpriteKey = spriteKey;
            binding.MarkerSprite.ChangeSprite(spriteKey, nativeSize: true);

            DebugTool.Log(
                $"[NyangQuariumStoryQuestMapUI] Slot:{binding.SlotIndex} 상태 갱신. QuestId:{binding.Quest.ID}, CanComplete:{canComplete}",
                DebugType.UI,
                this);
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

        public static bool CanCompleteStoryQuest(NyangQuariumQuestData quest)
        {
            if (quest == null)
                return false;

            if (NyangQuariumQuestManager.Instance != null)
                return NyangQuariumQuestManager.Instance.CanCompleteQuest(quest);

            if (!CanCompleteCondition(quest.QuestCondition1, quest.ConditionAmount1))
                return false;

            if (quest.HasCondition2 &&
                !CanCompleteCondition(quest.QuestCondition2, quest.ConditionAmount2))
            {
                return false;
            }

            return true;
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

        private Transform FindTransform(string objectName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);

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
                if (questIds[i] <= 0)
                    continue;

                if (questSO.TryGetQuest(questIds[i], out _))
                    return true;
            }

            return questSO.GetQuestsByType(NyangQuariumQuestType.Story).Count > 0;
        }

        private static bool TryResolveStoryQuestAt(int index, out NyangQuariumQuestData quest)
        {
            if (NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetStoryQuestAt(index, out quest))
            {
                return true;
            }

            if (!NyangQuariumQuestSOLocator.TryResolveQuestSO(out NyangQuariumQuestSO questSO))
            {
                quest = null;
                return false;
            }

            List<NyangQuariumQuestData> storyQuests =
                questSO.GetQuestsByType(NyangQuariumQuestType.Story);

            if (storyQuests.Count == 0)
            {
                quest = null;
                return false;
            }

            storyQuests.Sort((a, b) => a.ID.CompareTo(b.ID));

            if (index < 0 || index >= storyQuests.Count)
            {
                quest = null;
                return false;
            }

            quest = storyQuests[index];
            return true;
        }
    }
}
