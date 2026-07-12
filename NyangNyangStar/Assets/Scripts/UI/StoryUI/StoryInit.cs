using System.Collections;
using UI.NyangQuarium.Quest;
using UnityEngine;

/// <summary>
/// 냥쿠아리움 스토리-퀘스트 게이트 오케스트레이터. (스토리 총 5개, 각 최초 1회)
///
/// 게이트 순서:
///   게이트0: 첫 메인 진입          → 스토리1 재생만 (퀘스트 수락 없음)
///   게이트1: 뭉치 버튼 첫 클릭     → 스토리2 → 1번 퀘스트 수락
///   게이트2: 1번 퀘스트 완료       → 스토리3 → 수조 튜토리얼 + 2번 퀘스트 수락
///   게이트3: 2번 퀘스트 완료       → 스토리4 → 3번 퀘스트 수락
///   게이트4: 3번 퀘스트 완료       → 튜토리얼 숨김 + 스토리5
///            (냥쿠아리움 버튼 해금과 뭉치 일러 변경은 MainUI가 StoryQuestProgressChanged로 처리)
///
/// 트리거:
///   1) 퀘스트 팝업의 완료 버튼 → QuestManager.StoryMapQuestCompleted → 게이트2·3·4
///   2) 메인 화면 뭉치 버튼 클릭 → MainUI.MoongchiButtonClicked → 대기 중인 게이트 실행
///      (StoryUIController.ShowOnce는 이미 읽은 스토리를 건너뛰고 종료 콜백을 바로 호출하므로,
///       스토리만 보고 퀘스트 수락이 누락된 상태도 뭉치 버튼으로 복구된다)
///
/// 주의: 각 스토리 SO의 storyId는 반드시 서로 달라야 한다.
///       읽음 처리(NyangQuariumFirestoreSO._readStoryIds)가 storyId 기준이라
///       중복되면 뒤의 스토리가 출력되지 않는다.
/// </summary>
public class StoryInit : MonoBehaviour
{
    [Tooltip("스토리1: 첫 메인 진입 시 자동 재생 (최초 1회)")]
    [SerializeField] private StoryDataSO _story;

    [Tooltip("스토리2: 뭉치 버튼 첫 클릭 시 재생, 종료 시 1번 퀘스트 수락 (최초 1회)")]
    [SerializeField] private StoryDataSO _moongchiIntroStory;

    [Tooltip("스토리3: 1번 퀘스트 완료 후 재생, 종료 시 수조 튜토리얼 + 2번 퀘스트 수락 (최초 1회)")]
    [SerializeField] private StoryDataSO _afterFirstQuestStory;

    [Tooltip("스토리4: 2번 퀘스트 완료 후 재생, 종료 시 3번 퀘스트 수락 (최초 1회)")]
    [SerializeField] private StoryDataSO _afterSecondQuestStory;

    [Tooltip("스토리5: 3번 퀘스트 완료 후 재생 (최초 1회)")]
    [SerializeField] private StoryDataSO _afterThirdQuestStory;

    private MainUI _subscribedMainUI;

    private void Start()
    {
        StartCoroutine(InitializeFlow());
    }

    private IEnumerator InitializeFlow()
    {
        // 게이트0: 첫 진입 스토리는 재생만 하고 퀘스트는 수락하지 않는다.
        // 1번 퀘스트 수락은 뭉치 버튼 클릭(게이트1)에서 진행된다.
        StoryUIController.ShowOnce(_story);

        while (NyangQuariumQuestManager.Instance == null)
            yield return null;

        NyangQuariumQuestManager.Instance.StoryMapQuestCompleted -= HandleStoryMapQuestCompleted;
        NyangQuariumQuestManager.Instance.StoryMapQuestCompleted += HandleStoryMapQuestCompleted;

        while (MainUI.Instance == null)
            yield return null;

        _subscribedMainUI = MainUI.Instance;
        _subscribedMainUI.MoongchiButtonClicked -= HandleMoongchiButtonClicked;
        _subscribedMainUI.MoongchiButtonClicked += HandleMoongchiButtonClicked;
    }

    private void OnDestroy()
    {
        if (NyangQuariumQuestManager.Instance != null)
            NyangQuariumQuestManager.Instance.StoryMapQuestCompleted -= HandleStoryMapQuestCompleted;

        if (_subscribedMainUI != null)
            _subscribedMainUI.MoongchiButtonClicked -= HandleMoongchiButtonClicked;
    }

    // 퀘스트 팝업에서 완료 버튼 → 다음 게이트의 스토리 재생
    private void HandleStoryMapQuestCompleted(int questId)
    {
        int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

        if (mapQuestIds.Length > 0 && mapQuestIds[0] == questId)
        {
            PlayGate(2);
            return;
        }

        if (mapQuestIds.Length > 1 && mapQuestIds[1] == questId)
        {
            PlayGate(3);
            return;
        }

        if (mapQuestIds.Length > 2 && mapQuestIds[2] == questId)
            PlayGate(4);
    }

    // 뭉치 버튼 클릭 → 대기 중인 게이트가 있으면 스토리 재생 후 퀘스트 수락
    private void HandleMoongchiButtonClicked()
    {
        int gateIndex = ResolvePendingGateIndex();

        if (gateIndex < 0)
            return;

        PlayGate(gateIndex);
    }

    // 완료 상태 기준으로 아직 진행되지 않은 게이트를 찾는다.
    // 상태 복원 전이거나 다른 퀘스트 진행 중이면 -1 (동작 없음).
    private static int ResolvePendingGateIndex()
    {
        NyangQuariumQuestManager questManager = NyangQuariumQuestManager.Instance;

        if (questManager == null || !questManager.IsStoryQuestStateRestored)
            return -1;

        int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

        if (mapQuestIds.Length == 0)
            return -1;

        // 1번 퀘스트가 이미 수락된 상태라도 스토리2가 미출력일 수 있으므로 게이트1을 허용한다.
        // (ShowOnce가 읽은 스토리는 건너뛰므로 중복 출력·중복 수락은 일어나지 않는다)
        if (!questManager.IsQuestCompleted(mapQuestIds[0]))
        {
            if (questManager.HasActiveQuest && !IsActiveQuest(questManager, mapQuestIds[0]))
                return -1;

            return 1;
        }

        if (questManager.HasActiveQuest)
            return -1;

        if (mapQuestIds.Length > 1 && !questManager.IsQuestCompleted(mapQuestIds[1]))
            return 2;

        if (mapQuestIds.Length > 2 && !questManager.IsQuestCompleted(mapQuestIds[2]))
            return 3;

        return 4;
    }

    private static bool IsActiveQuest(NyangQuariumQuestManager questManager, int questId)
    {
        return questManager.TryGetActiveQuest(out NyangQuariumQuestData activeQuest) &&
               activeQuest != null &&
               activeQuest.ID == questId;
    }

    // 스토리 SO가 연결되지 않은 게이트는 스토리 없이 수락 동작만 실행한다 (에셋 연결 전 테스트용).
    private void PlayGate(int gateIndex)
    {
        switch (gateIndex)
        {
            case 0:
                StoryUIController.ShowOnce(_story);
                break;

            case 1:
                if (_moongchiIntroStory == null)
                {
                    HandleMoongchiIntroStoryFinished();
                    return;
                }

                StoryUIController.ShowOnce(_moongchiIntroStory, HandleMoongchiIntroStoryFinished);
                break;

            case 2:
                if (_afterFirstQuestStory == null)
                {
                    HandleStory3Finished();
                    return;
                }

                StoryUIController.ShowOnce(_afterFirstQuestStory, HandleStory3Finished);
                break;

            case 3:
                if (_afterSecondQuestStory == null)
                {
                    HandleStory4Finished();
                    return;
                }

                StoryUIController.ShowOnce(_afterSecondQuestStory, HandleStory4Finished);
                break;

            case 4:
                NyangQuariumStoryQuestMapUI.HideTankTutorial(tierIndex: 0);

                if (_afterThirdQuestStory != null)
                    StoryUIController.ShowOnce(_afterThirdQuestStory);
                break;
        }
    }

    // 스토리2 종료 → 1번 퀘스트 수락
    private void HandleMoongchiIntroStoryFinished()
    {
        StartCoroutine(ActivateFirstQuestWhenReady());
    }

    // 스토리3 종료 → 수조 튜토리얼 + 2번 퀘스트 수락
    private void HandleStory3Finished()
    {
        StartCoroutine(ActivateSecondQuestAndTankTutorial01WhenReady());
    }

    // 스토리4 종료 → 3번 퀘스트 수락
    private void HandleStory4Finished()
    {
        StartCoroutine(ActivateThirdQuestWhenReady());
    }

    private static IEnumerator ActivateFirstQuestWhenReady()
    {
        while (NyangQuariumQuestManager.Instance == null)
            yield return null;

        NyangQuariumQuestManager.Instance.OnIntroStoryFinished();
    }

    private static IEnumerator ActivateSecondQuestAndTankTutorial01WhenReady()
    {
        while (NyangQuariumQuestManager.Instance == null)
            yield return null;

        NyangQuariumStoryQuestMapUI.ShowTankTutorial(tierIndex: 0);
        NyangQuariumQuestManager.Instance.ActivateStoryMapQuestAtSlot(slotIndex: 1);
    }

    private static IEnumerator ActivateThirdQuestWhenReady()
    {
        while (NyangQuariumQuestManager.Instance == null)
            yield return null;

        NyangQuariumQuestManager.Instance.ActivateStoryMapQuestAtSlot(slotIndex: 2);
    }
}
