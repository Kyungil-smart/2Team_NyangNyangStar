using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class FishSlotSprite : UIBase
{
    private const string RectangleKey = "Shape_Rectangle";

    private UISpriteController[] _spriteController;

    [Tooltip("텍스트 배경")][SerializeField] private Color _fishSlotColor;
    [Tooltip("이미지 배경")][SerializeField] private Color _fishBackgroundColor;
    [Tooltip("미해금 배경")][SerializeField] private Color _lockedOverlayColor;

    public override void Init()
    {
        Bind<Image>(typeof(FishSlotImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(FishSlotImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(FishSlotImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(FishSlotImages.FishSlot, RectangleKey, _fishSlotColor);
        SetSprite(FishSlotImages.FishBackground, RectangleKey, _fishBackgroundColor);
        SetSprite(FishSlotImages.LockedOverlay, RectangleKey, _lockedOverlayColor);
    }

    private void SetSprite(FishSlotImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(FishSlotImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void SetFishImage(string key)
    {
        SetSprite(FishSlotImages.FishImage, key);
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

public enum FishSlotImages
{ 
    FishSlot,
    FishBackground,
    FishImage,
    LockedOverlay
}

