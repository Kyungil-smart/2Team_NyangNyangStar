using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

/// <summary>
/// 오디오 전체 관리
/// 오디오 믹서를 통해 볼륨 설정
/// </summary>
public class AudioManager : ISubManager
{
    private AudioSource[] _audioSources = new AudioSource[(int)AudioType.MaxCount];
    private Dictionary<string, AudioClip> _audioClips = new();
    
    [Space(10)][Header("오디오 믹서")]
    private AudioMixer _audioMixer;
    
    [Space(10)][Header("오디오 그룹")]
    private AudioMixerGroup _masterGroup;
    private AudioMixerGroup _bgmGroup;
    private AudioMixerGroup _sfxGroup;
    
    public AudioMixerGroup MasterGroup => _masterGroup;
    public AudioMixerGroup BgmGroup => _bgmGroup;
    public AudioMixerGroup SfxGroup => _sfxGroup;
    
    /// <summary>
    /// 믹서에서 볼륨 조절을 위한 키
    /// </summary>
    private const string MasterStateKey = "Master";
    private const string BGMStateKey = "BGM";
    private const string SfxStateKey = "SFX";

    [Space(5)][Header("Audio On/Off 여부")]
    private bool _bgmOnOff = true;
    public  bool BgmOnOff => _bgmOnOff;
    private bool _sfxOnOff;
    public bool SfxOnOff => _sfxOnOff;

    private const int DefaultVolume = 1;
    
    public int MasterState => PlayerPrefs.GetInt(MasterStateKey, DefaultVolume);
    public int BGMState => PlayerPrefs.GetInt(BGMStateKey, DefaultVolume);
    public int SfxState => PlayerPrefs.GetInt(SfxStateKey, DefaultVolume);
    
    /// <summary>
    /// 볼륨 조절
    /// </summary>
    public void SetBGMState()
    { 
        _bgmOnOff = !_bgmOnOff;
        float volume = _bgmOnOff ? 0 : 80f;
        int state = _bgmOnOff ? 1 : 0;
        
        SetMixerVolume(BGMStateKey, volume);
        
        PlayerPrefs.SetInt(BGMStateKey, state);
    }

    /// <summary>
    /// SFX 볼륨 조절 
    /// </summary>
    public void SetSFXState()
    {
        _sfxOnOff = !_sfxOnOff;
        float volume = _sfxOnOff ? 0f : 80f;
        int state = _sfxOnOff ? 1 : 0;
        
        SetMixerVolume(SfxStateKey, volume);
        PlayerPrefs.SetInt(SfxStateKey, state);
    }

    private void SetMixerVolume(string parameter, float value)
    {
        bool result = _audioMixer.SetFloat(parameter, value);
        
        if(!result)
            DebugTool.Log($"{parameter} Audio mixer를 찾을 수 없습니다.", DebugType.Game);
    }

    /// <summary>
    /// 게임 시작시 저장된 볼륨 값 초기화
    /// </summary>
    private void VolumeInit()
    {
        SetMixerVolume(MasterStateKey, MasterState);
        SetMixerVolume(BGMStateKey, BGMState);
        SetMixerVolume(SfxStateKey, SfxState);
    }

    public void Init()
    {
        GameObject root = GameObject.Find("@Audio");
        if (root == null)
        {
            root = new GameObject("@Audio");
            Object.DontDestroyOnLoad(root);
            
            string[] soundNames = Enum.GetNames(typeof(AudioType));
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new() { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = root.transform;
            }

            _audioSources[(int)AudioType.BGM].loop = true;
        }
        
        VolumeInit();
    }

    public void Clear()
    {
        foreach (AudioSource source in _audioSources)
        {
            source.Stop();
            source.clip = null;
        }
        
        _audioClips.Clear();
    }
}