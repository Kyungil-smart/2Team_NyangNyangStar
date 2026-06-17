using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NotebookPopupSprite : UIBase
{
    [SerializeField] private Color _locationColor;

    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NotebookPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NotebookPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NotebookPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NotebookPopupImages.Panel, "Snap_Panel_Note");
        SetSprite(NotebookPopupImages.CloseButton, "Btn_Close");
        //SetSprite(NotebookPopupImages.CatBackground1, "");
        //SetSprite(NotebookPopupImages.CatName1, "");
        SetSprite(NotebookPopupImages.CatImage1, "FM_Target_Mungchi");
        SetSprite(NotebookPopupImages.Location1, "Shape_Square", _locationColor);
        SetSprite(NotebookPopupImages.RedPoint1, "Shape_Circle", Color.red);
    }

    private void SetSprite(NotebookPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NotebookPopupImages image, string key, Color color)
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
}

public enum NotebookPopupImages
{
    Panel,
    CloseButton,
    //CatBackground1,
    //CatName1,
    CatImage1,
    Location1,
    RedPoint1
}