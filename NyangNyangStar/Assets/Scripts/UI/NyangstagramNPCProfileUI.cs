using Core.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangstagramNPCProfileUI : UIPopup
{
    public static event Action OnRequestProfileView;

    [Header("버튼")]
    [Tooltip("스토리 버튼")][SerializeField] private Button _storyButton;
    [Tooltip("이미지 Post 버튼")][SerializeField] private Button _image;
    [Tooltip("홈 버튼")][SerializeField] private Button _homeButton;
    [Tooltip("테그 버튼")][SerializeField] private Button _tagButton;
    [Tooltip("게시물 추가 버튼")][SerializeField] private Button _addPostButton;
    [Tooltip("알림 버튼")][SerializeField] private Button _notificationButton;
    [Tooltip("프로필 버튼")][SerializeField] private Button _profileButton;
    [Tooltip("냥스타그램 나가기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("패널닫기 버튼")][SerializeField] private Button _backButton;


    //private void Awake()
    //{
    //    Init();
    //}

    public override void Init()
    {
        Bind<Button>(typeof(NyangstagramNPCProfileUIButton));

        _storyButton = Get<Button>((int)NyangstagramNPCProfileUIButton.StoryButton);
        _image = Get<Button>((int)NyangstagramNPCProfileUIButton.Image);
        _homeButton = Get<Button>((int)NyangstagramNPCProfileUIButton.HomeButton);
        _tagButton = Get<Button>((int)NyangstagramNPCProfileUIButton.TagButton);
        _addPostButton = Get<Button>((int)NyangstagramNPCProfileUIButton.AddPostButton);
        _notificationButton = Get<Button>((int)NyangstagramNPCProfileUIButton.NotificationButton);
        _profileButton = Get<Button>((int)NyangstagramNPCProfileUIButton.ProfileButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangstagramNPCProfileUIButton.NyangstagramCloseButton);
        _backButton = Get<Button>((int)NyangstagramNPCProfileUIButton.GoHomeButton);


        BindButtons();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddPopupButton(_image, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        AddCloseButton(_homeButton);

        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddPopupButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        AddPopupButton(_notificationButton, KeyContainer.Prefabs.NyangStargramNoticePopUpUI);
        AddProfileViewButton(_profileButton);
        AddCloseAllPopUpButton(_nyangstagramCloseButton);
        AddCloseButton(_backButton);
    }

    private void OnDisable()
    {
        RemovePopupButton(_image, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        RemoveCloseButton(_homeButton);
        RemovePopupButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        RemovePopupButton(_notificationButton, KeyContainer.Prefabs.NyangStargramNoticePopUpUI);
        RemoveProfileViewButton(_profileButton);
        RemoveCloseAllPopUpButton(_nyangstagramCloseButton);
        RemoveCloseButton(_backButton);

    }

    private void AddProfileViewButton(Button button)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            OnRequestProfileView?.Invoke();
            ClosePopup();
        });
    }

    private void RemoveProfileViewButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveListener(() =>
        {
            OnRequestProfileView?.Invoke();
            ClosePopup();
        });
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

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;
        popup.PlayOpenAnimation();
    }
    private void AddCloseAllPopUpButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            UIPopup[] activePopups = FindObjectsByType<UIPopup>
            (
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None

            );

            for (int i = 0; i < activePopups.Length; i++)
            {
                ClosePopup();
            }
        });
    }
    private void RemoveCloseAllPopUpButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
    }
}

public enum NyangstagramNPCProfileUIButton
{
    StoryButton,
    Image,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton,
    GoHomeButton
}
