using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumFreshLayoutUISprite : UIBase
{
    [SerializeField] private Color _freshwaterLayoutPanelColor;

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
        SetSprite(NyangQuariumFreshLayoutUIImage.NyangQuariumLayoutPanel, "NQ_BG_FreshWater");

        SetSprite(NyangQuariumFreshLayoutUIImage.BackButton, "NQ_Btn_MergeBack");
        SetSprite(NyangQuariumFreshLayoutUIImage.FreshWaterFishButton, "NQ_Btn_Inventory");
        SetSprite(NyangQuariumFreshLayoutUIImage.ChangeButton, "NQ_Btn_SaltWater");
        SetSprite(NyangQuariumFreshLayoutUIImage.LockIcon, "NQ_Icon_Lock");


        SetSprite(NyangQuariumFreshLayoutUIImage.Window, "NQ_Img_AquaTunnel");
        SetSprite(NyangQuariumFreshLayoutUIImage.CatImage, "NQ_Char_Moongchi");
        SetSprite(NyangQuariumFreshLayoutUIImage.Confirm, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumFreshLayoutUIImage.Cancle, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumFreshLayoutUIImage.DeleteButton, "NQ_Btn_Delete",new Color32(0,0,0,255));
        SetSprite(NyangQuariumFreshLayoutUIImage.RealDeleteButton, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumFreshLayoutUIImage.RealCancelButton, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumFreshLayoutUIImage.UIToggleOnButton, "NQ_Btn_On");
        SetSprite(NyangQuariumFreshLayoutUIImage.UIToggleOffButton, "NQ_Btn_Off");


        SetSprite(NyangQuariumFreshLayoutUIImage.FreshFishCountIcon, "NQ_Icon_Fish");
        SetSprite(NyangQuariumFreshLayoutUIImage.FreshNatureCountIcon, "NQ_Icon_Environment");


        SetSprite(NyangQuariumFreshLayoutUIImage.FreshwaterLayoutPanel, "Shape_Rectangle", _freshwaterLayoutPanelColor);
        SetSprite(NyangQuariumFreshLayoutUIImage.FreshwaterFishView, "Shape_Rectangle");

        SetSprite(NyangQuariumFreshLayoutUIImage.FishItemTemplate, "Shape_Rectangle");
        SetSprite(NyangQuariumFreshLayoutUIImage.CloseButton, "Btn_Close");
        SetSprite(NyangQuariumFreshLayoutUIImage.confirmButton, "Snap_Btn_White");
        SetSprite(NyangQuariumFreshLayoutUIImage.FishTabButton, "Snap_Btn_White");
        SetSprite(NyangQuariumFreshLayoutUIImage.NatureTextButton, "Snap_Btn_White");
        SetSprite(NyangQuariumFreshLayoutUIImage.FilterButton, "FilterButton");
        
        SetSprite(NyangQuariumFreshLayoutUIImage.FishFilterTab, "Shape_Rectangle");
        SetSprite(NyangQuariumFreshLayoutUIImage.NatureFilterTab, "Shape_Rectangle");

        SetSprite(NyangQuariumFreshLayoutUIImage.FishTextImage, "Snap_Btn_White");

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
    Window,
    CatImage,
    BackButton,
    FreshWaterFishButton,
    ChangeButton,
    LockIcon,
    Confirm,
    Cancle,
    DeleteButton,
    RealDeleteButton,
    RealCancelButton,
    UIToggleOnButton,
    UIToggleOffButton,
    FreshFishCountIcon,
    FreshNatureCountIcon,
    FreshwaterLayoutPanel,
    FishItemTemplate,
    CloseButton,
    confirmButton,
    FishTabButton,
    NatureTextButton,
    FilterButton,
    FreshwaterFishView,
    FishTextImage,
    FishFilterTab,
    NatureFilterTab
}
