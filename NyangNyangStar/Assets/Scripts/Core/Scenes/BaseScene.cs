using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class BaseScene : MonoBehaviour
{
    private const string EventSystemAddress = "EventSystem";


    void Awake() => Init();

    protected virtual void Init()
    {
        Object obj = FindFirstObjectByType(typeof(EventSystem));

        if (obj == null)
        {
            LoadEventSystem();
        }
    }

    protected void LoadEventSystem()
    {
        // 어드레서블에 동록된 EventSystem 프래팹을 비동기 생성
        Addressables.InstantiateAsync(EventSystemAddress).Completed += handle =>
        {
            // 로드 및 생성 성공
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // EventSystem 가져오기
                GameObject eventSystem = handle.Result;

                DontDestroyOnLoad(eventSystem);
            }
            else
            {
                DebugTool.Log($"EventSystem Addresable 로드 실패", DebugType.Missing);
            }
        };
    }
}