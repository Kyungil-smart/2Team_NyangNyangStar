using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using static NyangNyangSnapSprite;

public class NyangStargramNotificationUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    private const byte SelectedTabAlpha = 255;
    private const byte UnselectedTabAlpha = 90;

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

        SetNotificationTab();
    }
    public void SetNotificationTab()
    {
        SetTabAlpha(
            NyangStargramNotificationUIImages.HomeButton,
            false
        );

        SetTabAlpha(
            NyangStargramNotificationUIImages.TagButton,
            false
        );

        SetTabAlpha(
            NyangStargramNotificationUIImages.AddPostButton,
            false
        );

        SetTabAlpha(
            NyangStargramNotificationUIImages.NotificationButton,
            true
        );

        SetTabAlpha(
            NyangStargramNotificationUIImages.ProfileButton,
            false
        );
    }

    private void SetTabAlpha(
        NyangStargramNotificationUIImages imageType,
        bool isSelected)
    {
        Image image = GetImage((int)imageType);

        if (image == null)
        {
            Debug.LogWarning(
                $"[NyangStargramNotificationUISprite] " +
                $"하단 탭 이미지를 찾지 못했습니다: {imageType}"
            );

            return;
        }

        float alpha = isSelected
            ? SelectedTabAlpha / 255f
            : UnselectedTabAlpha / 255f;

        Button button = image.GetComponent<Button>();

        if (button != null)
        {
            ColorBlock colors = button.colors;

            colors.normalColor = SetAlpha(colors.normalColor, alpha);
            colors.highlightedColor = SetAlpha(colors.highlightedColor, alpha);
            colors.pressedColor = SetAlpha(colors.pressedColor, alpha);
            colors.selectedColor = SetAlpha(colors.selectedColor, alpha);
            colors.disabledColor = SetAlpha(colors.disabledColor, alpha);

            button.colors = colors;
        }

        Color imageColor = image.color;
        imageColor.a = alpha;
        image.color = imageColor;
    }

    private Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
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