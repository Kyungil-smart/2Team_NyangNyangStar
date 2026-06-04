using UI;
using UnityEngine;
using UnityEngine.UI;

public class NyangstagramNPCProfileUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStarGramProfileUIImages));

        _spriteController = new UISpriteController[(int)NyangStarGramProfileUIImages.Count];
        for (int i = 0; i < (int)NyangStarGramProfileUIImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangStarGramProfileUIImages.backGroundpanel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangStarGramProfileUIImages.ProfileFixedHeaderPanel, "NYS_TopBar");
        SetSprite(NyangStarGramProfileUIImages.VerifyIcon, "NYS_ProfileBadge");
        SetSprite(NyangStarGramProfileUIImages.GoHomeButton, "NYS_Btn_Back");
        SetSprite(NyangStarGramProfileUIImages.ImageFrame, "NYS_Profile_Frame");
        SetSprite(NyangStarGramProfileUIImages.Image, "NYS_Profile_User_01");
        SetSprite(NyangStarGramProfileUIImages.StoryButton, "NYS_Btn_Feed");

        //네비게이션 버튼
        SetSprite(NyangStarGramProfileUIImages.HomeButton, "NYS_Btn_Home", new Color32(160, 0, 255, 255));
        SetSprite(NyangStarGramProfileUIImages.TagButton, "NYS_Btn_Hashtag", new Color32(160, 0, 255, 255));
        SetSprite(NyangStarGramProfileUIImages.AddPostButton, "NYS_Btn_AddPost", new Color32(160, 0, 255, 255));
        SetSprite(NyangStarGramProfileUIImages.NotificationButton, "NYS_Btn_Notification", new Color32(160, 0, 255, 255));
        SetSprite(NyangStarGramProfileUIImages.ProfileButton, "NYS_Btn_Profile", new Color32(160, 0, 255, 255));
        SetSprite(NyangStarGramProfileUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");




    }

    private void SetSprite(NyangStarGramProfileUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangStarGramProfileUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangStarGramProfileUIImages
{
    backGroundpanel,
    ProfileFixedHeaderPanel,
    VerifyIcon,
    GoHomeButton,
    ImageFrame,
    Image,
    StoryButton,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton,

    Count


}