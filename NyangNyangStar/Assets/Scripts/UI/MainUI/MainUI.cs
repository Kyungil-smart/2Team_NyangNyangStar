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

    private MainUISprite _mainUISprite;

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

        InitPopups();

        _mainUISprite = GetComponent<MainUISprite>();
        _mainUISprite.Init();
    }

    private void InitPopups()
    {
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI, onLoaded =>
        { ShowPopupUI(_shopButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.EventPopupUI, onLoaded =>
        { ShowPopupUI(_eventButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.DailyCheckInPopupUI, onLoaded =>
        { ShowPopupUI(_dailyCheckInButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.MailPopupUI, onLoaded =>
        { ShowPopupUI(_mailButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.SettingsPopupUI, onLoaded =>
        { ShowPopupUI(_settingsButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.CollectionPopupUI, onLoaded =>
        { ShowPopupUI(_collectionButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.StoryBookPopupUI, onLoaded =>
        { ShowPopupUI(_storyBookButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.NotebookPopupUI, onLoaded =>
        { ShowPopupUI(_notebookButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.RoulettePopupUI, onLoaded =>
        { ShowPopupUI(_rouletteButton, onLoaded); }, false);
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.AffinityPopupUI, onLoaded =>
        { ShowPopupUI(_affinityButton, onLoaded); }, false);
    }

    private void OnDisable()
    {
        RemovePopupButton(_shopButton);
        RemovePopupButton(_eventButton);
        RemovePopupButton(_dailyCheckInButton);
        RemovePopupButton(_mailButton);
        RemovePopupButton(_settingsButton);
        RemovePopupButton(_collectionButton);
        RemovePopupButton(_storyBookButton);
        RemovePopupButton(_notebookButton);
        RemovePopupButton(_rouletteButton);
        RemovePopupButton(_affinityButton);
    }

    private void ShowPopupUI(Button button, UIPopup popup)
    {
        if (button == null) return;
        button.onClick.AddListener(() => 
        { 
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup); 
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
