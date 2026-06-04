using UI;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramPostUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramPostUIImages));

        _spriteController = new UISpriteController[(int)NyangStargramPostUIImages.Count];
        for (int i = 0; i < (int)NyangStargramPostUIImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangStargramPostUIImages.backGroundpanel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangStargramPostUIImages.PostHeaderpanel, "NYS_TopBar");
        SetSprite(NyangStargramPostUIImages.BackButton, "NYS_Btn_Back");
        SetSprite(NyangStargramPostUIImages.Viewport, "NYS_Content");
        SetSprite(NyangStargramPostUIImages.VerifyIconFront, "NYS_Btn_Profile", new Color32(0, 0, 0, 255));
        SetSprite(NyangStargramPostUIImages.VerifyIcon, "NYS_ProfileBadge");
        SetSprite(NyangStargramPostUIImages.LikeButton, "NYS_Btn_Heart_Empty");
        SetSprite(NyangStargramPostUIImages.NPCImage, "NYS_Btn_Profile", new Color32(0, 0, 0, 255));


        //네비게이션 버튼
        SetSprite(NyangStargramPostUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");




    }

    private void SetSprite(NyangStargramPostUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangStargramPostUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
}

public enum NyangStargramPostUIImages
{
    backGroundpanel,
    PostHeaderpanel,
    BackButton,
    Viewport,
    VerifyIconFront,
    VerifyIcon,
    PostImage,
    LikeButton,
    NPCImage,
    NyangstagramCloseButton,

    Count


}