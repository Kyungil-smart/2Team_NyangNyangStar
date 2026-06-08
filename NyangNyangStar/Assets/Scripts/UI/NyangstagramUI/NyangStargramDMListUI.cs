using Core.Managers;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangstagramDMListUI : UIPopup
{

    [Header("DoTween 설정")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("버튼")]
    [Tooltip("DM 채팅 리스트 버튼")][SerializeField] private Button _dmItem;
    [Tooltip("홈 버튼")][SerializeField] private Button _homeButton;
    [Tooltip("테그 버튼")][SerializeField] private Button _tagButton;
    [Tooltip("게시물 추가 버튼")][SerializeField] private Button _addPostButton;
    [Tooltip("알림 버튼")][SerializeField] private Button _notificationButton;
    [Tooltip("프로필 버튼")][SerializeField] private Button _profileButton;
    [Tooltip("냥스타그램 나가기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("패널 닫기 버튼")][SerializeField] private Button _backButton;


    private NyangStargramDMListUISprite _nyangStargramDMListUISprite;
    public override void Init()
    {
        Bind<Button>(typeof(NyangstagramDMListUIButton));

        _dmItem = Get<Button>((int)NyangstagramDMListUIButton.DMItem);
        _homeButton = Get<Button>((int)NyangstagramDMListUIButton.HomeButton);
        _tagButton = Get<Button>((int)NyangstagramDMListUIButton.TagButton);
        _addPostButton = Get<Button>((int)NyangstagramDMListUIButton.AddPostButton);
        _notificationButton = Get<Button>((int)NyangstagramDMListUIButton.NotificationButton);
        _profileButton = Get<Button>((int)NyangstagramDMListUIButton.ProfileButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangstagramDMListUIButton.NyangstagramCloseButton);
        _backButton = Get<Button>((int)NyangstagramDMListUIButton.BackButton);


        BindButtons();

        _nyangStargramDMListUISprite = GetComponent<NyangStargramDMListUISprite>();
        _nyangStargramDMListUISprite.Init();
        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddDMChatButton(_dmItem);
        AddHomeViewButton(_homeButton);

        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

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
    private void AddDMChatButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            HideSelf();
            NyangstagramUIRouter.RequestOpenPopup(KeyContainer.Prefabs.NyangStargramDMchatPopUpUI);

            DebugTool.Log("DMList → DMChat 전환 요청", DebugType.UI, this);
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
        button.onClick.AddListener(ClosePopup);
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

    private void ClosePopup()
    {
        if (_panel == null) return;

        _panel.DOAnchorPos(new Vector2(1000f, 0f), _popupScaleDuration)
            .SetEase(Ease.OutSine).OnComplete(() => gameObject.SetActive(false));

    }

    public enum NyangstagramDMListUIButton
    {
        BackButton,
        DMItem,
        HomeButton,
        TagButton,
        AddPostButton,
        NotificationButton,
        ProfileButton,
        NyangstagramCloseButton
    }
}