using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumCollectionPopupSprite : UIBase
{
    private const string RectangleKey = "Shape_Rectangle";
    private const string DefaultTabButtonKey = "Snap_Btn_White";
    private const string BarKey = "Bar_Progress";

    [Tooltip("메인 배경1")][SerializeField] private Color _popupColor;
    [Tooltip("메인 배경2")][SerializeField] private Color _mainBackgroundColor;
    [Tooltip("제목 배경")][SerializeField] private Color _titleColor;
    [Tooltip("슬롯 전체 배경")][SerializeField] private Color _tabPanelColor;
    [Tooltip("비활성화탭")][SerializeField] private Color _tabButtonColor;
    [Tooltip("게이지 배경")][SerializeField] private Color _progressBackgroundColor;
    [Tooltip("게이지 채움")][SerializeField] private Color _progressFillColor;


    private UISpriteController[] _spriteController;
    private Image _progressFill;

    public override void Init()
    {
        Bind<Image>(typeof(NyangQuariumCollectionPopupImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(NyangQuariumCollectionPopupImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(NyangQuariumCollectionPopupImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _progressFill = GetImage((int)NyangQuariumCollectionPopupImages.ProgressFill);

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(NyangQuariumCollectionPopupImages.Popup, RectangleKey, _popupColor);
        SetSprite(NyangQuariumCollectionPopupImages.MainBackground, RectangleKey, _mainBackgroundColor);
        SetSprite(NyangQuariumCollectionPopupImages.Title, RectangleKey, _titleColor);
        SetSprite(NyangQuariumCollectionPopupImages.CloseButton, "Btn_Close");
        SetSprite(NyangQuariumCollectionPopupImages.FreshwaterFishButton, DefaultTabButtonKey, _tabButtonColor);
        SetSprite(NyangQuariumCollectionPopupImages.SaltwaterFishButton, DefaultTabButtonKey, _tabButtonColor);
        SetSprite(NyangQuariumCollectionPopupImages.BrackishWaterFishButton, DefaultTabButtonKey, _tabButtonColor);
        SetSprite(NyangQuariumCollectionPopupImages.EnvironmentsButton, DefaultTabButtonKey, _tabButtonColor);
        SetSprite(NyangQuariumCollectionPopupImages.TabPanel, RectangleKey, _tabPanelColor);
        SetSprite(NyangQuariumCollectionPopupImages.ProgressBackground, BarKey, _progressBackgroundColor);
        SetSprite(NyangQuariumCollectionPopupImages.ProgressFill, BarKey, _progressFillColor);
    }

    private void SetSprite(NyangQuariumCollectionPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(NyangQuariumCollectionPopupImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void SetSelectedTab(FishType fishType)
    {
        SetTab(NyangQuariumCollectionPopupImages.FreshwaterFishButton,
            fishType == FishType.Freshwater);

        SetTab(NyangQuariumCollectionPopupImages.SaltwaterFishButton,
            fishType == FishType.Saltwater);

        SetTab(NyangQuariumCollectionPopupImages.BrackishWaterFishButton,
            fishType == FishType.BrackishWater);

        SetTab(NyangQuariumCollectionPopupImages.EnvironmentsButton,
            fishType == FishType.Environments);
    }

    private void SetTab(NyangQuariumCollectionPopupImages image, bool selected)
    {
        _spriteController[(int)image].ChangeColor(selected ? _tabPanelColor : _tabButtonColor);
        _spriteController[(int)image].ChangeSprite(selected ? RectangleKey : DefaultTabButtonKey);
    }

    public void SetProgress(float progress)
    {
        _progressFill.fillAmount = Mathf.Clamp01(progress);
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

public enum NyangQuariumCollectionPopupImages
{
    Popup,
    MainBackground,
    Title,
    CloseButton,
    FreshwaterFishButton,
    SaltwaterFishButton,
    BrackishWaterFishButton,
    EnvironmentsButton,
    TabPanel,
    ProgressBackground,
    ProgressFill,
}
