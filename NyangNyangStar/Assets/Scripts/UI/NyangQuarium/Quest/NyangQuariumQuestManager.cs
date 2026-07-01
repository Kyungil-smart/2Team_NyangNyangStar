using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Managers;
using Data.Loader;
using Data.ScriptableObjects.NyangQuariumSO;
using UI.MergeBoard;
using UnityEngine;

namespace UI.NyangQuarium.Quest
{
    public class NyangQuariumQuestManager : MonoBehaviour
    {
        
        // NyangQuariumQuestManager -> 퀘스트 상태 관리
        public static NyangQuariumQuestManager Instance { get; private set; }

        [Header("Quest Data")]
        [SerializeField] private NyangQuariumQuestSO _questSO;

        [Header("Intro Quest")]
        [Tooltip("첫 스토리 종료 후 활성화할 300코인 퀘스트 ID")]
        [SerializeField] private int _introCoinQuestId;

        private int _activeQuestId;
        private readonly HashSet<int> _completedQuestIds = new();
        private bool _storyQuestInitialized;

        // 활성 퀘스트 변경 시 알람 UI, 상세 팝업 갱신용
        public event Action<NyangQuariumQuestData> ActiveQuestChanged;

        // Story 퀘스트 완료/슬롯 전환 시 맵 UI 갱신용
        public event Action StoryQuestProgressChanged;

        // Story 맵 퀘스트(FirstQuest 등) 클리어 시 스토리 연출용
        public event Action<int> StoryMapQuestCompleted;

        public bool HasActiveQuest => _activeQuestId > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            EnsureStoryQuestInitialized();
        }

        public bool IsQuestCompleted(int questId)
            => questId > 0 && _completedQuestIds.Contains(questId);

        public bool TryGetQuest(int questId, out NyangQuariumQuestData quest)
        {
            quest = null;
            ResolveQuestSO();

            if (_questSO == null || questId <= 0)
                return false;

            return _questSO.TryGetQuest(questId, out quest);
        }

        public void EnsureStoryQuestInitialized()
        {
            if (_storyQuestInitialized)
                return;

            _activeQuestId = 0;
            _storyQuestInitialized = true;

            DebugTool.Log(
                "[NyangQuariumQuestManager] Story 맵 퀘스트 대기 — 첫 스토리 종료 후 FirstQuest 활성화",
                DebugType.UI,
                this);

            StoryQuestProgressChanged?.Invoke();
        }

        public bool TryGetActiveStoryMapSlotIndex(out int slotIndex)
        {
            slotIndex = -1;

            if (_activeQuestId <= 0)
                return false;

            int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

            for (int i = 0; i < mapQuestIds.Length; i++)
            {
                if (mapQuestIds[i] != _activeQuestId)
                    continue;

                slotIndex = i;
                return true;
            }

            return false;
        }

        public bool TryGetActiveStoryQuestSlotIndex(out int slotIndex)
        {
            slotIndex = -1;
            ResolveQuestSO();

            if (_questSO == null || _activeQuestId <= 0)
                return false;

            if (!TryGetStoryQuestIndexById(_activeQuestId, out slotIndex))
                return false;

            return slotIndex >= 0;
        }

        private bool TryGetStoryQuestIndexById(int questId, out int index)
        {
            index = -1;
            ResolveQuestSO();

            if (_questSO == null || questId <= 0)
                return false;

            List<NyangQuariumQuestData> storyQuests =
                _questSO.GetQuestsByType(NyangQuariumQuestType.Story);

            storyQuests.Sort((a, b) => a.ID.CompareTo(b.ID));

            for (int i = 0; i < storyQuests.Count; i++)
            {
                if (storyQuests[i].ID != questId)
                    continue;

                index = i;
                return true;
            }

            return false;
        }

        // 첫 스토리(Chapter1) 종료 시 StoryInit에서 호출
        public void OnIntroStoryFinished()
        {
            int questId = ResolveFirstStoryMapQuestId();

            DebugTool.Log(
                $"[NyangQuariumQuestManager] 첫 스토리 종료 → FirstQuest 활성화. QuestId:{questId}",
                DebugType.UI,
                this);

            if (questId > 0)
                ActivateQuest(questId);
        }

        private static int ResolveFirstStoryMapQuestId()
        {
            int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();
            return mapQuestIds.Length > 0 ? mapQuestIds[0] : 0;
        }

        public bool ActivateStoryMapQuestAtSlot(int slotIndex)
        {
            int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

            if (slotIndex < 0 || slotIndex >= mapQuestIds.Length)
                return false;

            return ActivateQuest(mapQuestIds[slotIndex]);
        }

        // 특정 퀘스트를 현재 활성 퀘스트로 설정
        public bool ActivateQuest(int questId)
        {
            ResolveQuestSO();

            if (_questSO == null)
            {
                DebugTool.Warning("[NyangQuariumQuestManager] QuestSO가 연결되지 않았습니다.", DebugType.UI, this);
                return false;
            }

            if (questId <= 0)
            {
                DebugTool.Warning($"[NyangQuariumQuestManager] 유효하지 않은 퀘스트 ID입니다. ID:{questId}", DebugType.UI, this);
                return false;
            }

            if (_completedQuestIds.Contains(questId))
            {
                DebugTool.Log($"[NyangQuariumQuestManager] 이미 완료한 퀘스트입니다. ID:{questId}", DebugType.UI, this);
                return false;
            }

            if (!_questSO.TryGetQuest(questId, out NyangQuariumQuestData quest))
            {
                DebugTool.Warning($"[NyangQuariumQuestManager] 퀘스트를 찾을 수 없습니다. ID:{questId}", DebugType.UI, this);
                return false;
            }

            // 선행 퀘스트 완료 여부 확인
            if (quest.HasPreQuest && !_completedQuestIds.Contains(quest.PreQuestId))
            {
                DebugTool.Warning(
                    $"[NyangQuariumQuestManager] 선행 퀘스트가 완료되지 않았습니다. QuestId:{questId}, PreQuest:{quest.PreQuestId}",
                    DebugType.UI,
                    this);
                return false;
            }

            _activeQuestId = quest.ID;
            ActiveQuestChanged?.Invoke(quest);
            StoryQuestProgressChanged?.Invoke();

            DebugTool.Log(
                $"[NyangQuariumQuestManager] 퀘스트 활성화. ID:{quest.ID}, NameKey:{quest.QuestNameKey}, " +
                $"Condition1:{quest.QuestCondition1} x{quest.ConditionAmount1}",
                DebugType.UI,
                this);

            return true;
        }

        // Story 타입 퀘스트를 ID 순으로 조회 (0 = 첫 번째 Story 퀘스트)
        public bool TryGetStoryQuestAt(int index, out NyangQuariumQuestData quest)
        {
            quest = null;
            ResolveQuestSO();

            if (_questSO == null)
                return false;

            List<NyangQuariumQuestData> storyQuests =
                _questSO.GetQuestsByType(NyangQuariumQuestType.Story);

            if (storyQuests.Count == 0)
                return false;

            storyQuests.Sort((a, b) => a.ID.CompareTo(b.ID));

            if (index < 0 || index >= storyQuests.Count)
                return false;

            quest = storyQuests[index];
            return true;
        }

        // Story 퀘스트 표시용 순번 (1부터 시작)
        public bool TryGetStoryQuestDisplayIndex(NyangQuariumQuestData quest, out int displayIndex)
        {
            displayIndex = 0;
            ResolveQuestSO();

            if (_questSO == null || quest == null)
                return false;

            List<NyangQuariumQuestData> storyQuests =
                _questSO.GetQuestsByType(NyangQuariumQuestType.Story);

            if (storyQuests.Count == 0)
                return false;

            storyQuests.Sort((a, b) => a.ID.CompareTo(b.ID));

            for (int i = 0; i < storyQuests.Count; i++)
            {
                if (storyQuests[i].ID != quest.ID)
                    continue;

                displayIndex = i + 1;
                return true;
            }

            return false;
        }

        // 현재 활성 퀘스트 조회
        public bool TryGetActiveQuest(out NyangQuariumQuestData quest)
        {
            quest = null;
            ResolveQuestSO();

            if (_questSO == null || _activeQuestId <= 0)
                return false;

            return _questSO.TryGetQuest(_activeQuestId, out quest);
        }

        // 현재 활성 퀘스트 완료 가능 여부
        public bool CanCompleteActiveQuest()
        {
            if (!TryGetActiveQuest(out NyangQuariumQuestData quest))
                return false;

            return CanCompleteQuest(quest);
        }

        // 단일 조건(아이템/코인) 보유 여부 — 상세 팝업 조건 아이콘 체크 표시용
        public bool HasConditionResource(string condition, int amount)
        {
            return CanCompleteCondition(condition, amount);
        }

        // 퀘스트 조건 검사
        public bool CanCompleteQuest(NyangQuariumQuestData quest)
        {
            if (quest == null)
                return false;

            if (!CanCompleteCondition(quest.QuestCondition1, quest.ConditionAmount1))
                return false;

            if (quest.HasCondition2 &&
                !CanCompleteCondition(quest.QuestCondition2, quest.ConditionAmount2))
            {
                return false;
            }

            return true;
        }

        private bool CanCompleteCondition(string condition, int amount)
        {
            if (string.IsNullOrWhiteSpace(condition) || amount <= 0)
                return true;

            if (NyangQuariumQuestConditionUtil.IsCoinCondition(condition))
            {
                bool hasEnough = PlayerResourceManager.Instance.HasEnough(PlayerResourceType.Coin, amount);

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 코인 조건 확인. Need:{amount}, Has:{PlayerResourceManager.Instance.Coin}, Result:{hasEnough}",
                    DebugType.UI,
                    this);

                return hasEnough;
            }

            if (int.TryParse(condition, out int itemId))
            {
                int owned = GetOwnedMergeItemCount(itemId);
                bool hasEnough = owned >= amount;

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 아이템 조건 확인. ItemId:{itemId}, Need:{amount}, Has:{owned}, Result:{hasEnough}",
                    DebugType.UI,
                    this);

                return hasEnough;
            }

            DebugTool.Warning(
                $"[NyangQuariumQuestManager] 지원하지 않는 조건입니다. Condition:{condition}, Amount:{amount}",
                DebugType.UI,
                this);
            return false;
        }

        // 현재 활성 퀘스트 완료 (코인 차감 포함)
        public async Task<bool> CompleteActiveQuestAsync()
        {
            if (!TryGetActiveQuest(out NyangQuariumQuestData quest))
            {
                DebugTool.Warning("[NyangQuariumQuestManager] 활성 퀘스트가 없어 완료할 수 없습니다.", DebugType.UI, this);
                return false;
            }

            return await CompleteQuestAsync(quest);
        }

        public async Task<bool> CompleteQuestAsync(NyangQuariumQuestData quest)
        {
            if (quest == null)
            {
                DebugTool.Warning("[NyangQuariumQuestManager] 완료할 퀘스트가 없습니다.", DebugType.UI, this);
                return false;
            }

            if (IsQuestCompleted(quest.ID))
            {
                DebugTool.Log($"[NyangQuariumQuestManager] 이미 완료한 퀘스트입니다. ID:{quest.ID}", DebugType.UI, this);
                return false;
            }

            if (!CanCompleteQuest(quest))
            {
                DebugTool.Log($"[NyangQuariumQuestManager] 완료 조건 미충족. QuestId:{quest.ID}", DebugType.UI, this);
                return false;
            }

            bool consumed = await ConsumeQuestConditionsAsync(quest);

            if (!consumed)
            {
                DebugTool.Warning($"[NyangQuariumQuestManager] 조건 소비 실패. QuestId:{quest.ID}", DebugType.UI, this);
                return false;
            }

            _completedQuestIds.Add(quest.ID);

            if (IsStoryMapQuestId(quest.ID))
                StoryMapQuestCompleted?.Invoke(quest.ID);

            if (TryAdvanceStoryMapQuest(quest.ID))
            {
                DebugTool.Log(
                    $"[NyangQuariumQuestManager] Story 맵 퀘스트 완료 → 다음 슬롯 활성화. Completed:{quest.ID}, Next:{_activeQuestId}",
                    DebugType.UI,
                    this);
            }
            else if (TryGetStoryQuestIndexById(quest.ID, out int completedIndex) &&
                TryGetStoryQuestAt(completedIndex + 1, out NyangQuariumQuestData nextStoryQuest))
            {
                _activeQuestId = nextStoryQuest.ID;

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] Story 퀘스트 완료 → 다음 퀘스트 활성화. Completed:{quest.ID}, Next:{nextStoryQuest.ID}, Slot:{completedIndex + 1}",
                    DebugType.UI,
                    this);
            }
            else
            {
                if (_activeQuestId == quest.ID)
                    _activeQuestId = 0;

                ActiveQuestChanged?.Invoke(null);

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 퀘스트 완료. ID:{quest.ID}, NextStory:false",
                    DebugType.UI,
                    this);
            }

            StoryQuestProgressChanged?.Invoke();
            return true;
        }

        private static bool IsStoryMapQuestId(int questId)
        {
            if (questId <= 0)
                return false;

            int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

            for (int i = 0; i < mapQuestIds.Length; i++)
            {
                if (mapQuestIds[i] == questId)
                    return true;
            }

            return false;
        }

        private bool TryAdvanceStoryMapQuest(int completedQuestId)
        {
            int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

            if (mapQuestIds == null || mapQuestIds.Length == 0)
                return false;

            for (int i = 0; i < mapQuestIds.Length; i++)
            {
                if (mapQuestIds[i] != completedQuestId)
                    continue;

                if (_activeQuestId == completedQuestId)
                    _activeQuestId = 0;

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] Story 맵 퀘스트 완료 — 다음 슬롯은 스토리/튜토리얼 게이트 후 활성화. Completed:{completedQuestId}",
                    DebugType.UI,
                    this);

                return true;
            }

            return false;
        }

        private async Task<bool> ConsumeQuestConditionsAsync(NyangQuariumQuestData quest)
        {
            if (!await ConsumeConditionAsync(quest.QuestCondition1, quest.ConditionAmount1))
                return false;

            if (quest.HasCondition2 &&
                !await ConsumeConditionAsync(quest.QuestCondition2, quest.ConditionAmount2))
            {
                return false;
            }

            return true;
        }

        private async Task<bool> ConsumeConditionAsync(string condition, int amount)
        {
            if (string.IsNullOrWhiteSpace(condition) || amount <= 0)
                return true;

            if (NyangQuariumQuestConditionUtil.IsCoinCondition(condition))
            {
                bool spent = await PlayerResourceManager.Instance.TrySpendAsync(PlayerResourceType.Coin, amount);

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 코인 소비. Amount:{amount}, Success:{spent}, Remain:{PlayerResourceManager.Instance.Coin}",
                    DebugType.UI,
                    this);

                return spent;
            }

            if (int.TryParse(condition, out int itemId))
            {
                if (MergeBoardItemService.Instance == null)
                {
                    DebugTool.Warning(
                        $"[NyangQuariumQuestManager] MergeBoardItemService 없음. ItemId:{itemId}",
                        DebugType.UI,
                        this);
                    return false;
                }

                bool consumed = await MergeBoardItemService.Instance.ConsumeItemByIdAsync(itemId, amount);

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 아이템 소비. ItemId:{itemId}, Amount:{amount}, Success:{consumed}",
                    DebugType.UI,
                    this);

                return consumed;
            }

            DebugTool.Warning(
                $"[NyangQuariumQuestManager] 지원하지 않는 조건 소비입니다. Condition:{condition}, Amount:{amount}",
                DebugType.UI,
                this);
            return false;
        }

        private static int GetOwnedMergeItemCount(int itemId)
        {
            return MergeBoardItemService.Instance != null
                ? MergeBoardItemService.Instance.GetOwnedItemCount(itemId)
                : 0;
        }

        // SheetLoader / NyangQuariumSheetLoader(TestLoader)에서 같은 SO를 채우므로, 비어 있으면 로더에서 가져옴
        private void ResolveQuestSO()
        {
            if (_questSO != null)
                return;

            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumQuestSO(out NyangQuariumQuestSO questSO))
            {
                _questSO = questSO;
                return;
            }

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;

            if (quariumLoader != null)
                _questSO = quariumLoader.QuestSO;
        }
    }
}
