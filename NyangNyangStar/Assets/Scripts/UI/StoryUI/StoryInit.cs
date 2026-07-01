using System.Collections;
using UI.NyangQuarium.Quest;
using UnityEngine;

public class StoryInit : MonoBehaviour
{
    [SerializeField] private StoryDataSO _story;
    [SerializeField] private StoryDataSO _afterFirstQuestStory;

    private void Start()
    {
        StartCoroutine(InitializeFlow());
    }

    private IEnumerator InitializeFlow()
    {
        StoryUIController.ShowOnce(_story, HandleIntroStoryFinished);

        while (NyangQuariumQuestManager.Instance == null)
            yield return null;

        NyangQuariumQuestManager.Instance.StoryMapQuestCompleted += HandleStoryMapQuestCompleted;
    }

    private void OnDestroy()
    {
        if (NyangQuariumQuestManager.Instance != null)
            NyangQuariumQuestManager.Instance.StoryMapQuestCompleted -= HandleStoryMapQuestCompleted;
    }

    private void HandleIntroStoryFinished()
    {
        StartCoroutine(ActivateFirstQuestWhenReady());
    }

    private static IEnumerator ActivateFirstQuestWhenReady()
    {
        while (NyangQuariumQuestManager.Instance == null)
            yield return null;

        NyangQuariumQuestManager.Instance.OnIntroStoryFinished();
    }

    private void HandleStoryMapQuestCompleted(int questId)
    {
        int[] mapQuestIds = NyangQuariumStoryQuestMapUI.GetStoryMapQuestIds();

        if (mapQuestIds.Length > 0 && mapQuestIds[0] == questId)
        {
            if (_afterFirstQuestStory == null)
                return;

            StoryUIController.ShowOnce(_afterFirstQuestStory, HandleChapter2Finished);
            return;
        }

        if (mapQuestIds.Length > 1 && mapQuestIds[1] == questId)
        {
            StartCoroutine(ActivateThirdQuestWhenReady());
            return;
        }

        if (mapQuestIds.Length > 2 && mapQuestIds[2] == questId)
            NyangQuariumStoryQuestMapUI.ShowTankTutorial(tierIndex: 1);
    }

    private void HandleChapter2Finished()
    {
        StartCoroutine(ActivateSeconQuestAndTankTutorial01WhenReady());
    }

    private static IEnumerator ActivateSeconQuestAndTankTutorial01WhenReady()
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
