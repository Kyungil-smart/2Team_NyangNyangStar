using System;
using Data.LibrarySystem;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapSprite : UIBase
{
    [SerializeField] private Color _panelColor = new Color32(255, 255, 255, 255);

    [SerializeField] private NyangNyangSnapBackgroundSO _backgroundSO;
    private UISpriteController[] _spriteController;

    public NyangNyangSnapBackgroundData CurrentBackgroundData { get; private set; }

    public override void Init()
    {
        Bind<Image>(typeof(NyangNyangSnapImages));
        ResolveBackgroundSO();

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangNyangSnapImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangNyangSnapImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    public void SetBackground(int stage)
    {
        NyangNyangSnapBackgroundSO backgroundSO = ResolveBackgroundSO();

        if (backgroundSO == null)
        {
            CurrentBackgroundData = null;
            DebugTool.Warning("[NyangNyangSnapSprite] 배경 SO가 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        CurrentBackgroundData = backgroundSO.GetRandomBackgroundData(stage);

        if (CurrentBackgroundData == null || string.IsNullOrEmpty(CurrentBackgroundData.BackgroundKey))
        {
            DebugTool.Warning(
                $"[NyangNyangSnapSprite] Stage {stage}에 사용할 배경 데이터가 없습니다.",
                DebugType.UI,
                this);
            return;
        }

        DebugTool.Log($"SetBackground: {stage}", DebugType.UI);
        SetSprite(NyangNyangSnapImages.BackPanel, CurrentBackgroundData.BackgroundKey);
    }

    private NyangNyangSnapBackgroundSO ResolveBackgroundSO()
    {
        if (LocalDataAccess.Instance != null &&
            LocalDataAccess.Instance.Game != null &&
            LocalDataAccess.Instance.Game.TryGetNyangNyangSnapBackgroundSO(out NyangNyangSnapBackgroundSO loadedSO) &&
            loadedSO != null)
        {
            _backgroundSO = loadedSO;
        }

        return _backgroundSO;
    }

    private void SetSprites()
    {
        SetSprite(NyangNyangSnapImages.BackButton, "Btn_Back");
        SetSprite(NyangNyangSnapImages.StartPanelBackButton, "Btn_Back");

        SetSprite(NyangNyangSnapImages.SnackPanelButton, "Shape_Rectangle", _panelColor);
        SetSprite(NyangNyangSnapImages.ToyPanelButton, "Shape_Rectangle", _panelColor);
        SetSprite(NyangNyangSnapImages.PhotoButton, "Snap_Icon_RangeCircle");
        SetSprite(NyangNyangSnapImages.PhotoButtonImage, "Shape_Circle");

        SetSprite(NyangNyangSnapImages.RemainingAttempts, "Snap_Btn_Gray");
        SetSprite(NyangNyangSnapImages.StartButton, "Snap_Btn_Gray");
        SetSprite(NyangNyangSnapImages.SettingsButton, "Main_Btn_Settings");
        SetSprite(NyangNyangSnapImages.ToyIocn, "Item_Toy_YarnBall");
        SetSprite(NyangNyangSnapImages.SnackIcon, "Item_Treat_Slice");

    }

    private void SetSprite(NyangNyangSnapImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }
    private void SetSprite(NyangNyangSnapImages image, string key, Color color)
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

    public enum NyangNyangSnapImages
    {
        BackPanel,
        BackButton,
        StartPanelBackButton,
        SnackPanelButton,
        ToyPanelButton,
        PhotoButton,
        PhotoButtonImage,
        RemainingAttempts,
        StartButton,
        SettingsButton,
        ToyIocn,
        SnackIcon,
    }
}


