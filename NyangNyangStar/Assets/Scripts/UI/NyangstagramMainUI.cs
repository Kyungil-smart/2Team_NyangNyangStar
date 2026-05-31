using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangstagramMainUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("스토리 버튼")][SerializeField] private Button _storyButton;
    [Tooltip("이미지 Post 버튼")][SerializeField] private Button _image;
    [Tooltip("홈 버튼")][SerializeField] private Button _homeButton;
    [Tooltip("테그 버튼")][SerializeField] private Button _tagButton;
    [Tooltip("게시물 추가 버튼")][SerializeField] private Button _addPostButton;
    [Tooltip("알림 버튼")][SerializeField] private Button _notificationButton;
    [Tooltip("프로필 버튼")][SerializeField] private Button _profileButton;
    [Tooltip("냥스타그램 나가기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("DM Button")][SerializeField] private Button _dmButton;
    [Tooltip("계정명 버튼")][SerializeField] private Button _accountButton;

    [Header("내부 화면")]
    [SerializeField] private GameObject _homeView;
    [SerializeField] private GameObject _profileView;


    //private void Awake()
    //{
    //    Init();
    //}

    public override void Init()
    {
        Bind<Button>(typeof(NyangstagramButton));

        _storyButton = Get<Button>((int)NyangstagramButton.StoryButton);
        _image = Get<Button>((int)NyangstagramButton.Image);
        _homeButton = Get<Button>((int)NyangstagramButton.HomeButton);
        _tagButton = Get<Button>((int)NyangstagramButton.TagButton);
        _addPostButton = Get<Button>((int)NyangstagramButton.AddPostButton);
        _notificationButton = Get<Button>((int)NyangstagramButton.NotificationButton);
        _profileButton = Get<Button>((int)NyangstagramButton.ProfileButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangstagramButton.NyangstagramCloseButton);
        _dmButton = Get<Button>((int)NyangstagramButton.DMButton);
        _accountButton = Get<Button>((int)NyangstagramButton.AccountNameTextButton);

        BindButtons();
        SetProfileView();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        NyangstagramNPCProfileUI.OnRequestProfileView += SetProfileView;

        AddPopupButton(_image, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        AddViewButton(_homeButton, true, false);

        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddPopupButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        AddPopupButton(_notificationButton, KeyContainer.Prefabs.NyangStargramNoticePopUpUI);
        AddViewButton(_profileButton, false, true);
        AddCloseButton(_nyangstagramCloseButton);
        AddPopupButton(_dmButton, KeyContainer.Prefabs.NyangStargramDMListPopUpUI);
        AddPopupButton(_accountButton, KeyContainer.Prefabs.NyangStargramNPCProfilePopUpUI);

    }

    private void OnDisable()
    {
        NyangstagramNPCProfileUI.OnRequestProfileView -= SetProfileView;

        RemovePopupButton(_image, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        RemoveViewButton(_homeButton, true, false);
        RemovePopupButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        RemovePopupButton(_notificationButton, KeyContainer.Prefabs.NyangStargramNoticePopUpUI);
        RemoveViewButton(_profileButton, false, true);
        RemoveCloseButton(_nyangstagramCloseButton);
        RemovePopupButton(_dmButton, KeyContainer.Prefabs.NyangStargramDMListPopUpUI);

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

    private void AddViewButton(Button button, bool homeActive, bool profileActive)
    {
        if (button == null) return;
        button.onClick.AddListener(() => SetView(homeActive, profileActive));
    }

    private void RemoveViewButton(Button button, bool homeActive, bool profileActive)
    {
        if (button == null) return;
        button.onClick.RemoveListener(() => SetView(homeActive, profileActive));
    }

    private void AddCloseButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(() => ClosePopup());
    }
    private void RemoveCloseButton(Button button)
    {
        if (button == null) return;
        button?.onClick?.RemoveListener(() => ClosePopup());
    }

    private void SetProfileView()
    {
        SetView(false, true);
    }

    private void SetView(bool homeActive, bool profileActive)
    {
        if(_homeView != null)
        {
            _homeView.SetActive(homeActive);
        }

        if(_profileView != null)
        {
            _profileView.SetActive(profileActive);
        }
    }
    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;
        popup.PlayOpenAnimation();
    }
}

public enum NyangstagramButton
{
    StoryButton,
    Image,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton,
    DMButton,
    AccountNameTextButton
}
