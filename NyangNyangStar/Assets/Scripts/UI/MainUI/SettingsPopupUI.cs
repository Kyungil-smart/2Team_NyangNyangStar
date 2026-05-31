using Core.Managers;
using DG.Tweening;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupUI : UIPopup
{
    [Header("DOTween 설정")]
    [SerializeField] private Transform _panel;
    [SerializeField] private float _popupScale = 0.85f;
    [SerializeField] private float _popupScaleDuration = 0.1f;
    [Header("오디오 설정 버튼")]
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _bgmButton;
    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _background;

    private SettingsPopupSprite _settingsPopupSprite;

    public override void Init()
    {
        Bind<Button>(typeof(SettingsButtons));

        _sfxButton = GetButton((int)SettingsButtons.SFXButton);
        _bgmButton = GetButton((int)SettingsButtons.BGMButton);
        _closeButton = GetButton((int)SettingsButtons.CloseButton);
        _background = GetButton((int)SettingsButtons.Background);

        BindButtons();

        _settingsPopupSprite = GetComponent<SettingsPopupSprite>();
        _settingsPopupSprite.Init();
        _settingsPopupSprite.RefreshAudioButtonSprite(GameManager.Audio.SfxOnOff, GameManager.Audio.BgmOnOff);
    }

    private void BindButtons()
    {
        if (_sfxButton != null) _sfxButton.onClick.AddListener(SfxVolumeSetting);
        if (_bgmButton != null) _bgmButton.onClick.AddListener(BgmVolumeSetting);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseSettingsPopup);
        
        if (_background != null) _background.onClick.AddListener(CloseSettingsPopup);
    }

    private void OnDestroy()
    {
        if (_sfxButton != null) _sfxButton.onClick.RemoveListener(SfxVolumeSetting);
        if (_bgmButton != null) _bgmButton.onClick.RemoveListener(BgmVolumeSetting);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(CloseSettingsPopup);
        if (_background != null) _background.onClick.RemoveListener(CloseSettingsPopup);
    }

    public void SfxVolumeSetting()
    {
        GameManager.Audio.SetSFXState();
        _settingsPopupSprite.RefreshAudioButtonSprite(GameManager.Audio.SfxOnOff, GameManager.Audio.BgmOnOff);
        DebugTool.Log($"SFX 볼륨 변경 : {GameManager.Audio.SfxOnOff}", DebugType.UI, this);
    }

    public void BgmVolumeSetting()
    {
        GameManager.Audio.SetBGMState();
        _settingsPopupSprite.RefreshAudioButtonSprite(GameManager.Audio.SfxOnOff, GameManager.Audio.BgmOnOff);
        DebugTool.Log($"BGM 볼륨 변경 : {GameManager.Audio.BgmOnOff}", DebugType.UI, this);
    }

    public override void PlayOpenAnimation()
    {
        if (_panel == null) return;
        
        _panel.localScale = Vector3.one * _popupScale;
        _panel.DOScale(1f, _popupScaleDuration)
            .SetEase(Ease.OutSine);
    }

    private void CloseSettingsPopup()
    {
        if (_panel == null) return;
        _panel.DOScale(Vector3.one * _popupScale, _popupScaleDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(() => gameObject.SetActive(false));
    }
}

public enum SettingsButtons
{
    SFXButton,
    BGMButton,
    CloseButton,
    Background
}