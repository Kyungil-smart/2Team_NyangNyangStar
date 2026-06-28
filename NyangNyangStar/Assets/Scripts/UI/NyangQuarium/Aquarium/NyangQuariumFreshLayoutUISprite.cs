using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumFreshLayoutUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangQuariumFreshLayoutUIImage));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangQuariumFreshLayoutUIImage)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangQuariumFreshLayoutUIImage)).Length; i++)
            _spriteController[i] = new UISpriteController(GetImage(i));

        SetSprites();
    }

    private void SetSprites()
    {
        // 담수 냥쿠아리움 배경
        SetSprite(NyangQuariumFreshLayoutUIImage.NyangQuariumLayoutPanel, "NQ_BQ_FreshWater");

        SetSprite(NyangQuariumFreshLayoutUIImage.WindowButton, "NQ_Img_AquaTunnel");
        SetSprite(NyangQuariumFreshLayoutUIImage.CatImage, "NQ_Char_Moongchi");
        SetSprite(NyangQuariumFreshLayoutUIImage.LevelImage, "NQ_Icon_TankLevel");
        SetSprite(NyangQuariumFreshLayoutUIImage.BackButton, "NQ_Btn_Back");
        SetSprite(NyangQuariumFreshLayoutUIImage.FreshWaterWeedButton, "NQ_Btn_CreatureFresh");
        SetSprite(NyangQuariumFreshLayoutUIImage.FreshWaterFishButton, "NQ_Btn_FishFresh");
        SetSprite(NyangQuariumFreshLayoutUIImage.ChangeButton, "NQ_Btn_FishSalt");
    }

    private void SetSprite(NyangQuariumFreshLayoutUIImage image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangQuariumFreshLayoutUIImage image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangQuariumFreshLayoutUIImage
{
    NyangQuariumLayoutPanel,
    WindowButton,
    CatImage,
    LevelImage,
    BackButton,
    FreshWaterWeedButton,
    FreshWaterFishButton,
    ChangeButton
}
