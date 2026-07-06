using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapToyUISprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Debug.Log("[NyangNyangSnapSnackUISprite] Init 실행", this);


        Bind<Image>(typeof(NyangNyangSnapToyUIImage));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapToyUIImage)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapToyUIImage)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapToyUIImage.ScrollView, "Snap_Panel_ToolList");
        SetSprite(NyangNyangSnapToyUIImage.CloseButton, "Btn_Close", new Color32(0, 0, 0, 255));

    }

    private void SetSprite(NyangNyangSnapToyUIImage image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangNyangSnapToyUIImage image, string key, Color color)
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

    public enum NyangNyangSnapToyUIImage
    {
        ScrollView,
        CloseButton
    }
}


