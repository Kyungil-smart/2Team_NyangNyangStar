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
        private const float SpriteWaitTimeoutSeconds = 12f;

        private readonly List<MergeQuestSlotBinding> _slotBindings = new();

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

            _slotBindings.Clear();
        }

        public void Init()
        {
            if (_initialized || _initStarted)
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
                yield break;

            if (!NyangQuariumFishSpriteCache.IsLoaded && !NyangQuariumFishSpriteCache.IsLoading)
                NyangQuariumFishSpriteCache.BeginPreload(this, fishSO);

            List<Transform> questBoardSlots = CollectQuestBoardSlots();
            IReadOnlyList<int> activeQuestIds = NyangQuariumMergeQuestSession.ActiveQuestIds;

            for (int i = 0; i < questBoardSlots.Count && i < activeQuestIds.Count; i++)
            {
                if (!questSO.TryGetQuest(activeQuestIds[i], out NyangQuariumQuestData quest))
                    continue;

                yield return BindQuestSlotCoroutine(questBoardSlots[i], quest, fishSO);
            }

            RefreshCompleteButtons();
            _initialized = true;

            DebugTool.Log(
                "[NyangQuariumMergeQuestBoardUI] merge 퀘스트 보드 바인딩 완료",
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

            slots.Sort((a, b) => a.GetComponent<RectTransform>().anchoredPosition.x
                .CompareTo(b.GetComponent<RectTransform>().anchoredPosition.x));

            return slots;
        }

        private IEnumerator BindQuestSlotCoroutine(
            Transform slotTransform,
            NyangQuariumQuestData quest,
            NyangQuariumFishSO fishSO)
        {
            if (slotTransform == null || quest == null)
                yield break;

            Transform mergeTargetTransform = FindDirectChildByName(slotTransform, MergeTargetItemName);
            if (mergeTargetTransform == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] MergeTargetItem을 찾지 못했습니다. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                yield break;
            }

            Image targetImage = mergeTargetTransform.GetComponent<Image>();
            TMP_Text amountText = ResolveMergeTargetAmountText(mergeTargetTransform);

            if (!int.TryParse(quest.QuestCondition1, out int fishId) || fishId <= 0)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 유효하지 않은 조건 ID. Quest:{quest.ID}, Condition:{quest.QuestCondition1}",
                    DebugType.UI,
                    this);
                yield break;
            }

            if (!TryGetFishData(fishSO, fishId, out NyangQuariumFishData fishData))
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 물고기 데이터 없음. Quest:{quest.ID}, FishId:{fishId}",
                    DebugType.UI,
                    this);
                yield break;
            }

            if (targetImage == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] MergeTargetItem Image 없음. Slot:{slotTransform.name}",
                    DebugType.UI,
                    this);
                yield break;
            }

            ApplyMergeTargetAmountText(amountText, quest);

            yield return ApplyFishSpriteCoroutine(fishData.FishKey, targetImage);

            SetupCompleteButton(slotTransform, quest, fishId);
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

        private IEnumerator ApplyFishSpriteCoroutine(string fishKey, Image targetImage)
        {
            if (string.IsNullOrWhiteSpace(fishKey) || targetImage == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < SpriteWaitTimeoutSeconds)
            {
                if (targetImage == null)
                    yield break;

                if (NyangQuariumFishSpriteCache.TryGetSprite(fishKey, out Sprite cachedSprite))
                {
                    ApplySprite(targetImage, cachedSprite);
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (GameManager.Addressable == null)
            {
                DebugTool.Warning(
                    $"[NyangQuariumMergeQuestBoardUI] 스프라이트 로드 실패. Addressable 없음. FishKey:{fishKey}",
                    DebugType.UI,
                    this);
                yield break;
            }

            bool completed = false;

            GameManager.Addressable.LoadSprite(
                fishKey,
                (sprite, _) =>
                {
                    if (targetImage != null && sprite != null)
                        ApplySprite(targetImage, sprite);

                    completed = true;
                },
                failedKey =>
                {
                    DebugTool.Warning(
                        $"[NyangQuariumMergeQuestBoardUI] 스프라이트 로드 실패. FishKey:{failedKey}",
                        DebugType.UI,
                        this);
                    completed = true;
                });

            while (!completed)
                yield return null;
        }

        private static void ApplySprite(Image targetImage, Sprite sprite)
        {
            if (targetImage == null || sprite == null)
                return;

            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
            targetImage.enabled = true;

            Color color = targetImage.color;
            color.a = 1f;
            targetImage.color = color;
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
            SheetLoader sheetLoader = Object.FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumQuestSO(out questSO))
                return questSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            questSO = quariumLoader != null ? quariumLoader.QuestSO : null;
            return questSO != null;
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
