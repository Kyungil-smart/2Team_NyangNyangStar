using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PhotoCollectionPopupSprite : UIBase
{
    [SerializeField] private Color _backgroundColor;
    [SerializeField] private Color _filterPanelColor;

    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(PhotoCollectionPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(PhotoCollectionPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(PhotoCollectionPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(PhotoCollectionPopupImages.Panel, "Snap_Panel_Note");
        SetSprite(PhotoCollectionPopupImages.BackButton, "Btn_Back");
        SetSprite(PhotoCollectionPopupImages.CloseButton, "Btn_Close");
        SetSprite(PhotoCollectionPopupImages.DecorationImage, "NYS_FeedImage_Player_01");
        SetSprite(PhotoCollectionPopupImages.FilterButton, "Btn_Filter");
        SetSprite(PhotoCollectionPopupImages.FilterPanel, "Shape_Square", _filterPanelColor);
        SetSprite(PhotoCollectionPopupImages.FilterCloseButton, "Btn_Close");
        SetSpriteRange(PhotoCollectionPopupImages.Background1, PhotoCollectionPopupImages.Background5, "Shape_Circle", _backgroundColor);
        SetSpriteRange(PhotoCollectionPopupImages.Checkmark1, PhotoCollectionPopupImages.Checkmark5, "Icon_Check");
        SetSpriteRange(PhotoCollectionPopupImages.Star1_1, PhotoCollectionPopupImages.Star4_4, "Snap_Icon_Star");
        SetSpriteRange(PhotoCollectionPopupImages.Star5_1, PhotoCollectionPopupImages.Star5_5, "Snap_Icon_PinkStar");
    }

    private void SetSprite(PhotoCollectionPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(PhotoCollectionPopupImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSpriteRange(PhotoCollectionPopupImages start, PhotoCollectionPopupImages end, string key)
    {
        for (int i = (int)start; i <= (int)end; i++)
        {
            _spriteController[i].ChangeSprite(key);
        }
    }

    private void SetSpriteRange(PhotoCollectionPopupImages start, PhotoCollectionPopupImages end, string key, Color color)
    {
        for (int i = (int)start; i <= (int)end; i++)
        {
            _spriteController[i].ChangeColor(color);
            _spriteController[i].ChangeSprite(key);
        }
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

public enum PhotoCollectionPopupImages
{
    Panel,
    BackButton,
    CloseButton,
    DecorationImage,
    FilterButton,
    FilterPanel,
    FilterCloseButton,
    Background1, Background2, Background3, Background4, Background5,
    Checkmark1, Checkmark2, Checkmark3, Checkmark4, Checkmark5,
    Star1_1,
    Star2_1, Star2_2, 
    Star3_1, Star3_2, Star3_3,
    Star4_1, Star4_2, Star4_3, Star4_4,
    Star5_1, Star5_2, Star5_3, Star5_4, Star5_5,
}