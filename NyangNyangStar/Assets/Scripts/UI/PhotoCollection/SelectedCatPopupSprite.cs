using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class SelectedCatPopupSprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(SelectedCatPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(SelectedCatPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(SelectedCatPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(SelectedCatPopupImages.Panel, "Snap_Panel_Note");
        SetSprite(SelectedCatPopupImages.BackButton, "Btn_Back");
        SetSprite(SelectedCatPopupImages.CloseButton, "Btn_Close");
        SetSprite(SelectedCatPopupImages.CatBackground, "Snap_BG");
        SetSprite(SelectedCatPopupImages.CatImage, "Profile_ProfileImages_2");
        SetSprite(SelectedCatPopupImages.CollectionButton, "Btn_Camera");
        SetSprite(SelectedCatPopupImages.RedPoint, "Shape_Circle", Color.red);
    }

    private void SetSprite(SelectedCatPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(SelectedCatPopupImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void OnDestroy()
    {
        if (_spriteController == null) return;

        foreach (UISpriteController controller in _spriteController)
        {
            controller?.ReleaseSprite();
        }
    }
}

public enum SelectedCatPopupImages
{
    Panel,
    BackButton,
    CloseButton,
    CatBackground,
    CatImage,
    CollectionButton,
    RedPoint
}