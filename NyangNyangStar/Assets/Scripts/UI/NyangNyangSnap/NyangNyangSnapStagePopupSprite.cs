using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapStagePopupSprite : UIBase
{
    [SerializeField] private Color _backgroundColor;

    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapStagePopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapStagePopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapStagePopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapStagePopupImages.Background, "Shape_Square", _backgroundColor);
        SetSprite(NyangNyangSnapStagePopupImages.Pattern1, "Snap_BG_Tile");
        SetSprite(NyangNyangSnapStagePopupImages.Pattern2, "Snap_BG_Tile");
        SetSprite(NyangNyangSnapStagePopupImages.Pattern3, "Snap_BG_Tile");
        SetSprite(NyangNyangSnapStagePopupImages.Pattern4, "Snap_BG_Tile");
        SetSprite(NyangNyangSnapStagePopupImages.TitleImage, "Snap_Logo");
        SetSprite(NyangNyangSnapStagePopupImages.Stage1Image, "Snap_Img_Stage_1");
        SetSprite(NyangNyangSnapStagePopupImages.Stage2Image, "Snap_Img_Stage_2");
        SetSprite(NyangNyangSnapStagePopupImages.Stage1Title, "Shape_Rectangle", new Color32(255, 213, 233, 255));
        SetSprite(NyangNyangSnapStagePopupImages.Stage2Title, "Shape_Rectangle", new Color32(245, 249, 121, 255));


        SetSprite(NyangNyangSnapStagePopupImages.Stage1Button, "Snap_Btn_White", new Color32(155, 255, 130, 255));
        SetSprite(NyangNyangSnapStagePopupImages.Stage2Button, "Snap_Btn_White", new Color32(155, 255, 130, 255));
        SetSprite(NyangNyangSnapStagePopupImages.BackButton, "Main_Btn_Home");
        SetSprite(NyangNyangSnapStagePopupImages.HowToPlay, "FM_Btn_Help");

    }

    private void SetSprite(NyangNyangSnapStagePopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangNyangSnapStagePopupImages image, string key, Color color)
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

    public enum NyangNyangSnapStagePopupImages
    {
        Background,
        Pattern1,
        Pattern2,
        Pattern3,
        Pattern4,
        TitleImage,
        Stage1Button,
        Stage2Button,
        BackButton,
        HowToPlay,
        Stage1Image,
        Stage2Image,
        Stage1Title,
        Stage2Title
    }
}