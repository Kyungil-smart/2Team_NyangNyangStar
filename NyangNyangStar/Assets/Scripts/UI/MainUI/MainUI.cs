using Core.Managers;
using UI;
using UI.Base;
using UI.MainUI;
using UI.MergeBoard;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class MainUI : UIScene
{
    [SerializeField] private Canvas _mainUICanvas;
    
    [Header("버튼")]
    [Tooltip("상점")][SerializeField] private Button _shopButton;
    [Tooltip("스크래칭 타임")][SerializeField] private Button _scratchingTimeButton;
    [Tooltip("일일 출석")][SerializeField] private Button _dailyCheckInButton;
    [Tooltip("우편함")][SerializeField] private Button _mailButton;
    [Tooltip("설정")][SerializeField] private Button _settingsButton;
    [Tooltip("아이템 도감")][SerializeField] private Button _collectionButton;
    [Tooltip("스토리북")][SerializeField] private Button _storyBookButton;
    [Tooltip("임시보호 수첩")][SerializeField] private Button _notebookButton;
    [Tooltip("룰렛")][SerializeField] private Button _rouletteButton;
    [Tooltip("교감")][SerializeField] private Button _affinityButton;
    [Tooltip("냥냥스냅")][SerializeField] private Button _nyangNyangSnapButton;
    [Tooltip("냥스타그램")][SerializeField] private Button _meowMeowStarButton;
    [Tooltip("공방 머지 보드판")][SerializeField] private Button _workshopMergeBoardButton;
    [Tooltip("기본 머지 보드판")][SerializeField] private Button _mainMergeBoardButton;
    [Tooltip("로그 아웃")] [SerializeField] private Button _logOutButton;

    [Space(10)] [Header("이미지")]
    [SerializeField] private UpDownScreenController _upDownCon;

    private MainUISprite _mainUISprite;

    public override void Init()
    {
        Bind<Button>(typeof(MainUIButtons));

        _shopButton = Get<Button>((int)MainUIButtons.ShopButton);
        _scratchingTimeButton = Get<Button>((int)MainUIButtons.ScratchingTimeButton);
        _dailyCheckInButton = Get<Button>((int)MainUIButtons.DailyCheckInButton);
        _mailButton = Get<Button>((int)MainUIButtons.MailButton);
        _settingsButton = Get<Button>((int)MainUIButtons.SettingsButton);
        _collectionButton = Get<Button>((int)MainUIButtons.CollectionButton);
        _storyBookButton = Get<Button>((int)MainUIButtons.StoryBookButton);
        _notebookButton = Get<Button>((int)MainUIButtons.NotebookButton);
        _rouletteButton = Get<Button>((int)MainUIButtons.RouletteButton);
        _affinityButton = Get<Button>((int)MainUIButtons.AffinityButton);
        _nyangNyangSnapButton = Get<Button>((int)MainUIButtons.NyangNyangSnapButton);
        _meowMeowStarButton = Get<Button>((int)MainUIButtons.MeowMeowStarButton);
        _workshopMergeBoardButton = Get<Button>((int)MainUIButtons.WorkshopMergeBoardButton);
        _mainMergeBoardButton = Get<Button>((int)MainUIButtons.MainMergeBoardButton);
        _logOutButton = Get<Button>((int)MainUIButtons.LogOutButton);

        InitPopups();
        
        _mainUICanvas.overrideSorting = true;
        _mainUICanvas.sortingOrder = 2;

        _mainUISprite = GetComponent<MainUISprite>();
        _mainUISprite.Init();
    }

    private void InitPopups()
    {
        InitPopup(KeyContainer.Prefabs.ShopPopupUI, _shopButton);
        InitPopup(KeyContainer.Prefabs.DailyCheckInPopupUI, _dailyCheckInButton);
        InitPopup(KeyContainer.Prefabs.MailPopupUI, _mailButton);
        InitPopup(KeyContainer.Prefabs.SettingsPopupUI, _settingsButton);
        InitPopup(KeyContainer.Prefabs.CollectionPopupUI, _collectionButton);
        InitPopup(KeyContainer.Prefabs.StoryBookPopupUI, _storyBookButton);
        InitPopup(KeyContainer.Prefabs.NotebookPopupUI, _notebookButton);
        InitPopup(KeyContainer.Prefabs.RoulettePopupUI, _rouletteButton);
        InitPopup(KeyContainer.Prefabs.AffinityPopupUI, _affinityButton);
        InitPopup(KeyContainer.Prefabs.NyangNyangSnapStagePopUpUI, _nyangNyangSnapButton);
        InitPopup(KeyContainer.Prefabs.NyangStargramHomeProfile, _meowMeowStarButton);
        _logOutButton.onClick.AddListener(LogOutButton);
        LoadMergeBoard();
        LoadScratchingTime();
    }

    private void OnDisable()
    {
        RemovePopupButton(_shopButton);
        RemovePopupButton(_dailyCheckInButton);
        RemovePopupButton(_mailButton);
        RemovePopupButton(_settingsButton);
        RemovePopupButton(_collectionButton);
        RemovePopupButton(_storyBookButton);
        RemovePopupButton(_notebookButton);
        RemovePopupButton(_rouletteButton);
        RemovePopupButton(_affinityButton);
        RemovePopupButton(_nyangNyangSnapButton);
        RemovePopupButton(_meowMeowStarButton);
        _mainMergeBoardButton.onClick.RemoveAllListeners();
        _scratchingTimeButton.onClick.RemoveAllListeners();
    }

    private void LoadMergeBoard()
    {
        GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.MergeBoard,
            onLoaded =>
            {
                MergeBoardController boardCon = onLoaded.GetComponentInChildren<MergeBoardController>();

                if (boardCon == null)
                {
                    DebugTool.Warning($"{onLoaded.name}의 머지보드 컨트롤러를 찾을 수 없습니다.", DebugType.Board);
                    return;
                }
                
                boardCon.OpenBoard();
            },
            onFailed =>
        {
            DebugTool.Warning($"{onFailed} 를 불러올 수 없습니다.", DebugType.Addressable);
        });
        
        _mainMergeBoardButton.onClick.AddListener(() =>
        {
            _upDownCon.DownAnimation(_mainUICanvas);
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
        });
    }
    
    private void LoadScratchingTime()
    {
        ScratchingTimeManager manager = null;
        GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.ScratchingTime,
            onLoaded =>
            {
                manager = onLoaded.GetComponent<ScratchingTimeManager>();
                
                if (manager == null)
                    DebugTool.Warning($"{onLoaded.name}의 스크래칭 타임 매니저를 찾을 수 없습니다.", DebugType.Board);
            },
            onFailed =>
            {
                DebugTool.Warning($"{onFailed} 를 불러올 수 없습니다.", DebugType.Addressable);
            });
        
        _scratchingTimeButton.onClick.AddListener(() =>
        {
            manager.OpenScratchingTimeUI();
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
        });
    }
    

    private void LogOutButton()
    {
        AuthManager auth = FindObjectOfType<AuthManager>();

        if (auth == null)
        {
            DebugTool.Warning("AuthManager 를 찾을 수 없습니다.", DebugType.Network);
            return;
        }
        
        auth.Logout();
        GameManager.Scene.LoadPreviousScene();
    }
    
    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<UIPopup>(key, onLoaded => AddPopupButton(button, onLoaded), false);
    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null) return;
        button.onClick.AddListener(() => 
        { 
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
        });
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;
        popup.PlayOpenAnimation();
    }
}

public enum MainUIButtons
{
    ShopButton,
    ScratchingTimeButton,
    DailyCheckInButton,
    MailButton,
    SettingsButton,
    CollectionButton,
    StoryBookButton,
    NotebookButton,
    RouletteButton,
    AffinityButton,
    NyangNyangSnapButton,
    MeowMeowStarButton,
    WorkshopMergeBoardButton,
    MainMergeBoardButton,
    LogOutButton,
}
