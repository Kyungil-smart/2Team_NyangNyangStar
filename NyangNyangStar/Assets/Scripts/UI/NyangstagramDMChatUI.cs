using Core.Managers;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangstagramDMChatUI : UIPopup
{
    [Header("DoTween 설정")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("버튼")]
    [Tooltip("패널닫기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("냥스타그램 닫기 버튼")][SerializeField] private Button _nyangstagramCloseButton;

    private NyangstagramDMChatUISprite _nyangstagramDMChatUISprite;

    public override void Init()
    {
        Bind<Button>(typeof(NyangstagramDMChatUIButton));

        _backButton = Get<Button>((int)NyangstagramDMChatUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangstagramDMChatUIButton.NyangstagramCloseButton);


        BindButtons();

        _nyangstagramDMChatUISprite = GetComponent<NyangstagramDMChatUISprite>();
        _nyangstagramDMChatUISprite.Init();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddBackToDmList(_backButton);
        AddCloseAllButton(_nyangstagramCloseButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }

    private void OnDisable()
    {

    }

    private void AddHideSelfButton(Button button)
    {
        if(button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HideSelf);
    }

    //모든 팝업 다닫기
    private void AddCloseAllButton(Button button)
    {
        if(button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            NyangstagramUIRouter.RequestCloseAll();
        });
    }
    private void AddBackToDmList(Button button)
    {
        if(button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            ClosePopup();
            NyangstagramUIRouter.RequestOpenPopup(KeyContainer.Prefabs.NyangStargramDMListPopUpUI);
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

}

public enum NyangstagramDMChatUIButton
{
    BackButton,
    NyangstagramCloseButton
}
