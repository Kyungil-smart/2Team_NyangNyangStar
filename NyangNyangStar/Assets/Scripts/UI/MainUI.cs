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
        AddPopupButton(_shopButton, KeyContainer.Prefabs.ShopPopupUI);
        AddPopupButton(_eventButton, KeyContainer.Prefabs.EventPopupUI);
        AddPopupButton(_dailyCheckInButton, KeyContainer.Prefabs.DailyCheckInPopupUI);
        AddPopupButton(_mailButton, KeyContainer.Prefabs.MailPopupUI);
        AddPopupButton(_settingsButton, KeyContainer.Prefabs.SettingsPopupUI);
        AddPopupButton(_collectionButton, KeyContainer.Prefabs.CollectionPopupUI);
        AddPopupButton(_storyBookButton, KeyContainer.Prefabs.StoryBookPopupUI);
        AddPopupButton(_notebookButton, KeyContainer.Prefabs.NotebookPopupUI);
        AddPopupButton(_rouletteButton, KeyContainer.Prefabs.RoulettePopupUI);
        AddPopupButton(_affinityButton, KeyContainer.Prefabs.AffinityPopupUI);

        //if (_meowMeowStarButton != null)
        //    _meowMeowStarButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MeowMeowStarPopupUI));
        //if (_workshopMergeBoardButton != null)
        //    _workshopMergeBoardButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.WorkshopMergeBoardPopupUI));
        //if (_mainMergeBoardButton != null)
        //    _mainMergeBoardButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MainMergeBoardPopupUI));
    }

    private void OnDisable()
    {
        RemovePopupButton(_shopButton, KeyContainer.Prefabs.ShopPopupUI);
        RemovePopupButton(_eventButton, KeyContainer.Prefabs.EventPopupUI);
        RemovePopupButton(_dailyCheckInButton, KeyContainer.Prefabs.DailyCheckInPopupUI);
        RemovePopupButton(_mailButton, KeyContainer.Prefabs.MailPopupUI);
        RemovePopupButton(_settingsButton, KeyContainer.Prefabs.SettingsPopupUI);
        RemovePopupButton(_collectionButton, KeyContainer.Prefabs.CollectionPopupUI);
        RemovePopupButton(_storyBookButton, KeyContainer.Prefabs.StoryBookPopupUI);
        RemovePopupButton(_notebookButton, KeyContainer.Prefabs.NotebookPopupUI);
        RemovePopupButton(_rouletteButton, KeyContainer.Prefabs.RoulettePopupUI);
        RemovePopupButton(_affinityButton, KeyContainer.Prefabs.AffinityPopupUI);

        //if (_meowMeowStarButton != null)
        //    _meowMeowStarButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MeowMeowStarPopupUI));
        //if (_workshopMergeBoardButton != null)
        //    _workshopMergeBoardButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.WorkshopMergeBoardPopupUI));
        //if (_mainMergeBoardButton != null)
        //    _mainMergeBoardButton.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MainMergeBoardPopupUI));
    }

    private void AddPopupButton(Button button, string key)
    {
        if (button == null) return;
        button.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(key, PlayPopupOpenAnimation));
    }

    private void RemovePopupButton(Button button, string key)
    {
        if (button == null) return;
        button.onClick.RemoveListener(() => GameManager.UI.ShowPopupUI<UIPopup>(key, PlayPopupOpenAnimation));
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
