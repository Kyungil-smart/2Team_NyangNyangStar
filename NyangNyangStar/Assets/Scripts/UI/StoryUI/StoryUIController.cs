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
    private VerticalLayoutGroup _contentLayout;
    private int _baseTopPadding;

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

        if (_contentLayout == null && _content != null)
        {
            _contentLayout = _content.GetComponent<VerticalLayoutGroup>();
            if (_contentLayout != null)
                _baseTopPadding = _contentLayout.padding.top;
        }
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

        ApplyBackground(story);

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


    private void ApplyBackground(StoryDataSO story)
    {
        if (_backgroundImage == null)
            return;

        if (string.IsNullOrEmpty(story.backgroundKey))
        {
            SetBackgroundAlpha(0f);            
            _backgroundSprite?.ClearSprite();  
            return;
        }

        SetBackgroundAlpha(0f);              
        _backgroundSprite?.ChangeSprite(
            story.backgroundKey,
            onLoaded: () =>
            {
                if (_backgroundImage != null && _backgroundImage.sprite != null)
                    SetBackgroundAlpha(1f);   
            });
    }


    private void SetBackgroundAlpha(float alpha)
    {
        if (_backgroundImage == null)
            return;

        Color c = _backgroundImage.color;
        c.a = alpha;
        _backgroundImage.color = c;
    }

    private void ClearCards()
    {
        _pushTween?.Kill();
        if (_contentLayout != null)
            _contentLayout.padding.top = _baseTopPadding;
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

        StartCoroutine(AnimatePushUp(card.GetComponent<RectTransform>()));
    }


    private IEnumerator AnimatePushUp(RectTransform newCard)
    {
        yield return null;                                  


        if (_contentLayout != null)
            _contentLayout.padding.top = _baseTopPadding;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        Canvas.ForceUpdateCanvases();
        float naturalHeight = _content.rect.height;


        float viewportHeight = GetViewportHeight();
        if (_contentLayout != null)
        {
            int targetTop = Mathf.Max(_baseTopPadding, Mathf.CeilToInt(viewportHeight - naturalHeight));
            _contentLayout.padding.top = targetTop;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            Canvas.ForceUpdateCanvases();
        }

 
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();

 
        if (newCard == null)
            yield break;

        CanvasGroup cg = newCard.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = newCard.gameObject.AddComponent<CanvasGroup>();
        Vector2 slot = newCard.anchoredPosition;              
        float slide = newCard.rect.height * 0.5f + 40f;      
        newCard.anchoredPosition = slot - new Vector2(0f, slide);
        cg.alpha = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Append(newCard.DOAnchorPos(slot, _pushDuration).SetEase(Ease.OutCubic));
        seq.Join(cg.DOFade(1f, _pushDuration));
        _pushTween = seq;
    }

    private float GetViewportHeight()
    {
        if (_scrollRect == null)
            return 0f;

        RectTransform vp = _scrollRect.viewport != null
            ? _scrollRect.viewport
            : _scrollRect.transform as RectTransform;
        return vp != null ? vp.rect.height : 0f;
    }
}
