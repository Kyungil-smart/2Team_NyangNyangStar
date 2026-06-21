using System;
using UI;
using UI.Base;
using UnityEngine.UI;

public class RewardPanelSprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(RewardPanelImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(RewardPanelImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(RewardPanelImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(RewardPanelImages.RewardPanel, "Snap_Btn_Gray");
        SetSprite(RewardPanelImages.GemIcon, "Main_Icon_Jewel");
        SetSprite(RewardPanelImages.ADIcon, "Btn_AD");
        SetSprite(RewardPanelImages.RetryButton, "Snap_Btn_Green");
        SetSprite(RewardPanelImages.MainButton, "Snap_Btn_Blue");
    }

    private void SetSprite(RewardPanelImages image, string key)
    {
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

public enum RewardPanelImages
{
    RewardPanel,
    GemIcon,
    ADIcon,
    RetryButton,
    MainButton,
}
