using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramAlbumSlotUI : UIBase
{
    [SerializeField] private Button _photoButton;

    private NyangStargramAlbumSlotSprite _sprite;
    private NyangStargramAddPostUI _addPostUI;
    private NyangNyangSnapRuntimePhotoData _photoData;

    private bool _isUploaded;
    private bool _isSelected;

    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramAlbumSlotButtons));

        _photoButton = GetButton((int)NyangStargramAlbumSlotButtons.PhotoImage);

        if (_photoButton != null)
        {
            _photoButton.onClick.RemoveAllListeners();
            _photoButton.onClick.AddListener(OnClickSlot);
        }

        _sprite = GetComponent<NyangStargramAlbumSlotSprite>();
        _sprite.Init();
    }

    public void SetAddPostUI(NyangStargramAddPostUI addPostUI)
    {
        _addPostUI = addPostUI;
    }

    public void SetData(NyangNyangSnapRuntimePhotoData photoData, bool isUploaded)
    {
        _photoData = photoData;
        _isUploaded = isUploaded;

        _sprite.SetPhoto(photoData.Sprite);

        SetSelected(false);
        SetUploaded(_isUploaded);
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;

        if (_isUploaded)
        {
            _sprite.SetChecked(true);
            _sprite.SetDark(true);
            return;
        }

        _sprite.SetChecked(false);
        _sprite.SetDark(_isSelected);
    }

    public void SetUploaded(bool isUploaded)
    {
        _isUploaded = isUploaded;

        _sprite.SetChecked(_isUploaded);
        _sprite.SetDark(_isUploaded || _isSelected);

        if (_photoButton != null)
            _photoButton.interactable = !_isUploaded;
    }

    private void OnClickSlot()
    {
        if (_isUploaded) return;

        Sprite sprite = _sprite.PhotoSprite;

        _addPostUI.SelectAlbumImage(this, _photoData, sprite);
    }

    private void OnDestroy()
    {
        if (_photoButton != null)
            _photoButton.onClick.RemoveAllListeners();
    }
}

public enum NyangStargramAlbumSlotButtons
{
    PhotoImage
}