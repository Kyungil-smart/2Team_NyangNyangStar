using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;
using System;

public class NyangstagramDMChatUI : UIPopup
{

    [Header("버튼")]
    [Tooltip("패널닫기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("냥스타그램 닫기 버튼")][SerializeField] private Button _nyangstagramCloseButton;


    public override void Init()
    {
        Bind<Button>(typeof(NyangstagramDMChatUIButton));

        _backButton = Get<Button>((int)NyangstagramDMChatUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangstagramDMChatUIButton.NyangstagramCloseButton);


        BindButtons();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddCloseButton(_backButton);
        AddCloseAllPopUpButton(_nyangstagramCloseButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }

    private void OnDisable()
    {
        RemovePopupButton(_backButton, KeyContainer.Prefabs.NyangStargramDMchatPopUpUI);
        RemoveCloseAllPopUpButton(_nyangstagramCloseButton);


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

    //모든 팝업 다닫기
    private void AddCloseAllPopUpButton(Button button)
    {
        if(button == null) return;
        button.onClick.AddListener(() =>
        {
            UIPopup[] activePopups = FindObjectsByType<UIPopup>
            (
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None

            );

            for(int i = 0; i < activePopups.Length; i++)
            {
                ClosePopup();
            }
        });
    }
    private void RemoveCloseAllPopUpButton( Button button)
    {
        if(button == null) return;
        button.onClick.RemoveAllListeners();
    }

}

public enum NyangstagramDMChatUIButton
{
    BackButton,
    NyangstagramCloseButton
}
