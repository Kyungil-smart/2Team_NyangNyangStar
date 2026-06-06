using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using static NyangNyangSnapSprite;
using static UnityEngine.Rendering.DebugUI;

public class NyangStargramAddPostUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramAddPostUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangStargramAddPostUIImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangStargramAddPostUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangStargramAddPostUIImages.backGroundpanel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangStargramAddPostUIImages.HeaderPanel, "NYS_TopBar");
        SetSprite(NyangStargramAddPostUIImages.BackButton, "Btn_Back");
        SetSprite(NyangStargramAddPostUIImages.ImagePanel, "NYS_Content");
        SetSprite(NyangStargramAddPostUIImages.NewImageUploadButton, "NYS_Btn_DropDown");
        SetSprite(NyangStargramAddPostUIImages.DropdownPanel, "NYS_TopBar");
        SetSprite(NyangStargramAddPostUIImages.FolderImage, "NYS_Folder");
        SetSprite(NyangStargramAddPostUIImages.PostImagePanel, "NYS_Content");


        //네비게이션 버튼
        SetSprite(NyangStargramAddPostUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");




    }

    private void SetSprite(NyangStargramAddPostUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangStargramAddPostUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangStargramAddPostUIImages
{
    backGroundpanel,
    HeaderPanel,
    BackButton,
    ImagePanel,
    NewImageUploadButton,
    DropdownPanel,
    FolderImage,
    PostImagePanel,
    엘범,
    NyangstagramCloseButton,

    


}