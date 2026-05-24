using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    // Addressables에 등록된 BGM 주소를 입력
    [SerializeField] private string _bgmKey = "Assets/Audio/BGM/Title.mp3";

    private AudioSource _audioSource;
    private AsyncOperationHandle<AudioClip> _handle;
    private bool _isLoaded;

    private void Awake()
    {
        // AudioSource를 가져오고, 없으면 새로 추가합니다.
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // BGM은 코드에서 로드가 끝난 뒤 재생하고, 반복 재생되도록 설정
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
    }

    // 시작하고
    private void Start()
    {
        PlayBGM();
    }

    private void PlayBGM()
    {
        // 이미 로드된 상태라면 중복 로드를 막습니다.
        if (_isLoaded)
        {
            return;
        }

        // Addressables에서 _bgmKey에 해당하는 AudioClip을 비동기로 로드
        Addressables.LoadAssetAsync<AudioClip>(_bgmKey).Completed += OnBgmLoaded;
    }

    private void OnBgmLoaded(AsyncOperationHandle<AudioClip> handle)
    {
        // 로드에 실패한 핸들은 Release해서 Addressables 참조를 정리해줘야함
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            DebugTool.Log($"BGM load failed: {_bgmKey}", DebugType.Addressable);
            Addressables.Release(handle);
            return;
        }

        // 나중에 OnDestroy에서 해제할 수 있도록 성공한 핸들을 저장
        _handle = handle;
        _isLoaded = true;

        // 로드된 AudioClip을 AudioSource에 연결하고 재생
        _audioSource.clip = handle.Result;
        _audioSource.Play();

        DebugTool.Log("BGM play succeeded", DebugType.Addressable);
    }

    private void OnDestroy()
    {
        // 오브젝트가 사라질 때 -> 재생 중인 클립 참조를 먼저 끊기
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
        }

        
        if (!_isLoaded)
        {
            return;
        }

        // Addressables로 로드한 AudioClip 참조를 해제
        Addressables.Release(_handle);
        _handle = default;
        _isLoaded = false;
    }
}
