using Core.Managers;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;


public class StoryUIController : UIPopup, IPointerClickHandler
{
    [Header("연결")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _cardPrefab;
    [Tooltip("상대방 대사용 카드 (포트레잇 좌측)")]
    [SerializeField] private GameObject _opponentCardPrefab;
    [SerializeField] private Button _nextButton;
    [Tooltip("챕터 배경 (전체화면). 비어 있으면 배경 변경 안 함")]
    [SerializeField] private Image _backgroundImage;

    [Header("스토리 데이터")]
    [SerializeField] private List<StoryDataSO> _stories = new List<StoryDataSO>();
    [SerializeField] private TMP_Text _titleText;

    [Header("연출")]
    [Tooltip("새 카드가 밀려 올라가는 시간(초)")]
    [SerializeField] private float _pushDuration = 0.25f;

    private Tween _pushTween;
    private UISpriteController _backgroundSprite;

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

        if (_backgroundSprite == null && _backgroundImage != null)
            _backgroundSprite = new UISpriteController(_backgroundImage);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        ShowNextCard();
    }


    public override void ClosePopup()
    {
        _pushTween?.Kill();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _pushTween?.Kill();
        _backgroundSprite?.Dispose();
        if (_instance == this)
            _instance = null;
    }


    [ContextMenu("테스트: 첫 스토리 재생")]
    private void TestPlayFirstStory()
    {
        Init();
        PlayStory(0);
    }


    private static StoryUIController _instance; 

    public static async void ShowOnce(StoryDataSO story, System.Action onFinished = null)
    {
        if (story == null)
        {
            Debug.LogWarning("[StoryUI] ShowOnce: story가 null 입니다.");
            return;
        }

        NyangQuariumFirestoreSO fso = await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (fso != null && fso.HasReadStory(story.storyId))
        {
            onFinished?.Invoke();
            return;
        }

        System.Action combinedOnFinished = () =>
        {
            if (fso != null)
                _ = fso.MarkStoryReadAsync(story.storyId);

            onFinished?.Invoke();
        };

        
        if (_instance != null)
        {
            _instance.gameObject.SetActive(true);
            _instance.transform.SetAsLastSibling();
            _instance.PlayStory(story, combinedOnFinished);
            return;
        }


        GameManager.UI.ShowPopupUI<StoryUIController>(
            KeyContainer.Prefabs.StoryUI,
            popup =>
            {
                _instance = popup;
                popup.PlayStory(story, combinedOnFinished);
            },
            closeMode: UIPopupCloseMode.Hide);
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

        if (_backgroundSprite != null && !string.IsNullOrEmpty(story.backgroundKey))
            _backgroundSprite.ChangeSprite(story.backgroundKey);

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
        _pushTween?.Kill();
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

        _pushTween?.Complete();                  
        float prevHeight = _content.rect.height;

        DialogueCard data = _currentStory.cards[_currentIndex];

        
        GameObject prefab = (data.isOpponent && _opponentCardPrefab != null) ? _opponentCardPrefab : _cardPrefab;
        GameObject card = Instantiate(prefab, _content);

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

        StartCoroutine(AnimatePushUp(card.GetComponent<RectTransform>(), prevHeight));
    }


    private IEnumerator AnimatePushUp(RectTransform newCard, float prevHeight)
    {
        yield return null;                                     
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content); 
        Canvas.ForceUpdateCanvases();

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;     
        Canvas.ForceUpdateCanvases();

        float delta = _content.rect.height - prevHeight;      
        Vector2 endPos = _content.anchoredPosition;        

        if (_scrollRect != null)
            _scrollRect.enabled = false;                      
        _content.anchoredPosition = endPos - new Vector2(0f, delta); 

  
        CanvasGroup cg = null;
        if (newCard != null)
            cg = newCard.GetComponent<CanvasGroup>() ?? newCard.gameObject.AddComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 0f;


        float viewportHeight = 0f;
        if (_scrollRect != null)
        {
            RectTransform vp = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : _scrollRect.transform as RectTransform;
            if (vp != null) viewportHeight = vp.rect.height;
        }
        bool scrollable = _content.rect.height > viewportHeight + 1f;

        Sequence seq = DOTween.Sequence();
        seq.Append(_content.DOAnchorPos(endPos, _pushDuration).SetEase(Ease.OutCubic)); 
        if (cg != null)
            seq.Join(cg.DOFade(1f, _pushDuration));             
        seq.OnComplete(() =>
        {
            _content.anchoredPosition = endPos;                 
            if (_scrollRect != null)
            {
                _scrollRect.enabled = scrollable;               
                if (scrollable)
                    _scrollRect.verticalNormalizedPosition = 0f; 
            }
        });
        _pushTween = seq;
    }
}
