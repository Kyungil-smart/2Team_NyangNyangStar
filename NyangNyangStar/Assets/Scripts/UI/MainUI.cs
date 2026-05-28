using Core.Managers;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class MainUI : UIScene
{
    [Header("버튼")]
    [Tooltip("상점")][SerializeField] private Button _shopButton;
    [Tooltip("시즌 이벤트")][SerializeField] private Button _eventButton;
    [Tooltip("일일 출석")][SerializeField] private Button _dailyCheckInButton;
    [Tooltip("우편함")][SerializeField] private Button _mailButton;
    [Tooltip("설정")][SerializeField] private Button _settingsButton;
    [Tooltip("아이템 도감")][SerializeField] private Button _collectionButton;
    [Tooltip("스토리북")][SerializeField] private Button _storyBookButton;
    [Tooltip("임시보호 수첩")][SerializeField] private Button _notebookButton;
    [Tooltip("룰렛")][SerializeField] private Button _rouletteButton;
    [Tooltip("교감")][SerializeField] private Button _affinityButton;
    [Tooltip("냥스타그램")][SerializeField] private Button _meowMeowStarButton;
    [Tooltip("공방 머지 보드판")][SerializeField] private Button _workshopMergeBoardButton;
    [Tooltip("기본 머지 보드판")][SerializeField] private Button _mainMergeBoardButton;

    public override void Init()
    {
        Bind<Button>(typeof(MainUIButtons));

        _shopButton = Get<Button>((int)MainUIButtons.ShopButton);
        _eventButton = Get<Button>((int)MainUIButtons.EventButton);
        _dailyCheckInButton = Get<Button>((int)MainUIButtons.DailyCheckInButton);
        _mailButton = Get<Button>((int)MainUIButtons.MailButton);
        _settingsButton = Get<Button>((int)MainUIButtons.SettingsButton);
        _collectionButton = Get<Button>((int)MainUIButtons.CollectionButton);
        _storyBookButton = Get<Button>((int)MainUIButtons.StoryBookButton);
        _notebookButton = Get<Button>((int)MainUIButtons.NotebookButton);
        _rouletteButton = Get<Button>((int)MainUIButtons.RouletteButton);
        _affinityButton = Get<Button>((int)MainUIButtons.AffinityButton);
        _meowMeowStarButton = Get<Button>((int)MainUIButtons.MeowMeowStarButton);
        _workshopMergeBoardButton = Get<Button>((int)MainUIButtons.WorkshopMergeBoardButton);
        _mainMergeBoardButton = Get<Button>((int)MainUIButtons.MainMergeBoardButton);

        BindButtons();
    }

    private void BindButtons()
    {
        if (_shopButton != null) 
            _shopButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));
        if (_eventButton != null)
            _eventButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.EventPopupUI));
        if (_dailyCheckInButton != null)
            _dailyCheckInButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.DailyCheckInPopupUI));
        if (_mailButton != null)
            _mailButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MailPopupUI));
        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.SettingsPopupUI));
        if (_collectionButton != null)
            _collectionButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.CollectionPopupUI));
        if (_storyBookButton != null)
            _storyBookButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.StoryBookPopupUI));
        if (_notebookButton != null)
            _notebookButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.NotebookPopupUI));
        if (_rouletteButton != null)
            _rouletteButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.RoulettePopupUI));
        if (_affinityButton != null)
            _affinityButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.AffinityPopupUI));
        //if (_meowMeowStarButton != null)
        //    _meowMeowStarButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MeowMeowStarPopupUI));
        //if (_workshopMergeBoardButton != null)
        //    _workshopMergeBoardButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.WorkshopMergeBoardPopupUI));
        //if (_mainMergeBoardButton != null)
        //    _mainMergeBoardButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MainMergeBoardPopupUI));
    }

    private void OnDisable()
    {
        if (_shopButton != null)
            _shopButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));
        if (_eventButton != null)
            _eventButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.EventPopupUI));
        if (_dailyCheckInButton != null)
            _dailyCheckInButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.DailyCheckInPopupUI));
        if (_mailButton != null)
            _mailButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MailPopupUI));
        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.SettingsPopupUI));
        if (_collectionButton != null)
            _collectionButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.CollectionPopupUI));
        if (_storyBookButton != null)
            _storyBookButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.StoryBookPopupUI));
        if (_notebookButton != null)
            _notebookButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.NotebookPopupUI));
        if (_rouletteButton != null)
            _rouletteButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.RoulettePopupUI));
        if (_affinityButton != null)
            _affinityButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.AffinityPopupUI));
        //if (_meowMeowStarButton != null)
        //    _meowMeowStarButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MeowMeowStarPopupUI));
        //if (_workshopMergeBoardButton != null)
        //    _workshopMergeBoardButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.WorkshopMergeBoardPopupUI));
        //if (_mainMergeBoardButton != null)
        //    _mainMergeBoardButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MainMergeBoardPopupUI));
    }
}

public enum MainUIButtons
{
    ShopButton,
    EventButton,
    DailyCheckInButton,
    MailButton,
    SettingsButton,
    CollectionButton,
    StoryBookButton,
    NotebookButton,
    RouletteButton,
    AffinityButton,
    MeowMeowStarButton,
    WorkshopMergeBoardButton,
    MainMergeBoardButton
}
