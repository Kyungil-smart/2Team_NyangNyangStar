using Services.Scriptable_Object;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class AudioManager : ISubManager
    {
        private AudioSource[] _audioSources = new AudioSource[(int)AudioType.MaxCount];
        private Dictionary<string, AudioClip> _audioClips = new();

        private const string AudioSettingPath = "Settings/AudioMixerSettings";
        
        private AudioMixerSettingSo _audioSettings;
        private AudioMixer _audioMixer;

        private AudioMixerGroup _masterGroup;
        private AudioMixerGroup _bgmGroup;
        private AudioMixerGroup _sfxGroup;
    
        private const int DefaultState = 1;
        
        private const float VolumeOn = 0f;
        private const float VolumeOff = -80f;
        
        private bool _bgmOnOff = true;
        public bool BgmOnOff => _bgmOnOff;
        private bool _sfxOnOff = true;
        public bool SfxOnOff => _sfxOnOff;
        
        // 오디오 믹서 볼륨 조절 키
        private const string MasterStateKey = "Master";
        private const string BGMStateKey = "BGM";
        private const string SfxStateKey = "SFX";

        private int MasterState => PlayerPrefs.GetInt(MasterStateKey, DefaultState);
        private int BGMState => PlayerPrefs.GetInt(BGMStateKey, DefaultState);
        private int SfxState => PlayerPrefs.GetInt(SfxStateKey, DefaultState);

        public void Init()
        {
            LoadAudioSettings();
            CreateAudioRoot();
            ApplyMixerGroup();
            VolumeInit();
        }
        
        private void LoadAudioSettings()
        {
            _audioSettings = Resources.Load<AudioMixerSettingSo>(AudioSettingPath);

            if (_audioSettings == null)
            {
                DebugTool.Log($"AudioMixerSettingsSO 를 찾을 수 없습니다. 경로 : Resources/{AudioSettingPath}", DebugType.Missing);
                return;
            }

            _audioMixer = _audioSettings.AudioMixer;
            _masterGroup = _audioSettings.MasterGroup;
            _bgmGroup = _audioSettings.BgmGroup;
            _sfxGroup = _audioSettings.SfxGroup;
        }
        
        public void CreateAudioRoot()
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
                    
                    _audioSources[i].loop = true;
                    _audioSources[i].playOnAwake = false;
                }
                _audioSources[(int)AudioType.BGM].loop = true;
            }
            DebugTool.Log("오디오 매니저 초기화 완료 ", DebugType.Game);
        }

        private void ApplyMixerGroup()
        {
            if(_audioSources[(int)AudioType.BGM] != null)
                _audioSources[(int)AudioType.BGM].outputAudioMixerGroup = _bgmGroup;
            
            if(_audioSources[(int)AudioType.SFX] != null)
                _audioSources[(int)AudioType.SFX].outputAudioMixerGroup = _sfxGroup;
        }

        public void SetBGMState()
        { 
            _bgmOnOff = !_bgmOnOff;
            
            float volume = _bgmOnOff ? VolumeOn : VolumeOff;
            int state = _bgmOnOff ? 1 : 0;
        
            SetMixerVolume(BGMStateKey, volume);
            
            PlayerPrefs.SetInt(BGMStateKey, state);
            PlayerPrefs.Save();
        }
        
        public void SetSFXState()
        {
            _sfxOnOff = !_sfxOnOff;
            
            float volume = _sfxOnOff ? VolumeOn : VolumeOff;
            int state = _sfxOnOff ? 1 : 0;
        
            SetMixerVolume(SfxStateKey, volume);
            
            PlayerPrefs.SetInt(SfxStateKey, state);
            PlayerPrefs.Save();
        }

        private void SetMixerVolume(string parameter, float value)
        {
            if (_audioMixer == null)
            {
                DebugTool.Warning("AudioMixer가 할당되지 않았습니다.", DebugType.Missing);
                return;
            }
            bool result = _audioMixer.SetFloat(parameter, value);
            
            if(!result)
                DebugTool.Warning($"{parameter} Parameter 를 찾을 수 없습니다.", DebugType.Missing);
        }

        public void Clear()
        {
            foreach (AudioSource source in _audioSources)
            {
                if (source == null) continue;
                
                source.Stop();
                source.clip = null;
            }
        
            _audioClips.Clear();
            
            DebugTool.Log("오디오 매니저 제거 완료 ", DebugType.Game);
        }

        private void VolumeInit()
        {
            _bgmOnOff = BGMState == 1;
            _sfxOnOff = SfxState == 1;
            
            SetMixerVolume(MasterStateKey, MasterState == 1 ? VolumeOn : VolumeOff);
            SetMixerVolume(BGMStateKey, MasterState == 1 ? VolumeOn : VolumeOff);
            SetMixerVolume(SfxStateKey, SfxState == 1 ? VolumeOn : VolumeOff);
        }
    }
}