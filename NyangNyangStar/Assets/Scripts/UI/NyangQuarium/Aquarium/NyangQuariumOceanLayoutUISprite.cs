using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumOceanLayoutUISprite : UIBase
{

    private const string UIToggleOnSpriteKey = "NQ_Btn_On";
    private const string UIToggleOffSpriteKey = "NQ_Btn_Off";

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
        SetSprite(NyangQuariumOceanLayoutUIImage.Cancle, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumOceanLayoutUIImage.DeleteButton, "NQ_Btn_Delete");
        SetSprite(NyangQuariumOceanLayoutUIImage.RealDeleteButton, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumOceanLayoutUIImage.RealCancelButton, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumOceanLayoutUIImage.UIToggleButton, UIToggleOnSpriteKey);
        //SetSprite(NyangQuariumFreshLayoutUIImage.FreshwaterLayoutPanel, "NQ_Char_Moongchi");
        //SetSprite(NyangQuariumFreshLayoutUIImage.FishItemTemplate, "NQ_Char_Moongchi");
        SetSprite(NyangQuariumOceanLayoutUIImage.CloseButton, "Btn_Close");
        SetSprite(NyangQuariumOceanLayoutUIImage.confirmButton, "Snap_Btn_White");
        SetSprite(NyangQuariumOceanLayoutUIImage.FishTabButton, "Snap_Btn_White");
        SetSprite(NyangQuariumOceanLayoutUIImage.NatureTextButton, "Snap_Btn_White");
        SetSprite(NyangQuariumOceanLayoutUIImage.FilterButton, "FilterButton");
    }
    public void SetUIToggleSprite(bool isUIVisible)
    {
        string spriteKey = isUIVisible
            ? UIToggleOnSpriteKey
            : UIToggleOffSpriteKey;

        SetSprite(
            NyangQuariumOceanLayoutUIImage.UIToggleButton,
            spriteKey);

        DebugTool.Log(
            $"[NyangQuariumOceanLayoutUISprite] UI 토글 이미지 변경: {spriteKey}",
            DebugType.UI,
            this);
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
    Cancle,
    DeleteButton,
    RealDeleteButton,
    RealCancelButton,
    UIToggleButton,
    FreshwaterLayoutPanel,
    FishItemTemplate,
    CloseButton,
    confirmButton,
    FishTabButton,
    NatureTextButton,
    FilterButton
}
