using Core.Managers;
using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Data.ScriptableObjects.NyangQuariumSO;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium.MergeBoard
{
    // merge 퀘스트 풀에서 3개를 한 번만 랜덤 등록하고, 머지보드 재진입 시에도 유지합니다.
    public static class NyangQuariumMergeQuestSession
    {
        private const int ActiveSlotCount = 3;

        private static readonly List<int> _activeQuestIds = new();
        private static bool _isRegistered;

        public static IReadOnlyList<int> ActiveQuestIds => _activeQuestIds;
        public static bool IsRegistered => _isRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionState()
        {
            _activeQuestIds.Clear();
            _isRegistered = false;
        }

        public static bool RemoveQuest(int questId)
        {
            return questId > 0 && _activeQuestIds.Remove(questId);
        }

        public static async Task EnsureRegisteredAsync(NyangQuariumQuestSO questSO)
        {
            if (_isRegistered)
                return;

            if (questSO == null)
                return;

            List<NyangQuariumQuestData> mergeQuests =
                questSO.GetQuestsByType(NyangQuariumQuestType.Merge);

            if (mergeQuests.Count == 0)
                return;

            NyangQuariumFirestoreSO store = await NyangQuariumFirestoreSO.WaitForReadyAsync();
            if (store != null && await store.LoadOrCreateFromServerAsync())
            {
                if (store.MergeQuestBoardInitialized)
                {
                    RestoreRegisteredQuests(store.ActiveMergeQuestBoardQuestIds, questSO);
                    _isRegistered = true;
                    return;
                }
            }

            List<NyangQuariumQuestData> pool = new(mergeQuests);
            int pickCount = Mathf.Min(ActiveSlotCount, pool.Count);

            for (int i = 0; i < pickCount; i++)
            {
                int randomIndex = Random.Range(i, pool.Count);

                if (randomIndex != i)
                {
                    NyangQuariumQuestData swap = pool[i];
                    pool[i] = pool[randomIndex];
                    pool[randomIndex] = swap;
                }

                _activeQuestIds.Add(pool[i].ID);
            }

            _isRegistered = true;
            await SaveCurrentStateAsync();

            DebugTool.Log(
                $"[NyangQuariumMergeQuestSession] merge 퀘스트 {pickCount}개 등록: {string.Join(", ", _activeQuestIds)}",
                DebugType.UI);
        }

        public static async Task SaveCurrentStateAsync()
        {
            NyangQuariumFirestoreSO store = await NyangQuariumFirestoreSO.WaitForReadyAsync();
            if (store == null)
                return;

            await store.SaveMergeQuestBoardStateAsync(_activeQuestIds, _isRegistered);
        }

        private static void RestoreRegisteredQuests(
            IReadOnlyList<int> savedQuestIds,
            NyangQuariumQuestSO questSO)
        {
            _activeQuestIds.Clear();

            if (savedQuestIds == null || questSO == null)
                return;

            HashSet<int> seen = new();

            for (int i = 0; i < savedQuestIds.Count; i++)
            {
                int questId = savedQuestIds[i];
                if (questId <= 0 || !seen.Add(questId))
                    continue;

                if (!questSO.TryGetQuest(questId, out NyangQuariumQuestData quest) ||
                    quest == null ||
                    quest.QuestType != NyangQuariumQuestType.Merge)
                {
                    continue;
                }

                _activeQuestIds.Add(questId);
            }
        }
    }

    // QuestBoardPanel 아래 3개 슬롯에 등록된 merge 퀘스트를 바인딩합니다.
    public sealed class NyangQuariumMergeQuestBoardUI : MonoBehaviour
    {
        private const string QuestBoardNamePrefix = "QuestBoard";
        private const string MergeTargetItemName = "MergeTargetItem";
        private const string RewardImageName = "RewardImage";
        private const string MergeTargetAmountTextName = "Text (TMP)";
        private const string CompleteButtonName = "Button";

        private readonly List<MergeQuestSlotBinding> _slotBindings = new();
        private readonly List<UI.UISpriteController> _spriteControllers = new();

        private NyangQuariumExpItemSO _expItemSO;
        private NyangQuariumRewardQueue _rewardQueue;
        private bool _initialized;
        private bool _initStarted;

        private sealed class MergeQuestSlotBinding
        {
            public int QuestId;
            public int FishId;
            public int RequiredAmount;
            public Transform SlotTransform;
            public Button CompleteButton;
            public bool IsCompleted;
        }

        private void OnEnable()
        {
            NyangQuariumMergeBoardInventoryService.InventoryChanged += RefreshCompleteButtons;

            if (!_initialized)
                Init();
        }

        private void OnDisable()
        {
            NyangQuariumMergeBoardInventoryService.InventoryChanged -= RefreshCompleteButtons;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _slotBindings.Count; i++)
            {
                MergeQuestSlotBinding binding = _slotBindings[i];
                if (binding?.CompleteButton == null)
                    continue;

                binding.CompleteButton.onClick.RemoveAllListeners();
            }

            for (int i = 0; i < _spriteControllers.Count; i++)
                _spriteControllers[i]?.Dispose();

            _spriteControllers.Clear();
            _slotBindings.Clear();
        }

        public void Init()
        {
            if (_initialized || _initStarted)
                return;

            if (!isActiveAndEnabled)
                return;

            _initStarted = true;
            StartCoroutine(InitializeCoroutine());
        }

        private IEnumerator InitializeCoroutine()
        {
            NyangQuariumQuestSO questSO = null;

            while (questSO == null || questSO.GetQuestsByType(NyangQuariumQuestType.Merge).Count == 0)
            {
                if (!TryResolveQuestSO(out questSO))
                {
                    yield return null;
                    continue;
                }

                if (questSO.GetQuestsByType(NyangQuariumQuestType.Merge).Count == 0)
                    yield return null;
            }

            NyangQuariumFishSO fishSO = null;
            while (!TryResolveFishSO(out fishSO) || fishSO.FishData == null || fishSO.FishData.Count == 0)
                yield return null;

            TryResolveExpItemSO(out _expItemSO);
            _rewardQueue = ResolveRewardQueue();

            Task registerTask = NyangQuariumMergeQuestSession.EnsureRegisteredAsync(questSO);
            while (registerTask != null && !registerTask.IsCompleted)
                yield return null;

            if (registerTask != null && registerTask.IsFaulted)
            {

                _initStarted = false;
                yield break;
            }

            if (!NyangQuariumMergeQuestSession.IsRegistered)
            {
                _initStarted = false;
                yield break;
            }

            if (!NyangQuariumFishSpriteCache.IsLoaded)
            {
                if (!NyangQuariumFishSpriteCache.IsLoading)
                    NyangQuariumFishSpriteCache.BeginPreload(this, fishSO);

                float elapsed = 0f;
                const float cacheWaitTimeoutSeconds = 15f;

                while (!NyangQuariumFishSpriteCache.IsLoaded && elapsed < cacheWaitTimeoutSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            List<Transform> questBoardSlots = CollectQuestBoardSlots();
            List<int> activeQuestIds = new(NyangQuariumMergeQuestSession.ActiveQuestIds);
            ShuffleList(activeQuestIds);
            ShuffleList(questBoardSlots);

            int bindCount = Mathf.Min(questBoardSlots.Count, activeQuestIds.Count);

            for (int i = 0; i < bindCount; i++)
            {
                if (!questSO.TryGetQuest(activeQuestIds[i], out NyangQuariumQuestData quest))
                    continue;

                questBoardSlots[i].gameObject.SetActive(true);
                BindQuestSlot(questBoardSlots[i], quest, fishSO, _expItemSO);
            }

            for (int i = bindCount; i < questBoardSlots.Count; i++)
                questBoardSlots[i].gameObject.SetActive(false);

            if (activeQuestIds.Count == 0)
            {
                _initialized = true;
                _initStarted = false;
                yield break;
            }

            if (_expItemSO == null || _expItemSO.DataCount == 0)
                StartCoroutine(ApplyRewardSpritesWhenReady(questSO));

            RefreshCompleteButtons();

            if (_slotBindings.Count == 0)
            {
                DebugTool.Warning(
                    "[NyangQuariumMergeQuestBoardUI] merge 퀘스트 슬롯 바인딩에 실패했습니다.",
                    DebugType.UI,
                    this);
                _initStarted = false;
                yield break;
            }

            _initialized = true;

            DebugTool.Log(
                $"[NyangQuariumMergeQuestBoardUI] merge 퀘스트 보드 바인딩 완료 ({_slotBindings.Count}개)",
                DebugType.UI,
                this);
        }

        private List<Transform> CollectQuestBoardSlots()
        {
            List<Transform> slots = new();

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform)
                    continue;

                string childName = child.name;
                if (childName == QuestBoardNamePrefix || childName.StartsWith(QuestBoardNamePrefix + " "))
                    slots.Add(child);
            }

            return slots;
        }

        private static void ShuffleList<T>(IList<T> list)
        {
            if (list == null || list.Count <= 1)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);

                if (randomIndex == i)
                    continue;

                T swap = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = swap;
            }
        }

        private void BindQuestSlot(
            Transform slotTransform,
            NyangQuariumQuestData quest,
            NyangQuariumFishSO fishSO,
            NyangQuariumExpItemSO expItemSO)
        {
            if (slotTransform == null || quest == null)
                return;

            Transform mergeTargetTransform = FindDirectChildByName(slotTransform, MergeTargetItemName);
            if (mergeTargetTransform == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] MergeTargetItem을 찾지 못했습니다. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                return;
            }

            Image targetImage = mergeTargetTransform.GetComponent<Image>();
            TMP_Text amountText = ResolveMergeTargetAmountText(mergeTargetTransform);

            if (!int.TryParse(quest.QuestCondition1, out int fishId) || fishId <= 0)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 유효하지 않은 조건 ID. Quest:{quest.ID}, Condition:{quest.QuestCondition1}",
                    DebugType.UI,
                    this);
                return;
            }

            if (!TryGetFishData(fishSO, fishId, out NyangQuariumFishData fishData))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 물고기 데이터 없음. Quest:{quest.ID}, FishId:{fishId}",
                    DebugType.UI,
                    this);
                return;
            }

            if (targetImage == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] MergeTargetItem Image 없음. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                return;
            }

            ApplyMergeTargetAmountText(amountText, quest);
            ApplyFishSprite(fishData.FishKey, targetImage);
            ApplyRewardSprite(slotTransform, quest, expItemSO);
            SetupCompleteButton(slotTransform, quest, fishId);
        }

        private void ApplyFishSprite(string fishKey, Image targetImage)
        {
            if (string.IsNullOrWhiteSpace(fishKey) || targetImage == null)
                return;

            targetImage.sprite = null;
            targetImage.enabled = true;
            targetImage.gameObject.SetActive(true);

            Color color = targetImage.color;
            color.a = 1f;
            targetImage.color = color;

            if (NyangQuariumFishSpriteCache.TryGetSprite(fishKey, out Sprite cachedSprite))
            {
                targetImage.sprite = cachedSprite;
                targetImage.preserveAspect = true;
                return;
            }

            UI.UISpriteController controller = new UI.UISpriteController(targetImage);
            controller.ChangeSprite(fishKey);
            _spriteControllers.Add(controller);
        }

        private void ApplyRewardSprite(
            Transform slotTransform,
            NyangQuariumQuestData quest,
            NyangQuariumExpItemSO expItemSO)
        {
            Transform rewardTransform = FindDirectChildByName(slotTransform, RewardImageName);
            if (rewardTransform == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] RewardImage를 찾지 못했습니다. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                return;
            }

            Image rewardImage = rewardTransform.GetComponent<Image>();
            if (rewardImage == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] RewardImage Image 컴포넌트 없음. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                return;
            }

            rewardImage.sprite = null;
            rewardImage.enabled = false;
            rewardImage.preserveAspect = true;

            if (quest == null || !quest.HasReward)
                return;

            if (expItemSO == null || expItemSO.DataCount == 0)
                return;

            if (!expItemSO.TryGetById(quest.QuestRewardId, out NyangQuariumExpItemData expItemData))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 퀘스트 보상 경험치 아이템 데이터 없음. Quest:{quest.ID}, RewardId:{quest.QuestRewardId}",
                    DebugType.UI,
                    this);
                return;
            }

            if (string.IsNullOrWhiteSpace(expItemData.AddressableKey))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 퀘스트 보상 AddressableKey가 비어 있습니다. Quest:{quest.ID}, RewardId:{quest.QuestRewardId}",
                    DebugType.UI,
                    this);
                return;
            }

            rewardImage.enabled = true;
            rewardImage.gameObject.SetActive(true);

            Color color = rewardImage.color;
            color.a = 1f;
            rewardImage.color = color;

            UI.UISpriteController controller = new UI.UISpriteController(rewardImage);
            controller.ChangeSprite(expItemData.AddressableKey);
            _spriteControllers.Add(controller);
        }

        private IEnumerator ApplyRewardSpritesWhenReady(NyangQuariumQuestSO questSO)
        {
            float elapsed = 0f;
            const float waitTimeoutSeconds = 15f;

            while ((_expItemSO == null || _expItemSO.DataCount == 0) &&
                   elapsed < waitTimeoutSeconds)
            {
                TryResolveExpItemSO(out _expItemSO);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_expItemSO == null || _expItemSO.DataCount == 0 || questSO == null)
                yield break;

            for (int i = 0; i < _slotBindings.Count; i++)
            {
                MergeQuestSlotBinding binding = _slotBindings[i];
                if (binding == null ||
                    binding.SlotTransform == null ||
                    !questSO.TryGetQuest(binding.QuestId, out NyangQuariumQuestData quest))
                {
                    continue;
                }

                ApplyRewardSprite(binding.SlotTransform, quest, _expItemSO);
            }
        }

        private void SetupCompleteButton(
            Transform slotTransform,
            NyangQuariumQuestData quest,
            int fishId)
        {
            if (slotTransform == null || quest == null)
                return;

            Transform buttonTransform = FindDirectChildByName(slotTransform, CompleteButtonName);
            if (buttonTransform == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 완료 Button을 찾지 못했습니다. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                return;
            }

            Button completeButton = buttonTransform.GetComponent<Button>();
            if (completeButton == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 완료 Button 컴포넌트 없음. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                return;
            }

            MergeQuestSlotBinding binding = new()
            {
                QuestId = quest.ID,
                FishId = fishId,
                RequiredAmount = quest.ConditionAmount1 > 0 ? quest.ConditionAmount1 : 1,
                SlotTransform = slotTransform,
                CompleteButton = completeButton
            };

            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(() => OnCompleteButtonClicked(binding));
            completeButton.interactable = false;

            _slotBindings.Add(binding);
            RefreshCompleteButton(binding);
        }

        private void OnCompleteButtonClicked(MergeQuestSlotBinding binding)
        {
            if (binding == null || binding.IsCompleted)
                return;

            if (!CanCompleteQuest(binding))
            {
                RefreshCompleteButton(binding);
                return;
            }

            if (!TryCreateQuestRewardItem(binding.QuestId, out NyangQuariumBoardItem rewardItem, out int rewardCount))
            {
                RefreshCompleteButton(binding);
                return;
            }

            if (!NyangQuariumMergeBoardInventoryService.TryConsumeFish(binding.FishId, binding.RequiredAmount))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 퀘스트 조건 아이템 소비 실패. QuestId:{binding.QuestId}, FishId:{binding.FishId}, Amount:{binding.RequiredAmount}",
                    DebugType.UI,
                    this);
                RefreshCompleteButton(binding);
                return;
            }

            EnqueueQuestReward(binding.QuestId, rewardItem, rewardCount);

            binding.IsCompleted = true;
            NyangQuariumMergeQuestSession.RemoveQuest(binding.QuestId);
            _ = NyangQuariumMergeQuestSession.SaveCurrentStateAsync();

            if (binding.CompleteButton != null)
                binding.CompleteButton.onClick.RemoveAllListeners();

            if (binding.SlotTransform != null)
                binding.SlotTransform.gameObject.SetActive(false);

            _slotBindings.Remove(binding);

            DebugTool.Log(
                $"[NyangQuariumMergeQuestBoardUI] merge 퀘스트 완료. QuestId:{binding.QuestId}, FishId:{binding.FishId}, Amount:{binding.RequiredAmount}",
                DebugType.UI,
                this);
        }

        private bool TryCreateQuestRewardItem(
            int questId,
            out NyangQuariumBoardItem rewardItem,
            out int rewardCount)
        {
            rewardItem = null;
            rewardCount = 1;

            if (!TryResolveQuestSO(out NyangQuariumQuestSO questSO) ||
                !questSO.TryGetQuest(questId, out NyangQuariumQuestData quest))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 보상 지급용 퀘스트 데이터를 찾지 못했습니다. QuestId:{questId}",
                    DebugType.UI,
                    this);
                return false;
            }

            if (!quest.HasReward)
                return true;

            if (_rewardQueue == null)
                _rewardQueue = ResolveRewardQueue();

            if (_rewardQueue == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] RewardQueue를 찾지 못해 퀘스트를 완료할 수 없습니다. QuestId:{questId}",
                    DebugType.UI,
                    this);
                return false;
            }

            rewardCount = Mathf.Max(1, quest.RewardAmount);

            if (_expItemSO == null)
                TryResolveExpItemSO(out _expItemSO);

            if (_expItemSO == null ||
                !_expItemSO.TryGetById(quest.QuestRewardId, out NyangQuariumExpItemData expItemData))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 퀘스트 보상 경험치 아이템 데이터 없음. Quest:{quest.ID}, RewardId:{quest.QuestRewardId}",
                    DebugType.UI,
                    this);
                return false;
            }

            ItemData itemData = new(
                expItemData.ItemId,
                expItemData.ItemName,
                Mathf.Max(1, expItemData.ItemLevel),
                ItemType.Common,
                expItemData.AddressableKey);

            rewardItem = new NyangQuariumBoardItem(itemData);
            return rewardItem.HasItem;
        }

        private void EnqueueQuestReward(int questId, NyangQuariumBoardItem rewardItem, int rewardCount)
        {
            if (rewardItem == null || !rewardItem.HasItem)
                return;

            if (_rewardQueue == null)
                _rewardQueue = ResolveRewardQueue();

            if (_rewardQueue == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] RewardQueue를 찾지 못해 퀘스트 보상을 지급하지 못했습니다. QuestId:{questId}",
                    DebugType.UI,
                    this);
                return;
            }

            int safeCount = Mathf.Max(1, rewardCount);
            for (int i = 0; i < safeCount; i++)
                _rewardQueue.EnqueueItem(rewardItem);

            DebugTool.Log(
                $"[NyangQuariumMergeQuestBoardUI] 퀘스트 보상 큐 지급 완료. QuestId:{questId}, RewardId:{rewardItem.Id}, Count:{safeCount}",
                DebugType.UI,
                this);
        }

        private void RefreshCompleteButtons()
        {
            for (int i = 0; i < _slotBindings.Count; i++)
            {
                MergeQuestSlotBinding binding = _slotBindings[i];

                if (binding == null || binding.IsCompleted)
                    continue;

                RefreshCompleteButton(binding);
            }
        }

        private static bool CanCompleteQuest(MergeQuestSlotBinding binding)
        {
            if (binding == null || !NyangQuariumMergeBoardInventoryService.IsReady)
                return false;

            int ownedCount = NyangQuariumMergeBoardInventoryService.GetOwnedFishCount(binding.FishId);
            return ownedCount >= binding.RequiredAmount;
        }

        private static void RefreshCompleteButton(MergeQuestSlotBinding binding)
        {
            if (binding?.CompleteButton == null)
                return;

            binding.CompleteButton.interactable = CanCompleteQuest(binding);
        }

        private static TMP_Text ResolveMergeTargetAmountText(Transform mergeTargetTransform)
        {
            if (mergeTargetTransform == null)
                return null;

            Transform amountTextTransform =
                FindDirectChildByName(mergeTargetTransform, MergeTargetAmountTextName);

            if (amountTextTransform != null)
                return amountTextTransform.GetComponent<TMP_Text>();

            return mergeTargetTransform.GetComponentInChildren<TMP_Text>(true);
        }

        private static void ApplyMergeTargetAmountText(TMP_Text amountText, NyangQuariumQuestData quest)
        {
            if (amountText == null || quest == null)
                return;

            int amount = quest.ConditionAmount1 > 0 ? quest.ConditionAmount1 : 1;
            amountText.text = $"x{amount}";
        }

        private static Transform FindDirectChildByName(Transform root, string objectName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name == objectName)
                    return child;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform child = descendants[i];
                if (child != null && child.name == objectName)
                    return child;
            }

            return null;
        }

        private static bool TryGetFishData(
            NyangQuariumFishSO fishSO,
            int fishId,
            out NyangQuariumFishData fishData)
        {
            fishData = null;

            if (fishSO?.FishData == null)
                return false;

            for (int i = 0; i < fishSO.FishData.Count; i++)
            {
                NyangQuariumFishData candidate = fishSO.FishData[i];
                if (candidate == null || candidate.FishId != fishId)
                    continue;

                fishData = candidate;
                return true;
            }

            return false;
        }

        private static bool TryResolveQuestSO(out NyangQuariumQuestSO questSO)
        {
            return NyangQuariumQuestSOLocator.TryResolveQuestSO(out questSO);
        }

        private static bool TryResolveFishSO(out NyangQuariumFishSO fishSO)
        {
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumFishSO(out fishSO))
                return fishSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            fishSO = quariumLoader != null ? quariumLoader.FishSO : null;
            return fishSO != null;
        }

        private static bool TryResolveExpItemSO(out NyangQuariumExpItemSO expItemSO)
        {
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumExpItemSO(out expItemSO))
                return expItemSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            expItemSO = quariumLoader != null ? quariumLoader.ExpItemSO : null;
            return expItemSO != null;
        }

        private static NyangQuariumRewardQueue ResolveRewardQueue()
        {
            return Object.FindFirstObjectByType<NyangQuariumRewardQueue>();
        }
    }
}
