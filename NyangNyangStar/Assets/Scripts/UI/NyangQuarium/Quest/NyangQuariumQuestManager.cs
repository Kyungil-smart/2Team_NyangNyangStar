using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Managers;
using Data.ScriptableObjects.NyangQuariumSO;
using UnityEngine;

namespace UI.NyangQuarium.Quest
{
    public class NyangQuariumQuestManager : MonoBehaviour
    {
        public static NyangQuariumQuestManager Instance { get; private set; }

        [Header("Quest Data")]
        [SerializeField] private NyangQuariumQuestSO _questSO;

        [Header("Intro Quest")]
        [Tooltip("첫 스토리 종료 후 활성화할 300코인 퀘스트 ID")]
        [SerializeField] private int _introCoinQuestId;

        private int _activeQuestId;
        private readonly HashSet<int> _completedQuestIds = new();

        // 활성 퀘스트 변경 시 알람 UI, 상세 팝업 갱신용
        public event Action<NyangQuariumQuestData> ActiveQuestChanged;

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

        // 첫 스토리 종료 시 스토리 시스템에서 호출
        public void OnIntroStoryFinished()
        {
            DebugTool.Log(
                $"[NyangQuariumQuestManager] 첫 스토리 종료 → 퀘스트 활성화 시도. QuestId:{_introCoinQuestId}",
                DebugType.UI,
                this);

            ActivateQuest(_introCoinQuestId);
        }

        // 특정 퀘스트를 현재 활성 퀘스트로 설정
        public bool ActivateQuest(int questId)
        {
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

            DebugTool.Log(
                $"[NyangQuariumQuestManager] 퀘스트 활성화. ID:{quest.ID}, NameKey:{quest.QuestNameKey}, " +
                $"Condition1:{quest.QuestCondition1} x{quest.ConditionAmount1}",
                DebugType.UI,
                this);

            return true;
        }

        // 현재 활성 퀘스트 조회
        public bool TryGetActiveQuest(out NyangQuariumQuestData quest)
        {
            quest = null;

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

        // 퀘스트 조건 검사 (현재는 코인 조건만 처리)
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

            if (IsCoinCondition(condition))
            {
                bool hasEnough = PlayerResourceManager.Instance.HasEnough(PlayerResourceType.Coin, amount);

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 코인 조건 확인. Need:{amount}, Has:{PlayerResourceManager.Instance.Coin}, Result:{hasEnough}",
                    DebugType.UI,
                    this);

                return hasEnough;
            }

            // 머지 아이템 조건은 MergeBoardItemService 연결 후 처리
            DebugTool.Warning(
                $"[NyangQuariumQuestManager] 아직 지원하지 않는 조건입니다. Condition:{condition}, Amount:{amount}",
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
            _activeQuestId = 0;

            // 알람 UI 숨김 처리
            ActiveQuestChanged?.Invoke(null);

            DebugTool.Log($"[NyangQuariumQuestManager] 퀘스트 완료. ID:{quest.ID}", DebugType.UI, this);
            return true;
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

            if (IsCoinCondition(condition))
            {
                bool spent = await PlayerResourceManager.Instance.TrySpendAsync(PlayerResourceType.Coin, amount);

                DebugTool.Log(
                    $"[NyangQuariumQuestManager] 코인 소비. Amount:{amount}, Success:{spent}, Remain:{PlayerResourceManager.Instance.Coin}",
                    DebugType.UI,
                    this);

                return spent;
            }

            // 머지 아이템 소비는 다음 단계에서 연결
            DebugTool.Warning(
                $"[NyangQuariumQuestManager] 아직 지원하지 않는 조건 소비입니다. Condition:{condition}, Amount:{amount}",
                DebugType.UI,
                this);
            return false;
        }

        private bool IsCoinCondition(string condition)
        {
            return condition.Equals("Coin", StringComparison.OrdinalIgnoreCase) ||
                   condition.Equals("코인", StringComparison.OrdinalIgnoreCase);
        }
    }
}
