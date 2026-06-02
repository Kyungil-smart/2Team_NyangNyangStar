using UI;
using UnityEngine.UI;

public class NyangNyangSnapStagePopupSprite : UIBase
{
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapStagePopupImages));

        _spriteController = new UISpriteController[(int)NyangNyangSnapStagePopupImages.Count];

        for (int i = 0; i < (int)NyangNyangSnapStagePopupImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        //SetSprite(NyangNyangSnapStagePopupImages.Background, "");
        //SetSprite(NyangNyangSnapStagePopupImages.Title, "");
        //SetSprite(NyangNyangSnapStagePopupImages.Stage1Button, "");
        //SetSprite(NyangNyangSnapStagePopupImages.Stage2Button, "");
        //SetSprite(NyangNyangSnapStagePopupImages.CloseButton, "");
    }

    private void SetSprite(NyangNyangSnapStagePopupImages image, string key)
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

public enum NyangNyangSnapStagePopupImages
{
    Background,
    Title,
    Stage1Button,
    Stage2Button,
    CloseButton,

    Count
}