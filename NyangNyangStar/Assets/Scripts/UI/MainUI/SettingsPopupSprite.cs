using UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupSprite : UIBase
{
    private Image _sfxButtonImage;
    private Image _bgmButtonImage;

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

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(SettingsPopupImages.SFXButton, "Main_Btn_Sound");
        SetSprite(SettingsPopupImages.BGMButton, "Main_Btn_Music");
    }

    private void SetSprite(SettingsPopupImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void RefreshAudioButtonSprite(bool sfxOn, bool bgmOn)
    {
        _sfxButtonImage.color = sfxOn ? Color.white : Color.gray;
        _bgmButtonImage.color = bgmOn ? Color.white : Color.gray;
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

    Count
}