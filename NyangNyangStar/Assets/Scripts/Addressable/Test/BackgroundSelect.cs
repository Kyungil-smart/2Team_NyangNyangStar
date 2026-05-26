using Core.Managers;
using Services.AddressableKey;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class BackgroundSelect : UIBase
{
    [SerializeField] private Image _backgroundImage;
    private Sprite _sprite;
    private AsyncOperationHandle<Sprite> _handle;

    private void Awake()
        => _backgroundImage = GetComponentInChildren<Image>();

    private void Start()
    {
        ChangeSprite(KeyContainer.Sprite.Title);
    }

    public void ChangeSprite(string key)
    {
        GameManager.Addressable.LoadSprite(key,
            (loadedSprite, handle) =>
            {
                ReleaseSprite();

                _handle = handle;
                _sprite = loadedSprite;
                _backgroundImage.sprite = _sprite;
            },
            failedKey =>
            {
                DebugTool.Warning($"{failedKey} : Sprite 로드 실패", DebugType.UI);
            });
    }

    private void ReleaseSprite()
    {
        if (!_handle.IsValid())
            return;
        
        Addressables.Release(_handle);
        
        _handle = default;
        _sprite = null;
        
        DebugTool.Log("기존 Sprite Release 완료", DebugType.Addressable);
    }

    private void OnDestroy()
    {
        ReleaseSprite();
    }

    public override void Init()
    {
    }
}
