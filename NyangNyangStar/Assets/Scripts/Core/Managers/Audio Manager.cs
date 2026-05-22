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

        private const string AudioSettingPath = "Assets/Audio/BGM";

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

        // ?ㅻ뵒??誘뱀꽌 蹂쇰ⅷ 議곗젅 ??
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

        // ?꾩옱 ?ъ깮 以묒씤 bGM
        private string _currentBgmKey;

        // ?꾩옱 濡쒕뵫 以묒씤 BGM 
        private string _loadingBgmKey;

        // ?ㅻⅨ BGM ?붿껌???ㅼ뼱?붾뒗吏 ?뺤씤
        private string _requestedBgmKey;

        // ?섏쨷??Release ?섍린 ?꾪빐 諛섎뱶????ν빐????
        private AsyncOperationHandle<AudioClip> _bgmHandle;

        // ?꾩옱 BGM??Addressables濡?濡쒕뱶?섏뼱 ?덈뒗吏 ?뺤씤?섎뒗 媛?
        private bool _isBgmLoaded;


        public void PlayBgm(string bgmKey)
        {
            if (string.IsNullOrEmpty(bgmKey))
            {
                DebugTool.Error("BGM Key媛 鍮꾩뼱 ?덉뒿?덈떎.", DebugType.Addressable);
                return;
            }

            // BGM ?꾩슜 AudioSource 媛?몄샂
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];
            
            // AudioSource媛 ?놁쑝硫??ъ깮 遺덇???
            if (bgmSource == null)
            {
                DebugTool.Log("BGM AudioSource媛 ?놁뒿?덈떎.", DebugType.Missing);
                return;
            }
            
            // 留덉?留됱뿉 ?붿껌??Bgm Key  ???
            _requestedBgmKey = bgmKey;
            
            
            // ?대? 媛숈? BGM???ъ깮 以??대㈃
            // ?ㅼ떆 Load ?섏? ?딆쓬
            if (_currentBgmKey == bgmKey && _isBgmLoaded)
            {
                DebugTool.Log($"媛숈? BGM ?좎? : {bgmKey}", DebugType.Addressable);
                return;
            }
            
            // ?대? 媛숈? BGM??濡쒕뵫 以?-> 以묐났 諛⑹?
            if (_loadingBgmKey == bgmKey)
            {
                DebugTool.Log($"?대? BGM 濡쒕뵫 以?: {bgmKey}", DebugType.Addressable);
            }
            
            _loadingBgmKey = bgmKey;
            
            // ?쇰떒 ?몄텧 ??鍮꾨룞湲곕줈
            Addressables.LoadAssetAsync<AudioClip>(bgmKey).Completed += handle =>
            {
                OnBgmLoaded(handle, bgmKey);
            };
        }

        private void OnBgmLoaded(AsyncOperationHandle<AudioClip> handle, string loadedKey)
        {
            // 濡쒕뵫???앸궗?쇰?濡?濡쒕뵫 以?Key 珥덇린??
            _loadingBgmKey = null;

            // 濡쒕뱶 ?ㅽ뙣 ??handle ?뺣━ ??醫낅즺
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                DebugTool.Log($"BGM 濡쒕뱶 ?ㅽ뙣: {loadedKey}", DebugType.Addressable);
                Addressables.Release(handle);
                return;
            }

            // 濡쒕뵫 ?꾩쨷 ?ㅻⅨ BGM ?붿껌???ㅼ뼱??寃쎌슦
            // ?? BGM1 濡쒕뵫 以묒씠?덈뒗??媛묒옄湲?BGM2 ?붿껌???ㅼ뼱???곹솴
            // 吏湲?濡쒕뱶??BGM? ???댁긽 ?꾩슂 ?놁쑝誘濡?Release
            if (_requestedBgmKey != loadedKey)
            {
                DebugTool.Log($"?대? ?ㅻⅨ BGM???붿껌?섏뼱 濡쒕뱶 痍⑥냼 泥섎━: {loadedKey}", DebugType.Addressable);
                Addressables.Release(handle);
                return;
            }

            // BGM ?꾩슜 AudioSource 媛?몄삤湲?
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            // AudioSource媛 ?놁쑝硫?諛⑷툑 濡쒕뱶??handle??Release ?댁빞 ??
            if (bgmSource == null)
            {
                Addressables.Release(handle);
                DebugTool.Warning("BGM AudioSource媛 ?놁뒿?덈떎.", DebugType.Missing);
                return;
            }

            // ??BGM???뺤긽?곸쑝濡?濡쒕뱶???ㅼ뿉 湲곗〈 BGM ?뺣━
            // ?쒖꽌 以묒슂:
            // ??BGM Load ?깃났 ??湲곗〈 BGM Release ????BGM ?ъ깮
            ReleaseCurrentBGM();

            // ??BGM handle ???
            // ?섏쨷???ㅻⅨ BGM?쇰줈 諛붽? ??Release ?섍린 ?꾪빐 ?꾩슂
            _bgmHandle = handle;

            // ?꾩옱 BGM??濡쒕뱶?섏뿀?ㅺ퀬 ?쒖떆
            _isBgmLoaded = true;

            // ?꾩옱 ?ъ깮 以묒씤 BGM Key ???
            _currentBgmKey = loadedKey;

            // AudioSource????AudioClip ?곌껐 ???ъ깮
            bgmSource.Stop();
            bgmSource.clip = handle.Result;
            bgmSource.loop = true;
            bgmSource.Play();

            DebugTool.Log($"BGM ?ъ깮 ?깃났: {loadedKey}", DebugType.Addressable);
        }
        
        private void ReleaseCurrentBGM()
        {
            // BGM ?꾩슜 AudioSource 媛?몄삤湲?
            AudioSource bgmSource = _audioSources[(int)AudioType.BGM];

            // ?꾩옱 ?ъ깮 以묒씤 BGM ?뺤?
            // clip 李몄“???쒓굅
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }

            // Addressables濡?濡쒕뱶??BGM???놁쑝硫?Release ???꾩슂 ?놁쓬
            if (!_isBgmLoaded)
            {
                return;
            }

            // ?꾩옱 BGM Addressables Handle Release
            // ?닿구 ???섎㈃ Addressables 李몄“媛 怨꾩냽 ?⑥쓬
            Addressables.Release(_bgmHandle);

            // BGM ?곹깭 珥덇린??
            _bgmHandle = default;
            _isBgmLoaded = false;
            _currentBgmKey = null;

            DebugTool.Log("湲곗〈 BGM Release ?꾨즺", DebugType.Addressable);
        }
        
        public void StopBGM()
        {
            // ?꾩옱 ?ъ깮 以묒씤 BGM ?뺤? 諛?Release
            ReleaseCurrentBGM();

            // ?붿껌 以묒씤 BGM ?뺣낫 珥덇린??
            _requestedBgmKey = null;
            _loadingBgmKey = null;
        }


        private void LoadAudioSettings()
        {
            _audioSettings = Resources.Load<AudioMixerSettingSo>(AudioSettingPath);

            if (_audioSettings == null)
            {
                DebugTool.Log($"AudioMixerSettingsSO 瑜?李얠쓣 ???놁뒿?덈떎. 寃쎈줈 : Resources/{AudioSettingPath}",
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

            DebugTool.Log("?ㅻ뵒??留ㅻ땲? 珥덇린???꾨즺 ", DebugType.Game);
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
                DebugTool.Warning("AudioMixer媛 ?좊떦?섏? ?딆븯?듬땲??", DebugType.Missing);
                return;
            }

            bool result = _audioMixer.SetFloat(parameter, value);

            if (!result)
                DebugTool.Warning($"{parameter} Parameter 瑜?李얠쓣 ???놁뒿?덈떎.", DebugType.Missing);
        }

        public void Clear()
        {
            foreach (AudioSource source in _audioSources)
            {
                if (source == null) continue;

                source.Stop();
                source.clip = null;
            }
            
            // ?꾩옱 Addressables濡?濡쒕뱶??BGM ?뺣━

            _audioClips.Clear();

            DebugTool.Log("?ㅻ뵒??留ㅻ땲? ?쒓굅 ?꾨즺 ", DebugType.Game);
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
