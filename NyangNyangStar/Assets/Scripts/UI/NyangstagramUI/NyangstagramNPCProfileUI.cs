using Core.Managers;
using DG.Tweening;
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
    
    [Header("DOTween 설정")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _popupScaleDuration = 0.1f;

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

    private NyangstagramNPCProfileUISprite _nyangstagramNPCProfileUISprite;
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

        _nyangstagramNPCProfileUISprite = GetComponent<NyangstagramNPCProfileUISprite>();
        _nyangstagramNPCProfileUISprite.Init();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        AddOpenPopUpButton(_image, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        AddHomeViewButton(_homeButton);
        AddOpenPopUpButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        AddOpenPopUpButton(_notificationButton, KeyContainer.Prefabs.NyangStargramNoticePopUpUI);
        AddProfileViewButton(_profileButton);
        AddCloseAllButton(_nyangstagramCloseButton);
        AddHideSelfButton(_backButton);
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
            NyangstagramUIRouter.RequestHomeView();
        });
    }
    private void AddHideSelfButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HideSelf);
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

    private void HideSelf()
    {
        gameObject.SetActive(false);
    }
    public override void PlayOpenAnimation()
    {
        if (_panel == null) return;

        _panel.anchoredPosition = new Vector2(1000f, 0f);
        _panel.DOAnchorPos(Vector2.zero, _popupScaleDuration)
            .SetEase(Ease.OutSine);
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
