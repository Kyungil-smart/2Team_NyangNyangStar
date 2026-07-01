using Core.Managers;
using Data.Loader;
using Data.ScriptableObjects.NyangQuariumSO;
using System.Collections;
using System.Collections.Generic;
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

        public static void EnsureRegistered(NyangQuariumQuestSO questSO)
        {
            if (_isRegistered)
                return;

            if (questSO == null)
                return;

            List<NyangQuariumQuestData> mergeQuests =
                questSO.GetQuestsByType(NyangQuariumQuestType.Merge);

            if (mergeQuests.Count == 0)
                return;

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

            DebugTool.Log(
                $"[NyangQuariumMergeQuestSession] merge 퀘스트 {pickCount}개 등록: {string.Join(", ", _activeQuestIds)}",
                DebugType.UI);
        }
    }

    // QuestBoardPanel 아래 3개 슬롯에 등록된 merge 퀘스트를 바인딩합니다.
    public sealed class NyangQuariumMergeQuestBoardUI : MonoBehaviour
    {
        private const string QuestBoardNamePrefix = "QuestBoard";
        private const string MergeTargetItemName = "MergeTargetItem";
        private const string MergeTargetAmountTextName = "Text (TMP)";
        private const string CompleteButtonName = "Button";

        private readonly List<MergeQuestSlotBinding> _slotBindings = new();
        private readonly List<UI.UISpriteController> _spriteControllers = new();

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

            NyangQuariumMergeQuestSession.EnsureRegistered(questSO);

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
                BindQuestSlot(questBoardSlots[i], quest, fishSO);
            }

            for (int i = bindCount; i < questBoardSlots.Count; i++)
                questBoardSlots[i].gameObject.SetActive(false);

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
            NyangQuariumFishSO fishSO)
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

            if (!NyangQuariumMergeBoardInventoryService.TryConsumeFish(binding.FishId, binding.RequiredAmount))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 퀘스트 조건 아이템 소비 실패. QuestId:{binding.QuestId}, FishId:{binding.FishId}, Amount:{binding.RequiredAmount}",
                    DebugType.UI,
                    this);
                RefreshCompleteButton(binding);
                return;
            }

            binding.IsCompleted = true;
            NyangQuariumMergeQuestSession.RemoveQuest(binding.QuestId);

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
    }
}
