using Core.Managers;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangStargramPostUI : UIPopup
{
    [Header("DoTween 설정")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _popupScale = 0.85f;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("버튼")]
    [Tooltip("패널닫기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("냥스타그램 닫기 버튼")][SerializeField] private Button _nyangstagramCloseButton;
    [Tooltip("좋아요 버튼")][SerializeField] private Button _likeButton;
    [Tooltip("좋아요 text")][SerializeField] private TMP_Text _likeCountText;

    [SerializeField] private int _likeCount = 26667;
    private bool _isLiked;

    private NyangStargramPostUISprite _nyangStargramPostUISprite;

    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramPostUIButton));

        _backButton = Get<Button>((int)NyangStargramPostUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramPostUIButton.NyangstagramCloseButton);
        _likeButton = Get<Button>((int)NyangStargramPostUIButton.LikeButton);
        _likeCountText = UIBase.FindChild<TMP_Text>(gameObject, "Like Count", true);


        BindButtons();
        ReFreshLikeCountText();

        _nyangStargramPostUISprite = GetComponent<NyangStargramPostUISprite>();
        _nyangStargramPostUISprite.Init();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddHideSelfButton(_backButton);
        AddCloseAllButton(_nyangstagramCloseButton);
        AddLikeButton(_likeButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }

    private void OnDisable()
    {

    }

    private void AddHideSelfButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(ClosePopup);
    }

    //모든 팝업 다닫기
    private void AddCloseAllButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            NyangstagramUIRouter.RequestCloseAll();
        });
    }

    private void AddLikeButton(Button button)
    {
        if(button == null) return;
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
    private void HideSelf()
    {
        gameObject.SetActive(false);
    }

    public override void PlayOpenAnimation()
    {
        if (_panel == null) return;

        _panel.localScale = Vector3.one * _popupScale;
        _panel.DOScale(1f, _popupScaleDuration)
            .SetEase(Ease.OutSine);
    }

    private void ClosePopup()
    {
        if (_panel == null) return;
        _panel.DOScale(Vector3.one * _popupScale, _popupScaleDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(() => gameObject.SetActive(false));
    }
}

public enum NyangStargramPostUIButton
{
    BackButton,
    NyangstagramCloseButton,
    LikeButton
}
