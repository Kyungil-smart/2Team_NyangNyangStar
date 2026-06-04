using Services.Scriptable_Object;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using AudioType = Services.Enums.AudioType;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class AudioManager : ISubManager
    {
        private AudioSource[] _audioSources = new AudioSource[(int)AudioType.MaxCount];

        private const string AudioSettingPath = "Settings/AudioMixerSettings";

        private AudioMixerSettingSo _audioSettings;
        private AudioMixer _audioMixer;

        private AudioMixerGroup _bgmGroup;
        private AudioMixerGroup _sfxGroup;

        private const int DefaultState = 1;

        private const float VolumeOn = -10f;
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
        
        // 현재 재생 중인 Audio
        private string _currentBgmKey;
        private string _currentSfxKey;
        // 현재 로딩 중인 Audio
        private string _loadingBgmKey;
        private string _loadingSfxKey;
        // 다른 Audio 요청이 들어왔는지 확인
        private string _requestedBgmKey;
        private string _requestedSfxKey;
        // 나중에 Release 하기 위해 저장해두는 BGM 핸들
        private AsyncOperationHandle<AudioClip> _bgmHandle;
        private AsyncOperationHandle<AudioClip> _SfxHandle;
        private bool _isBgmLoaded;
        private bool _isSfxLoaded;

        public void Init()
        {
            LoadAudioSettings();
            CreateAudioRoot();
            ApplyMixerGroup();
            VolumeInit();
        }

        public void PlayBgm(string bgmKey)
        {
            if (string.IsNullOrEmpty(bgmKey))
            {
                DebugTool.Warning("BGM Key가 비어 있습니다.", DebugType.Audio);
                return;
            }

            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            if (bgmSource == null)
            {
                DebugTool.Warning("BGM AudioSource가 없습니다.", DebugType.Missing);
                return;
            }

            _requestedBgmKey = bgmKey;

            if (_currentBgmKey == bgmKey && _isBgmLoaded)
            {
                DebugTool.Log($"같은 BGM 유지: {bgmKey}", DebugType.Audio);
                return;
            }

            if (_loadingBgmKey == bgmKey)
            {
                DebugTool.Log($"이미 BGM 로딩 중: {bgmKey}", DebugType.Audio);
                return;
            }

            _loadingBgmKey = bgmKey;

            GameManager.Addressable.LoadAudioClip(
                bgmKey,
                (clip, handle) =>
                {
                    _loadingBgmKey = null;

                    if (_requestedBgmKey != bgmKey)
                    {
                        DebugTool.Log($"다른 BGM 요청으로 로드 취소 처리: {bgmKey}", DebugType.Addressable);
                        Addressables.Release(handle);
                        return;
                    }

                    ReleaseCurrentBGM();

                    _bgmHandle = handle;
                    _isBgmLoaded = true;
                    _currentBgmKey = bgmKey;

                    bgmSource.Stop();
                    bgmSource.clip = clip;
                    bgmSource.loop = true;

                    ApplyAudioState();

                    bgmSource.Play();

                    DebugTool.Log($"BGM 재생 시작: {bgmKey}", DebugType.Audio);
                },
                failedKey =>
                {
                    _loadingBgmKey = null;

                    if (_requestedBgmKey == failedKey)
                        _requestedBgmKey = null;

                    DebugTool.Warning($"{failedKey} : BGM 로드 실패", DebugType.Audio);
                });
        }

        private void ReleaseCurrentBGM()
        {
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }

            if (!_isBgmLoaded)
                return;

            Addressables.Release(_bgmHandle);

            _bgmHandle = default;
            _isBgmLoaded = false;
            _currentBgmKey = null;

            DebugTool.Log("기존 BGM Release 완료", DebugType.Addressable);
        }

        public void StopBGM()
        {
            // 현재 재생 중인 BGM 정지 및 Release
            ReleaseCurrentBGM();

            // 요청 중인 BGM 정보 초기화
            _requestedBgmKey = null;
            _loadingBgmKey = null;
            
            DebugTool.Log("BGM 정지", DebugType.Audio);
        }

        public void PlaySfx(string sfxKey)
        {
            
            if (string.IsNullOrEmpty(sfxKey))
            {
                DebugTool.Warning("Sfx Key가 비어 있습니다.", DebugType.Audio);
                return;
            }

            AudioSource sfxSource = _audioSources[(int)AudioType.SFX];

            if (sfxSource == null)
            {
                DebugTool.Warning("SFX AudioSource가 없습니다.", DebugType.Missing);
                return;
            }

            _requestedSfxKey = sfxKey;

            if (_currentSfxKey == sfxKey && _isSfxLoaded)
            {
                DebugTool.Log($"같은 BGM 유지: {sfxKey}", DebugType.Audio);
                return;
            }

            if (_loadingSfxKey == sfxKey)
            {
                DebugTool.Log($"이미 BGM 로딩 중: {sfxKey}", DebugType.Audio);
                return;
            }

            _loadingSfxKey = sfxKey;

            GameManager.Addressable.LoadAudioClip(
                sfxKey,
                (clip, handle) =>
                {
                    _loadingSfxKey = null;

                    if (_requestedSfxKey != sfxKey)
                    {
                        DebugTool.Log($"다른 BGM 요청으로 로드 취소 처리: {sfxKey}", DebugType.Addressable);
                        GameManager.Addressable.Release(handle);
                        return;
                    }

                    ReleaseCurrentBGM();

                    _SfxHandle = handle;
                    _isSfxLoaded = true;
                    _currentSfxKey = sfxKey;

                    sfxSource.clip = null;
                    sfxSource.loop = false;

                    ApplyAudioState();

                    sfxSource.PlayOneShot(clip);

                    DebugTool.Log($"SFX 재생 : {sfxKey}", DebugType.Audio);
                },
                failedKey =>
                {
                    _loadingSfxKey = null;

                    if (_requestedSfxKey == failedKey)
                        _requestedSfxKey = null;

                    DebugTool.Warning($"{failedKey} : BGM 로드 실패", DebugType.Audio);
                });
        }

        private void LoadAudioSettings()
        {
            _audioSettings = Resources.Load<AudioMixerSettingSo>(AudioSettingPath);

            if (_audioSettings == null)
            {
                DebugTool.Log($"AudioMixerSettingsSO를 찾을 수 없습니다. 경로: Resources/{AudioSettingPath}",
                    DebugType.Missing);
                return;
            }

            _audioMixer = _audioSettings.AudioMixer;
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
                    GameObject go = new(soundNames[i]);
                    go.transform.parent = root.transform;
                    
                    AudioSource source = go.AddComponent<AudioSource>();
                    source.loop = false;
                    source.playOnAwake = false;

                    _audioSources[i] = source;
                }
                _audioSources[(int)AudioType.BGM].loop = true;
            }
            else
            {
                string[] soundNames = Enum.GetNames(typeof(AudioType));

                for (int i = 0; i < soundNames.Length - 1; i++)
                {
                    Transform child = root.transform.Find(soundNames[i]);

                    if (child == null)
                    {
                        GameObject go = new(soundNames[i]);
                        go.transform.parent = root.transform;
                        _audioSources[i] = go.AddComponent<AudioSource>();
                    }
                    else
                    {
                        _audioSources[i] = child.GetComponent<AudioSource>();

                        if (_audioSources[i] == null)
                            _audioSources[i] = child.gameObject.AddComponent<AudioSource>();
                    }

                    _audioSources[i].playOnAwake = false;
                }

                _audioSources[(int)AudioType.BGM].loop = true;
            }

            DebugTool.Log("오디오 매니저 초기화 완료", DebugType.Game);
        }

        private void ApplyMixerGroup()
        {
            if (_audioSources[(int)AudioType.BGM] != null)
            {
                _audioSources[(int)AudioType.BGM].outputAudioMixerGroup = _bgmGroup;
                DebugTool.Log($"BGM OutputGroup = {_audioSources[(int)AudioType.BGM].outputAudioMixerGroup?.name}", DebugType.Audio);
            }
            else
            {
                DebugTool.Warning("BGM AudioSource가 없습니다.", DebugType.Missing);
            }

            if (_audioSources[(int)AudioType.SFX] != null)
            {
                _audioSources[(int)AudioType.SFX].outputAudioMixerGroup = _sfxGroup;
                DebugTool.Log($"SFX OutputGroup = {_audioSources[(int)AudioType.SFX].outputAudioMixerGroup?.name}", DebugType.Audio);
            }
            else
            {
                DebugTool.Warning("SFX AudioSource가 없습니다.", DebugType.Missing);
            }
        }

        public void SetBGMState()
        {
            _bgmOnOff = !_bgmOnOff;
            
            int state = _bgmOnOff ? 1 : 0;

            PlayerPrefs.SetInt(BGMStateKey, state);
            PlayerPrefs.Save();
            
            ApplyAudioState();
        }

        public void SetSFXState()
        {
            _sfxOnOff = !_sfxOnOff;
            
            int state = _sfxOnOff ? 1 : 0;

            PlayerPrefs.SetInt(SfxStateKey, state);
            PlayerPrefs.Save();
            
            ApplyAudioState();
        }

        private void SetMixerVolume(string parameter, float value)
        {
            if (_audioMixer == null)
            {
                DebugTool.Warning("AudioMixer가 할당되지 않았습니다.", DebugType.Missing);
                return;
            }

            bool result = _audioMixer.SetFloat(parameter, value);

            if (!result)
                DebugTool.Warning($"{parameter} Parameter를 찾을 수 없습니다.", DebugType.Missing);
        }

        public void Clear()
        {
            foreach (AudioSource source in _audioSources)
            {
                if (source == null) continue;

                source.Stop();
                source.clip = null;
            }

            if(_bgmHandle.IsValid())
                GameManager.Addressable.Release(_bgmHandle);

            DebugTool.Log("오디오 매니저 제거 완료", DebugType.Game);
        }

        private void VolumeInit()
        {
            _bgmOnOff = BGMState == 1;
            _sfxOnOff = SfxState == 1;
            
            DebugTool.Log($"BGM = {_bgmOnOff}, SFX = {_sfxOnOff}", DebugType.Audio);
            
            ApplyAudioState();
        }
        
        public void ApplyAudioState()
        {
            SetMixerVolume(MasterStateKey, MasterState == 1 ? VolumeOn : VolumeOff);
            SetMixerVolume(BGMStateKey, _bgmOnOff ? VolumeOn : VolumeOff);
            SetMixerVolume(SfxStateKey, _sfxOnOff ? VolumeOn : VolumeOff);
        }
    }
}
