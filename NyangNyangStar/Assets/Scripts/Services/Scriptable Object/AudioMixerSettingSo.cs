using UnityEngine;
using UnityEngine.Audio;

namespace Services.Scriptable_Object
{
    [CreateAssetMenu(fileName = "AudioMixerSettings", menuName = "SO/Audio/AudioMixerSettingSO", order = 99)]
    public class AudioMixerSettingSo : ScriptableObject
    {
        [Header("오디오 믹서")]
        [SerializeField] private AudioMixer audioMixer;
        public AudioMixer AudioMixer => audioMixer;
    
        [Space(8)][Header("오디오 그룹")]
        [SerializeField] private AudioMixerGroup masterGroup;
        public AudioMixerGroup MasterGroup => masterGroup;
        
        [SerializeField] private AudioMixerGroup bgmGroup;
        public AudioMixerGroup BgmGroup => bgmGroup;
        
        [SerializeField] private AudioMixerGroup sfxGroup;
        public AudioMixerGroup SfxGroup => sfxGroup;
    }
}