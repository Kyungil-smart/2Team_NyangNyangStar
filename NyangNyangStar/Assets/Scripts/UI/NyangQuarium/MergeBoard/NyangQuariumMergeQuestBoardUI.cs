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
        private const float SpriteWaitTimeoutSeconds = 12f;

        private bool _initialized;
        private bool _initStarted;

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
            TMP_Text amountText = mergeTargetTransform.GetComponentInChildren<TMP_Text>(true);

            if (amountText != null && quest.ConditionAmount1 > 0)
                amountText.text = $"x{quest.ConditionAmount1}";

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

            yield return ApplyFishSpriteCoroutine(fishData.FishKey, targetImage);
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
