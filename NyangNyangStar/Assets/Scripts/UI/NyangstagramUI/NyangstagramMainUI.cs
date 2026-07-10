using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangstagramMainUI : UIPopup
{
    private const int PostColumnCount = 3;

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

    [Header("기능 준비 중 안내")]
    [SerializeField] private TMP_Text _comingSoonText;
    [SerializeField] private string _comingSoonMessage = "기능 준비 중!";
    [SerializeField, Min(0f)] private float _comingSoonDisplayDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float _comingSoonFadeDuration = 0.5f;

    [SerializeField] private int _likeCount = 26667;
    private bool _isLiked;


    [Header("내부 화면")]
    [SerializeField] private GameObject _homeView;
    [SerializeField] private GameObject _profileView;

    [Header("게시물")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _postContent;
    [SerializeField] private NyangStargramPostSlotUI _postSlotPrefab;

    [Header("냥스타그램 게시물 SO")]
    [SerializeField] private NyangStargramPostSO _postSO;

    private GridLayoutGroup _postGrid;
    private LayoutElement _postGridLayoutElement;

    private readonly Dictionary<string, UIPopup> _cachedPopups = new();
    private readonly Dictionary<string, NyangStargramPostSlotUI> _postSlotDic = new();
    private readonly List<NyangStargramPostData> _sortedPosts = new();
    private readonly HashSet<string> _serverPhotoIds = new();
    private readonly List<string> _removeIds = new();

    private bool _isInitialized;
    private Coroutine _comingSoonCoroutine;

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
        _comingSoonText ??= UIBase.FindChild<TMP_Text>(gameObject, "ComingSoonText", true);

        _postGrid = _postContent.GetComponent<GridLayoutGroup>();
        _postGridLayoutElement = _postContent.GetComponent<LayoutElement>();

        RefreshPostGridCellSize();
        RefreshPostGridHeight();

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
        LoadPostsAsync();
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
        // 알림/DM은 아직 미구현이므로 팝업을 미리 생성하지 않는다.
    }

    private void OnEnable()
    {
        SetProfileView();
        RefreshPostSlots();

        StartCoroutine(ResetScrollPosition());
    }

    private IEnumerator ResetScrollPosition()
    {
        yield return null;
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshPostGridCellSize()
    {
        if (_postGrid == null) return;

        float contentWidth = _postContent.rect.width;
        _postGrid.padding.left = 10;
        _postGrid.padding.right = 10;
        float padding = _postGrid.padding.left + _postGrid.padding.right;
        float spacing = _postGrid.spacing.x * (PostColumnCount - 1);

        float cellSize = (contentWidth - padding - spacing) / PostColumnCount;

        _postGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _postGrid.constraintCount = PostColumnCount;
        _postGrid.cellSize = new Vector2(cellSize, cellSize);
    }

    private void RefreshPostGridHeight()
    {
        if (_postGrid == null || _postGridLayoutElement == null) return;

        int postCount = _postSlotDic.Count;
        int rowCount = Mathf.CeilToInt(postCount / (float)PostColumnCount);

        float height =
            _postGrid.padding.top +
            _postGrid.padding.bottom +
            rowCount * _postGrid.cellSize.y +
            Mathf.Max(0, rowCount - 1) * _postGrid.spacing.y;

        _postGridLayoutElement.preferredHeight = height;
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

    private async void LoadPostsAsync()
    {
        await _postSO.UpdateFromServerAsync(false);

        RefreshPostSlots();
    }

    private void RefreshPostSlots()
    {
        // 정렬
        _sortedPosts.Clear();
        _sortedPosts.AddRange(_postSO.Posts);
        _sortedPosts.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));

        _serverPhotoIds.Clear();

        for (int i = 0; i < _sortedPosts.Count; i++)
        {
            NyangStargramPostData post = _sortedPosts[i];

            NyangNyangSnapRuntimePhotoData photoData = NyangNyangSnapPhotoManager.Instance.GetPhoto(post.photoId);

            if (photoData == null) continue;

            _serverPhotoIds.Add(post.photoId);

            if (!_postSlotDic.TryGetValue(post.photoId, out NyangStargramPostSlotUI slot))
            {
                slot = CreatePostSlot(post.photoId);
            }

            slot.SetData(photoData, OpenPostPopup);
            slot.transform.SetSiblingIndex(i);
        }

        RemoveDeletedPostSlots(_serverPhotoIds);
        RefreshPostGridHeight();
    }

    public void AddPost(NyangNyangSnapRuntimePhotoData photoData)
    {
        NyangStargramPostSlotUI slot = CreatePostSlot(photoData.PhotoId);
        slot.SetData(photoData, OpenPostPopup);
        slot.transform.SetSiblingIndex(0);

        RefreshPostGridHeight();
    }

    private NyangStargramPostSlotUI CreatePostSlot(string photoId)
    {
        NyangStargramPostSlotUI slot = Instantiate(_postSlotPrefab, _postContent);
        slot.Init();

        _postSlotDic.Add(photoId, slot);

        DebugTool.Log($"게시물 슬롯 생성 : {photoId}", DebugType.UI, this);

        return slot;
    }

    private void RemoveDeletedPostSlots(HashSet<string> serverPhotoIds)
    {
        _removeIds.Clear();

        foreach (KeyValuePair<string, NyangStargramPostSlotUI> pair in _postSlotDic)
        {
            if (!serverPhotoIds.Contains(pair.Key))
                _removeIds.Add(pair.Key);
        }

        foreach (string photoId in _removeIds)
        {
            if (_postSlotDic[photoId] != null)
                Destroy(_postSlotDic[photoId].gameObject);

            _postSlotDic.Remove(photoId);
        }
    }

    private void OpenPostPopup(NyangNyangSnapRuntimePhotoData photoData)
    {
        if (!_cachedPopups.TryGetValue(KeyContainer.Prefabs.NyangStargramPostPopUpUI, out UIPopup popup)) return;

        if (popup is NyangStargramPostUI postUI)
        {
            postUI.SetPhoto(photoData.Sprite);
        }

        ShowCachedPopup(popup);
    }

    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddLikeButton(_likeButton);
        AddComingSoonButton(_tagButton);
        AddComingSoonButton(_notificationButton);
        AddComingSoonButton(_dmButton);
        AddComingSoonButton(_dmHomeButton);

        HideComingSoonImmediately();


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }


    private void AddComingSoonButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowComingSoonMessage);
    }

    private void ShowComingSoonMessage()
    {
        if (_comingSoonText == null)
        {
            DebugTool.Warning("ComingSoonText가 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        if (_comingSoonCoroutine != null)
            StopCoroutine(_comingSoonCoroutine);

        _comingSoonCoroutine = StartCoroutine(ComingSoonRoutine());
    }

    private IEnumerator ComingSoonRoutine()
    {
        _comingSoonText.gameObject.SetActive(true);
        _comingSoonText.text = _comingSoonMessage;

        Color color = _comingSoonText.color;
        color.a = 1f;
        _comingSoonText.color = color;

        yield return new WaitForSecondsRealtime(_comingSoonDisplayDuration);

        float elapsed = 0f;
        while (elapsed < _comingSoonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / _comingSoonFadeDuration);
            _comingSoonText.color = color;
            yield return null;
        }

        HideComingSoonImmediately();
        _comingSoonCoroutine = null;
    }

    private void HideComingSoonImmediately()
    {
        if (_comingSoonText == null) return;

        Color color = _comingSoonText.color;
        color.a = 0f;
        _comingSoonText.color = color;
        _comingSoonText.gameObject.SetActive(false);
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

    private void AddViewButton(Button button, bool homeActive, bool profileActive, NyangstagramTab selectedTab)
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
