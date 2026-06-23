using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;
using System;
using UI.Base;

public class NyangStargramNotificationUI : UIPopup
{
    public static event Action OnRequestProfileView;

    [Header("버튼")]
    [Tooltip("DM 채팅 리스트 버튼")][SerializeField] private Button _notice;
    [Tooltip("홈 버튼")][SerializeField] private Button _homeButton;
    [Tooltip("테그 버튼")][SerializeField] private Button _tagButton;
    [Tooltip("게시물 추가 버튼")][SerializeField] private Button _addPostButton;
    [Tooltip("알림 버튼")][SerializeField] private Button _notificationButton;
    [Tooltip("프로필 버튼")][SerializeField] private Button _profileButton;
    [Tooltip("냥스타그램 나가기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("패널 닫기 버튼")][SerializeField] private Button _backButton;


    private NyangStargramNotificationUISprite _nyangStargramNotificationUISprite;
    private NyangstagramMainUI _mainUI;

    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramNotificationUIButton));

        _notice = Get<Button>((int)NyangStargramNotificationUIButton.Notice);
        _homeButton = Get<Button>((int)NyangStargramNotificationUIButton.HomeButton);
        _tagButton = Get<Button>((int)NyangStargramNotificationUIButton.TagButton);
        
        _addPostButton = Get<Button>((int)NyangStargramNotificationUIButton.AddPostButton);
        _notificationButton = Get<Button>((int)NyangStargramNotificationUIButton.NotificationButton);
        _profileButton = Get<Button>((int)NyangStargramNotificationUIButton.ProfileButton);
        
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramNotificationUIButton.NyangstagramCloseButton);
        _backButton = Get<Button>((int)NyangStargramNotificationUIButton.BackButton);


        BindButtons();

        _nyangStargramNotificationUISprite =
     GetComponent<NyangStargramNotificationUISprite>();

        if (_nyangStargramNotificationUISprite != null)
        {
            _nyangStargramNotificationUISprite.Init();
            _nyangStargramNotificationUISprite.SetNotificationTab();
        }

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    public void SetMainUI(NyangstagramMainUI mainUI)
    {
        _mainUI = mainUI;
    }

    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddOpenPopUpButton(_notice, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        AddHomeViewButton(_homeButton);

        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddOpenPopUpButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        AddProfileViewButton(_profileButton);
        AddCloseAllButton(_nyangstagramCloseButton);
        AddHideSelfButton(_backButton);

        ClearButton(_notificationButton);
    }

    private void OnDisable()
    {

    }

    private void AddOpenPopUpButton(Button button, string key)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            HideSelf();
            NyangstagramUIRouter.RequestOpenPopup(key);
        });
    }

    private void AddHomeViewButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            HideSelf();
            NyangstagramUIRouter.RequestHomeView();
        });
    }
    private void AddProfileViewButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            HideSelf();
            NyangstagramUIRouter.RequestProfileView();
        });
    }
    private void AddHideSelfButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (_mainUI != null)
            {
                _mainUI.RestoreMainTab();
            }

            HideSelf();
        });
    }

    private void AddCloseAllButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            NyangstagramUIRouter.RequestCloseAll();
        });
    }
    private void ClearButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
    }
    private void HideSelf()
    {
        gameObject.SetActive(false);
    }
}

public enum NyangStargramNotificationUIButton
{
    BackButton,
    Notice,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton
}
