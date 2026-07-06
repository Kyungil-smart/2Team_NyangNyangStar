using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapSnackUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Debug.Log("[NyangNyangSnapSnackUISprite] Init 실행", this);


        Bind<Image>(typeof(NyangNyangSnapSnackUIImage));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapSnackUIImage)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapSnackUIImage)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapSnackUIImage.ScrollView, "Snap_Panel_ToolList");
        SetSprite(NyangNyangSnapSnackUIImage.CloseButton, "Btn_Close", new Color32(0,0,0,255));

    }

    private void SetSprite(NyangNyangSnapSnackUIImage image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangNyangSnapSnackUIImage image, string key, Color color)
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

    public enum NyangNyangSnapSnackUIImage
    {
        ScrollView,
        CloseButton
    }
}


