using UI;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapSprite : UIBase
{
    [SerializeField] private NyangNyangSnapBackgroundSO _backgroundSO;
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapImages));

        _spriteController = new UISpriteController[(int)NyangNyangSnapImages.Count];

        for (int i = 0; i < (int)NyangNyangSnapImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    public void SetBackground(int stage)
    {
        string key = _backgroundSO.GetRandomBackgroundKey(stage);
        DebugTool.Log($"SetBackground: {key}", DebugType.UI);
        SetSprite(NyangNyangSnapImages.BackPanel, key);
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapImages.BackButton, "Btn_Back");
        SetSprite(NyangNyangSnapImages.SnackPanelButton, "Snap_Btn_Pink");
        SetSprite(NyangNyangSnapImages.ToyPanelButton, "Snap_Btn_White");
        SetSprite(NyangNyangSnapImages.Photo, "Snap_Btn_Red");
        SetSprite(NyangNyangSnapImages.RemainingAttempts, "Snap_Btn_Gray");
        SetSprite(NyangNyangSnapImages.StartButton, "Snap_Btn_Gray");
        SetSprite(NyangNyangSnapImages.SettingButton, "Main_Btn_Settings");
    }

    private void SetSprite(NyangNyangSnapImages image, string key)
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

public enum NyangNyangSnapImages
{
    BackPanel,
    BackButton,
    SnackPanelButton,
    ToyPanelButton,
    Photo,
    RemainingAttempts,
    StartButton,
    SettingButton,

    Count
}
