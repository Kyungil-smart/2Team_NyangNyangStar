using Services.Scriptable_Object;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        // 현재 재생 중인 BGM
        private string _currentBgmKey;

        // 현재 로딩 중인 BGM
        private string _loadingBgmKey;

        // 다른 BGM 요청이 들어왔는지 확인
        private string _requestedBgmKey;

        // 나중에 Release 하기 위해 저장해두는 BGM 핸들
        private AsyncOperationHandle<AudioClip> _bgmHandle;

        // 현재 BGM이 Addressables로 로드되어 있는지 확인하는 값
        private bool _isBgmLoaded;

        public void PlayBgm(string bgmKey)
        {
            if (string.IsNullOrEmpty(bgmKey))
            {
                DebugTool.Error("BGM Key가 비어 있습니다.", DebugType.Addressable);
                return;
            }

            // BGM 전용 AudioSource 가져오기
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            // AudioSource가 없으면 재생 불가
            if (bgmSource == null)
            {
                DebugTool.Log("BGM AudioSource가 없습니다.", DebugType.Missing);
                return;
            }

            // 마지막으로 요청한 BGM Key 저장
            _requestedBgmKey = bgmKey;

            // 이미 같은 BGM이 재생 중이면 다시 Load 하지 않음
            if (_currentBgmKey == bgmKey && _isBgmLoaded)
            {
                DebugTool.Log($"같은 BGM 유지: {bgmKey}", DebugType.Addressable);
                return;
            }

            // 이미 같은 BGM을 로딩 중이면 중복 요청 방지
            if (_loadingBgmKey == bgmKey)
            {
                DebugTool.Log($"이미 BGM 로딩 중: {bgmKey}", DebugType.Addressable);
            }

            _loadingBgmKey = bgmKey;

            // Addressables로 AudioClip 비동기 로드
            Addressables.LoadAssetAsync<AudioClip>(bgmKey).Completed += handle =>
            {
                OnBgmLoaded(handle, bgmKey);
            };
        }

        private void OnBgmLoaded(AsyncOperationHandle<AudioClip> handle, string loadedKey)
        {
            // 로딩이 끝났으므로 로딩 중인 Key 초기화
            _loadingBgmKey = null;

            // 로드 실패 시 handle 정리 후 종료
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                DebugTool.Log($"BGM 로드 실패: {loadedKey}", DebugType.Addressable);
                Addressables.Release(handle);
                return;
            }

            // 로딩 도중 다른 BGM 요청이 들어온 경우
            // 지금 로드된 BGM은 더 이상 필요 없으므로 Release
            if (_requestedBgmKey != loadedKey)
            {
                DebugTool.Log($"다른 BGM 요청으로 로드 취소 처리: {loadedKey}", DebugType.Addressable);
                Addressables.Release(handle);
                return;
            }

            // BGM 전용 AudioSource 가져오기
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            // AudioSource가 없으면 방금 로드한 handle을 Release 해야 함
            if (bgmSource == null)
            {
                Addressables.Release(handle);
                DebugTool.Warning("BGM AudioSource가 없습니다.", DebugType.Missing);
                return;
            }

            // 새 BGM이 정상적으로 로드된 뒤 기존 BGM 정리
            // 순서 중요: 새 BGM Load 성공 후 기존 BGM Release, 이후 새 BGM 재생
            ReleaseCurrentBGM();

            // 새 BGM handle 저장
            // 나중에 다른 BGM으로 바뀔 때 Release 하기 위해 필요
            _bgmHandle = handle;

            // 현재 BGM이 로드되었다고 표시
            _isBgmLoaded = true;

            // 현재 재생 중인 BGM Key 저장
            _currentBgmKey = loadedKey;

            // AudioSource에 AudioClip 연결 후 재생
            bgmSource.Stop();
            bgmSource.clip = handle.Result;
            bgmSource.loop = true;
            bgmSource.Play();

            DebugTool.Log($"BGM 재생 성공: {loadedKey}", DebugType.Addressable);
        }

        private void ReleaseCurrentBGM()
        {
            // BGM 전용 AudioSource 가져오기
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            // 현재 재생 중인 BGM 정지
            // clip 참조 제거
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }

            // Addressables로 로드한 BGM이 없으면 Release 할 필요 없음
            if (!_isBgmLoaded)
            {
                return;
            }

            // 현재 BGM Addressables Handle Release
            // 호출하지 않으면 Addressables 참조가 계속 남음
            Addressables.Release(_bgmHandle);

            // BGM 상태 초기화
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

                    _audioSources[i].loop = false;
                    _audioSources[i].playOnAwake = false;
                }

                _audioSources[(int)AudioType.BGM].loop = true;
            }

            DebugTool.Log("오디오 매니저 초기화 완료", DebugType.Game);
        }

        private void ApplyMixerGroup()
        {
            if (_audioSources[(int)AudioType.BGM] != null)
                _audioSources[(int)AudioType.BGM].outputAudioMixerGroup = _bgmGroup;

            if (_audioSources[(int)AudioType.SFX] != null)
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

            // 현재 Addressables로 로드한 BGM 정리

            _audioClips.Clear();

            DebugTool.Log("오디오 매니저 제거 완료", DebugType.Game);
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
