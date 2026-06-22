using System;
using System.IO;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PhotoDetailPopupSprite : UIBase
{
    private const string NormalStarKey = "Snap_Icon_Star";
    private const string PinkStarKey = "Snap_Icon_PinkStar";

    private UISpriteController[] _spriteController;

    private Image _catImage;
    private Image _star1;
    private Image _star2;
    private Image _star3;
    private Image _star4;
    private Image _star5;
    private Image[] _stars;

    private Sprite _photoSprite;

    public override void Init()
    {
        Bind<Image>(typeof(PhotoDetailPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(PhotoDetailPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(PhotoDetailPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _catImage = GetImage((int)PhotoDetailPopupImages.CatImage);
        _star1 = GetImage((int)PhotoDetailPopupImages.Star1);
        _star2 = GetImage((int)PhotoDetailPopupImages.Star2);
        _star3 = GetImage((int)PhotoDetailPopupImages.Star3);
        _star4 = GetImage((int)PhotoDetailPopupImages.Star4);
        _star5 = GetImage((int)PhotoDetailPopupImages.Star5);
        _stars = new Image[] { _star1, _star2, _star3, _star4, _star5 };

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(PhotoDetailPopupImages.Panel, "Snap_Panel_Note");
        SetSprite(PhotoDetailPopupImages.CloseButton, "Btn_Close");
        SetSprite(PhotoDetailPopupImages.UploadButton, "Btn_Nyangstagram");
        SetSprite(PhotoDetailPopupImages.DeleteButton, "Snap_Btn_Delete");
    }

    private void SetSprite(PhotoDetailPopupImages image, string key)
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

        if (_catImage != null)
            _catImage.sprite = _photoSprite;
    }

    public void SetStar(int starCount)
    {
        bool isPink = starCount == 5;

        for (int i = 0; i < _stars.Length; i++)
        {
            SetSprite((PhotoDetailPopupImages)((int)PhotoDetailPopupImages.Star1 + i), isPink ? PinkStarKey : NormalStarKey);

            _stars[i].gameObject.SetActive(i < starCount);
        }
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

public enum PhotoDetailPopupImages
{
    Panel,
    CloseButton,
    CatImage,
    Star1, Star2, Star3, Star4, Star5,
    UploadButton,
    DeleteButton,
}
