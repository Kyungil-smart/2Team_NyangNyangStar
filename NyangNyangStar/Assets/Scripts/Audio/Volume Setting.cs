using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    public class VolumeSetting : MonoBehaviour
    {
        [Space(8)] [Header("오디오 설정 버튼")]
        [SerializeField] private Button _bgmButton;
        [SerializeField] private Button _sfxButton;
        
        private void OnEnable()
        {
            if(_bgmButton != null)
                _bgmButton.onClick.AddListener(BgmVolumeSetting);
            if(_sfxButton != null)
                _sfxButton.onClick.AddListener(SfxVolumeSetting);
        }

        private void OnDisable()
        {
            if (_bgmButton != null)
                _bgmButton.onClick.RemoveListener(BgmVolumeSetting);
            
            if (_sfxButton != null)
                _sfxButton.onClick.RemoveListener(SfxVolumeSetting);
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
    }
}
