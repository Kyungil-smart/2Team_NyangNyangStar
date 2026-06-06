using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangstagramDMChatUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangstagramDMChatUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangstagramDMChatUIImages)).Length];
        for (int i = 0; i < Enum.GetValues(typeof(NyangstagramDMChatUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //메인 패널
        SetSprite(NyangstagramDMChatUIImages.backGroundpanel, "NYS_Frame");

        //profile뷰
        SetSprite(NyangstagramDMChatUIImages.HeaderPanel, "NYS_TopBar");
        SetSprite(NyangstagramDMChatUIImages.BackButton, "NYS_Btn_Back");
        SetSprite(NyangstagramDMChatUIImages.NpcMessageBubble, "NYS_DM_Box_Opponent");
        SetSprite(NyangstagramDMChatUIImages.NPCProfileImage, "NYS_Btn_Profile",new Color32(0,0,0,255));
        SetSprite(NyangstagramDMChatUIImages.PlayerMassageBubble, "NYS_DM_Box_Me");

        //네비게이션 버튼
        SetSprite(NyangstagramDMChatUIImages.NyangstagramCloseButton, "NYS_Btn_Exit");




    }

    private void SetSprite(NyangstagramDMChatUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangstagramDMChatUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }
    public enum NyangstagramDMChatUIImages
    {
        backGroundpanel,
        HeaderPanel,
        BackButton,
        NpcMessageBubble,
        NPCProfileImage,
        PlayerMassageBubble,
        NyangstagramCloseButton
    }
}