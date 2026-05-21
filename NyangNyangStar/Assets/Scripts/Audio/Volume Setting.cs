using UnityEngine;
using UnityEngine.UI;

public class VolumeSetting : MonoBehaviour
{
    [SerializeField] private Button _bgmVolumeButton;
    [SerializeField] private Button _sfxVolumeButton;
    
    private void OnEnable()
    {
        _bgmVolumeButton.onClick.AddListener(BgmVolumeSetting);
        _sfxVolumeButton.onClick.AddListener(SfxVolumeSetting);
    }

    private void Start()
        => VolumeInit();

    private void OnDisable()
    {
        _bgmVolumeButton.onClick.RemoveListener(BgmVolumeSetting);
        _sfxVolumeButton.onClick.RemoveListener(SfxVolumeSetting);
    }

    public void BgmVolumeSetting()
    {
        GameManager.Audio.SetBGMState();
        DebugTool.Log($"BGM 볼륨 변경 : {GameManager.Audio.BgmOnOff}", DebugType.UI, this);
    }

    public void SfxVolumeSetting()
    {
        GameManager.Audio.SetSFXState();
        DebugTool.Log($"SFX 볼륨 변경 : {GameManager.Audio.SfxOnOff}", DebugType.UI, this);
    }

    private void VolumeInit()
    {
        // TODO : PlayerPrefs에서 오디오 상태를 로드하여 게임시작시 버튼의 초기상태 변경
    }
}
