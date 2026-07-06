using Core.Managers;
using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PhotoDetailPopupUI : UIPopup
{
    [Header("닫기 버튼")]
    [Tooltip("뒤로 가기")][SerializeField] private Button _backButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;
    [Tooltip("배경")][SerializeField] private Button _background;

    [Header("냥스타그램 업로드 버튼")]
    [SerializeField] private Button _uploadButton;

    [Header("삭제 버튼")]
    [SerializeField] private Button _deleteButton;

    [Header("사진 데이터")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;

    private NyangNyangSnapRuntimePhotoData _photoData;
    private PhotoDetailPopupSprite _sprite;
    private PhotoCollectionPopupUI _photoCollectionPopup;

    public event Action OnDeleted;

    public override void Init()
    {
        Bind<Button>(typeof(PhotoDetailPopupButtons));

        _closeButton = GetButton((int)PhotoDetailPopupButtons.CloseButton);
        _background = GetButton((int)PhotoDetailPopupButtons.Background);
        _backButton = GetButton((int)PhotoDetailPopupButtons.BackButton);
        _uploadButton = GetButton((int)PhotoDetailPopupButtons.UploadButton);
        _deleteButton = GetButton((int)PhotoDetailPopupButtons.DeleteButton);

        BindButtons();

        _sprite = GetComponent<PhotoDetailPopupSprite>();
        _sprite.Init();
    }

    public void SetData(NyangNyangSnapRuntimePhotoData photoData)
    {
        _photoData = photoData;

        _sprite.SetPhoto(photoData.Sprite);
        _sprite.SetStar(photoData.StarCount);
    }

    public void SetPhotoCollectionPopup(PhotoCollectionPopupUI popup)
    {
        _photoCollectionPopup = popup;
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseAllPopups);
        if (_background != null) _background.onClick.AddListener(CloseAllPopups);
        if (_backButton != null) _backButton.onClick.AddListener(ClosePhotoDetailPopup);
        if (_uploadButton != null) _uploadButton.onClick.AddListener(OpenNyangstagram);
        if (_deleteButton != null) _deleteButton.onClick.AddListener(DeletePhoto);
    }

    private void OnDestroy()
    {
        RemovePopupButton(_closeButton);
        RemovePopupButton(_background);
        RemovePopupButton(_backButton);
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

    private void CloseAllPopups()
    {
        HidePhotoDetailPopup();

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void OpenNyangstagram()
    {
        HidePhotoDetailPopup();
        MainUI.Instance.OpenNyangStargramPopup();
    }

    private void HidePhotoDetailPopup()
    {
        _photoCollectionPopup.HidePhotoCollectionPopup();
        gameObject.SetActive(false);
    }

    private async void DeletePhoto()
    {
        if (!EnsurePhotoAlbumReady())
            return;

        if (!string.IsNullOrEmpty(_photoData.StoragePath))
        {
            bool deleted = await FirebaseStorageHelper.DeleteUserImageAsync(_photoData.StoragePath);

            DebugTool.Log(
                $"[PhotoDetailPopupUI] 사진 삭제 {(deleted ? "성공" : "실패")}: {_photoData.StoragePath}",
                DebugType.Network,
                this);
        }

        try
        {
            _photoAlbumSO.RemovePhoto(_photoData.PhotoId);
            NyangNyangSnapPhotoManager.Instance.RemovePhoto(_photoData.PhotoId);

            await _photoAlbumSO.UpdateDataAsync();
        }
        catch (Exception e)
        {
            DebugTool.Warning($"[PhotoDetailPopupUI] 사진 메타데이터 삭제 실패: {e.Message}", DebugType.Network, this);
            return;
        }

        OnDeleted?.Invoke();

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        gameObject.SetActive(false);
    }

    private bool EnsurePhotoAlbumReady()
    {
        if (_photoAlbumSO == null)
        {
            DebugTool.Warning("[PhotoDetailPopupUI] PhotoAlbumSO가 연결되지 않았습니다.", DebugType.UI, this);
            return false;
        }

        if (_photoAlbumSO.TryEnsureDatabaseReady())
            return true;

        DebugTool.Warning("[PhotoDetailPopupUI] Firestore 준비 전이라 사진 삭제를 건너뜁니다.", DebugType.UI, this);
        return false;
    }
}

public enum PhotoDetailPopupButtons
{
    CloseButton,
    Background,
    BackButton,
    UploadButton,
    DeleteButton
}
