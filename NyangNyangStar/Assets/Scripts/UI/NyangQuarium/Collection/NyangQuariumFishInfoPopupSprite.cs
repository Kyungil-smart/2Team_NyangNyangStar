using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumFishInfoPopupSprite : UIBase
{
    private UISpriteController[] _spriteController;

    [Tooltip("메인 배경")][SerializeField] private Color _popupColor;
    [Tooltip("텍스트 배경")][SerializeField] private Color _infoBackgroundColor;

    public override void Init()
    {
        Bind<Image>(typeof(NyangQuariumFishInfoPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangQuariumFishInfoPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangQuariumFishInfoPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangQuariumFishInfoPopupImages.Popup, "Shape_Rectangle", _popupColor);
        SetSprite(NyangQuariumFishInfoPopupImages.CloseButton, "Btn_Close");
        SetSprite(NyangQuariumFishInfoPopupImages.FishBackground, "NQ_BG_Info");
        SetSprite(NyangQuariumFishInfoPopupImages.InfoBackground, "Shape_Square", _infoBackgroundColor);
        SetSprite(NyangQuariumFishInfoPopupImages.PreviousButton, "Main_Btn_Mergeboard");
        SetSprite(NyangQuariumFishInfoPopupImages.NextButton, "Main_Btn_Mergeboard");
    }

    private void SetSprite(NyangQuariumFishInfoPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangQuariumFishInfoPopupImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void SetFishImage(string key, bool isUnlocked)
    {
        SetSprite(NyangQuariumFishInfoPopupImages.FishImage, key, isUnlocked ? Color.white : Color.black);
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

public enum NyangQuariumFishInfoPopupImages
{
    Popup,
    CloseButton,
    FishBackground,
    FishImage,
    InfoBackground,
    PreviousButton,
    NextButton
}