using Data.ScriptableObjects.NyangQuariumSO;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium.Quest
{
    // 맵상 관련 장소 위 퀘스트 알람 UI
    // 퀘스트 여기 있음 하고 띄워 두는 작은 알람 UI 
    public class NyangQuariumQuestAlarmView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Button")]
        [SerializeField] private Button _alarmButton;

        [Header("State Color")]
        [SerializeField] private Image _background;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _completeReadyColor = Color.green;

        private NyangQuariumQuestData _currentQuest;

        private void Awake()
        {
            if (_root == null)
                _root = gameObject;
        }

        private void OnEnable()
        {
            if (NyangQuariumQuestManager.Instance != null)
                NyangQuariumQuestManager.Instance.ActiveQuestChanged += Refresh;

            if (_alarmButton != null)
                _alarmButton.onClick.AddListener(OnClickAlarm);

            RefreshCurrentQuest();
        }

        private void OnDisable()
        {
            if (NyangQuariumQuestManager.Instance != null)
                NyangQuariumQuestManager.Instance.ActiveQuestChanged -= Refresh;

            if (_alarmButton != null)
                _alarmButton.onClick.RemoveListener(OnClickAlarm);
        }

        private void RefreshCurrentQuest()
        {
            if (NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetActiveQuest(out NyangQuariumQuestData quest))
            {
                Refresh(quest);
                return;
            }

            Refresh(null);
        }

        // 활성 퀘스트 있으면 표시, 없으면 숨김
        private void Refresh(NyangQuariumQuestData quest)
        {
            _currentQuest = quest;

            bool hasQuest = _currentQuest != null;

            if (_root != null)
                _root.SetActive(hasQuest);

            if (!hasQuest)
            {
                DebugTool.Log("[NyangQuariumQuestAlarmView] 활성 퀘스트 없음 → 알람 숨김", DebugType.UI, this);
                return;
            }

            bool canComplete = NyangQuariumQuestManager.Instance != null &&
                               NyangQuariumQuestManager.Instance.CanCompleteQuest(_currentQuest);

            if (_background != null)
                _background.color = canComplete ? _completeReadyColor : _normalColor;

            DebugTool.Log(
                $"[NyangQuariumQuestAlarmView] 알람 갱신. QuestId:{_currentQuest.ID}, CanComplete:{canComplete}",
                DebugType.UI,
                this);
        }

        // 알람 클릭 → 퀘스트 상세 팝업 열기
        private void OnClickAlarm()
        {
            if (_currentQuest == null)
            {
                DebugTool.Warning("[NyangQuariumQuestAlarmView] 클릭했지만 표시 중인 퀘스트가 없습니다.", DebugType.UI, this);
                return;
            }

            DebugTool.Log(
                $"[NyangQuariumQuestAlarmView] 알람 클릭. QuestId:{_currentQuest.ID}, NameKey:{_currentQuest.QuestNameKey}",
                DebugType.UI,
                this);

            // TODO: NyangQuariumQuestDetailPopup 연결
        }
    }
}
