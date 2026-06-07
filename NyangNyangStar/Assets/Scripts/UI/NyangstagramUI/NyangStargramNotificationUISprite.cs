using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using static NyangNyangSnapSprite;

public class NyangStargramNotificationUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramNotificationUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangStargramNotificationUIImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangStargramNotificationUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangStargramNotificationUIImages.backGroundpanel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangStargramNotificationUIImages.HeaderPanel, "NYS_TopBar");
        SetSprite(NyangStargramNotificationUIImages.BackButton, "Btn_Back");
        SetSprite(NyangStargramNotificationUIImages.Viewport, "NYS_Content"); 
        SetSprite(NyangStargramNotificationUIImages.NPCImage, "NYS_Btn_Profile", new Color32(0,0,0,255));

        //네비게이션 버튼
        SetSprite(NyangStargramNotificationUIImages.HomeButton, "NYS_Btn_Home", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramNotificationUIImages.TagButton, "NYS_Btn_Hashtag", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramNotificationUIImages.AddPostButton, "NYS_Btn_AddPost", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramNotificationUIImages.NotificationButton, "NYS_Btn_Notification", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramNotificationUIImages.ProfileButton, "NYS_Btn_Profile", new Color32(160, 0, 255, 255));
        SetSprite(NyangStargramNotificationUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");





    }

    private void SetSprite(NyangStargramNotificationUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangStargramNotificationUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangStargramNotificationUIImages
{
    backGroundpanel,
    HeaderPanel,
    BackButton,
    Viewport,
    NPCImage,
    NyangstagramCloseButton,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,

    


}