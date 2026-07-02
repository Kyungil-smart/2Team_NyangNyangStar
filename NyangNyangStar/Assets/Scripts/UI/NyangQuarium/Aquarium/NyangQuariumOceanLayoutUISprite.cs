using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumOceanLayoutUISprite : UIBase
{
    [SerializeField] private Color _freshwaterLayoutPanel;

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

        SetSprite(NyangQuariumOceanLayoutUIImage.Window, "NQ_Img_AquaTunnel");
        SetSprite(NyangQuariumOceanLayoutUIImage.BackButton, "NQ_Btn_Back");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanFishButton, "NQ_Btn_Inventory");
        SetSprite(NyangQuariumOceanLayoutUIImage.ChangeButton, "NQ_Btn_FreshWater");
        SetSprite(NyangQuariumOceanLayoutUIImage.Confirm, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumOceanLayoutUIImage.Cancle, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumOceanLayoutUIImage.DeleteButton, "NQ_Btn_Delete", new Color32(0,0,0,255));
        SetSprite(NyangQuariumOceanLayoutUIImage.RealDeleteButton, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumOceanLayoutUIImage.RealCancelButton, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumOceanLayoutUIImage.UIToggleOnButton, "NQ_Btn_On");
        SetSprite(NyangQuariumOceanLayoutUIImage.UIToggleOffButton, "NQ_Btn_Off");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanFishCountIcon, "NQ_Icon_Fish");
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanNatureCountIcon, "NQ_Icon_Environment");

        SetSprite(NyangQuariumOceanLayoutUIImage.OceanwaterLayoutPanel, "Shape_Rectangle", _freshwaterLayoutPanel);
        SetSprite(NyangQuariumOceanLayoutUIImage.OceanWaterFishView, "Shape_Rectangle");

        SetSprite(NyangQuariumOceanLayoutUIImage.FishItemTemplate, "Shape_Rectangle");
        SetSprite(NyangQuariumOceanLayoutUIImage.CloseButton, "Btn_Close");
        SetSprite(NyangQuariumOceanLayoutUIImage.confirmButton, "Snap_Btn_White");
        SetSprite(NyangQuariumOceanLayoutUIImage.FishTabButton, "Snap_Btn_White");
        SetSprite(NyangQuariumOceanLayoutUIImage.NatureTextButton, "Snap_Btn_White");
        SetSprite(NyangQuariumOceanLayoutUIImage.FilterButton, "FilterButton");

        SetSprite(NyangQuariumOceanLayoutUIImage.FishTextImage, "Snap_Btn_White");

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
    Window,
    CatImage,
    BackButton,
    OceanFishButton,
    ChangeButton,
    Confirm,
    Cancle,
    DeleteButton,
    RealDeleteButton,
    RealCancelButton,
    UIToggleOnButton,
    UIToggleOffButton,
    OceanFishCountIcon,
    OceanNatureCountIcon,
    OceanwaterLayoutPanel,
    FishItemTemplate,
    CloseButton,
    confirmButton,
    FishTabButton,
    NatureTextButton,
    FilterButton,
    OceanWaterFishView,
    FishTextImage
}
