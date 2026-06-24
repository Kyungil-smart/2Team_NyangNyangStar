using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangstagramMainUISprite : UIBase
{
    private const string LikeEmptySpriteKey = "NYS_Btn_Heart_Empty";
    private const string LikeFilledSpriteKey = "NYS_Btn_Heart_Filled";

    private const byte _selectedTabAlpha = 255;
    private const byte _unselectedTabAlpha = 90;

    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangstagramMainUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangstagramMainUIImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangstagramMainUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangstagramMainUIImages.Panel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangstagramMainUIImages.ProfileFixedHeaderPanel, "NYS_TopBar");
        SetSprite(NyangstagramMainUIImages.VerifyIconProfile, "NYS_ProfileBadge");
        SetSprite(NyangstagramMainUIImages.Prifilepanel, "NYS_Content");
        SetSprite(NyangstagramMainUIImages.ImageFrame, "NYS_Profile_Frame");
        SetSprite(NyangstagramMainUIImages.Image, "NYS_Profile_User_01");
        SetSprite(NyangstagramMainUIImages.DMButton, "NYS_Btn_DM");



        //홈 뷰
        SetSprite(NyangstagramMainUIImages.HomeHeaderPanel, "NYS_TopBar");
        SetSprite(NyangstagramMainUIImages.HomeHeaderImage, "NYS_TitleLogo");
        SetSprite(NyangstagramMainUIImages.HomeDMButton, "NYS_Btn_DM");
        SetSprite(NyangstagramMainUIImages.VerifyIconFront, "NYS_Profile_User_01");
        SetSprite(NyangstagramMainUIImages.VerifyIcon, "NYS_ProfileBadge");
        SetSprite(NyangstagramMainUIImages.LikeButton, LikeEmptySpriteKey);
        SetSprite(NyangstagramMainUIImages.NPCImage, "NYS_Btn_Profile", new Color32(160, 0, 255, 255));
        SetSprite(NyangstagramMainUIImages.Viewport, "NYS_Content");

        
        //네비게이션 버튼
        SetSprite(NyangstagramMainUIImages.HomeButton, "NYS_Btn_Home",new Color32(160,0,255,255));
        SetSprite(NyangstagramMainUIImages.TagButton, "NYS_Btn_Hashtag", new Color32(160, 0, 255, 255));
        SetSprite(NyangstagramMainUIImages.AddPostButton, "NYS_Btn_AddPost", new Color32(160, 0, 255, 255));
        SetSprite(NyangstagramMainUIImages.NotificationButton, "NYS_Btn_Notification", new Color32(160, 0, 255, 255));
        SetSprite(NyangstagramMainUIImages.ProfileButton, "NYS_Btn_Profile", new Color32(160, 0, 255, 255));
        SetSprite(NyangstagramMainUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");
    }

    private void SetSprite(NyangstagramMainUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangstagramMainUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key,true);
    }
    public void SetSelectedTab(NyangstagramTab selectedTab)
    {
        SetTabAlpha(
            NyangstagramMainUIImages.HomeButton,
            selectedTab == NyangstagramTab.Home
        );

        SetTabAlpha(
            NyangstagramMainUIImages.TagButton,
            selectedTab == NyangstagramTab.Tag
        );

        SetTabAlpha(
            NyangstagramMainUIImages.AddPostButton,
            selectedTab == NyangstagramTab.AddPost
        );

        SetTabAlpha(
            NyangstagramMainUIImages.NotificationButton,
            selectedTab == NyangstagramTab.Notification
        );

        SetTabAlpha(
            NyangstagramMainUIImages.ProfileButton,
            selectedTab == NyangstagramTab.Profile
        );

        DebugTool.Log(
            $"냥스타그램 하단 탭 변경: {selectedTab}",
            DebugType.UI,
            this
        );
    }

    private void SetTabAlpha(
        NyangstagramMainUIImages imageType,
        bool isSelected)
    {
        Image image = GetImage((int)imageType);

        if (image == null)
        {
            DebugTool.Warning(
                $"하단 탭 Image를 찾지 못했습니다: {imageType}",
                DebugType.UI,
                this
            );

            return;
        }

        Button button = image.GetComponent<Button>();

        if (button == null)
        {
            SetImageAlpha(image, isSelected);
            return;
        }

        float alpha = isSelected
            ? _selectedTabAlpha / 255f
            : _unselectedTabAlpha / 255f;

        ColorBlock colors = button.colors;

        colors.normalColor = SetColorAlpha(colors.normalColor, alpha);
        colors.highlightedColor = SetColorAlpha(colors.highlightedColor, alpha);
        colors.pressedColor = SetColorAlpha(colors.pressedColor, alpha);
        colors.selectedColor = SetColorAlpha(colors.selectedColor, alpha);
        colors.disabledColor = SetColorAlpha(colors.disabledColor, alpha);

        button.colors = colors;
    }

    private void SetImageAlpha(Image image, bool isSelected)
    {
        Color color = image.color;

        color.a = isSelected
            ? _selectedTabAlpha / 255f
            : _unselectedTabAlpha / 255f;

        image.color = color;
    }

    private Color SetColorAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    public void SetLikeSprite(bool isLiked)
    {
        string spriteKey = isLiked ? LikeFilledSpriteKey : LikeEmptySpriteKey;

        SetSprite(NyangstagramMainUIImages.LikeButton, spriteKey);
        DebugTool.Log($"좋아요 아이콘 변경: {spriteKey}", DebugType.UI, this);
    }

}

public enum NyangstagramMainUIImages
{
    Panel,
    HomeHeaderPanel,
    HomeHeaderImage,
    HomeDMButton,
    DMButton,
    VerifyIconFront,
    VerifyIcon,
    LikeButton,
    NPCImage,
    Viewport,
    HomeButton,
    TagButton,
    AddPostButton,
    NotificationButton,
    ProfileButton,
    NyangstagramCloseButton,
    ProfileFixedHeaderPanel,
    VerifyIconProfile,
    Prifilepanel,
    ImageFrame,
    Image,
}
public enum NyangstagramTab
{
    Home,
    Tag,
    AddPost,
    Notification,
    Profile
}