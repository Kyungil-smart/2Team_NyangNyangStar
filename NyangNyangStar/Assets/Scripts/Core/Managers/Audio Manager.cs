using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 전체 관리
/// 오디오 믹서를 통해 볼륨 설정
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Space(10)][Header("오디오 믹서")]
    [SerializeField] private AudioMixer _audioMixer;
    
    [Space(10)][Header("오디오 그룹")]
    [SerializeField] private AudioMixerGroup _masterGroup;
    [SerializeField] private AudioMixerGroup _bgmGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _uiGroup;
    public AudioMixerGroup MasterGroup => _masterGroup;
    public AudioMixerGroup BgmGroup => _bgmGroup;
    public AudioMixerGroup SfxGroup => _sfxGroup;
    public AudioMixerGroup UIGroup => _uiGroup;
    
    /// <summary>
    /// 믹서에서 볼륨 조절을 위한 키
    /// </summary>
    private const string MasterVolumeKey = "Master_Volume";
    private const string BGMVolumeKey = "BGM_Volume";
    private const string SfxVolumeKey = "SFX_Volume";
    private const string UIVolumeKey = "UI_Volume";

    [Space(5)][Header("슬라이더 최대 값")]
    [SerializeField][Range(1f, 1.5f)] private float _maxSliderValue = 1.2f;
    private float _minSliderValue = 0.0001f;
    private const float DefaultVolume = 1f;
    
    public float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
    public float BGMVolume => PlayerPrefs.GetFloat(BGMVolumeKey, DefaultVolume);
    public float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);
    public float UIVolume => PlayerPrefs.GetFloat(UIVolumeKey, DefaultVolume);
    
    
    private void Awake()
    {
        SingletonInit();
        VolumeInit();
    }
    
    /// <summary>
    /// 마스터 볼륨 조절
    /// </summary>
    /// <param name="value"></param>
    /// 슬라이더 값
    public void SetMasterVolume(float value)
    {
        SetMixerVolume(MasterVolumeKey, value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
    }

    /// <summary>
    /// BGM 볼륨 조절
    /// </summary>
    public void SetBGMVolume(float value)
    {
        SetMixerVolume(BGMVolumeKey, value);
        PlayerPrefs.SetFloat(BGMVolumeKey, value);
    }

    /// <summary>
    /// SFX 볼륨 조절 
    /// </summary>
    public void SetSFXVolume(float value)
    {
        SetMixerVolume(SfxVolumeKey, value);
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
    }

    /// <summary>
    /// UI 볼륨 조절 
    /// </summary>
    public void SetUIVolume(float value)
    {
        SetMixerVolume(UIVolumeKey, value);
        PlayerPrefs.SetFloat(UIVolumeKey, value);
    }

    /// <summary>
    /// 볼륨 변경 메서드
    /// </summary>
    /// <param name="parameter"></param>
    /// VolumeMixer의 Group별 파라미터 키
    /// <param name="value"></param>
    /// 볼륨 값
    /// AudioMixer는 0.5, 0.1 같은 선형 값이 아니라 데시벨(dB) 값을 받기 때문에 변환식 필요
    /// dB = 20Log10(x) 여기서 x 는 슬라이더 값
    private void SetMixerVolume(string parameter, float value)
    {
        float clampValue = Mathf.Clamp(value, _minSliderValue, _maxSliderValue);
        float volumeDb = Mathf.Log10(clampValue) * 20f;
        
        bool result = _audioMixer.SetFloat(parameter, volumeDb);
        
        if(!result)
            DebugTool.Log($"{parameter} Audio mixer를 찾을 수 없습니다.", DebugType.Game, this);
    }
    /// <summary>
    /// 오디오 매니저 싱글톤 적용
    /// </summary>
    private void SingletonInit()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 게임 시작시 저장된 볼륨 값 초기화
    /// </summary>
    private void VolumeInit()
    {
        SetMixerVolume(MasterVolumeKey, MasterVolume);
        SetMixerVolume(BGMVolumeKey, BGMVolume);
        SetMixerVolume(SfxVolumeKey, SfxVolume);
        SetMixerVolume(UIVolumeKey, UIVolume);
    }
}