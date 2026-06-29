using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StoryUIController : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("Scroll View 오브젝트의 ScrollRect")]
    [SerializeField] private ScrollRect _scrollRect;

    [Tooltip("Scroll View > Viewport > Content (카드가 쌓이는 곳)")]
    [SerializeField] private RectTransform _content;


    [SerializeField] private GameObject _cardPrefab;


    [SerializeField] private Button _nextButton;

    [Header("스토리 대사")]
    [SerializeField]
    private List<DialogueCard> dialogueCards = new List<DialogueCard>();

    private int _currentIndex = 0;
    private bool _reachedEnd = false; 

    private void Awake()
    {

        if (_nextButton != null)
            _nextButton.onClick.AddListener(ShowNextCard);
    }


    public void ShowNextCard()
    {
        if (_cardPrefab == null || _content == null)
        {
            Debug.LogWarning("[StoryUI] _cardPrefab 또는 _content가 비어 있습니다. 인스펙터를 확인하세요.");
            return;
        }

  

        if (_reachedEnd || _currentIndex >= dialogueCards.Count)
        {
            Debug.Log("[StoryUI] 더 출력할 대사가 없습니다.");
            return;
        }

        DialogueCard data = dialogueCards[_currentIndex];


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

    [System.Serializable]
    struct DialogueCard
    {
        public string ID;            // 예: S2_001 (문자열이라 string)
        public string char_name;     // 표시 이름
        public string frame;         // 포트레잇 프레임 키
        public string char_portrait; // 초상화 키
        [TextArea] public string dialogue; // 대사 (여러 줄 입력 가능)
        public string card_bg;       // 카드 배경 키
        public bool end;             // 중단점 (TRUE면 출력 중단)
    }
}
