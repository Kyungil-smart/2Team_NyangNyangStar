using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;


public class StoryUIController : UIPopup
{
    [Header("연결")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Button _nextButton;

    [Header("스토리 데이터")]
    [SerializeField] private List<StoryDataSO> _stories = new List<StoryDataSO>();
    [SerializeField] private TMP_Text _titleText;

    private StoryDataSO _currentStory;
    private int _currentStoryIndex = -1;
    private int _currentIndex = 0;
    private bool _reachedEnd = false;
    private System.Action _onFinished; 


  
    public override void Init()
    {
        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(ShowNextCard);
            _nextButton.onClick.AddListener(ShowNextCard);
        }
    }

    // 다른 토글형 팝업처럼: 닫을 때 파괴(Release)하지 않고 비활성화만 한다.
    // (기본 ClosePopup은 ClosePopupUI→TryReleasePrefab로 파괴되어 다른 팝업에 영향)
    public override void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }


    [ContextMenu("테스트: 첫 스토리 재생")]
    private void TestPlayFirstStory()
    {
        Init();
        PlayStory(0);
    }


    private static StoryUIController _instance; // 한 번 생성 후 재사용 (닫을 때 비활성화만 하므로 살아있음)

    public static async void ShowOnce(StoryDataSO story)
    {
        if (story == null)
        {
            Debug.LogWarning("[StoryUI] ShowOnce: story가 null 입니다.");
            return;
        }

        NyangQuariumFirestoreSO fso = await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (fso != null && fso.HasReadStory(story.storyId))
            return; 

        System.Action onFinished = () =>
        {
            if (fso != null)
                _ = fso.MarkStoryReadAsync(story.storyId);
        };

        // 이미 만들어둔 인스턴스가 있으면 재사용 (닫을 때 비활성화만 하므로 살아있음)
        if (_instance != null)
        {
            _instance.gameObject.SetActive(true);
            _instance.transform.SetAsLastSibling();
            _instance.PlayStory(story, onFinished);
            return;
        }

        // 처음 한 번만 생성
        GameManager.UI.ShowPopupUI<StoryUIController>(
            KeyContainer.Prefabs.StoryUI,
            popup =>
            {
                _instance = popup;
                popup.PlayStory(story, onFinished);
            });
    }

    public void PlayStory(StoryDataSO story) => PlayStory(story, null);


    public void PlayStory(StoryDataSO story, System.Action onFinished)
    {
        if (story == null)
        {
            Debug.LogWarning("[StoryUI] PlayStory: story가 null 입니다.");
            return;
        }

        _onFinished = onFinished;
        _currentStory = story;
        _currentIndex = 0;
        _reachedEnd = false;

        ClearCards();

        if (_titleText != null)
            _titleText.text = story.title;

        ShowNextCard();
    }


    public void PlayStory(int index)
    {
        if (index < 0 || index >= _stories.Count)
        {
            Debug.LogWarning($"[StoryUI] 재생할 스토리가 없습니다. index={index}");
            return;
        }

        _currentStoryIndex = index;
        PlayStory(_stories[index]);
    }

    public void PlayNextStory()
    {
        PlayStory(_currentStoryIndex + 1);
    }

    private void ClearCards()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
    }

    public void ShowNextCard()
    {
        if (_cardPrefab == null || _content == null)
        {
            Debug.LogWarning("[StoryUI] _cardPrefab 또는 _content가 비어 있습니다. 인스펙터를 확인하세요.");
            return;
        }

        if (_currentStory == null)
            return;


        if (_reachedEnd)
        {
            ClosePopup();
            _onFinished?.Invoke(); 
            return;
        }

        if (_currentIndex >= _currentStory.cards.Count)
        {
            Debug.Log("[StoryUI] 더 출력할 대사가 없습니다.");
            return;
        }

        DialogueCard data = _currentStory.cards[_currentIndex];

        GameObject card = Instantiate(_cardPrefab, _content);

        StoryDialogueCardView view = card.GetComponent<StoryDialogueCardView>();
        if (view != null)
            view.Setup(data.char_name, data.dialogue);
        else
            Debug.LogWarning("[StoryUI] 카드 프리팹에 StoryDialogueCardView가 없습니다.");

        _currentIndex++;

        if (data.end)
        {
            _reachedEnd = true;
            Debug.Log("[StoryUI] 중단점 도달 — 대사 출력 종료.");
        }

        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
