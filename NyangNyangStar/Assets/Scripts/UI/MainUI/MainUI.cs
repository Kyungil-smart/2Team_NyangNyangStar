using System.Collections;
using System.Threading.Tasks;
using Core.Managers;
using TMPro;
using UI.Common;
using UI;
using UI.Base;
using UI.MergeBoard;
using UI.NyangQuarium;
using UI.Transition;
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
    [Tooltip("로그 아웃")][SerializeField] private Button _logOutButton;
    
    [Tooltip("뭉치를 찾아라")] [SerializeField] private Button _findMoongchiButton;
    [Tooltip("냥쿠아 리움")] [SerializeField] private Button _nyangquariumButton;

    private MainUISprite _mainUISprite;
    private MergeBoardController _mergeBoardController;
    private ScratchingTimeManager _scratchingTimeManager;
    private PlayerResourceDisplay _resourceDisplay;
    private UIPopup _nyangStargramPopup;
    private NotebookPopupUI _notebookPopup;
    private bool _isMergeBoardTransitioning;
    private bool _isMergeBoardVisible;
    private bool _isNyangquariumTransitioning;

    [SerializeField] private UsersSO _usersSO;
    [SerializeField] private TMP_Text _uidText;

    public static MainUI Instance { get; private set; }

    private void Start()
    {
        UpdateUidText();
    }

    private void OnEnable()
    {
        SubscribeUserIdChanged();
        UpdateUidText();
    }

    public override void Init()
    {
        Instance = this;

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
        _findMoongchiButton = Get<Button>((int)MainUIButtons.FindMoongchiButton);
        _nyangquariumButton = Get<Button>((int)MainUIButtons.NyangquariumButton);

        if (_mainUICanvas != null)
        {
            _mainUICanvas.overrideSorting = true;
            _mainUICanvas.sortingOrder = 2;
        }

        _mainUISprite = GetComponent<MainUISprite>();
        _mainUISprite?.Init();
        EnsureResourceDisplay();

        InitPopups();
        SubscribeUserIdChanged();
        UpdateUidText();
        SetPhotoAlert(false);
    }

    private void InitPopups()
    {
        InitPopup(KeyContainer.Prefabs.ShopPopupUI, _shopButton);
        InitPopup(KeyContainer.Prefabs.DailyCheckInPopupUI, _dailyCheckInButton);
        InitPopup(KeyContainer.Prefabs.MailPopupUI, _mailButton);
        InitPopup(KeyContainer.Prefabs.SettingsPopupUI, _settingsButton);
        InitPopup(KeyContainer.Prefabs.CollectionPopupUI, _collectionButton);
        InitPopup(KeyContainer.Prefabs.StoryBookPopupUI, _storyBookButton);
        InitNotebookPopup();
        InitPopup(KeyContainer.Prefabs.RoulettePopupUI, _rouletteButton);
        InitPopup(KeyContainer.Prefabs.AffinityPopupUI, _affinityButton);
        InitPopup(KeyContainer.Prefabs.NyangNyangSnapStagePopUpUI, _nyangNyangSnapButton);
        InitNyangStargramPopup();
        InitPopup(KeyContainer.Prefabs.FindMoongchiPopupUI, _findMoongchiButton);
        InitNyangquariumPopup();

        if (_logOutButton != null)
            _logOutButton.onClick.AddListener(LogOutButton);

        LoadMergeBoard();
        LoadScratchingTime();
    }

    private void OnDisable()
    {
        UnsubscribeUserIdChanged();

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
        RemovePopupButton(_findMoongchiButton);
        RemovePopupButton(_nyangquariumButton);

        if (_mainMergeBoardButton != null)
            _mainMergeBoardButton.onClick.RemoveAllListeners();

        if (_workshopMergeBoardButton != null)
            _workshopMergeBoardButton.onClick.RemoveAllListeners();

        if (_scratchingTimeButton != null)
            _scratchingTimeButton.onClick.RemoveAllListeners();

        if (_logOutButton != null)
            _logOutButton.onClick.RemoveAllListeners();

        _isNyangquariumTransitioning = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LoadMergeBoard()
    {
        GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.MergeBoard,
            onLoaded =>
            {
                _mergeBoardController = onLoaded.GetComponentInChildren<MergeBoardController>(true);

                if (_mergeBoardController == null)
                {
                    DebugTool.Warning($"{onLoaded.name}의 머지보드 컨트롤러를 찾을 수 없습니다.", DebugType.Board);
                    return;
                }

                _mergeBoardController.OpenBoard();
            },
            onFailed =>
            {
                DebugTool.Warning($"{onFailed} 를 불러올 수 없습니다.", DebugType.Addressable);
            });

        if (_mainMergeBoardButton != null)
            _mainMergeBoardButton.onClick.AddListener(OpenMergeBoard);

        if (_workshopMergeBoardButton != null)
            _workshopMergeBoardButton.onClick.AddListener(OpenMergeBoard);
    }

    private void EnsureResourceDisplay()
    {
        if (_resourceDisplay == null)
            _resourceDisplay = GetComponent<PlayerResourceDisplay>();

        if (_resourceDisplay == null)
            _resourceDisplay = gameObject.AddComponent<PlayerResourceDisplay>();

        _resourceDisplay.ResolveReferencesFrom(transform);
        _ = PlayerResourceManager.Instance.RefreshAsync();
    }

    private void OpenMergeBoard()
    {
        if (_mergeBoardController == null)
        {
            DebugTool.Warning("MergeBoardController가 아직 로드되지 않았습니다.", DebugType.Board);
            return;
        }

        if (_isMergeBoardTransitioning || _isMergeBoardVisible)
            return;

        _scratchingTimeManager?.HideImmediately();

        if (ScreenTransitionManager.Instance == null)
        {
            SetMergeBoardVisible(true);
            return;
        }

        BeginMergeBoardTransition();
        ScreenTransitionManager.Instance.Cover(() =>
        {
            SetMergeBoardVisible(true);
            ScreenTransitionManager.Instance.Reveal(EndMergeBoardTransition);
        });

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    public void CloseMergeBoard()
    {
        if (_mergeBoardController == null)
            return;

        if (_isMergeBoardTransitioning || !_isMergeBoardVisible)
            return;

        if (ScreenTransitionManager.Instance == null)
        {
            SetMergeBoardVisible(false);
            return;
        }

        BeginMergeBoardTransition();
        ScreenTransitionManager.Instance.Cover(() =>
        {
            SetMergeBoardVisible(false);
            ScreenTransitionManager.Instance.Reveal(EndMergeBoardTransition);
        });

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void BeginMergeBoardTransition()
    {
        _isMergeBoardTransitioning = true;
        SetMergeBoardButtonsInteractable(false);
    }

    private void EndMergeBoardTransition()
    {
        _isMergeBoardTransitioning = false;
        SetMergeBoardButtonsInteractable(true);
    }

    private void SetMergeBoardButtonsInteractable(bool interactable)
    {
        if (_mainMergeBoardButton != null)
            _mainMergeBoardButton.interactable = interactable;

        if (_workshopMergeBoardButton != null)
            _workshopMergeBoardButton.interactable = interactable;
    }

    private void SetMergeBoardVisible(bool isOpen)
    {
        _isMergeBoardVisible = isOpen;
        _mergeBoardController.SetVisible(isOpen);

        if (_mainUICanvas != null)
            _mainUICanvas.sortingOrder = isOpen ? 0 : 2;
    }

    private void LoadScratchingTime()
    {
        GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.ScratchingTime,
            onLoaded =>
            {
                _scratchingTimeManager = onLoaded.GetComponent<ScratchingTimeManager>();

                if (_scratchingTimeManager == null)
                {
                    DebugTool.Warning($"{onLoaded.name}의 스크래칭 타임 매니저를 찾을 수 없습니다.", DebugType.Board);
                    return;
                }

                _scratchingTimeManager.HideImmediately();
            },
            onFailed =>
            {
                DebugTool.Warning($"{onFailed} 를 불러올 수 없습니다.", DebugType.Addressable);
            });

        if (_scratchingTimeButton != null)
        {
            _scratchingTimeButton.onClick.AddListener(() =>
            {
                if (_scratchingTimeManager == null)
                {
                    DebugTool.Warning("ScratchingTimeManager가 아직 로드되지 않았습니다.", DebugType.UI);
                    return;
                }

                _scratchingTimeManager.OpenScratchingTimeUI();
                GameManager.Audio.PlaySfx("Main_SFX_Touch");
            });
        }
    }


    private void SubscribeUserIdChanged()
    {
        if (AuthManager.Instance == null)
            return;

        AuthManager.Instance.OnUserIdChanged -= HandleUserIdChanged;
        AuthManager.Instance.OnUserIdChanged += HandleUserIdChanged;
    }

    private void UnsubscribeUserIdChanged()
    {
        if (AuthManager.Instance == null)
            return;

        AuthManager.Instance.OnUserIdChanged -= HandleUserIdChanged;
    }

    private void HandleUserIdChanged(string userId)
    {
        UpdateUidText();
    }

    private void UpdateUidText()
    {
        if (_uidText == null)
            return;

        string userId = AuthManager.Instance != null ? AuthManager.Instance.CurrentUserId : string.Empty;

        if (string.IsNullOrEmpty(userId) && _usersSO != null)
            userId = _usersSO.GetUserId();

        _uidText.text = string.IsNullOrEmpty(userId) ? "-" : userId;
    }

    private void LogOutButton()
    {
        if (ScreenTransitionManager.Instance == null)
        {
            StartCoroutine(LogoutProcessCoroutine());
            return;
        }

        ScreenTransitionManager.Instance.Cover(() => StartCoroutine(LogoutProcessCoroutine()));
    }

    private IEnumerator LogoutProcessCoroutine()
    {
        Task recordTask = UserSessionTimeService.RecordLogoutAsync();

        while (!recordTask.IsCompleted)
            yield return null;

        AuthManager auth = AuthManager.Instance;

        if (auth != null)
            auth.LogoutAndClearSession();
        else
            GameManager.ClearSession();

        GameManager.Scene.LoadPreviousScene();
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<UIPopup>(key, onLoaded => AddPopupButton(button, onLoaded), false);
    }

    private void InitNotebookPopup()
    {
        GameManager.UI.ShowPopupUI<NotebookPopupUI>(KeyContainer.Prefabs.NotebookPopupUI,
            onLoaded =>
            {
                _notebookPopup = onLoaded;
                AddPopupButton(_notebookButton, onLoaded);
            },
            false);
    }

    private void InitNyangStargramPopup()
    {
        GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.NyangStargramHomeProfile,
            onLoaded =>
            {
                _nyangStargramPopup = onLoaded;
                AddPopupButton(_meowMeowStarButton, onLoaded);
            },
            false);
    }

    private void InitNyangquariumPopup()
    {
        GameManager.UI.ShowPopupUI<UIPopup>(
            KeyContainer.Prefabs.Nyangquarium,
            onLoaded => AddNyangquariumButton(_nyangquariumButton, onLoaded),
            false);
    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() =>
        {
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
        });
    }

    private void AddNyangquariumButton(Button button, UIPopup popup)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() => OpenNyangquariumPopup(popup));
    }

    private void OpenNyangquariumPopup(UIPopup popup)
    {
        if (popup == null || _isNyangquariumTransitioning)
            return;

        ScreenTransitionManager transition = ScreenTransitionManager.Instance;

        if (transition == null)
        {
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            return;
        }

        if (transition.IsTransitioning)
            return;

        BeginNyangquariumTransition();
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        transition.Cover(() =>
        {
            ShowNyangquariumPopupImmediately(popup);
            transition.Reveal(EndNyangquariumTransition);
        });
    }

    private void ShowNyangquariumPopupImmediately(UIPopup popup)
    {
        if (popup is NyangquariumHubUI hubUI)
        {
            hubUI.ShowImmediately();
            return;
        }

        popup.gameObject.SetActive(true);
    }

    private void BeginNyangquariumTransition()
    {
        _isNyangquariumTransitioning = true;

        if (_nyangquariumButton != null)
            _nyangquariumButton.interactable = false;
    }

    private void EndNyangquariumTransition()
    {
        _isNyangquariumTransitioning = false;

        if (_nyangquariumButton != null)
            _nyangquariumButton.interactable = true;
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null)
            return;

        popup.PlayOpenAnimation();
    }

    public void SetPhotoAlert(bool isOn)
    {
        _mainUISprite?.SetNotebookAlert(isOn);
        _notebookPopup?.SetPhotoAlert(isOn);
    }

    public void OpenNyangStargramPopup()
    {
        _nyangStargramPopup.gameObject.SetActive(true);
        PlayPopupOpenAnimation(_nyangStargramPopup);
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
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
    FindMoongchiButton,
    NyangquariumButton
}
