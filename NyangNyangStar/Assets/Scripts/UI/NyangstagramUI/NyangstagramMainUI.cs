using Core.Managers;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangstagramMainUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("스토리 버튼")][SerializeField] private Button _storyButton;
    [Tooltip("홈 버튼")][SerializeField] private Button _homeButton;
    [Tooltip("테그 버튼")][SerializeField] private Button _tagButton;
    [Tooltip("게시물 추가 버튼")][SerializeField] private Button _addPostButton;
    [Tooltip("알림 버튼")][SerializeField] private Button _notificationButton;
    [Tooltip("프로필 버튼")][SerializeField] private Button _profileButton;
    [Tooltip("냥스타그램 나가기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("DM Button")][SerializeField] private Button _dmButton;
    [Tooltip("Home DM Button")][SerializeField] private Button _dmHomeButton;
    [Tooltip("계정명 버튼")][SerializeField] private Button _accountButton;
    [Tooltip("좋아요 버튼")][SerializeField] private Button _likeButton;
    [Tooltip("좋아요 text")][SerializeField] private TMP_Text _likeCountText;

    [SerializeField] private int _likeCount = 26667;
    private bool _isLiked;


    [Header("내부 화면")]
    [SerializeField] private GameObject _homeView;
    [SerializeField] private GameObject _profileView;

    [Header("게시물")]
    [SerializeField] private Transform _postContent;
    [SerializeField] private NyangStargramPostSlotUI _postSlotPrefab;

    private readonly Dictionary<string, UIPopup> _cachedPopups = new();
    private readonly List<NyangStargramPostSlotUI> _postSlots = new();
    private bool _isInitialized;

    private NyangstagramMainUISprite _nyangstagramMainUISprite;
    private NyangstagramTab _currentMainTab = NyangstagramTab.Profile;

    public override void Init()
    {
        if (_isInitialized)
        {
            DebugTool.Log("NyangstagramMainUI Init 중복 실행 방지", DebugType.UI, this);
            return;
        }

        _isInitialized = true;

        Bind<Button>(typeof(NyangstagramButton));

        _storyButton = Get<Button>((int)NyangstagramButton.StoryButton);
        _homeButton = Get<Button>((int)NyangstagramButton.HomeButton);
        _tagButton = Get<Button>((int)NyangstagramButton.TagButton);
        _addPostButton = Get<Button>((int)NyangstagramButton.AddPostButton);
        _notificationButton = Get<Button>((int)NyangstagramButton.NotificationButton);
        _profileButton = Get<Button>((int)NyangstagramButton.ProfileButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangstagramButton.NyangstagramCloseButton);
        _dmButton = Get<Button>((int)NyangstagramButton.DMButton);
        _dmHomeButton = Get<Button>((int)NyangstagramButton.HomeDMButton);
        _accountButton = Get<Button>((int)NyangstagramButton.AccountNameTextButton);
        _likeButton = Get<Button>((int)NyangstagramButton.LikeButton);
        _likeCountText = UIBase.FindChild<TMP_Text>(gameObject, "Like Count", true);


        BindViewButtons();
        BindCloseButton();
        BindRouterEvents();

        BindButtons();

        PreloadPopups();

        _nyangstagramMainUISprite = GetComponent<NyangstagramMainUISprite>();

        if (_nyangstagramMainUISprite != null)
        {
            _nyangstagramMainUISprite.Init();
        }

        SetProfileView();

        ReFreshLikeCountText();

        DebugTool.Log("NyangstagramMainUI Init 완료", DebugType.UI, this);
    }

    private void PreloadPopups()
    {
        // 자기 자신(NyangStargramHomeProfile)은 여기서 다시 로드하면 안 된다.
        // 이 스크립트가 붙은 메인 UI는 이미 열려있는 1개로 취급한다.
        InitPopup(KeyContainer.Prefabs.NyangStargramPostPopUpUI, null);
        InitPopup(KeyContainer.Prefabs.NyangStargramNPCProfilePopUpUI, _accountButton);
        InitPopup(KeyContainer.Prefabs.NyangStargramAddPostPopUpUI, _addPostButton);
        InitPopup(KeyContainer.Prefabs.NyangStargramNoticePopUpUI, _notificationButton);
        InitPopup(KeyContainer.Prefabs.NyangStargramDMListPopUpUI, _dmButton);
        InitPopup(KeyContainer.Prefabs.NyangStargramDMListPopUpUI, _dmHomeButton);

        InitPopup(KeyContainer.Prefabs.NyangStargramDMchatPopUpUI, null);
    }
    private void OnEnable()
    {
        SetProfileView();
    }

    private void InitPopup(string key, Button openButton)
    {
        if (string.IsNullOrEmpty(key))
        {
            DebugTool.Warning("팝업 키가 비어있습니다.", DebugType.UI, this);
            return;
        }

        if (_cachedPopups.ContainsKey(key))
        {
            DebugTool.Log($"이미 캐싱된 팝업입니다: {key}", DebugType.UI, this);
            return;
        }

        GameManager.UI.ShowPopupUI<UIPopup>(
            key,
            popup =>
            {
                if (popup == null)
                {
                    DebugTool.Warning($"냥스타그램 팝업 로드 실패: {key}", DebugType.UI, this);
                    return;
                }

                _cachedPopups[key] = popup;
                popup.gameObject.SetActive(false);

                if (popup is NyangStargramAddPostUI addPostUI)
                {
                    addPostUI.SetMainUI(this);
                }

                if (popup is NyangStargramNotificationUI notificationUI)
                {
                    notificationUI.SetMainUI(this);
                }

                if (openButton != null)
                {
                    AddPopupButton(openButton, popup);
                }

                DebugTool.Log($"냥스타그램 팝업 캐싱 완료: {key}", DebugType.UI, this);
            },
            false
        );
    }

    public void AddPost(NyangNyangSnapSavedPhotoData photoData)
    {
        NyangStargramPostSlotUI slot = Instantiate(_postSlotPrefab, _postContent);

        slot.Init();
        slot.SetData(photoData, OpenPostPopup);
        slot.transform.SetSiblingIndex(0);

        _postSlots.Add(slot);

        DebugTool.Log($"게시물 추가 : {photoData.photoId}", DebugType.UI, this);
    }

    private void OpenPostPopup(NyangNyangSnapSavedPhotoData photoData)
    {
        if (!_cachedPopups.TryGetValue(KeyContainer.Prefabs.NyangStargramPostPopUpUI, out UIPopup popup)) return;

        if (popup is NyangStargramPostUI postUI)
        {
            postUI.SetPhoto(photoData.storagePath);
        }

        ShowCachedPopup(popup);
    }

    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddLikeButton(_likeButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null || popup == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            SetPopupTab(button);
            ShowCachedPopup(popup);
        });
    }
    private void SetPopupTab(Button button)
    {
        if (button == _addPostButton)
        {
            SetSelectedTab(NyangstagramTab.AddPost);
            return;
        }

        if (button == _notificationButton)
        {
            SetSelectedTab(NyangstagramTab.Notification);
        }
    }

    private void SetMainTab(NyangstagramTab tab)
    {
        _currentMainTab = tab;
        SetSelectedTab(tab);
    }

    private void SetSelectedTab(NyangstagramTab tab)
    {
        if (_nyangstagramMainUISprite == null)
            return;

        _nyangstagramMainUISprite.SetSelectedTab(tab);
    }

    public void RestoreMainTab()
    {
        SetSelectedTab(_currentMainTab);
    }
    private void BindViewButtons()
    {
        AddViewButton(
            _homeButton,
            true,
            false,
            NyangstagramTab.Home
        );

        AddViewButton(
            _profileButton,
            false,
            true,
            NyangstagramTab.Profile
        );
    }

    private void AddViewButton( Button button, bool homeActive, bool profileActive,NyangstagramTab selectedTab)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            SetView(homeActive, profileActive);
            SetMainTab(selectedTab);
        });
    }

    private void BindCloseButton()
    {
        AddCloseAllButton(_nyangstagramCloseButton);
    }

    private void AddCloseAllButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HideAllNyangstagramUI);
    }

    private void BindRouterEvents()
    {
        NyangstagramUIRouter.OnRequestOpenPopup -= ShowCachedPopup;
        NyangstagramUIRouter.OnRequestCloseAll -= HideAllNyangstagramUI;
        NyangstagramUIRouter.OnRequestHomeView -= SetHomeView;
        NyangstagramUIRouter.OnRequestProfileView -= SetProfileView;

        NyangstagramUIRouter.OnRequestOpenPopup += ShowCachedPopup;
        NyangstagramUIRouter.OnRequestCloseAll += HideAllNyangstagramUI;
        NyangstagramUIRouter.OnRequestHomeView += SetHomeView;
        NyangstagramUIRouter.OnRequestProfileView += SetProfileView;
    }

    private void ShowCachedPopup(string key)
    {
        if (!_cachedPopups.TryGetValue(key, out UIPopup popup))
        {
            DebugTool.Warning($"캐싱된 팝업이 없습니다: {key}", DebugType.UI, this);
            return;
        }

        ShowCachedPopup(popup);
    }

    private void ShowCachedPopup(UIPopup popup)
    {
        if (popup == null) return;

        if (popup.gameObject.activeSelf)
        {
            DebugTool.Log($"이미 열려있는 팝업입니다: {popup.name}", DebugType.UI, this);
            return;
        }

        popup.gameObject.SetActive(true);
        PlayPopupOpenAnimation(popup);

        DebugTool.Log($"캐싱 팝업 열기: {popup.name}", DebugType.UI, this);
    }

    private void HideAllNyangstagramUI()
    {
        foreach (UIPopup popup in _cachedPopups.Values)
        {
            if (popup == null) continue;
            popup.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);

        DebugTool.Log("냥스타그램 전체 UI 비활성화", DebugType.UI, this);
    }

    private void SetHomeView()
    {
        SetView(true, false);
        SetMainTab(NyangstagramTab.Home);
    }

    private void SetProfileView()
    {
        SetView(false, true);
        SetMainTab(NyangstagramTab.Profile);
    }

    private void SetView(bool homeActive, bool profileActive)
    {
        if (_homeView != null)
            _homeView.SetActive(homeActive);

        if (_profileView != null)
            _profileView.SetActive(profileActive);

        DebugTool.Log($"냥스타그램 View 변경 / Home: {homeActive}, Profile: {profileActive}", DebugType.UI, this);
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;
        popup.PlayOpenAnimation();
    }
    private void AddLikeButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(OnClickLikeButton);
    }

    private void OnClickLikeButton()
    {

        if (_isLiked)
        {
            _likeCount--;
            _isLiked = false;
        }
        else
        {
            _likeCount++;
            _isLiked = true;
        }

        ReFreshLikeCountText();
        _nyangstagramMainUISprite.SetLikeSprite(_isLiked);
    }
    private void ReFreshLikeCountText()
    {
        if (_likeCountText == null) return;

        //_likeCountText.text = $"Like {_likeCount:N0}";
        _likeCountText.text = $"Like {_likeCount}";
    }

    private void OnDestroy()
    {
        NyangstagramUIRouter.OnRequestOpenPopup -= ShowCachedPopup;
        NyangstagramUIRouter.OnRequestCloseAll -= HideAllNyangstagramUI;
        NyangstagramUIRouter.OnRequestHomeView -= SetHomeView;
        NyangstagramUIRouter.OnRequestProfileView -= SetProfileView;
    }
}

public enum NyangstagramButton
{
    StoryButton,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton,
    DMButton,
    HomeDMButton,
    AccountNameTextButton,
    LikeButton
}
