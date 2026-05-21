using Core.Managers;
using UnityEngine;
using Services.AddressableKey;
using UnityEngine.EventSystems;

public class BaseScene : MonoBehaviour
{
    void Awake() => Init();

    protected virtual void Init()
    {
        Object obj = FindFirstObjectByType(typeof(EventSystem));

        if (obj == null)
        {
            if(!GameManager.Addressable.TryLoadPrefab(KeyContainer.EventSystem))
                DebugTool.Warning("Failed to find EventSystem", DebugType.Missing);
        }
    }
}