using Core.Managers;
using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PhotoDetailPopupUI : UIPopup
{
    [Header("닫기 버튼")]
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;
    [Tooltip("배경")][SerializeField] private Button _background;

    [Header("냥스타그램 업로드 버튼")]
    [SerializeField] private Button _uploadButton;

    [Header("삭제 버튼")]
    [SerializeField] private Button _deleteButton;

    [Header("사진 데이터")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;

    private NyangNyangSnapSavedPhotoData _photoData;
    private PhotoDetailPopupSprite _sprite;
    private PhotoCollectionPopupUI _photoCollectionPopup;

    public event Action OnDeleted;

    public override void Init()
    {
        Bind<Button>(typeof(PhotoDetailPopupButtons));

        _closeButton = GetButton((int)PhotoDetailPopupButtons.CloseButton);
        _background = GetButton((int)PhotoDetailPopupButtons.Background);
        _uploadButton = GetButton((int)PhotoDetailPopupButtons.UploadButton);
        _deleteButton = GetButton((int)PhotoDetailPopupButtons.DeleteButton);

        BindButtons();

        _sprite = GetComponent<PhotoDetailPopupSprite>();
        _sprite.Init();
    }

    public void SetData(NyangNyangSnapSavedPhotoData photoData)
    {
        _photoData = photoData;

        _sprite.SetPhoto(photoData.storagePath);
        _sprite.SetStar(photoData.starCount);
    }

    public void SetPhotoCollectionPopup(PhotoCollectionPopupUI popup)
    {
        _photoCollectionPopup = popup;
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(() => ClosePhotoDetailPopup());
        if (_background != null) _background.onClick.AddListener(() => ClosePhotoDetailPopup());
        if (_uploadButton != null) _uploadButton.onClick.AddListener(() => OpenNyangstagram());
        if (_deleteButton != null) _deleteButton.onClick.AddListener(() => DeletePhoto());
    }

    private void OnDestroy()
    {
        RemovePopupButton(_closeButton);
        RemovePopupButton(_background);
        RemovePopupButton(_uploadButton);
        RemovePopupButton(_deleteButton);
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
    }

    private void ClosePhotoDetailPopup()
    {
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void OpenNyangstagram()
    {
        _photoCollectionPopup.HidePhotoCollectionPopup();
        gameObject.SetActive(false);
        MainUI.Instance.OpenNyangStargramPopup();
    }

    private async void DeletePhoto()
    {
        if (!string.IsNullOrEmpty(_photoData.storagePath))
        {
            bool deleted = await FirebaseStorageHelper.DeleteUserImageAsync(_photoData.storagePath);

            DebugTool.Log(
                $"[PhotoDetailPopupUI] 사진 삭제 {(deleted ? "성공" : "실패")}: {_photoData.storagePath}",
                DebugType.Network,
                this);
        }

        _photoAlbumSO.RemovePhoto(_photoData.photoId);
        await _photoAlbumSO.UpdateDataAsync();

        OnDeleted?.Invoke();

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        gameObject.SetActive(false);
    }
}

public enum PhotoDetailPopupButtons
{
    CloseButton,
    Background,
    UploadButton,
    DeleteButton
}