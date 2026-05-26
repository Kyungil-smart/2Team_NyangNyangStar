using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    [SerializeField] private Image _image;
    private AsyncOperationHandle<Sprite> _handle;
    private bool _isLoaded;


    private int count;

    public void ClickLoad()
    {
        if (_isLoaded)
        {
            return;
        }
        
        _isLoaded = true;

        Addressables.LoadAssetAsync<Sprite>("ZepCharacter").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handle = handle;

                _image.sprite = handle.Result;

                count++;
                DebugTool.Error(count.ToString(), DebugType.Game);
            }
            else if (_isLoaded)
            {
                DebugTool.Error("이미 존재", DebugType.Game);
            }
            else
            {
                DebugTool.Error("불러오기 실패.", DebugType.Game);
            }
        };
    }

    public void ClickUnload()
    {
        if (!_isLoaded)
        {
            DebugTool.Error("해제할 에셋이 없음.", DebugType.Game);
            return;
        }

        _image.sprite = null;

        Addressables.Release(_handle);

        count--;
        DebugTool.Error(count.ToString(), DebugType.Game);

        _handle = default;
        _isLoaded = false;
    }
}