using System;
using System.IO;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class CatPhotoSlotSprite : UIBase
{
    private const string NormalStarKey = "Snap_Icon_Star";
    private const string PinkStarKey = "Snap_Icon_PinkStar";

    private UISpriteController[] _spriteController;

    private Image _catImage;
    private Image _star1;
    private Image _star2;
    private Image _star3;
    private TMP_Text _starCountText;

    public override void Init()
    {
        Bind<Image>(typeof(CatPhotoSlotImages));
        Bind<TMP_Text>(typeof(CatPhotoSlotTexts));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(CatPhotoSlotImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(CatPhotoSlotImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _catImage = GetImage((int)CatPhotoSlotImages.CatImage);
        _star1 = GetImage((int)CatPhotoSlotImages.Star1);
        _star2 = GetImage((int)CatPhotoSlotImages.Star2);
        _star3 = GetImage((int)CatPhotoSlotImages.Star3);

        _starCountText = GetText((int)CatPhotoSlotTexts.StarCountText);

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(CatPhotoSlotImages.Star1, NormalStarKey);
        SetSprite(CatPhotoSlotImages.Star2, NormalStarKey);
        SetSprite(CatPhotoSlotImages.Star3, NormalStarKey);
    }

    private void SetSprite(CatPhotoSlotImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void SetPhoto(string imagePath)
    {
        if (!File.Exists(imagePath)) return;

        byte[] bytes = File.ReadAllBytes(imagePath);

        Texture2D texture = new Texture2D(1,1);
        texture.LoadImage(bytes);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (_catImage != null)
        {
            _catImage.sprite = sprite;
        }
    }

    public void SetStar(int starCount)
    {
        SetStarActive(false, false, false);
        _starCountText.gameObject.SetActive(false);

        SetSprite(CatPhotoSlotImages.Star1, NormalStarKey);

        if (starCount <= 0) return;

        if (starCount <= 3)
        {
            SetStarActive(starCount >= 1, starCount >= 2, starCount >= 3);
            return;
        }

        SetStarActive(true, false, false);

        if (starCount == 5)
            SetSprite(CatPhotoSlotImages.Star1, PinkStarKey);

        _starCountText.gameObject.SetActive(true);
        _starCountText.text = $"X {starCount}";
    }

    private void SetStarActive(bool star1, bool star2, bool star3)
    {
        _star1.gameObject.SetActive(star1);
        _star2.gameObject.SetActive(star2);
        _star3.gameObject.SetActive(star3);
    }
}

public enum CatPhotoSlotImages
{
    CatImage,
    Star1,
    Star2,
    Star3
}

public enum CatPhotoSlotTexts
{
    StarCountText
}