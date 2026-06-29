using Data.ScriptableObjects.NyangQuariumSO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium.Quest
{
    // NyangQuariumQuestPopUp — 텍스트·슬라이더·버튼 등 화면 갱신 partial
    public sealed partial class NyangQuariumQuestPopUp
    {
        private const string CompleteLabel = "완료";
        private const string FindLabel = "찾기";
        private const string StoryThemeLabel = "냥쿠아리움";

        // 바인딩된 퀘스트 기준으로 팝업 전체 UI 갱신
        private void RefreshView()
        {
            if (_boundQuest == null)
                return;

            RefreshStoryAndChapter();
            RefreshQuestInfo();
            RefreshReward();
            RefreshActionButtons();
        }

        // Story / Main 퀘스트에 따라 테마·챕터 영역 표시
        private void RefreshStoryAndChapter()
        {
            if (_boundQuest.QuestType == NyangQuariumQuestType.Story)
            {
                RefreshStoryQuestHeader();
                return;
            }

            bool isMainQuest = _boundQuest.QuestSection == NyangQuariumQuestSection.Main;
            bool hasTheme = isMainQuest && !string.IsNullOrWhiteSpace(_storyThemeName);

            if (_storyThemeRoot != null)
                _storyThemeRoot.SetActive(hasTheme);

            if (_storyThemeText != null && hasTheme)
                _storyThemeText.text = _storyThemeName;

            bool hasChapter = isMainQuest && !string.IsNullOrWhiteSpace(_chapterTitle);

            if (_chapterProgressRoot != null)
                _chapterProgressRoot.SetActive(hasChapter);

            if (!hasChapter)
                return;

            if (_chapterTitleText != null)
                _chapterTitleText.text = _chapterTitle;

            if (_chapterProgressSlider != null)
                _chapterProgressSlider.value = _chapterProgress;

            if (_chapterProgressText != null)
                _chapterProgressText.text = $"{Mathf.RoundToInt(_chapterProgress * 100f)}%";
        }

        // Story 퀘스트 전용 헤더 (테마명 + N 퀘스트명)
        private void RefreshStoryQuestHeader()
        {
            string questName = ResolveQuestNameText();

            if (_storyThemeRoot != null)
                _storyThemeRoot.SetActive(true);

            if (_storyThemeText != null)
                _storyThemeText.text = StoryThemeLabel;

            if (_chapterProgressRoot != null)
                _chapterProgressRoot.SetActive(true);

            if (_chapterTitleText != null)
            {
                int displayIndex = ResolveStoryQuestDisplayIndex();
                _chapterTitleText.text = $"{displayIndex}. {questName}";
            }

            if (_chapterProgressSlider != null)
                _chapterProgressSlider.value = 0f;

            if (_chapterProgressText != null)
                _chapterProgressText.text = "0%";
        }

        // QuestStringSO에서 퀘스트 이름 로컬라이즈 텍스트 조회
        private string ResolveQuestNameText()
        {
            if (_boundQuest == null)
                return string.Empty;

            NyangQuariumQuestStringSO stringSO = NyangQuariumQuestSOLocator.ResolveQuestStringSO();

            return stringSO != null
                ? stringSO.GetStringOrKey(_boundQuest.QuestNameKey)
                : _boundQuest.QuestNameKey;
        }

        // Story 퀘스트 표시용 순번 (1, 2, 3 …)
        private int ResolveStoryQuestDisplayIndex()
        {
            if (_boundQuest != null &&
                NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetStoryQuestDisplayIndex(
                    _boundQuest,
                    out int displayIndex))
            {
                return displayIndex;
            }

            return 1;
        }

        // 퀘스트명·조건 아이콘·완료 체크 마크 갱신
        private void RefreshQuestInfo()
        {
            if (_questNameText != null)
                _questNameText.text = ResolveQuestNameText();

            bool hasRequiredResource = CanCompleteBoundQuest();

            if (_conditionCompleteMark != null)
                _conditionCompleteMark.SetActive(hasRequiredResource);

            ApplyConditionIcon(_boundQuest.QuestCondition1);
        }

        // 보상 아이콘·수량 표시
        private void RefreshReward()
        {
            if (_rewardRoot != null)
                _rewardRoot.SetActive(true);

            ClearIcon(_rewardIconPrimary);

            if (_rewardPrimaryText != null)
                _rewardPrimaryText.text = string.Empty;

            if (!_boundQuest.HasReward)
                return;

            NyangQuariumQuestRewardSO rewardSO = NyangQuariumQuestSOLocator.ResolveQuestRewardSO();
            int primaryAmount = _boundQuest.RewardAmount;

            if (rewardSO != null &&
                rewardSO.TryGetReward(_boundQuest.QuestRewardId, out NyangQuariumQuestRewardData rewardData))
            {
                if (rewardData.HasRewardItem1)
                {
                    ApplyItemIcon(rewardData.RewardItem1, _rewardIconPrimary);
                    primaryAmount = rewardData.RewardAmount1;
                }
                else if (rewardData.HasExpReward)
                {
                    ApplyExpRewardIcon();
                    primaryAmount = rewardData.ExpAmount;
                }
            }

            if (_rewardPrimaryText != null)
                _rewardPrimaryText.text = primaryAmount > 0 ? primaryAmount.ToString() : string.Empty;
        }

        // 조건 충족 -> 완료 버튼 + 알림 뱃지, 미충족 -> 찾기 버튼
        private void RefreshActionButtons()
        {
            bool canComplete = CanCompleteBoundQuest();

            if (_completeButton != null)
                _completeButton.gameObject.SetActive(canComplete);

            if (_findButton != null)
                _findButton.gameObject.SetActive(!canComplete);

            if (_alertBadge != null)
                _alertBadge.SetActive(canComplete);

            TMP_Text completeText = _completeButton != null
                ? _completeButton.GetComponentInChildren<TMP_Text>(true)
                : null;

            if (completeText != null)
                completeText.text = CompleteLabel;

            TMP_Text findText = _findButton != null
                ? _findButton.GetComponentInChildren<TMP_Text>(true)
                : null;

            if (findText != null)
                findText.text = FindLabel;
        }

        private bool CanCompleteBoundQuest()
        {
            if (_boundQuest == null)
                return false;

            return NyangQuariumStoryQuestMapUI.CanCompleteStoryQuest(_boundQuest);
        }
    }
}
