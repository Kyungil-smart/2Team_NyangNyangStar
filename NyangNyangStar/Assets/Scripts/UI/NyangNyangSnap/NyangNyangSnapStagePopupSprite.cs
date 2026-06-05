using UI;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapStagePopupSprite : UIBase
{
    [SerializeField] private Color _backgroundColor;

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
        SetSprite(NyangNyangSnapStagePopupImages.Background, "Shape_Square", _backgroundColor);
        SetSprite(NyangNyangSnapStagePopupImages.Pattern1, "BG_Tile_Cats");
        SetSprite(NyangNyangSnapStagePopupImages.Pattern2, "BG_Tile_Cats");
        SetSprite(NyangNyangSnapStagePopupImages.Pattern3, "BG_Tile_Cats");
        SetSprite(NyangNyangSnapStagePopupImages.Pattern4, "BG_Tile_Cats");
        SetSprite(NyangNyangSnapStagePopupImages.Stage1Button, "Snap_Btn_Gray");
        SetSprite(NyangNyangSnapStagePopupImages.Stage2Button, "Snap_Btn_Gray");
        SetSprite(NyangNyangSnapStagePopupImages.BackButton, "Btn_Back");
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
}

public enum NyangNyangSnapStagePopupImages
{
    Background,
    Pattern1,
    Pattern2,
    Pattern3,
    Pattern4,
    Stage1Button,
    Stage2Button,
    BackButton,

    Count
}