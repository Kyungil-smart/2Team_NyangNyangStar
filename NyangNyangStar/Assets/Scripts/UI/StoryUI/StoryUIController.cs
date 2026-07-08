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

    // 챕터 배경 적용: 키 있으면 로드 성공 시 알파 1, 없으면 알파 0(투명). Dim은 별도 오브젝트라 영향 없음.
    private void ApplyBackground(StoryDataSO story)
    {
        if (_backgroundImage == null)
            return;

        if (string.IsNullOrEmpty(story.backgroundKey))
        {
            SetBackgroundAlpha(0f);            // 이미지 없음 → 투명
            _backgroundSprite?.ClearSprite();  // 이전 스프라이트/핸들 해제
            return;
        }

        SetBackgroundAlpha(0f);                // 로드 끝나기 전엔 투명(깜빡임 방지)
        _backgroundSprite?.ChangeSprite(
            story.backgroundKey,
            onLoaded: () =>
            {
                if (_backgroundImage != null && _backgroundImage.sprite != null)
                    SetBackgroundAlpha(1f);    // 로드 성공 시에만 보이게
            });
    }

    // _backgroundImage(ChapterBackground)의 알파만 조절. Dim은 건드리지 않음.
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

        _pushTween?.Complete();                  // 진행 중 트윈이 있으면 즉시 마감 (연타 대비)
        float prevHeight = _content.rect.height; // 카드 추가 전 Content 높이 기억

        DialogueCard data = _currentStory.cards[_currentIndex];

        // 상대방 대사면 상대방 카드, 아니면 주인공 카드 (없으면 기본 카드로 폴백)
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

    // 새 카드는 바닥에 나타나고, 기존 카드들이 부드럽게 위로 밀려 올라가는 연출.
    private IEnumerator AnimatePushUp(RectTransform newCard, float prevHeight)
    {
        yield return null;                                     // Instantiate 반영 대기
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content); // 레이아웃 확정(높이 갱신)
        Canvas.ForceUpdateCanvases();

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;       // 바닥 고정 → 최종 위치 확정
        Canvas.ForceUpdateCanvases();

        float delta = _content.rect.height - prevHeight;       // 늘어난 높이 = 밀어 올릴 거리(간격 포함)
        Vector2 endPos = _content.anchoredPosition;            // 트윈이 끝날 최종 위치

        if (_scrollRect != null)
            _scrollRect.enabled = false;                       // 트윈 중 스크롤 간섭 차단
        _content.anchoredPosition = endPos - new Vector2(0f, delta); // delta만큼 내렸다가

        // 새 카드 페이드용 CanvasGroup (없으면 런타임에 추가)
        CanvasGroup cg = null;
        if (newCard != null)
            cg = newCard.GetComponent<CanvasGroup>() ?? newCard.gameObject.AddComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 0f;

        // 콘텐츠가 뷰포트보다 클 때만 스크롤이 의미 있음.
        // 짧을 땐 스크롤을 꺼서, ScrollRect가 짧은 콘텐츠를 상단으로 끌어올리는
        // 기본 동작을 막고 바닥 정렬을 유지한다.
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
        seq.Append(_content.DOAnchorPos(endPos, _pushDuration).SetEase(Ease.OutCubic)); // 위로 스르륵
        if (cg != null)
            seq.Join(cg.DOFade(1f, _pushDuration));             // 새 카드 페이드 인
        seq.OnComplete(() =>
        {
            _content.anchoredPosition = endPos;                 // 최종 위치 확정
            if (_scrollRect != null)
            {
                _scrollRect.enabled = scrollable;               // 넘칠 때만 스크롤 허용
                if (scrollable)
                    _scrollRect.verticalNormalizedPosition = 0f; // 최신 카드(바닥) 고정
            }
        });
        _pushTween = seq;
    }
}
