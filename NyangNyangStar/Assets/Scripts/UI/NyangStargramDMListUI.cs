using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;
using System;

public class NyangstagramDMListUI : UIPopup
{
    public static event Action OnRequestProfileView;

    [Header("버튼")]
    [Tooltip("DM 채팅 리스트 버튼")][SerializeField] private Button _dmItem;
    [Tooltip("홈 버튼")][SerializeField] private Button _homeButton;
    [Tooltip("테그 버튼")][SerializeField] private Button _tagButton;
    [Tooltip("게시물 추가 버튼")][SerializeField] private Button _addPostButton;
    [Tooltip("알림 버튼")][SerializeField] private Button _notificationButton;
    [Tooltip("프로필 버튼")][SerializeField] private Button _profileButton;
    [Tooltip("냥스타그램 나가기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("패널 닫기 버튼")][SerializeField] private Button _backButton;

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

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddPopupButton(_dmItem, KeyContainer.Prefabs.NyangStargramDMchatPopUpUI);
        AddCloseButton(_homeButton);

        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddChangePopupButton(_addPostButton, KeyContainer.Prefabs.NyangStargramAddPostPopUpUI);
        AddChangePopupButton(_notificationButton, KeyContainer.Prefabs.NyangStargramNoticePopUpUI);
        AddProfileViewButton(_profileButton);
        AddCloseAllPopUpButton(_nyangstagramCloseButton);
        AddCloseButton(_backButton);
    }

    private void OnDisable()
    {
        RemovePopupButton(_dmItem, KeyContainer.Prefabs.NyangStargramPostPopUpUI);
        RemoveCloseButton(_homeButton);
        RemoveChangePopupButton(_addPostButton);
        RemoveChangePopupButton(_notificationButton);
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
    private void AddChangePopupButton(Button button, string key)
    {
        if(button == null) return;
        button.onClick.AddListener(() =>
        {
            ClosePopup();
            GameManager.UI.ShowPopupUI<UIPopup>(key, PlayPopupOpenAnimation);
        });
    }
    private void RemoveChangePopupButton(Button button)
    {
        if(button == null) return;
        button.onClick?.RemoveAllListeners();
    }

    //모든 팝업 다닫기
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
