using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using static NyangNyangSnapSprite;

public class NyangStargramDMListUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramDMListUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangStargramDMListUIImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangStargramDMListUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangStargramDMListUIImages.backGroundpanel, "NYS_Frame");

        //view 패널
        SetSprite(NyangStargramDMListUIImages.HeaderPanel, "NYS_TopBar");
        SetSprite(NyangStargramDMListUIImages.BackButton, "Btn_Back");
        SetSprite(NyangStargramDMListUIImages.Viewport, "NYS_Content");
        SetSprite(NyangStargramDMListUIImages.Image, "NYS_Btn_Profile", new Color32(0,0,0,255));



        //네비게이션 버튼
        SetSprite(NyangStargramDMListUIImages.HomeButton, "NYS_Btn_Home", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramDMListUIImages.TagButton, "NYS_Btn_Hashtag", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramDMListUIImages.AddPostButton, "NYS_Btn_AddPost", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramDMListUIImages.NotificationButton, "NYS_Btn_Notification", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramDMListUIImages.ProfileButton, "NYS_Btn_Profile", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramDMListUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");




    }

    private void SetSprite(NyangStargramDMListUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangStargramDMListUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangStargramDMListUIImages
{
    backGroundpanel,
    HeaderPanel,
    BackButton,
    Viewport,
    Image,

    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton,

    


}