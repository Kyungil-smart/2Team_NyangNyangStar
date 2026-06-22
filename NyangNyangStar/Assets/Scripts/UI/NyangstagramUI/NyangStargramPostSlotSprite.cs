using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramPostSlotSprite : UIBase
{
    private UISpriteController[] _spriteController;
    private Image _photoImage;

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

    public void SetPhoto(Sprite sprite)
    {
        if (_photoImage == null || sprite == null)
            return;

        _photoImage.sprite = sprite;
    }

    private void OnDestroy()
    {
        if (_spriteController == null)
            return;

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