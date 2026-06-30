using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumOceanLayoutUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangQuariumOceanLayoutUIImage));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangQuariumOceanLayoutUIImage)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangQuariumOceanLayoutUIImage)).Length; i++)
            _spriteController[i] = new UISpriteController(GetImage(i));

        SetSprites();
    }

    private void SetSprites()
    {
        // 해수 냥쿠아리움 배경
        SetSprite(NyangQuariumOceanLayoutUIImage.NyangQuariumLayoutPanel, "NQ_BG_SaltWater");

        SetSprite(NyangQuariumOceanLayoutUIImage.WindowButton, "NQ_Img_AquaTunnel");
        SetSprite(NyangQuariumOceanLayoutUIImage.LevelImage, "NQ_Icon_TankLevel");
        SetSprite(NyangQuariumOceanLayoutUIImage.BackButton, "NQ_Btn_Back");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanWaterWeedButton, "NQ_Btn_CreatureSalt");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanFishButton, "NQ_Btn_FishSalt");
        SetSprite(NyangQuariumOceanLayoutUIImage.ChangeButton, "NQ_Btn_FishFresh");
        //SetSprite(NyangQuariumOceanLayoutUIImage.Confirm, "NQ_Btn_FishFresh");
        SetSprite(NyangQuariumOceanLayoutUIImage.Cancle, "Btn_Close");

    }

    private void SetSprite(NyangQuariumOceanLayoutUIImage image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangQuariumOceanLayoutUIImage image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangQuariumOceanLayoutUIImage
{
    NyangQuariumLayoutPanel,
    WindowButton,
    CatImage,
    LevelImage,
    BackButton,
    OceanWaterWeedButton,
    OceanFishButton,
    ChangeButton,
    Confirm,
    Cancle
}
