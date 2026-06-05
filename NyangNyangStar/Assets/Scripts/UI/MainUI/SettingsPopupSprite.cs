using UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupSprite : UIBase
{
    private Image _sfxButtonImage;
    private Image _bgmButtonImage;
    private Image _sfxIcon;
    private Image _bgmIcon;

    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(SettingsPopupImages));

        _spriteController = new UISpriteController[(int)SettingsPopupImages.Count];

        for (int i = 0; i < (int)SettingsPopupImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _sfxButtonImage = GetImage((int)SettingsPopupImages.SFXButton);
        _bgmButtonImage = GetImage((int)SettingsPopupImages.BGMButton);
        _sfxIcon = GetImage((int)SettingsPopupImages.SFXIcon);
        _bgmIcon = GetImage((int) SettingsPopupImages.BGMIcon);

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(SettingsPopupImages.SFXButton, "Main_Panel_Audio");
        SetSprite(SettingsPopupImages.BGMButton, "Main_Panel_Audio");
        SetSprite(SettingsPopupImages.SFXIcon, "Main_Btn_Sound");
        SetSprite(SettingsPopupImages.BGMIcon, "Main_Btn_Music");
        SetSprite(SettingsPopupImages.CloseButton, "Btn_Close");
        SetSprite(SettingsPopupImages.Panel, "Shape_Rectangle");
        SetSprite(SettingsPopupImages.Header, "Main_Panel_Settings_Header");
    }

    private void SetSprite(SettingsPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void RefreshAudioButtonSprite(bool sfxOn, bool bgmOn)
    {
        _sfxButtonImage.color = sfxOn ? Color.white : Color.gray;
        _bgmButtonImage.color = bgmOn ? Color.white : Color.gray;
        _sfxIcon.color = sfxOn ? Color.white : Color.gray;
        _bgmIcon.color = bgmOn ? Color.white : Color.gray;
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

public enum SettingsPopupImages
{
    SFXButton,
    BGMButton,
    SFXIcon,
    BGMIcon,
    CloseButton,
    Panel,
    Header,

    Count
}