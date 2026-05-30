using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;
using System;
using TMPro;

public class NyangStargramPostUI : UIPopup
{

    [Header("버튼")]
    [Tooltip("패널닫기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("냥스타그램 닫기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("좋아요 버튼")][SerializeField] private Button _likeButton;
    [Tooltip("좋아요 text")][SerializeField] private TMP_Text _likeCountText;

    [SerializeField] private int _likeCount = 26667;
    private bool _isLiked;



    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramPostUIButton));

        _backButton = Get<Button>((int)NyangStargramPostUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramPostUIButton.NyangstagramCloseButton);
        _likeButton = Get<Button>((int)NyangStargramPostUIButton.LikeButton);
        _likeCountText = UIBase.FindChild<TMP_Text>(gameObject, "Like Count", true);


        BindButtons();
        ReFreshLikeCountText();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddCloseButton(_backButton);
        AddCloseAllPopUpButton(_nyangstagramCloseButton);
        AddLikeButton(_likeButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }

    private void OnDisable()
    {
        RemoveCloseButton(_backButton);
        RemoveCloseAllPopUpButton(_nyangstagramCloseButton);
        RemoveLikeButton(_likeButton);


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

    private void AddLikeButton(Button button)
    {
        if(button == null) return;
        button.onClick.AddListener(OnClickLikeButton);
    }
    private void RemoveLikeButton(Button button)
    {
        if(button == null) return;
        button.onClick.RemoveListener(OnClickLikeButton);
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
            _isLiked= true;
        }

        ReFreshLikeCountText();
    }
    private void ReFreshLikeCountText()
    {
        if (_likeCountText == null) return;

        //_likeCountText.text = $"Like {_likeCount:N0}";
        _likeCountText.text = $"Like {_likeCount}";
    }
}

public enum NyangStargramPostUIButton
{
    BackButton,
    NyangstagramCloseButton,
    LikeButton
}
