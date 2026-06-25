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
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //바다 냥쿠아리움 배경
        SetSprite(NyangQuariumOceanLayoutUIImage.NyangQuariumLayoutPanel, "NQ_BG_SaltWater");

        //냥쿠아리움 버튼들과 이미지
        SetSprite(NyangQuariumOceanLayoutUIImage.WindowButton, "NQ_Img_AquaTunnel");
        SetSprite(NyangQuariumOceanLayoutUIImage.CatImage, "NQ_Char_Moongchi");
        SetSprite(NyangQuariumOceanLayoutUIImage.LevelImage, "NQ_Icon_TankLevel");
        SetSprite(NyangQuariumOceanLayoutUIImage.BackButton, "NQ_Btn_Back");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanWaterWeedButton, "NQ_Btn_CreatureSalt");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanFishButton, "NQ_Btn_CreatureSalt");
        SetSprite(NyangQuariumOceanLayoutUIImage.ChangeButton, "NQ_Btn_FishFresh");

        //아직 어드레서블 없음
        //SetSprite(NyangQuariumOceanLayoutUIImage.OceanwaterLayoutCloseButton, "NYS_Btn_Exit");
        //SetSprite(NyangQuariumOceanLayoutUIImage.OceanwaterLayoutPanel, "NYS_Btn_Exit");

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
    OceanwaterLayoutCloseButton,
    OceanwaterLayoutPanel,

}