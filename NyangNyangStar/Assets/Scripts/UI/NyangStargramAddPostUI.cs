using Core.Managers;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;
using System;
using TMPro;

public class NyangStargramAddPostUI : UIPopup
{

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


    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramAddPostUIButton));

        _backButton = Get<Button>((int)NyangStargramAddPostUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramAddPostUIButton.NyangstagramCloseButton);
        _albumDropdown = UIBase.FindChild<TMP_Dropdown>(gameObject, "Dropdown", true);
        _newPostImage = UIBase.FindChild<Image>(gameObject, "New Post Image", true);
        _albumButtonRoots = UIBase.FindChild<Transform>(gameObject, "AlbumContent", true);
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramAddPostUIButton.NewImageUploadButton);




        BindButtons();
        BindDropdown();
        BindAlbumImages();

        DebugTool.Log("NyangstagramUI Init 실행됨", DebugType.UI, this);
    }
    private void BindButtons()
    {
        //if (_storyButton != null)
        //    _storyButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));

        AddCloseButton(_backButton);
        AddCloseAllPopUpButton(_nyangstagramCloseButton);
        AddUploadButton(_newImageUploadButton);


        //if (_tagButton != null)
        //    _tagButton.onClick.AddListener(() => GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.ShopPopupUI));


    }
    private void OnDisable()
    {
        RemoveCloseButton(_backButton);
        RemoveCloseAllPopUpButton(_nyangstagramCloseButton);
        RemoveUploadButton(_newImageUploadButton);

        //
        if (_albumDropdown != null)
            _albumDropdown.onValueChanged.RemoveListener(OnChangeAlbumFilter);

        foreach (Button button in _albumImageButton)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        _albumImageButton.Clear();
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
            buttons.onClick.AddListener(() =>
            {
                SelectAlbumImage(selectedImage);
            });
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
    //


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

    private void AddUploadButton(Button button)
    {
        if(button == null) return;
        button.onClick.AddListener(OnClickUploadButtons);
    }
    private void RemoveUploadButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(OnClickUploadButtons);
    }
    private void OnClickUploadButtons()
    {
        DebugTool.Log("게시물 업로드 / 아직 미구현",DebugType.UI,this);
    }

}

public enum NyangStargramAddPostUIButton
{
    BackButton,
    NyangstagramCloseButton,
    NewImageUploadButton
}
