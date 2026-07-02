using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramPostUISprite : UIBase
{
    private const string LikeEmptySpriteKey = "NYS_Btn_Heart_Empty";
    private const string LikeFilledSpriteKey = "NYS_Btn_Heart_Filled";

    private UISpriteController[] _spriteController;
    private Image _postImage;

    public override void Init()
    {
        Bind<Image>(typeof(NyangStargramPostUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangStargramPostUIImages)).Length];
        for (int i = 0; i < Enum.GetValues(typeof(NyangStargramPostUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _postImage = GetImage((int)NyangStargramPostUIImages.PostImage);

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangStargramPostUIImages.backGroundpanel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangStargramPostUIImages.PostHeaderpanel, "NYS_TopBar");
        SetSprite(NyangStargramPostUIImages.BackButton, "Btn_Back");
        SetSprite(NyangStargramPostUIImages.HomePostPanel, "NYS_Content");
        SetSprite(NyangStargramPostUIImages.VerifyIconFront, "NYS_Btn_Profile", new Color32(0, 0, 0, 255));
        SetSprite(NyangStargramPostUIImages.VerifyIcon, "NYS_ProfileBadge");
        //SetSprite(NyangStargramPostUIImages.LikeButton, "NYS_Btn_Heart_Empty");
        SetSprite(NyangStargramPostUIImages.LikeButton, LikeEmptySpriteKey);
        SetSprite(NyangStargramPostUIImages.NPCImage, "NYS_Btn_Profile", new Color32(0, 0, 0, 255));


        //네비게이션 버튼
        SetSprite(NyangStargramPostUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");
    }

    public void SetLikeSprite(bool isLiked)
    {
        string spriteKey = isLiked ? LikeFilledSpriteKey : LikeEmptySpriteKey;

        SetSprite(NyangStargramPostUIImages.LikeButton, spriteKey);

        DebugTool.Log($"좋아요 아이콘 변경: {spriteKey}", DebugType.UI, this);
    }

    public async void SetPhoto(Sprite sprite)
    {
        if (sprite == null) return;

        if (_postImage != null)
        {
            _postImage.sprite = sprite;
            _postImage.preserveAspect = true;
            _postImage.color = Color.white;
        }
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

    private void OnDestroy()
    {
        if (_spriteController == null) return;

        foreach (UISpriteController controller in _spriteController)
        {
            controller?.ReleaseSprite();
        }
    }

    public enum NyangStargramPostUIImages
    {
        backGroundpanel,
        PostHeaderpanel,
        BackButton,
        HomePostPanel,
        VerifyIconFront,
        VerifyIcon,
        PostImage,
        LikeButton,
        NPCImage,
        NyangstagramCloseButton,
    }
}