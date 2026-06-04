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
        //SetSprite(NyangNyangSnapImages.BackButton, "");
        //SetSprite(NyangNyangSnapImages.SnackPanelButton, "");
        //SetSprite(NyangNyangSnapImages.ToyPanelButton, "");
        //SetSprite(NyangNyangSnapImages.Photo, "");
        //SetSprite(NyangNyangSnapImages.StartButton, "");
        //SetSprite(NyangNyangSnapImages.SettingButton, "");
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
    StartButton,
    SettingButton,

    Count
}
