using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumFreshLayoutUISprite : UIBase
{
    private const string UIToggleOnSpriteKey = "NQ_Btn_On";
    private const string UIToggleOffSpriteKey = "NQ_Btn_Off";

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

        SetSprite(NyangQuariumFreshLayoutUIImage.BackButton, "NQ_Btn_Back");
        SetSprite(NyangQuariumFreshLayoutUIImage.FreshWaterFishButton, "NQ_Btn_Inventory");
        SetSprite(NyangQuariumFreshLayoutUIImage.ChangeButton, "NQ_Btn_SaltWater");

        SetSprite(NyangQuariumFreshLayoutUIImage.WindowButton, "NQ_Img_AquaTunnel");
        SetSprite(NyangQuariumFreshLayoutUIImage.CatImage, "NQ_Char_Moongchi");
        SetSprite(NyangQuariumFreshLayoutUIImage.Confirm, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumFreshLayoutUIImage.Cancle, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumFreshLayoutUIImage.DeleteButton, "NQ_Btn_Delete");
        SetSprite(NyangQuariumFreshLayoutUIImage.RealDeleteButton, "NQ_Btn_Confirm");
        SetSprite(NyangQuariumFreshLayoutUIImage.RealCancelButton, "NQ_Btn_Cancel");

        SetSprite(NyangQuariumFreshLayoutUIImage.UIToggleButton, UIToggleOnSpriteKey);
        //SetSprite(NyangQuariumFreshLayoutUIImage.FreshwaterLayoutPanel, "NQ_Char_Moongchi");
        //SetSprite(NyangQuariumFreshLayoutUIImage.FishItemTemplate, "NQ_Char_Moongchi");
        SetSprite(NyangQuariumFreshLayoutUIImage.CloseButton, "Btn_Close");
        SetSprite(NyangQuariumFreshLayoutUIImage.confirmButton, "Snap_Btn_White");
        SetSprite(NyangQuariumFreshLayoutUIImage.FishTabButton, "Snap_Btn_White");
        SetSprite(NyangQuariumFreshLayoutUIImage.NatureTextButton, "Snap_Btn_White");
        SetSprite(NyangQuariumFreshLayoutUIImage.FilterButton, "FilterButton");

    }
    public void SetUIToggleSprite(bool isUIVisible)
    {
        string spriteKey = isUIVisible
            ? UIToggleOnSpriteKey
            : UIToggleOffSpriteKey;

        SetSprite(
            NyangQuariumFreshLayoutUIImage.UIToggleButton,
            spriteKey);

        Debug.Log(
            $"[NyangQuariumFreshLayoutUISprite] UI 토글 이미지 변경: {spriteKey}",
            this);
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
    BackButton,
    FreshWaterFishButton,
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
