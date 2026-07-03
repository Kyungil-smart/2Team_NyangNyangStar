using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NotebookPopupSprite : UIBase
{
    [SerializeField] private Color _locationColor;

    private UISpriteController[] _spriteController;
    private Image _redPoint1;

    public override void Init()
    {
        Bind<Image>(typeof(NotebookPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NotebookPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NotebookPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _redPoint1 = GetImage((int)NotebookPopupImages.RedPoint1);

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NotebookPopupImages.Panel, "Snap_Panel_Note");
        SetSprite(NotebookPopupImages.CloseButton, "Btn_Close");
        SetSpriteRange(NotebookPopupImages.CatSilhouette1, NotebookPopupImages.CatSilhouette12, "Snap_Slot_Unknown");
        SetSprite(NotebookPopupImages.CatBackground1, "Shape_Square");
        SetSprite(NotebookPopupImages.CatName1, "Snap_Panel_Name");
        SetSprite(NotebookPopupImages.CatImage1, "FM_Target_Moongchi");
        SetSprite(NotebookPopupImages.Location1, "Shape_Rectangle", _locationColor);
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

    private void SetSpriteRange(NotebookPopupImages start, NotebookPopupImages end, string key)
    {
        for (int i = (int)start; i <= (int)end; i++)
        {
            _spriteController[i].ChangeSprite(key);
        }
    }

    public void SetRedPoint(bool isOn)
    {
        _redPoint1.gameObject.SetActive(isOn);
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
    CatSilhouette1, CatSilhouette2, CatSilhouette3, CatSilhouette4,
    CatSilhouette5, CatSilhouette6, CatSilhouette7, CatSilhouette8,
    CatSilhouette9, CatSilhouette10, CatSilhouette11, CatSilhouette12,
    CatBackground1,
    CatName1,
    CatImage1,
    Location1,
    RedPoint1
}