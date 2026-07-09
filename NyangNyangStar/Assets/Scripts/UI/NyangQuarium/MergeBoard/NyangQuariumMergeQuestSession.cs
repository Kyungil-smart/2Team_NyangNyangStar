using Data.ScriptableObjects.NyangQuariumSO;
using Services.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.NyangQuarium.Quest;
using UnityEngine;

namespace UI.NyangQuarium.MergeBoard
{
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
                    int restoredCount;

                    RestoreRegisteredQuests(store.ActiveMergeQuestBoardQuestIds, questSO);
                    restoredCount = _activeQuestIds.Count;
                    FillOpenSlots(questSO);
                    _isRegistered = true;

                    if (_activeQuestIds.Count != restoredCount)
                        await SaveCurrentStateAsync();

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

        public static bool TryRegisterRandomQuest(
            NyangQuariumQuestSO questSO,
            out NyangQuariumQuestData quest,
            int excludedQuestId = 0)
        {
            quest = null;

            if (questSO == null)
                return false;

            List<NyangQuariumQuestData> mergeQuests =
                questSO.GetQuestsByType(NyangQuariumQuestType.Merge);

            if (mergeQuests.Count == 0)
                return false;

            List<NyangQuariumQuestData> candidates = new();
            NyangQuariumQuestData excludedCandidate = null;

            for (int i = 0; i < mergeQuests.Count; i++)
            {
                NyangQuariumQuestData candidate = mergeQuests[i];
                if (candidate == null || candidate.ID <= 0)
                    continue;

                if (_activeQuestIds.Contains(candidate.ID))
                    continue;

                if (candidate.ID == excludedQuestId)
                {
                    excludedCandidate = candidate;
                    continue;
                }

                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
            {
                if (excludedCandidate == null)
                    return false;

                candidates.Add(excludedCandidate);
            }

            quest = candidates[Random.Range(0, candidates.Count)];
            _activeQuestIds.Add(quest.ID);
            return true;
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

        private static void FillOpenSlots(NyangQuariumQuestSO questSO)
        {
            while (_activeQuestIds.Count < ActiveSlotCount &&
                   TryRegisterRandomQuest(questSO, out _))
            {
            }
        }
    }
}
