using Core.Managers;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangStargramAddPostUI : UIPopup
{
    [Header("DoTween 설정")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _popupScaleDuration = 0.1f;


    [Header("버튼")]
    [Tooltip("패널닫기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("냥스타그램 닫기 버튼")][SerializeField] private Button _nyangstagramCloseButton;

    [Header("새 게시물 업드로 버튼")]
    [SerializeField] private Button _newImageUploadButton;


    [Header("드롭다운")]
    [SerializeField] private TMP_Dropdown _albumDropdown;

    [Header("이미지")]
    [SerializeField] private Image _newPostImage;
    [SerializeField] private List<Button> _albumImageButton = new();
    [SerializeField] private Transform _albumButtonRoots;

    private NyangStargramAddPostUISprite _nyangStargramAddPostUISprite;
    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramAddPostUIButton));

        _backButton = Get<Button>((int)NyangStargramAddPostUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramAddPostUIButton.NyangstagramCloseButton);
        _albumDropdown = UIBase.FindChild<TMP_Dropdown>(gameObject, "Dropdown", true);
        _newPostImage = UIBase.FindChild<Image>(gameObject, "New Post Image", true);
        _albumButtonRoots = UIBase.FindChild<Transform>(gameObject, "AlbumContent", true);

        BindButtons();
        BindDropdown();
        BindAlbumImages();

        _nyangStargramAddPostUISprite = GetComponent<NyangStargramAddPostUISprite>();
        _nyangStargramAddPostUISprite.Init();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddHideSelfButton(_backButton);
        AddCloseAllButton(_nyangstagramCloseButton);
        AddUploadButton(_newImageUploadButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }
    private void OnDisable()
    {
    }
    private void BindDropdown()
    {
        if( _albumDropdown == null) return;

        _albumDropdown.ClearOptions();
        List<string> options = new()
        {
            "모든 사진",
            "뭉치",
            "일상"
        };
        _albumDropdown.AddOptions(options);
        _albumDropdown.onValueChanged.AddListener(OnChangeAlbumFilter);
    }
    private void OnChangeAlbumFilter(int intdex)
    {
        if(_albumDropdown == null) return;
        if(intdex < 0 || intdex >= _albumDropdown.options.Count) return;
        string selectedOption = _albumDropdown.options[intdex].text;
        
    }

    private void BindAlbumImages()
    {
        _albumImageButton.Clear();

        Button[] albumButtons = _albumButtonRoots.GetComponentsInChildren<Button>(true);
        foreach (Button buttons in albumButtons)
        {
           Image albumImage = buttons.GetComponent<Image>();
            if(albumImage == null)
            {
                albumImage = buttons.GetComponentInChildren<Image>();
            }

            Image selectedImage = albumImage;
            buttons.onClick.RemoveAllListeners();
            buttons.onClick.AddListener(() => SelectAlbumImage(selectedImage));
            _albumImageButton.Add(buttons);

        }
    }

    private void SelectAlbumImage(Image image)
    {
        if (_newPostImage == null) return;
        if( image == null || image.sprite == null) return;
        _newPostImage.sprite = image.sprite;
        _newPostImage.color = Color.white;
        _newPostImage.preserveAspect = true;
    }

    private void AddHideSelfButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
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

    private void HideSelf()
    {
        gameObject.SetActive(false);
    }

    private void AddUploadButton(Button button)
    {
        if(button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickUploadButton);
    }

    private void OnClickUploadButton()
    {
        DebugTool.Log("게시물 업로드 / 아직 미구현", DebugType.UI, this);
    }
    public override void PlayOpenAnimation()
    {
        if (_panel == null) return;

        _panel.anchoredPosition = new Vector2(0f, -1000f);
        _panel.DOAnchorPos(Vector2.zero, _popupScaleDuration)
            .SetEase(Ease.OutSine);
    }

    private void ClosePopup()
    {
        if (_panel == null) return;

        _panel.DOAnchorPos(new Vector2(0f, -1000f), _popupScaleDuration)
            .SetEase(Ease.OutSine).OnComplete(() => gameObject.SetActive(false));

    }
}

public enum NyangStargramAddPostUIButton
{
    BackButton,
    NyangstagramCloseButton,
    NewImageUploadButton
}
