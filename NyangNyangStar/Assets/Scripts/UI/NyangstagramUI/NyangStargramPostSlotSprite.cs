using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramPostSlotSprite : UIBase
{
    private UISpriteController[] _spriteController;
    private Image _photoImage;

    private Sprite _photoSprite;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramPostSlotImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangStargramPostSlotImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangStargramPostSlotImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _photoImage = GetImage((int)NyangStargramPostSlotImages.PhotoImage);

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangStargramPostSlotImages.Frame, "NYS_FeedFrame");
    }

    private void SetSprite(NyangStargramPostSlotImages image, string key)
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
            _photoImage.sprite = _photoSprite;
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

        if (_spriteController == null) return;

        foreach (UISpriteController controller in _spriteController)
        {
            controller?.ReleaseSprite();
        }
    }
}

public enum NyangStargramPostSlotImages
{
    PhotoImage,
    Frame
}