using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramAlbumSlotSprite : UIBase
{
    private UISpriteController[] _spriteController;

    private Image _photoImage;
    private Image _checkMark;

    private Sprite _photoSprite;

    public Sprite PhotoSprite => _photoImage != null ? _photoImage.sprite : null;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramAlbumSlotImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangStargramAlbumSlotImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangStargramAlbumSlotImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _photoImage = GetImage((int)NyangStargramAlbumSlotImages.PhotoImage);
        _checkMark = GetImage((int)NyangStargramAlbumSlotImages.CheckMark);

        SetSprites();

        SetChecked(false);
        SetDark(false);
    }

    private void SetSprites()
    {
        SetSprite(NyangStargramAlbumSlotImages.CheckMark, "Icon_Check");
    }

    private void SetSprite(NyangStargramAlbumSlotImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    public async void SetPhoto(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath)) return;

        Sprite sprite = await FirebaseStorageHelper.LoadUserSpriteAsync(storagePath);

        if (sprite == null) return;

        ReleasePhoto();

        _photoSprite = sprite;

        if (_photoImage != null)
        {
            _photoImage.sprite = _photoSprite;
            _photoImage.color = Color.white;
        }
    }

    public void SetChecked(bool isOn)
    {
        if (_checkMark != null)
            _checkMark.gameObject.SetActive(isOn);
    }

    public void SetDark(bool isDark)
    {
        if (_photoImage == null) return;

        _photoImage.color = isDark
            ? new Color(0.35f, 0.35f, 0.35f, 1f)
            : Color.white;
    }

    private void ReleasePhoto()
    {
        if (_photoSprite == null) return;

        if (_photoSprite.texture != null)
            Destroy(_photoSprite.texture);

        Destroy(_photoSprite);
        _photoSprite = null;
    }

    private void OnDestroy()
    {
        ReleasePhoto();
    }
}

public enum NyangStargramAlbumSlotImages
{
    PhotoImage,
    CheckMark
}