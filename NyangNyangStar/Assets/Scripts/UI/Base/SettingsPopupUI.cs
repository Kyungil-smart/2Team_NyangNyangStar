using Core.Managers;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupUI : UIPopup
{
    [Header("오디오 설정 버튼")]
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _bgmButton;
    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _background;

    public override void Init()
    {
        Bind<Button>(typeof(SettingsButtons));

        _sfxButton = GetButton((int)SettingsButtons.SFXButton);
        _bgmButton = GetButton((int)SettingsButtons.BGMButton);
        _closeButton = GetButton((int)SettingsButtons.CloseButton);
        _background = GetButton((int)SettingsButtons.Background);

        BindButtons();
    }

    private void BindButtons()
    {
        if (_sfxButton != null) _sfxButton.onClick.AddListener(SfxVolumeSetting);
        if (_bgmButton != null) _bgmButton.onClick.AddListener(BgmVolumeSetting);
        if (_closeButton != null) _closeButton.onClick.AddListener(ClosePopup);
        if (_background != null) _background.onClick.AddListener(ClosePopup);
    }

    private void OnDisable()
    {
        if (_sfxButton != null) _sfxButton.onClick.RemoveListener(SfxVolumeSetting);
        if (_bgmButton != null) _bgmButton.onClick.RemoveListener(BgmVolumeSetting);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(ClosePopup);
        if (_background != null) _background.onClick.RemoveListener(ClosePopup);
    }

    public void SfxVolumeSetting()
    {
        GameManager.Audio.SetSFXState();
        DebugTool.Log($"SFX 볼륨 변경 : {GameManager.Audio.SfxOnOff}", DebugType.UI, this);
    }

    public void BgmVolumeSetting()
    {
        GameManager.Audio.SetBGMState();
        DebugTool.Log($"BGM 볼륨 변경 : {GameManager.Audio.BgmOnOff}", DebugType.UI, this);
    }
}

public enum SettingsButtons
{
    SFXButton,
    BGMButton,
    CloseButton,
    Background
}