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
        SetSprite(NyangQuariumOceanLayoutUIImage.BackButton, "NQ_Btn_Back");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanFishButton, "NQ_Btn_Inventory");
        SetSprite(NyangQuariumOceanLayoutUIImage.ChangeButton, "NQ_Btn_FreshWater");
        SetSprite(NyangQuariumOceanLayoutUIImage.Confirm, "NQ_Btn_Confirm");
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
    BackButton,
    OceanFishButton,
    ChangeButton,
    Confirm,
    Cancle
}
